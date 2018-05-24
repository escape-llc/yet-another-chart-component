using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline path.
	/// </summary>
	public class LineSeries : DataSeriesWithValue, IDataSourceRenderer, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Return item state.
		/// </summary>
		public override IEnumerable<ISeriesItem> SeriesItemValues { get { return ItemState.AsReadOnly(); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// The series drawing attributes etc. on the Canvas.
		/// </summary>
		protected Path Segments { get; set; }
		/// <summary>
		/// The series geometry.
		/// </summary>
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
		#endregion
		#region DPs
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public LineSeries() {
			Geometry = new PathGeometry();
			Segments = new Path() {
				Data = Geometry
			};
			ItemState = new List<ItemState<Path>>();
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			EnsureValuePath(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer(Segments);
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathLineSeries),
				PathStyle == null, Theme != null, Theme.PathLineSeries != null,
				() => PathStyle = Theme.PathLineSeries
			);
			BindTo(this, nameof(PathStyle), Segments, FrameworkElement.StyleProperty);
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			if (ClipToDataRegion) {
				Segments.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
			}
		}
		#endregion
		#region IProvideLegend
		private Legend _legend;
		IEnumerable<Legend> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Stroke, Stroke = Segments.Stroke };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal BindingEvaluator bx;
			internal BindingEvaluator by;
			internal BindingEvaluator byl;
			internal List<ItemState<Path>> itemstate;
			internal PathFigure pf;
			internal bool first = true;
			internal int ix;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			if (by == null) return null;
			ResetLimits();
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				by = by,
				byl = !String.IsNullOrEmpty(ValueLabelPath) ? new BindingEvaluator(ValueLabelPath) : null,
				pf = new PathFigure(),
				itemstate = new List<ItemState<Path>>()
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuey = CoerceValue(item, st.by);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			st.ix = index;
			UpdateLimits(valuex, valuey);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				return;
			}
			var mappedy = ValueAxis.For(valuey);
			var mappedx = CategoryAxis.For(valuex);
			var linex = mappedx + CategoryAxisOffset;
			_trace.Verbose($"{Name}[{index}] v:({valuex},{valuey}) m:({linex},{mappedy})");
			if (st.first) {
				// TODO handle multiple-sample "gaps", e.g. successive NaN values.
				// TODO handle multiple start-points.
				st.pf.StartPoint = new Point(linex, mappedy);
				st.first = false;
			} else {
				st.pf.Segments.Add(new LineSegment() { Point = new Point(linex, mappedy) });
			}
			if (st.byl == null) {
				st.itemstate.Add(new ItemState<Path>(index, mappedx, linex, mappedy, Segments));
			} else {
				var cs = st.byl.For(item);
				st.itemstate.Add(new ItemStateCustom<Path>(index, mappedx, linex, mappedy, cs, Segments));
			}
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
			Geometry.Figures.Clear();
			if (st.pf.Segments.Count > 0) {
				Geometry.Figures.Add(st.pf);
			}
			ItemState = st.itemstate;
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
