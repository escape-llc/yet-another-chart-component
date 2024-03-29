﻿using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline path.
	/// This series type tracks <see cref="double.NaN"/> values in <see cref="ItemState"/> explicitly, so it can accurately represent "gaps"
	/// by using multiple line segments.
	/// </summary>
	public class LineSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideSeriesItemUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Error);
		#region item state
		internal class LineItemState : ItemState<Path>, IProvidePlacement {
			internal LineItemState(int idx, double xv, double xo, double yv, Path ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) { }
			Placement IProvidePlacement.Placement => new MarkerPlacement(new Point(XValueAfterOffset, Value), Placement.DOWN_LEFT);
			void IProvidePlacement.ClearCache() { }
		}
		internal class LineItemState_Custom : ItemStateCustom<Path>, IProvidePlacement {
			internal LineItemState_Custom(int idx, double xv, double xo, double yv, object cs, Path ele, int ch = 0) : base(idx, xv, xo, yv, cs, ele, ch) { }
			Placement IProvidePlacement.Placement => new MarkerPlacement(new Point(XValueAfterOffset, Value), Placement.DOWN_LEFT);
			void IProvidePlacement.ClearCache() { }
		}
		#endregion
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
		/// <summary>
		/// Save all binding info one time.
		/// </summary>
		Evaluators BindPaths { get; set; }
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
		#region extensions
		/// <summary>
		/// Implement for this class.
		/// </summary>
		protected override void ReconfigureLimits() {
			ResetLimits();
			for (int ix = 0; ix < ItemState.Count; ix++) {
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Value);
			}
		}
		#endregion
		#region helpers
		/// <summary>
		/// Prepare the item state, but no <see cref="Geometry"/>.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="valuex"></param>
		/// <param name="valuey"></param>
		/// <param name="item"></param>
		/// <param name="evs"></param>
		/// <returns></returns>
		ItemState<Path> ElementPipeline(int index, double valuex, double valuey, object item, Evaluators evs) {
			var mappedy = ValueAxis.For(valuey);
			var mappedx = CategoryAxis.For(valuex);
			var linex = mappedx + CategoryAxisOffset;
			_trace.Verbose($"{Name}[{index}] v:({valuex},{valuey}) m:({linex},{mappedy})");
			var cs = evs.LabelFor(item);
			if (cs == null) {
				return new LineItemState(index, mappedx, CategoryAxisOffset, mappedy, Segments);
			} else {
				return new LineItemState_Custom(index, mappedx, CategoryAxisOffset, mappedy, cs, Segments);
			}
		}
		/// <summary>
		/// Create or augment a <see cref="PathFigure"/> as necessary.
		/// </summary>
		/// <param name="st">Render state.</param>
		/// <param name="istate">Next item to render.</param>
		void BuildFigure(State st, ItemState<Path> istate) {
			if (st.first) {
				// we need a new path figure
				st.pf = new PathFigure();
				st.allfigures.Add(st.pf);
				st.pf.StartPoint = new Point(istate.XValueAfterOffset, istate.Value);
				st.first = false;
			} else {
				st.pf.Segments.Add(new LineSegment() { Point = new Point(istate.XValueAfterOffset, istate.Value) });
			}
		}
		/// <summary>
		/// Completely rebuild the <see cref="Path"/> for this series.
		/// </summary>
		void UpdateGeometry() {
			var st = new State() {
				evs = BindPaths,
				first = true
			};
			foreach (var istate in ItemState) {
				if (double.IsNaN(istate.Value)) {
					// we are skipping one, so the next one is a start point
					st.first = true;
				} else {
					BuildFigure(st, istate);
				}
			}
			Geometry.Figures.Clear();
			foreach (var pf in st.allfigures) {
				if (pf.Segments.Count > 0) {
					Geometry.Figures.Add(pf);
				}
			}
		}
		#endregion
		#region IProvideSeriesItemUpdates
		/// <summary>
		/// Made public so it's easier to implement (auto).
		/// </summary>
		public event EventHandler<SeriesItemUpdateEventArgs> ItemUpdates;
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
			BindPaths = new Evaluators(CategoryPath, ValuePath, ValueLabelPath);
			if(!BindPaths.IsValid) {
				// report
			}
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"leave");
			BindPaths = null;
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
		private LegendBase _legend;
		IEnumerable<LegendBase> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = Segments.Stroke, Stroke = Segments.Stroke };
		}
		#endregion
		#region IDataSourceRenderer
		class State {
			internal Evaluators evs;
			internal List<ItemState<Path>> itemstate = new List<ItemState<Path>>();
			internal PathFigure pf;
			internal bool first = true;
			internal List<PathFigure> allfigures = new List<PathFigure>();
			internal int ix;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			return new State() {
				evs = BindPaths
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuey = st.evs.ValueFor(item);
			var valuex = st.evs.CategoryFor(item, index);
			st.ix = index;
			UpdateLimits(valuex, valuey);
			var istate = ElementPipeline(index, valuex, valuey, item, BindPaths);
			if (double.IsNaN(valuey)) {
				// we are skipping one, so the next one is a start point
				st.first = true;
			} else {
				BuildFigure(st, istate);
			}
			st.itemstate.Add(istate);
		}
		void IDataSourceRenderer.RenderComplete(object state) { }
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			Geometry.Figures.Clear();
			foreach (var pf in st.allfigures) {
				if (pf.Segments.Count > 0) {
					Geometry.Figures.Add(pf);
				}
			}
			ItemState = st.itemstate;
			Dirty = false;
		}
		#endregion
		#region IRequireDataSourceUpdates
		string IRequireDataSourceUpdates.UpdateSourceName => DataSourceName;
		void IRequireDataSourceUpdates.Remove(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var reproc = IncrementalRemove<ItemState<Path>>(startAt, items, ItemState, istate => istate.Element != null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, null);
			});
			// finish up
			ReconfigureLimits();
			UpdateGeometry();
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Remove, startAt, reproc);
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var reproc = IncrementalAdd<ItemState<Path>>(startAt, items, ItemState, (ix, item) => {
				var valuey = BindPaths.ValueFor(item);
				// short-circuit if it's NaN
				if (double.IsNaN(valuey)) { return null; }
				var valuex = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, valuex, valuey, item, BindPaths);
				return istate;
			}, (rpc, istate) => {
				istate.Shift(rpc, BindPaths, CategoryAxis, null);
			});
			// finish up
			ReconfigureLimits();
			UpdateGeometry();
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Add, startAt, reproc);
		}
		#endregion
	}
	#endregion
}
