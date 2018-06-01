using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ColumnSeries
	/// <summary>
	/// Data series that generates a series of <see cref="RectangleGeometry"/> each on its own <see cref="Path"/>.
	/// </summary>
	public class ColumnSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ColumnSeries", LogTools.Level.Error);
		static LogTools.Flag _traceg = LogTools.Add("ColumnSeriesPaths", LogTools.Level.Off);
		#region item state classes
		/// <summary>
		/// Implementation for item state custom label.
		/// Provides placement information.
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is set.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustomWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal SeriesItemState_Custom(int idx, double xv, double xvo, double yv, object cs, Path ele) : base(idx, xv, xvo, yv, cs, ele, 0) { }
		}
		/// <summary>
		/// Implementation for item state.
		/// Provides placement information.
		/// This one is used when <see cref="DataSeriesWithValue.ValueLabelPath"/> is NOT set.
		/// </summary>
		protected class SeriesItemState_Double : ItemStateWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal SeriesItemState_Double(int idx, double xv, double xvo, double yv, Path ele) : base(idx, xv, xvo, yv, ele, 0) { }
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
		/// <summary>
		/// Data needed for current state.
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Save the binding evaluators.
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public ColumnSeries() {
			ItemState = new List<ItemState<Path>>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Core element processing.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="valuex"></param>
		/// <param name="valuey"></param>
		/// <param name="item"></param>
		/// <param name="recycler"></param>
		/// <param name="byl"></param>
		/// <returns></returns>
		ItemState<Path> ElementPipeline(int index, double valuex, double valuey, object item, Recycler<Path, ItemState<Path>> recycler, BindingEvaluator byl) {
			var y1 = ValueAxis.For(valuey);
			var y2 = ValueAxis.For(0);
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var leftx = CategoryAxis.For(valuex);
			var barx = leftx + BarOffset;
			var rightx = barx + BarWidth;
			_trace.Verbose($"{Name}[{index}] {valuey} ({barx},{topy}) ({rightx},{bottomy})");
			var path = recycler.Next(null);
			if (path == null) return null;
			var rg = new RectangleGeometry() { Rect = new Rect(new Point(barx, topy), new Point(rightx, bottomy)) };
			path.Item2.Data = rg;
			if (byl == null) {
				return new SeriesItemState_Double(index, leftx, barx, y1, path.Item2);
			} else {
				var cs = byl.For(item);
				return new SeriesItemState_Custom(index, leftx, barx, y1, cs, path.Item2);
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <param name="isp">Not used.</param>
		/// <returns></returns>
		Path CreatePath(ItemState<Path> isp) {
			var path = new Path();
			BindTo(this, nameof(PathStyle), path, FrameworkElement.StyleProperty);
			return path;
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
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathColumnSeries),
				PathStyle == null, Theme != null, Theme.PathColumnSeries != null,
				() => PathStyle = Theme.PathColumnSeries
			);
			BindPaths = new Evaluators(CategoryPath, ValuePath, ValueLabelPath);
			if(!BindPaths.IsValid) {
				if(icelc is IChartErrorInfo icei) {
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
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the various components.
		/// Geometry: scaled to actual values in cartesian coordinates as indicated by axes.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matx };
			foreach(var ss in ItemState) {
				ss.Element.Data.Transform = mt;
				if (ClipToDataRegion) {
					var cg = new RectangleGeometry() { Rect = icrc.SeriesArea };
					ss.Element.Clip = cg;
				}
			}
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
		IEnumerable<Legend> IProvideLegend.LegendItems {
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
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			return new RenderState_ValueAndLabel<ItemState<Path>, Path>(new List<ItemState<Path>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as RenderState_ValueAndLabel<ItemState<Path>, Path>;
			var valuey = st.evs.ValueFor(item);
			var valuex = st.evs.CategoryFor(item, index);
			st.ix = index;
			UpdateLimits(valuex, valuey, 0);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				return;
			}
			var istate = ElementPipeline(index, valuex, valuey, item, st.recycler, st.evs.byl);
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
			var unused = new List<FrameworkElement>();
			for (int ix = 0; ix < items.Count; ix++) {
				// remove requested item
				if (ItemState[startAt].Element != null) {
					unused.Add(ItemState[startAt].Element);
				}
				ItemState.RemoveAt(startAt);
			}
			// re-sequence remaining items
			for (int ix = startAt; ix < ItemState.Count; ix++) {
				var valuex = BindPaths.CategoryValue(ItemState[ix].XValue, ix);
				var leftx = CategoryAxis.For(valuex);
				var offsetx = leftx + BarOffset;
				// TODO update index relative to existing value NOT ix
				ItemState[ix].Move(ix, leftx, offsetx);
				// update geometry
				var rg = ItemState[ix].Element.Data as RectangleGeometry;
				var rightx = offsetx + BarWidth;
				rg.Rect = new Rect(new Point(offsetx, rg.Rect.Top), new Point(rightx, rg.Rect.Bottom));
			}
			// reconfigure axis limits
			ResetLimits();
			for (int ix = 0; ix < ItemState.Count; ix++) {
				//_trace.Verbose($"remove-incr update-limits x:{ItemState[ix].XValue} y:{ItemState[ix].Value}");
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Value, 0);
			}
			// finish up
			Layer.Remove(unused);
			Dirty = false;
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var recycler = new Recycler<Path, ItemState<Path>>(new List<Path>(), CreatePath);
			var reproc = new List<ItemState<Path>>();
			for (int ix = 0; ix < items.Count; ix++) {
				var valuey = BindPaths.ValueFor(items[ix]);
				// short-circuit if it's NaN
				if (double.IsNaN(valuey)) { continue; }
				var valuex = BindPaths.CategoryFor(items[ix], startAt + ix);
				// add requested item
				var istate = ElementPipeline(startAt + ix, valuex, valuey, items[ix], recycler, BindPaths.byl);
				if (istate != null) {
					ItemState.Insert(startAt + ix, istate);
					reproc.Add(istate);
				}
			}
			if (reproc.Count > 0) {
				// re-sequence remaining items
				for (int ix = startAt + reproc.Count; ix < ItemState.Count; ix++) {
					var index = ItemState[ix].Index + reproc.Count();
					var valuex = BindPaths.CategoryValue(ItemState[ix].XValue, index);
					var leftx = CategoryAxis.For(valuex);
					var offsetx = leftx + BarOffset;
					ItemState[ix].Move(index, leftx, offsetx);
					// recalc geometry
					var rg = ItemState[ix].Element.Data as RectangleGeometry;
					var rightx = offsetx + BarWidth;
					rg.Rect = new Rect(new Point(offsetx, rg.Rect.Top), new Point(rightx, rg.Rect.Bottom));
				}
			}
			// reconfigure axis limits
			ResetLimits();
			for (int ix = 0; ix < ItemState.Count; ix++) {
				//_trace.Verbose($"add-incr update-limits x:{ItemState[ix].XValue} y:{ItemState[ix].Value}");
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Value, 0);
			}
			// finish up
			Layer.Add(recycler.Created);
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
