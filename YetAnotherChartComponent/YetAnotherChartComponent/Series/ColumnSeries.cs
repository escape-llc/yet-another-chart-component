using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ColumnSeries
	/// <summary>
	/// Data series that generates a series of <see cref="RectangleGeometry"/> each on its own <see cref="Path"/>.
	/// </summary>
	public class ColumnSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideSeriesItemUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Error);
		#region item state classes
		/// <summary>
		/// Implementation for item state custom label.
		/// Provides placement information.
		/// TODO MAY have to reconstitute the "full" x-coordinates for <see cref="Placement"/> if those ever get used (currently not).
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is set.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustomWithPlacement<Path> {
			internal bool flip;
			internal double bwidth;
			Point getPlacement() {
				if (flip) return Value >= 0 ? Placement.DOWN_RIGHT : Placement.UP_LEFT;
				return Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT;
			}
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() {
				var wc = DataFor();
				return new RectanglePlacement(getPlacement(), flip ? wc.Flip() : wc);
			}
			internal SeriesItemState_Custom(int idx, double xv, double xvo, double yv, object cs, Path ele) : base(idx, xv, xvo, yv, cs, ele, 0) { }
			WorldRect DataFor() {
				var ulc = new Point(XValueAfterOffset, Math.Max(0,  Value));
				var lrc = new Point(XValueAfterOffset + bwidth, Math.Min(0, Value));
				_trace.Verbose($"\t[{Index}] DataFor ulc:{ulc} lrc:{lrc}");
				return new WorldRect(ulc, lrc);
			}
		}
		/// <summary>
		/// Implementation for item state.
		/// Provides placement information.
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is NOT set.
		/// </summary>
		protected class SeriesItemState_Double : ItemStateWithPlacement<Path> {
			internal bool flip;
			internal double bwidth;
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() {
				var wc = DataFor();
				return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, flip ? wc.Flip() : wc);
			}
			internal SeriesItemState_Double(int idx, double xv, double xvo, double yv, Path ele) : base(idx, xv, xvo, yv, ele, 0) { }
			WorldRect DataFor() {
				var ulc = new Point(XValueAfterOffset, Math.Max(0, Value));
				var lrc = new Point(XValueAfterOffset + bwidth, Math.Min(0, Value));
				_trace.Verbose($"\t[{Index}] DataFor ulc:{ulc} lrc:{lrc}");
				return new WorldRect(ulc, lrc);
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Return current state as read-only.
		/// </summary>
		public override IEnumerable<ISeriesItem> SeriesItemValues { get{ return ItemState.AsReadOnly(); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Template to use for generated paths.
		/// If set, this overrides applying <see cref="DataSeriesWithValue.PathStyle"/> (assumed <see cref="Style"/> inside the template).
		/// If this is not set, then <see cref="IChartTheme.PathTemplate"/> is used and <see cref="DataSeriesWithValue.PathStyle"/> applied (if set).
		/// If Theme is not set, then <see cref="Path"/> is used (via ctor) and <see cref="DataSeriesWithValue.PathStyle"/> applied (if set).
		/// </summary>
		public DataTemplate PathTemplate { get { return (DataTemplate)GetValue(PathTemplateProperty); } set { SetValue(PathTemplateProperty, value); } }
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
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current state.
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Save the binding evaluators.
		/// TODO must re-create when any of the DPs change!
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathTemplateProperty = DependencyProperty.Register(
			nameof(PathTemplate), typeof(DataTemplate), typeof(ColumnSeries), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public ColumnSeries() {
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
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Value, 0);
			}
		}
		#endregion
		#region helpers
		/// <summary>
		/// Core element processing.
		/// The <see cref="RectangleGeometry"/> inside the <see cref="Path"/> is now location-invariant wrt x-axis.
		/// This means that during incremental updates, no re-calculation is required, only adjusting the <see cref="Canvas.LeftProperty"/>.
		/// </summary>
		/// <param name="index">Item index.</param>
		/// <param name="valuecat">Category axis value.</param>
		/// <param name="valueval">Value axis value.</param>
		/// <param name="item">Data item.</param>
		/// <param name="recycler"></param>
		/// <param name="byl"></param>
		/// <returns>New instance.</returns>
		ItemState<Path> ElementPipeline(int index, double valuecat, double valueval, object item, Recycler<Path, ItemState<Path>> recycler, BindingEvaluator byl) {
			var y1 = ValueAxis.For(valueval);
			var y2 = ValueAxis.For(0);
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var leftx = CategoryAxis.For(valuecat);
			var barx = BarOffset;
			var rightx = barx + BarWidth;
			_trace.Verbose($"{Name}[{index}] v:{valueval} ul:({barx},{topy}) lr:({rightx},{bottomy})");
			var path = recycler.Next(null);
			if (path == null) return null;
			var rrr = CategoryAxis.Orientation == AxisOrientation.Horizontal
				? new Rect(new Point(barx, topy), new Point(rightx, bottomy))
				: new Rect(new Point(topy, barx), new Point(bottomy, rightx));
			_trace.Verbose($"{Name}[{index}] rc:{rrr}");
			var shim = new GeometryWithOffsetShim<RectangleGeometry>() {
			 PathData = new RectangleGeometry() { Rect = rrr }
			};
			path.Item2.DataContext = shim;
			// connect the shim to template root element's Visibility
			BindTo(shim, nameof(shim.Visibility), path.Item2, UIElement.VisibilityProperty);
			BindTo(shim, nameof(shim.Offset), path.Item2, CategoryAxis.Orientation == AxisOrientation.Horizontal ? Canvas.LeftProperty : Canvas.TopProperty);
			if (byl == null) {
				return new SeriesItemState_Double(index, leftx, BarOffset, y1, path.Item2) { bwidth = BarWidth, flip = CategoryAxis.Orientation == AxisOrientation.Vertical };
			} else {
				var cs = byl.For(item);
				return new SeriesItemState_Custom(index, leftx, BarOffset, y1, cs, path.Item2) { bwidth = BarWidth, flip = CategoryAxis.Orientation == AxisOrientation.Vertical };
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <param name="isp">Not used.</param>
		/// <returns></returns>
		Path CreatePath(ItemState<Path> isp) {
			var path = default(Path);
			if (PathTemplate != null) {
				path = PathTemplate.LoadContent() as Path;
			} else if (Theme?.PathTemplate != null) {
				path = Theme.PathTemplate.LoadContent() as Path;
				if (PathStyle != null) {
					BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
				}
			}
			return path;
		}
		/// <summary>
		/// Currently nothing to do here.
		/// </summary>
		/// <param name="st">Updated state.</param>
		void UpdateGeometry(ItemStateCore st) {
			var isp = (st as ItemState<Path>);
			if (isp.Element.DataContext is GeometryWithOffsetShim<RectangleGeometry> gs) {
			}
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
			Layer = icelc.CreateLayer();
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			var icei = icelc as IChartErrorInfo;
			if (PathTemplate == null) {
				if (Theme?.PathTemplate == null) {
					icei?.Report(new ChartValidationResult(NameOrType(), $"No {nameof(PathTemplate)} and {nameof(Theme.PathTemplate)} was not found", new[] { nameof(PathTemplate), nameof(Theme.PathTemplate) }));
				}
			}
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathColumnSeries),
				PathStyle == null, Theme != null, Theme.PathColumnSeries != null,
				() => PathStyle = Theme.PathColumnSeries
			);
			BindPaths = new Evaluators(CategoryPath, ValuePath, ValueLabelPath);
			if(!BindPaths.IsValid) {
				icei?.Report(new ChartValidationResult(NameOrType(), $"ValuePath: must be specified", new[] { nameof(ValuePath) }));
			}
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
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
			if (ItemState.Count == 0) return;
			var cos = new ChartOrientationSupport(CategoryAxis, ValueAxis);
			var matx = cos.TransformForOffset(icrc.Area);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matx };
			foreach(var state in ItemState) {
				if (state.Element.DataContext is GeometryWithOffsetShim<RectangleGeometry> gs) {
					gs.GeometryTransform = mt;
					var output = matx.Transform(cos.For(CategoryAxis, state.XValue, ValueAxis, 0));
					//var output = matx.Transform(new Point(state.XValue, state.Value));
					// depends on orientation
					_trace.Verbose($"{Name}[{state.Index}] v:({state.XValue},{state.Value}) output:{output}");
					if (cos.Orientation == ChartOrientationSupport.ChartOrientation.Vertical) {
						gs.Offset = icrc.Area.Left + output.X;
					}
					else {
						gs.Offset = icrc.Area.Top + output.Y;
					}
				} else {
					state.Element.Data.Transform = mt;
				}
				if (ClipToDataRegion) {
					var cg = new RectangleGeometry() { Rect = icrc.SeriesArea };
					state.Element.Clip = cg;
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
		#region IProvideLegend
		private LegendBase _legend;
		IEnumerable<LegendBase> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
		}
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		#endregion
		#region IDataSourceRenderer
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element).Where(el => el != null);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			return new RenderState_ValueAndLabel<ItemState<Path>, Path>(new List<ItemState<Path>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_ValueAndLabel<ItemState<Path>, Path>;
			var value_val = st.evs.ValueFor(item);
			var value_cat = st.evs.CategoryFor(item, index);
			st.ix = index;
			// short-circuit if it's NaN
			if (double.IsNaN(value_val)) {
				return;
			}
			UpdateLimits(value_cat, value_val, 0);
			var istate = ElementPipeline(index, value_cat, value_val, item, st.recycler, st.evs.byl);
			if (istate != null) st.itemstate.Add(istate);
		}
		void IDataSourceRenderer.RenderComplete(object state) { }
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as RenderState_ValueAndLabel<ItemState<Path>, Path>;
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
		}
		#endregion
		#region IRequireDataSourceUpdates
		string IRequireDataSourceUpdates.UpdateSourceName => DataSourceName;
		void IRequireDataSourceUpdates.Remove(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var reproc = IncrementalRemove(startAt, items, ItemState, istate => istate.Element != null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, UpdateGeometry);
			});
			ReconfigureLimits();
			// finish up
			Layer.Remove(reproc.Select(xx => xx.Element).Where(el => el != null));
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Remove, startAt, reproc);
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var recycler = new Recycler<Path, ItemState<Path>>(CreatePath);
			var reproc = IncrementalAdd(startAt, items, ItemState, (ix, item) => {
				var value_val = BindPaths.ValueFor(item);
				// short-circuit if it's NaN
				if (double.IsNaN(value_val)) { return null; }
				var value_cat = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, value_cat, value_val, item, recycler, BindPaths.byl);
				return istate;
			}, (rpc, istate) => {
				istate.Shift(rpc, BindPaths, CategoryAxis, UpdateGeometry);
			});
			ReconfigureLimits();
			// finish up
			Layer.Add(recycler.Created);
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Add, startAt, reproc);
		}
		#endregion
	}
	#endregion
}
