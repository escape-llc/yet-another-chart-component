using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ColumnStackItem
	/// <summary>
	/// Represents an individual item in the stack.
	/// </summary>
	public class ColumnStackItem : DependencyObject {
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
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(ColumnStackItem), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(String), typeof(ColumnStackItem), new PropertyMetadata("Title")
		);
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(ColumnStackItem), new PropertyMetadata(null, new PropertyChangedCallback(ItemPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Cascade up to the series.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		protected static void ItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			//ColumnStackItem csi = d as ColumnStackItem;
			//ds.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
		}
		#endregion
	}
	/// <summary>
	/// Formality for XAML happiness.
	/// </summary>
	public class ColumnStackItemCollection : ObservableCollection<ColumnStackItem> { }
	#endregion
	#region StackedColumnSeries
	/// <summary>
	/// Stacked column series.
	/// Plots multiple series values on a stacked arrangement.
	/// </summary>
	public class StackedColumnSeries : DataSeriesWithAxes, IProvideSeriesItemValues, IProvideSeriesItemUpdates, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IDataSourceRenderer, IRequireDataSourceUpdates, IRequireTransforms {
		static readonly LogTools.Flag _trace = LogTools.Add("StackedColumnSeries", LogTools.Level.Error);
		#region Series and Channel item state
		/// <summary>
		/// Item state.
		/// </summary>
		protected class SeriesItemState : ItemStateCore {
			/// <summary>
			/// Track the "low" end of the column.
			/// </summary>
			public double Min { get; set; }
			/// <summary>
			/// Track the "high" end of the column.
			/// </summary>
			public double Max { get; set; }
			/// <summary>
			/// The list of paths created for the column.
			/// </summary>
			public List<Tuple<double, Path>> Elements { get; private set; } = new List<Tuple<double, Path>>();
			/// <summary>
			/// Bookkeeping for Min/Max values.
			/// </summary>
			/// <param name="vx"></param>
			public void UpdateLimits(double vx) {
				if (double.IsNaN(vx)) return;
				if (vx < Min) Min = vx;
				if (vx > Max) Max = vx;
			}
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="idx">Index.</param>
			/// <param name="xv">x-value.</param>
			/// <param name="xo">x-value offset.</param>
			public SeriesItemState(int idx, double xv, double xo) : base(idx, xv, xo) { }
		}
		/// <summary>
		/// Custom state version.
		/// </summary>
		protected class SeriesItemState_Custom : SeriesItemState {
			/// <summary>
			/// The custom state.
			/// </summary>
			public object CustomValue { get; private set; }
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="idx">Index.</param>
			/// <param name="xv">x-value.</param>
			/// <param name="xo">x-value offset.</param>
			/// <param name="cs">Custom state.</param>
			public SeriesItemState_Custom(int idx, double xv, double xo, object cs) : base(idx, xv, xo) { CustomValue = cs; }
		}
		/// <summary>
		/// Wrapper for the channel items.
		/// </summary>
		protected class ChannelItemState : ItemStateWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal ChannelItemState(int idx, double xv, double xo, double yv, Path ele, int ch) : base(idx, xv, xo, yv, ele, ch) { }
		}
		/// <summary>
		/// Wrapper for the channel items.
		/// </summary>
		protected class ChannelItemState_Custom : ItemStateCustomWithPlacement<Path> {
			/// <summary>
			/// Extract the rectangle geometry and create placement.
			/// </summary>
			/// <returns></returns>
			protected override Placement CreatePlacement() { return new RectanglePlacement(Value >= 0 ? Placement.UP_RIGHT : Placement.DOWN_RIGHT, (Element.Data as RectangleGeometry).Rect); }
			internal ChannelItemState_Custom(int idx, double xv, double xo, double yv, object cs, Path ele, int ch) : base(idx, xv, xo, yv, cs, ele, ch) { }
		}
		#endregion
		#region Evaluators
		class Evaluators : ICategoryEvaluator {
			internal readonly BindingEvaluator bx;
			internal readonly BindingEvaluator[] bys;
			internal readonly BindingEvaluator byl;
			public Evaluators(string categoryPath, string valueLabelPath, IEnumerable<string> columnValuePaths) {
				bx = !String.IsNullOrEmpty(categoryPath) ? new BindingEvaluator(categoryPath) : null;
				byl = !String.IsNullOrEmpty(valueLabelPath) ? new BindingEvaluator(valueLabelPath) : null;
				bys = new BindingEvaluator[columnValuePaths.Count()];
				int ix = 0;
				foreach (var cs in columnValuePaths) {
					bys[ix++] = String.IsNullOrEmpty(cs) ? null : new BindingEvaluator(cs);
				}
			}
			/// <summary>
			/// Valid if ALL the column values have a <see cref="BindingEvaluator"/>.
			/// </summary>
			public bool IsValid { get { return bys.All(xx => xx != null); } }
			/// <summary>
			/// Use the <see cref="bx"/> evaluator to return the x-axis value, or index if it is NULL.
			/// </summary>
			/// <param name="ox">Object to evaluate.</param>
			/// <param name="index">Index value if <see cref="bx"/> is NULL.</param>
			/// <returns></returns>
			public double CategoryFor(object ox, int index) {
				var valuex = bx != null ? (double)bx.For(ox) : index;
				return valuex;
			}
			/// <summary>
			/// Use the <see cref="bx"/> evaluator to decide between two <see cref="double"/> values.
			/// </summary>
			/// <param name="dx">Return if <see cref="bx"/> is NOT NULL.</param>
			/// <param name="index">Otherwise.</param>
			/// <returns></returns>
			public double CategoryValue(double dx, int index) {
				var valuex = bx != null ? dx : index;
				return valuex;
			}
		}
		#endregion
		#region Render state
		class State : RenderStateCore<SeriesItemState, Path> {
			internal readonly Evaluators evs;
			internal State(List<SeriesItemState> sis, Recycler<Path, SeriesItemState> rc, Evaluators evs) : base(sis, rc) {
				this.evs = evs;
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The stack of column items.
		/// </summary>
		public ColumnStackItemCollection ColumnStack { get; private set; }
		/// <summary>
		/// Template to use for generated paths.
		/// </summary>
		public DataTemplate PathTemplate { get { return (DataTemplate)GetValue(PathTemplateProperty); } set { SetValue(PathTemplateProperty, value); } }
		/// <summary>
		/// Binding path to the value axis label.
		/// MAY be NULL.
		/// If specified, this value will augment the one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
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
		/// Provide item values.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues => WrapItemState(ItemState.AsReadOnly());
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		/// <summary>
		/// Evaluate only once.
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(StackedColumnSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="PathTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathTemplateProperty = DependencyProperty.Register(
			nameof(PathTemplate), typeof(DataTemplate), typeof(StackedColumnSeries), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public StackedColumnSeries() {
			ColumnStack = new ColumnStackItemCollection();
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region extensions
		/// <summary>
		/// Implement for this class.
		/// </summary>
		protected override void ReconfigureLimits() {
			ResetLimits();
			for (int ix = 0; ix < ItemState.Count; ix++) {
				UpdateLimits(ItemState[ix].XValue, ItemState[ix].Min, ItemState[ix].Max);
			}
		}
		#endregion
		#region helpers
		IEnumerable<ISeriesItem> WrapItemState(IEnumerable<SeriesItemState> siss) {
			foreach (var sis in siss) {
				// IST check Custom first it's a subclass!
				if (sis is SeriesItemState_Custom sisc) {
					var sis2 = new ISeriesItemValue[sis.Elements.Count];
					for (int idx = 0; idx < sis.Elements.Count; idx++) {
						sis2[idx] = new ChannelItemState_Custom(sis.Index, sis.XValue, sis.XOffset, sis.Elements[idx].Item1, sisc.CustomValue, sis.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelWrapper(sis, sis2);
					yield return sivc;
				} else {
					var sis2 = new ISeriesItemValue[sis.Elements.Count];
					for (int idx = 0; idx < sis.Elements.Count; idx++) {
						sis2[idx] = new ChannelItemState(sis.Index, sis.XValue, sis.XOffset, sis.Elements[idx].Item1, sis.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelWrapper(sis, sis2);
					yield return sivc;
				}
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <param name="sis">Not used.</param>
		/// <returns></returns>
		Path CreatePath(SeriesItemState sis) {
			var path = default(Path);
			if (PathTemplate != null) {
				path = PathTemplate.LoadContent() as Path;
			} else if (Theme?.PathTemplate != null) {
				path = Theme.PathTemplate.LoadContent() as Path;
			}
			return path;
		}
		/// <summary>
		/// Create the next series state.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="valuex"></param>
		/// <param name="item"></param>
		/// <param name="recycler"></param>
		/// <param name="evs"></param>
		/// <returns>New instance or NULL.</returns>
		SeriesItemState ElementPipeline(int index, double valuex, object item, Recycler<Path, SeriesItemState> recycler, Evaluators evs) {
			var leftx = CategoryAxis.For(valuex);
			var barx = BarOffset;
			var rightx = barx + BarWidth;
			var sis = evs.byl == null ? new SeriesItemState(index, leftx, BarOffset) : new SeriesItemState_Custom(index, leftx, BarOffset, evs.byl.For(item));
			for (int ix = 0; ix < evs.bys.Length; ix++) {
				var valuey = CoerceValue(item, evs.bys[ix]);
				if (double.IsNaN(valuey)) {
					continue;
				}
				var colbase = valuey >= 0 ? sis.Max : sis.Min;
				var colend = colbase + valuey;
				var y1 = ValueAxis.For(colend);
				var y2 = ValueAxis.For(colbase);
				var topy = Math.Max(y1, y2);
				var bottomy = Math.Min(y1, y2);
				sis.UpdateLimits(y1);
				_trace.Verbose($"{Name}[{index},{ix}] {valuey} ({barx},{topy}) ({rightx},{bottomy}) sis ({sis.Min},{sis.Max})");
				var path = recycler.Next(null);
				if (path == null) return null;
				var shim = new GeometryWithOffsetShim<RectangleGeometry>() {
					PathData = new RectangleGeometry() { Rect = new Rect(new Point(barx, topy), new Point(rightx, bottomy)) }
				};
				path.Item2.DataContext = shim;
				BindTo(ColumnStack[ix], "PathStyle", path.Item2, FrameworkElement.StyleProperty);
				// bind offset
				BindTo(shim, nameof(shim.Offset), path.Item2, Canvas.LeftProperty);
				UpdateLimits(valuex, sis.Min, sis.Max);
				sis.Elements.Add(new Tuple<double, Path>(valuey, path.Item2));
			}
			return sis;
		}
		/// <summary>
		/// Cascade geometry update.
		/// </summary>
		/// <param name="st">State to update.</param>
		void UpdateGeometry(ItemStateCore st) { }
		#endregion
		#region IProvideSeriesItemUpdates
		/// <summary>
		/// Made public so it's easier to implement (auto).
		/// </summary>
		public event EventHandler<SeriesItemUpdateEventArgs> ItemUpdates;
		#endregion
		#region IProvideLegend
		private LegendBase[] _legend;
		IEnumerable<LegendBase> IProvideLegend.LegendItems {
			get { if (_legend == null) _legend = Legend(); return _legend; }
		}
		Legend[] Legend() {
			var items = new List<Legend>();
			foreach (var stk in ColumnStack) {
				items.Add(new Legend() { Title = stk.Title, Fill = stk.PathStyle.Find<Brush>(Path.FillProperty), Stroke = stk.PathStyle.Find<Brush>(Path.StrokeProperty) });
			}
			return items.ToArray();
		}
		#endregion
		#region IRequireEnterLeave
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
			BindPaths = new Evaluators(CategoryPath, ValueLabelPath, ColumnStack.Select(cs => cs.ValuePath));
			if (!BindPaths.IsValid) {
				for (int ix = 0; ix < ColumnStack.Count; ix++) {
					if (String.IsNullOrEmpty(ColumnStack[ix].ValuePath)) {
						if (icelc is IChartErrorInfo icei) {
							icei.Report(new ChartValidationResult(NameOrType(), $"ValuePath[{ix}] was not set, NO values are generated", new[] { $"ValuePath[{ix}]" }));
						}
					}
				}
			}
		}
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
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			var matx = MatrixSupport.TransformForOffsetX(icrc.Area, CategoryAxis, ValueAxis);
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
			var mt = new MatrixTransform() { Matrix = matx };
			foreach (var state in ItemState) {
				foreach (var el in state.Elements) {
					if(el.Item2.DataContext is GeometryWithOffsetShim<RectangleGeometry> gs) {
						gs.GeometryTransform = mt;
						var output = matx.Transform(new Point(state.XValue, 0));
						gs.Offset = icrc.Area.Left + output.X;
					}
					if (ClipToDataRegion) {
						var cg = new RectangleGeometry() { Rect = icrc.SeriesArea };
						el.Item2.Clip = cg;
					}
				}
			}
		}
		#endregion
		#region IDataSourceRenderer
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Elements).SelectMany(el => el).Select(el => el.Item2);
			var recycler = new Recycler<Path, SeriesItemState>(paths, CreatePath);
			return new State(new List<SeriesItemState>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuex = st.evs.bx != null ? (double)st.evs.bx.For(item) : index;
			st.ix = index;
			var sis = ElementPipeline(index, valuex, item, st.recycler, st.evs);
			if (sis != null) {
				st.itemstate.Add(sis);
			}
		}
		void IDataSourceRenderer.RenderComplete(object state) { }
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
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
			var reproc = IncrementalRemove<SeriesItemState>(startAt, items, ItemState, null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, UpdateGeometry);
			});
			ReconfigureLimits();
			// finish up
			var paths = reproc.Select(ms => ms.Elements).SelectMany(el => el).Select(el => el.Item2);
			Layer.Remove(paths);
			Dirty = false;
			RaiseItemsUpdated(ItemUpdates, icrc, NotifyCollectionChangedAction.Remove, startAt, reproc);
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (BindPaths == null || !BindPaths.IsValid) return;
			var recycler = new Recycler<Path, SeriesItemState>(new List<Path>(), CreatePath);
			var reproc = IncrementalAdd<SeriesItemState>(startAt, items, ItemState, (ix, item) => {
				var valuex = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, valuex, item, recycler, BindPaths);
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
