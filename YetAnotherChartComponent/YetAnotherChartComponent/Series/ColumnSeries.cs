using eScape.Core;
using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ColumnSeries
	/// <summary>
	/// Data series that generates a series of Rectangles on a single Path.
	/// If there's no CategoryMemberPath defined (i.e. using data index) this component reserves one "extra" cell on the Category Axis, to present the last column(s).
	/// Category axis cells start on the left and extend positive-X (in device units).  Each cell is one unit long.
	/// </summary>
	public class ColumnSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Error);
		static LogTools.Flag _traceg = LogTools.Add("ColumnSeriesPaths", LogTools.Level.Off);
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Fractional offset into the "cell" of the category axis.
		/// BarOffset + BarWidth &lt;= 1.0
		/// </summary>
		public double BarOffset { get; set; } = 0.25;
		/// <summary>
		/// Fractional width in the "cell" of the category axis.
		/// BarOffset + BarWidth &lt;= 1.0
		/// </summary>
		public double BarWidth { get; set; } = 0.5;
		/// <summary>
		/// Whether to display debug paths.
		/// Should only be on for ONE series for best results.
		/// </summary>
		public bool EnableDebugPaths { get; set; }
		/// <summary>
		/// Path for the column bars.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// Geometry for the column bars.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// Geometry for debug: clip region.
		/// </summary>
		protected GeometryGroup DebugClip { get; set; }
		/// <summary>
		/// Path for the debug graphics.
		/// </summary>
		protected Path DebugSegments { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(ColumnSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(ColumnSeries), new PropertyMetadata("Title"));
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public ColumnSeries() {
			Geometry = new PathGeometry();
			Segments = new Path() {
				StrokeThickness = 1,
				Data = Geometry
			};
		}
		#endregion
		#region extensions
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc);
			Layer = icelc.CreateLayer(Segments);
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			if (EnableDebugPaths) {
				_traceg.Verbose(() => {
					DebugClip = new GeometryGroup();
					DebugSegments = new Path() {
						StrokeThickness = 1,
						Fill = new SolidColorBrush(Color.FromArgb(32, Colors.LimeGreen.R, Colors.LimeGreen.G, Colors.LimeGreen.B)),
						Stroke = new SolidColorBrush(Colors.White),
						Data = DebugClip
					};
					return "Created Debug path";
				});
			}
			if (DebugSegments != null) {
				Layer.Add(DebugSegments);
			}
			if (PathStyle == null && Theme != null) {
				if (Theme.PathColumnSeries != null) PathStyle = Theme.PathColumnSeries;
			}
			BindTo(this, "PathStyle", Segments, Path.StyleProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			if (ClipToDataRegion) {
				Segments.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
			var mt = new MatrixTransform() { Matrix = matx };
			Geometry.Transform = mt;
			if (DebugClip != null) {
				DebugClip.Children.Clear();
				//DebugClip.Children.Add(new RectangleGeometry() { Rect = clip });
				DebugClip.Children.Add(new RectangleGeometry() { Rect = new Rect(icrc.Area.Left, icrc.Area.Top, matx.M11, ValueAxis.Range / 2 * matx.M22) });
				//_trace.Verbose($"{Name} rmat:{DebugClip.Transform}");
			}
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		Legend IProvideLegend.Legend {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Fill, Stroke = Segments.Stroke };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal int ix;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			Geometry.Figures.Clear();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			st.ix = index;
			UpdateLimits(valuex, valuey, 0);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				if (st.bl != null) {
					// still map the X
					CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
				}
				return;
			}
			var y1 = ValueAxis.For(valuey);
			var y2 = ValueAxis.For(0);
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()))) + BarOffset;
			var rightx = leftx + BarWidth;
			_trace.Verbose($"{Name}[{index}] {valuey} ({leftx},{topy}) ({rightx},{bottomy})");
			var pf = PathHelper.Rectangle(leftx, topy, rightx, bottomy);
			Geometry.Figures.Add(pf);
		}
		/// <summary>
		/// Have to perform update here and not in Postamble because we are altering axis limits.
		/// </summary>
		/// <param name="state"></param>
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				UpdateLimits(st.ix + 1, double.NaN);
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
