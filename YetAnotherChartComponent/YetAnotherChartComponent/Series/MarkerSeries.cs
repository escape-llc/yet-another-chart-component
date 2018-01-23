using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region MarkerSeries
	/// <summary>
	/// Series that places the given marker at each point.
	/// </summary>
	public class MarkerSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("MarkerSeries", LogTools.Level.Error);
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
		/// Geometry template for marker.
		/// Currently MUST be EllipseGeometry.
		/// </summary>
		public DataTemplate MarkerTemplate { get { return (DataTemplate)GetValue(MarkerTemplateProperty); } set { SetValue(MarkerTemplateProperty, value); } }
		/// <summary>
		/// Marker Offset in Category axis units [0..1].
		/// This is normalized to the Category axis unit.
		/// </summary>
		public double MarkerOffset { get; set; }
		/// <summary>
		/// Marker Origin is where the "center" of the marker geometry is (in NDC).
		/// Default value is (.5,.5)
		/// </summary>
		public Point MarkerOrigin { get; set; } = new Point(.5, .5);
		/// <summary>
		/// Marker Width/Height in Category axis units [0..1].
		/// Marker coordinates form a square in NDC.
		/// </summary>
		public double MarkerWidth { get; set; }
		/// <summary>
		/// The layer we're drawing into.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<MarkerItemState> MarkerState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(MarkerSeries), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(MarkerSeries), new PropertyMetadata("Title"));
		/// <summary>
		/// Identifies <see cref="MarkerTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MarkerTemplateProperty = DependencyProperty.Register("MarkerTemplate", typeof(DataTemplate), typeof(MarkerSeries), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public MarkerSeries() {
			MarkerState = new List<MarkerItemState>();
		}
		#endregion
		#region extensions
		/// <summary>
		/// Shorthand for marker state.
		/// </summary>
		protected class MarkerItemState : ItemState_Matrix { }
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			AssignFromSource(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathMarkerSeries),
				PathStyle == null && Theme != null,
				Theme.PathMarkerSeries != null,
				() => PathStyle = Theme.PathMarkerSeries
			);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
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
			if (MarkerState.Count == 0) return;
			// put the P matrix on everything
			var proj = MatrixSupport.ProjectionFor(icrc.Area);
			var world = MatrixSupport.ModelFor(CategoryAxis, ValueAxis);
			// get the local marker matrix
			var marker = MatrixSupport.LocalFor(world, MarkerWidth, icrc.Area, -MarkerOrigin.X, -MarkerOrigin.Y);
			foreach (var state in MarkerState) {
				if (state.World == default(Matrix)) {
					// TODO this can go in axis finalization
					state.World = MatrixSupport.Translate(world, state.XValue, state.YValue);
				}
				// assemble Mk * M * P transform for this path
				var model = MatrixSupport.Multiply(state.World, marker);
				var matx = MatrixSupport.Multiply(proj, model);
				state.Path.Data.Transform = new MatrixTransform() { Matrix = matx };
				// doesn't work for path
				//state.Path.RenderTransform = new MatrixTransform() { Matrix = matx };
				if (ClipToDataRegion) {
					state.Path.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{world} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		IEnumerable<Legend> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator bl;
			internal int ix;
			internal List<MarkerItemState> ms;
			internal Recycler<Path> recycler;
			internal IEnumerator<Path> paths;
			internal Path NextPath() {
				if (paths.MoveNext()) return paths.Current;
				else return null;
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath() {
			var path = new Path();
			BindTo(this, "PathStyle", path, Path.StyleProperty);
			return path;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			var paths = MarkerState.Select(ms => ms.Path);
			var recycler = new Recycler<Path>(paths, CreatePath);
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				by = by,
				ms = new List<MarkerItemState>(),
				recycler = recycler,
				paths = recycler.Items().GetEnumerator()
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// TODO handle datetime et al values that aren't double
			var valuey = (double)st.by.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			st.ix = index;
			UpdateLimits(valuex, valuey);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				if (st.bl != null) {
					// still map the X
					CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
				}
				return;
			}
			var mappedy = ValueAxis.For(valuey);
			var mappedx = st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
			mappedx += MarkerOffset;
			_trace.Verbose($"[{index}] {valuey} ({mappedx},{mappedy})");
			var mk = MarkerTemplate.LoadContent() as Geometry;
			// TODO allow MK to be other things like (Path or Image).
			// no path yet
			var path = st.NextPath();
			if (path == null) return;
			path.Data = mk;
			st.ms.Add(new MarkerItemState() { Index = index, XValue = mappedx, YValue = mappedy, Path = path });
		}
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			if (st.bx == null) {
				// needs one extra "cell"
				UpdateLimits(st.ix + 1, double.NaN);
			}
		}
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			MarkerState = st.ms;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
