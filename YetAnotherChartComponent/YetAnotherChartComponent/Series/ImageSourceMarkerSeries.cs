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

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Series type where the marker is an <see cref="ImageSource"/>.
	/// </summary>
	public class ImageSourceMarkerSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideSeriesItemUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ImageSourceMarkerSeries", LogTools.Level.Error);
		#region item state
		internal class MarkerItemState : ItemState_Matrix<Image>, IProvidePlacement {
			internal bool flip;
			internal MarkerItemState(int idx, double xv, double xo, double yv, Image ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) { }
			Placement IProvidePlacement.Placement => new MarkerPlacement(flip ? new Point(Value, XValueAfterOffset) : new Point(XValueAfterOffset, Value), Placement.DOWN_LEFT);
			void IProvidePlacement.ClearCache() { }
		}
		internal class MarkerItemState_Custom : ItemStateCustom_Matrix<Image>, IProvidePlacement {
			internal bool flip;
			internal MarkerItemState_Custom(int idx, double xv, double xo, double yv, object cs, Image ele, int ch = 0) : base(idx, xv, xo, yv, cs, ele, ch) { }
			Placement IProvidePlacement.Placement => new MarkerPlacement(flip ? new Point(Value, XValueAfterOffset) : new Point(XValueAfterOffset, Value), Placement.DOWN_LEFT);
			void IProvidePlacement.ClearCache() { }
		}
		#endregion
		#region properties
		/// <summary>
		/// Return current state as read-only.
		/// </summary>
		public override IEnumerable<ISeriesItem> SeriesItemValues { get { return ItemState.AsReadOnly(); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// Image source for marker.
		/// </summary>
		public ImageSource Source { get { return (ImageSource)GetValue(SourceProperty); } set { SetValue(SourceProperty, value); } }
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
		protected List<ItemState<Image>> ItemState { get; set; }
		/// <summary>
		/// Save the binding evaluators.
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Source"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
			nameof(Source), typeof(ImageSource), typeof(MarkerSeries), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public ImageSourceMarkerSeries() {
			ItemState = new List<ItemState<Image>>();
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
		LegendWithImageSource Legend() {
			return new LegendWithImageSource() { Title = Title, Source = Source };
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Image CreateElement(ItemState<Image> ist) {
			var fe = default(Image);
			if (Theme?.ImageTemplate != null) {
				fe = Theme.ImageTemplate.LoadContent() as Image;
				if (PathStyle != null) {
					BindTo(this, nameof(PathStyle), fe, FrameworkElement.StyleProperty);
				}
				var shim = new ImageSourceShim() { Source = Source };
				fe.DataContext = shim;
				BindTo(shim, nameof(shim.OffsetX), fe, Canvas.LeftProperty);
				BindTo(shim, nameof(shim.OffsetY), fe, Canvas.TopProperty);
				BindTo(shim, nameof(shim.Width), fe, Image.WidthProperty);
				BindTo(shim, nameof(shim.Height), fe, Image.HeightProperty);
			}
			return fe;
		}
		/// <summary>
		/// Core element creation.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="valuex"></param>
		/// <param name="valuey"></param>
		/// <param name="item"></param>
		/// <param name="recycler"></param>
		/// <param name="evs"></param>
		/// <returns></returns>
		ItemState<Image> ElementPipeline(int index, double valuex, double valuey, object item, Recycler<Image, ItemState<Image>> recycler, Evaluators evs) {
			var value_val = ValueAxis.For(valuey);
			var value_cat = CategoryAxis.For(valuex);
			_trace.Verbose($"[{index}] v:({valuex},{valuey}) mo:{MarkerOffset} vm:({value_cat},{value_val})");
			// TODO allow a MarkerTemplateSelector and a value Selector/Formatter
			// no path yet
			var el = recycler.Next(null);
			if (el == null) return null;
			var cs = evs.LabelFor(item);
			var flip = CategoryAxis.Orientation == AxisOrientation.Vertical;
			if (cs == null) {
				return new MarkerItemState(index, value_cat, MarkerOffset, value_val, el.Item2) { flip = flip };
			} else {
				return new MarkerItemState_Custom(index, value_cat, MarkerOffset, value_val, cs, el.Item2) { flip = flip };
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
			Layer = icelc.CreateLayer();
			_trace.Verbose($"{Name} enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathMarkerSeries),
				PathStyle == null, Theme != null, Theme.PathMarkerSeries != null,
				() => PathStyle = Theme.PathMarkerSeries
			);
			BindPaths = new Evaluators(CategoryPath, ValuePath, ValueLabelPath);
			if (!BindPaths.IsValid) {
				if (icelc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"ValuePath: must be specified", new[] { nameof(ValuePath) }));
				}
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
		#region IProvideLegend
		private LegendBase _legend;
		IEnumerable<LegendBase> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
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
			var flip = XAxis == ValueAxis;
			var mp = MatrixSupport.DataArea(XAxis, YAxis, icrc.Area, flip ? 4 : 1);
			// get the offset matrix
			var mato = MatrixSupport.Multiply(mp.Item1, mp.Item2);
			// TODO preserve aspect ratio of image; will require ImageOpened evh
			var dimi = MarkerWidth * Math.Abs(flip ? mato.M22 : mato.M11);
			_trace.Verbose($"transforms dim:{dimi} flip:{flip}");
			foreach (var state in ItemState) {
				if (state.Element.DataContext is ImageSourceShim iss) {
					var output = mato.Transform(flip ? new Point(state.Value, state.XValue + MarkerOffset) : new Point(state.XValue + MarkerOffset, state.Value));
					_trace.Verbose($"output {output.X:F1},{output.Y:F1}  value {state.XValue},{state.Value}  image {state.Element.ActualWidth:F1}x{state.Element.ActualHeight:F1}");
					var hw = dimi * MarkerOrigin.X;
					var hh = dimi * MarkerOrigin.Y;
					// these have bindings so effect is immediate
					iss.OffsetX = output.X - hw;
					iss.OffsetY = output.Y - hh;
					iss.Width = dimi;
					iss.Height = dimi;
				}
				if (ClipToDataRegion) {
					state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{mp.Item1} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IDataSourceRenderer
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Image, ItemState<Image>>(paths, CreateElement);
			return new RenderState_ValueAndLabel<ItemState<Image>, Image>(new List<ItemState<Image>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_ValueAndLabel<ItemState<Image>, Image>;
			var value_val = st.evs.ValueFor(item);
			var value_cat = st.evs.CategoryFor(item, index);
			st.ix = index;
			// short-circuit if it's NaN
			if (double.IsNaN(value_val)) {
				return;
			}
			UpdateLimits(value_cat, value_val);
			var istate = ElementPipeline(index, value_cat, value_val, item, st.recycler, st.evs);
			if (istate != null) st.itemstate.Add(istate);
		}
		void IDataSourceRenderer.RenderComplete(object state) { }
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as RenderState_ValueAndLabel<ItemState<Image>, Image>;
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
			var reproc = IncrementalRemove<ItemState<Image>>(startAt, items, ItemState, istate => istate.Element != null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, null);
				// NO geometry update ; done in later stages of render pipeline
			});
			ReconfigureLimits();
			// finish up
			Layer.Remove(reproc.Select(xx => xx.Element));
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Remove, startAt, reproc);
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var recycler = new Recycler<Image, ItemState<Image>>(CreateElement);
			var reproc = IncrementalAdd<ItemState<Image>>(startAt, items, ItemState, (ix, item) => {
				var value_val = BindPaths.ValueFor(item);
				// short-circuit if it's NaN
				if (double.IsNaN(value_val)) { return null; }
				var value_cat = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, value_cat, value_val, item, recycler, BindPaths);
				return istate;
			}, (rpc, istate) => {
				istate.Shift(rpc, BindPaths, CategoryAxis, null);
				// NO geometry update; done in later stages of render pipeline
			});
			ReconfigureLimits();
			// finish up
			Layer.Add(recycler.Created);
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Add, startAt, reproc);
		}
		#endregion
	}
}
