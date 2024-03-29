﻿using eScape.Core;
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
	/// <summary>
	/// Render a "candle stick" series, typically used for OHLC data.
	/// </summary>
	public class CandlestickSeries : DataSeriesWithAxes, IDataSourceRenderer, IRequireDataSourceUpdates, IProvideLegend, IProvideSeriesItemValues, IProvideSeriesItemUpdates, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("CandlestickSeries", LogTools.Level.Error);
		#region SeriesItemState
		internal interface IFigureData {
			/// <summary>
			/// Item1: value; Item2: figure representing this value.
			/// Element order: [0] Open, [1] Close, [2] High, [3] Low.
			/// </summary>
			Tuple<double, PathFigure>[] Elements { get; }
		}
		/// <summary>
		/// Item state.
		/// </summary>
		protected class SeriesItemState : ItemState_Matrix<Path>, IFigureData {
			/// <summary>
			/// The list of paths created for the figure.
			/// </summary>
			internal Tuple<double, PathFigure>[] Elements { get; private set; }
			internal SeriesItemState(int idx, double xv, double xo, double yv, Path ele, Tuple<double, PathFigure>[] figs) : base(idx, xv, xo, yv, ele, 0) { Elements = figs; }
			Tuple<double, PathFigure>[] IFigureData.Elements => Elements;
		}
		/// <summary>
		/// Custom item state.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustom_Matrix<Path>, IFigureData {
			/// <summary>
			/// The list of paths created for the figure.
			/// </summary>
			internal Tuple<double, PathFigure>[] Elements { get; private set; }
			internal SeriesItemState_Custom(int idx, double xv, double xo, double yv, object cs, Path ele, Tuple<double, PathFigure>[] figs) : base(idx, xv, xo, yv, cs, ele, 0) { Elements = figs; }
			Tuple<double, PathFigure>[] IFigureData.Elements => Elements;
		}
		#endregion
		#region Evaluators
		class Evaluators : ICategoryEvaluator {
			// category and label (optional)
			internal readonly BindingEvaluator bx;
			// values (required)
			internal readonly BindingEvaluator bopen;
			internal readonly BindingEvaluator bhigh;
			internal readonly BindingEvaluator blow;
			internal readonly BindingEvaluator bclose;
			// value label (optional)
			internal readonly BindingEvaluator bvl;
			#region properties
			public bool IsValid { get { return bopen != null && bhigh != null && blow != null && bclose != null; } }
			#endregion
			#region ctor
			/// <summary>
			/// Ctor.
			/// Initialize evaluators.
			/// Does not fail; check <see cref="IsValid"/> to determine status.
			/// </summary>
			/// <param name="category"></param>
			/// <param name="open"></param>
			/// <param name="high"></param>
			/// <param name="low"></param>
			/// <param name="close"></param>
			/// <param name="valueLabel"></param>
			public Evaluators(string category, string open, string high, string low, string close, string valueLabel) {
				bx = !String.IsNullOrEmpty(category) ? new BindingEvaluator(category) : null;
				bopen = !String.IsNullOrEmpty(open) ? new BindingEvaluator(open) : null;
				bhigh = !String.IsNullOrEmpty(high) ? new BindingEvaluator(high) : null;
				blow = !String.IsNullOrEmpty(low) ? new BindingEvaluator(low) : null;
				bclose = !String.IsNullOrEmpty(close) ? new BindingEvaluator(close) : null;
				bvl = !String.IsNullOrEmpty(valueLabel) ? new BindingEvaluator(valueLabel) : null;
			}
			#endregion
			#region public
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
			public double OpenFor(object item) {
				var value = DataSeries.CoerceValue(item, bopen);
				return value;
			}
			public double HighFor(object item) {
				var value = DataSeries.CoerceValue(item, bhigh);
				return value;
			}
			public double LowFor(object item) {
				var value = DataSeries.CoerceValue(item, blow);
				return value;
			}
			public double CloseFor(object item) {
				var value = DataSeries.CoerceValue(item, bclose);
				return value;
			}
			#endregion
		}
		#endregion
		#region render state
		class State : RenderStateCore<ItemState<Path>, Path> {
			internal readonly Evaluators evs;
			internal State(List<ItemState<Path>> sis, Recycler<Path, ItemState<Path>> rc, Evaluators evs) : base(sis, rc) {
				this.evs = evs;
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// The title for the series.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The style to use for Close above Open (went up).
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The style to use for Open above Close (went down).
		/// </summary>
		public Style ReversePathStyle { get { return (Style)GetValue(ReversePathStyleProperty); } set { SetValue(ReversePathStyleProperty, value); } }
		/// <summary>
		/// Template to use for generated paths.
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
		/// Binding path for the Open value.
		/// </summary>
		public String OpenValuePath { get { return (String)GetValue(OpenValuePathProperty); } set { SetValue(OpenValuePathProperty, value); } }
		/// <summary>
		/// Binding path for the High value.
		/// </summary>
		public String HighValuePath { get { return (String)GetValue(HighValuePathProperty); } set { SetValue(HighValuePathProperty, value); } }
		/// <summary>
		/// Binding path for the Low value.
		/// </summary>
		public String LowValuePath { get { return (String)GetValue(LowValuePathProperty); } set { SetValue(LowValuePathProperty, value); } }
		/// <summary>
		/// Binding path for the Close value.
		/// </summary>
		public String CloseValuePath { get { return (String)GetValue(CloseValuePathProperty); } set { SetValue(CloseValuePathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis label.
		/// MAY be NULL.
		/// If specified, this value will augment the one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
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
		protected List<ItemState<Path>> ItemState { get; set; }
		/// <summary>
		/// Create <see cref="BindingEvaluator"/> instances one time.
		/// TODO must re-create when any of the DPs change!
		/// </summary>
		Evaluators BindPaths { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(String), typeof(CandlestickSeries), new PropertyMetadata("Title")
		);
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(CandlestickSeries), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="ReversePathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ReversePathStyleProperty = DependencyProperty.Register(
			nameof(ReversePathStyle), typeof(Style), typeof(CandlestickSeries), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="PathTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathTemplateProperty = DependencyProperty.Register(
			nameof(PathTemplate), typeof(DataTemplate), typeof(CandlestickSeries), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="OpenValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty OpenValuePathProperty = DependencyProperty.Register(
			nameof(OpenValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="HighValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty HighValuePathProperty = DependencyProperty.Register(
			nameof(HighValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="LowValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LowValuePathProperty = DependencyProperty.Register(
			nameof(LowValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="CloseValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CloseValuePathProperty = DependencyProperty.Register(
			nameof(CloseValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(DataSeriesWithValue), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public CandlestickSeries()  {
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
				if (ItemState[ix] is IFigureData sis) {
					UpdateLimits(ItemState[ix].XValue, sis.Elements[0].Item1, sis.Elements[1].Item1, sis.Elements[2].Item1, sis.Elements[3].Item1);
				}
			}
		}
		#endregion
		#region helpers
		Legend Legend() {
			return new Legend() { Title = Title, Fill = PathStyle.Find<Brush>(Path.FillProperty), Stroke = PathStyle.Find<Brush>(Path.StrokeProperty) };
		}
		IEnumerable<ISeriesItem> WrapItemState(IEnumerable<ItemState<Path>> siss) {
			foreach (var state in siss) {
				if(state is SeriesItemState sis) {
					var sis2 = new ISeriesItemValue[sis.Elements.Length];
					for (int idx = 0; idx < sis.Elements.Length; idx++) {
						sis2[idx] = new ItemStateValueWrapper(sis, sis.Elements[idx].Item1, idx);
					}
					var sivc = new ItemStateMultiChannelWrapper(sis, sis2);
					yield return sivc;
				}
				else if(state is SeriesItemState_Custom sisc) {
					var sis2 = new ISeriesItemValue[sisc.Elements.Length];
					for (int idx = 0; idx < sisc.Elements.Length; idx++) {
						sis2[idx] = new ItemStateValueCustomWrapper(sisc, sisc.Elements[idx].Item1, sisc.CustomValue, idx);
					}
					var sivc = new ItemStateMultiChannelWrapper(sisc, sis2);
					yield return sivc;
				}
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
			}
			return path;
		}
		ItemState<Path> ElementPipeline (
			int index, double valuex, double valueO, double valueH, double valueL, double valueC,
			object item, Recycler<Path, ItemState<Path>> recycler, BindingEvaluator bvl
		) {
			// map through axes
			var y1 = ValueAxis.For(valueO);
			var y2 = ValueAxis.For(valueC);
			var y3 = ValueAxis.For(valueH);
			var y4 = ValueAxis.For(valueL);
			var leftx = CategoryAxis.For(valuex);
			var offsetx = BarOffset;
			var rightx = offsetx + BarWidth;
			// force them to be a min/max
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var highy = Math.Max(y3, y4);
			var lowy = Math.Min(y3, y4);
			_trace.Verbose($"{Name}[{index}] {valueO}/{valueH}/{valueL}/{valueC} ({offsetx},{topy}) ({rightx},{bottomy})");
			// create geometry
			var path = recycler.Next(null);
			if (path == null) return null;
			var pg = new PathGeometry();
			// body (open/close)
			var body = PathHelper.Rectangle(offsetx, topy, rightx, bottomy);
			pg.Figures.Add(body);
			// upper shadow (high)
			var centerx = offsetx + (rightx - offsetx) / 2;
			var upper = PathHelper.Line(centerx, topy, centerx, highy);
			pg.Figures.Add(upper);
			// lower shadow (low)
			var lower = PathHelper.Line(centerx, bottomy, centerx, lowy);
			pg.Figures.Add(lower);
			var shim = new GeometryWithOffsetShim<PathGeometry>() { PathData = pg };
			path.Item2.DataContext = shim;
			// establish the style for "forward" or "reverse" polarity
			BindTo(this, valueO < valueC ? nameof(PathStyle) : nameof(ReversePathStyle), path.Item2, Path.StyleProperty);
			// bind offset
			BindTo(shim, nameof(shim.Offset), path.Item2, Canvas.LeftProperty);
			var figs = new Tuple<double, PathFigure>[4];
			figs[0] = new Tuple<double, PathFigure>(y1, body);
			figs[1] = new Tuple<double, PathFigure>(y2, body);
			figs[2] = new Tuple<double, PathFigure>(y3, upper);
			figs[3] = new Tuple<double, PathFigure>(y4, lower);
			if (bvl == null) {
				return new SeriesItemState(index, leftx, BarOffset, y1, path.Item2, figs);
			} else {
				var cs = bvl.For(item);
				return new SeriesItemState_Custom(index, leftx, BarOffset, y1, cs, path.Item2, figs);
			}
		}
		/// <summary>
		/// Recalculate geometry based on current values.
		/// </summary>
		/// <param name="st">State to update.</param>
		void UpdateGeometry(ItemStateCore st) {
			//(st as IFigureData).UpdateGeometry(BarWidth);
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
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Initialize after entering VT.
		/// </summary>
		/// <param name="icelc"></param>
		public void Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			_trace.Verbose($"enter v:{ValueAxisName}:{ValueAxis} c:{CategoryAxisName}:{CategoryAxis} d:{DataSourceName}");
			AssignFromRef(icelc as IChartErrorInfo,NameOrType(), nameof(PathStyle), nameof(Theme.PathMarkerSeries),
				PathStyle == null, Theme != null, Theme.PathMarkerSeries != null,
				() => PathStyle = Theme.PathMarkerSeries
			);
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(ReversePathStyle), nameof(PathStyle),
				ReversePathStyle == null, true, PathStyle != null,
				() => ReversePathStyle = PathStyle
			);
			BindPaths = new Evaluators(CategoryPath, OpenValuePath, HighValuePath, LowValuePath, CloseValuePath, ValueLabelPath);
			if (!BindPaths.IsValid) {
				if (icelc is IChartErrorInfo icei) {
					var props = new List<String>();
					if (String.IsNullOrEmpty(OpenValuePath)) props.Add(nameof(OpenValuePath));
					if (String.IsNullOrEmpty(HighValuePath)) props.Add(nameof(HighValuePath));
					if (String.IsNullOrEmpty(LowValuePath)) props.Add(nameof(LowValuePath));
					if (String.IsNullOrEmpty(CloseValuePath)) props.Add(nameof(CloseValuePath));
					icei.Report(new ChartValidationResult(NameOrType(), $"{String.Join(",", props)}: must be specified", props));
				}
			}
		}
		/// <summary>
		/// Undo effects of Enter().
		/// </summary>
		/// <param name="icelc"></param>
		public void Leave(IChartEnterLeaveContext icelc) {
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
		public void Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			var matx = MatrixSupport.TransformForOffsetX(icrc.SeriesArea, CategoryAxis, ValueAxis);
			var mt = new MatrixTransform() { Matrix = matx };
			foreach (var state in ItemState) {
				if (state.Element.DataContext is GeometryWithOffsetShim<PathGeometry> gs) {
					gs.GeometryTransform = mt;
					var output = matx.Transform(new Point(state.XValue, 0));
					gs.Offset = icrc.Area.Left + output.X;
				} else {
					state.Element.Data.Transform = mt;
				}
				if (ClipToDataRegion) {
					state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IDataSourceRenderer
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (BindPaths == null || !BindPaths.IsValid) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path, ItemState<Path>>(paths, CreatePath);
			return new State(new List<ItemState<Path>>(), recycler, BindPaths);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// "raw" values
			var valueO = (double)st.evs.bopen.For(item);
			var valueH = (double)st.evs.bhigh.For(item);
			var valueL = (double)st.evs.blow.For(item);
			var valueC = (double)st.evs.bclose.For(item);
			var valuex = st.evs.bx != null ? (double)st.evs.bx.For(item) : index;
			st.ix = index;
			// short-circuit if any are NaN
			if (double.IsNaN(valueO) || double.IsNaN(valueH) || double.IsNaN(valueL) || double.IsNaN(valueC)) {
				return;
			}
			UpdateLimits(valuex, valueO, valueH, valueL, valueC);
			var istate = ElementPipeline(index, valuex, valueO, valueH, valueL, valueC, item, st.recycler, st.evs.bvl);
			if (istate != null) st.itemstate.Add(istate);
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
			var reproc = IncrementalRemove<ItemState<Path>>(startAt, items, ItemState, istate => istate.Element != null, (rpc, istate) => {
				istate.Shift(-rpc, BindPaths, CategoryAxis, UpdateGeometry);
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
			var recycler = new Recycler<Path, ItemState<Path>>(new List<Path>(), CreatePath);
			var reproc = IncrementalAdd<ItemState<Path>>(startAt, items, ItemState, (ix, item) => {
				var valueO = BindPaths.OpenFor(item);
				var valueH = BindPaths.HighFor(item);
				var valueL = BindPaths.LowFor(item);
				var valueC = BindPaths.CloseFor(item);
				// short-circuit if it's NaN
				if (double.IsNaN(valueO) || double.IsNaN(valueH) || double.IsNaN(valueL) || double.IsNaN(valueC)) { return null; }
				var valuex = BindPaths.CategoryFor(item, ix);
				// add requested item
				var istate = ElementPipeline(ix, valuex, valueO, valueH, valueL, valueC, item, recycler, BindPaths.bvl);
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
}
