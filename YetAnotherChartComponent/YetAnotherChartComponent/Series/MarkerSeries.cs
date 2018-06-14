using eScape.Core;
using System;
using System.Collections;
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
	public class MarkerSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireAfterAxesFinalized, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("MarkerSeries", LogTools.Level.Error);
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
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Save the binding evaluators.
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="MarkerTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty MarkerTemplateProperty = DependencyProperty.Register(
			nameof(MarkerTemplate), typeof(DataTemplate), typeof(MarkerSeries), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public MarkerSeries() {
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
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath(ItemState<Path> ist) {
			var path = new Path();
			BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
			return path;
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
		ItemState<Path> ElementPipeline(int index, double valuex, double valuey, object item, Recycler<Path, ItemState<Path>> recycler, Evaluators evs) {
			var mappedy = ValueAxis.For(valuey);
			var mappedx = CategoryAxis.For(valuex);
			var markerx = mappedx + MarkerOffset;
			_trace.Verbose($"[{index}] {valuey} ({markerx},{mappedy})");
			var mk = MarkerTemplate.LoadContent() as Geometry;
			// TODO allow MK to be other things like (Path or Image).
			// TODO allow a MarkerTemplateSelector and a value Selector/Formatter
			// no path yet
			var el = recycler.Next(null);
			if (el == null) return null;
			el.Item2.Data = mk;
			var cs = evs.LabelFor(item);
			if (cs == null) {
				return new ItemState_Matrix<Path>(index, mappedx, MarkerOffset, mappedy, el.Item2);
			} else {
				return new ItemStateCustom_Matrix<Path>(index, mappedx, MarkerOffset, mappedy, cs, el.Item2);
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
		private Legend _legend;
		IEnumerable<Legend> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return new[] { _legend }; }
		}
		#endregion
		#region IRequireAfterAxesFinalized
		void IRequireAfterAxesFinalized.AxesFinalized(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			var world = MatrixSupport.ModelFor(CategoryAxis, ValueAxis);
			foreach (var state in ItemState) {
				if (state is IItemStateMatrix ism) {
					ism.World = MatrixSupport.Translate(world, state.XValueAfterOffset, state.Value);
				}
			}
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
			// put the P matrix on everything
			var proj = MatrixSupport.ProjectionFor(icrc.Area);
			var world = MatrixSupport.ModelFor(CategoryAxis, ValueAxis);
			// get the local marker matrix
			var marker = MatrixSupport.LocalFor(world, MarkerWidth, icrc.Area, -MarkerOrigin.X, -MarkerOrigin.Y);
			foreach (var state in ItemState) {
				// assemble Mk * M * P transform for this path
				var model = MatrixSupport.Multiply((state as IItemStateMatrix).World, marker);
				var matx = MatrixSupport.Multiply(proj, model);
				state.Element.Data.Transform = new MatrixTransform() { Matrix = matx };
				// doesn't work for path
				//state.Path.RenderTransform = new MatrixTransform() { Matrix = matx };
				if (ClipToDataRegion) {
					state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{world} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IDataSourceRenderer
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			return new RenderState_ValueAndLabel<ItemState<Path>, Path>(new List<ItemState<Path>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_ValueAndLabel<ItemState<Path>, Path>;
			var valuey = st.evs.ValueFor(item);
			var valuex = st.evs.CategoryFor(item, index);
			st.ix = index;
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				return;
			}
			UpdateLimits(valuex, valuey);
			var istate = ElementPipeline(index, valuex, valuey, item, st.recycler, st.evs);
			if(istate != null) st.itemstate.Add(istate);
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
			var reproc = IncrementalRemove<ItemState<Path>>(startAt, items, ItemState, istate => istate.Element != null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, null);
				// NO geometry update ; done in later stages of render pipeline
			});
			ReconfigureLimits();
			// finish up
			Layer.Remove(reproc.Select(xx => xx.Element));
			Dirty = false;
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var recycler = new Recycler<Path, ItemState<Path>>(CreatePath);
			var reproc = IncrementalAdd<ItemState<Path>>(startAt, items, ItemState, (ix, item) => {
				var valuey = BindPaths.ValueFor(item);
				// short-circuit if it's NaN
				if (double.IsNaN(valuey)) { return null; }
				var valuex = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, valuex, valuey, item, recycler, BindPaths);
				return istate;
			}, (rpc, istate) => {
				istate.Shift(rpc, BindPaths, CategoryAxis, null);
				// NO geometry update; done in later stages of render pipeline
			});
			ReconfigureLimits();
			// finish up
			Layer.Add(recycler.Created);
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
