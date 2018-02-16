using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Render a "candle stick" series, typically used for OHLC data.
	/// </summary>
	public class CandlestickSeries : DataSeriesWithAxes, IDataSourceRenderer, IProvideLegend, IProvideSeriesItemValues, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("CandlestickSeries", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// Item state.
		/// </summary>
		protected class SeriesItemState : ItemState_Matrix<Path> {
			/// <summary>
			/// The list of paths created for the figure.
			/// </summary>
			internal Tuple<double, PathFigure>[] Elements { get; private set; }
			internal SeriesItemState(int idx, double xv, double xvo, double yv, Path ele, Tuple<double, PathFigure>[] figs) : base(idx, xv, xvo, yv, ele, 0) { Elements = figs; }
		}
		/// <summary>
		/// Custom item state.
		/// </summary>
		protected class SeriesItemState_Custom : ItemStateCustom_Matrix<Path> {
			/// <summary>
			/// The list of paths created for the figure.
			/// </summary>
			internal Tuple<double, PathFigure>[] Elements { get; private set; }
			internal SeriesItemState_Custom(int idx, double xv, double xvo, double yv, object cs, Path ele, Tuple<double, PathFigure>[] figs) : base(idx, xv, xvo, yv, cs, ele, 0) { Elements = figs; }
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
		/// If specified, this value will augmentthe one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
		/// <summary>
		/// Provide item values.
		/// </summary>
		public IEnumerable<ISeriesItem> SeriesItemValues => UnwrapItemState(ItemState.AsReadOnly());
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<ItemState<Path>> ItemState { get; set; }
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
		#region helpers
		IEnumerable<ISeriesItem> UnwrapItemState(IEnumerable<ItemState<Path>> siss) {
			foreach (var state in siss) {
				if(state is SeriesItemState sis) {
					var sis2 = new ISeriesItemValue[sis.Elements.Length];
					for (int idx = 0; idx < sis.Elements.Length; idx++) {
						sis2[idx] = new ItemState<PathFigure>(sis.Index, sis.XValueIndex, sis.XValueOffset, sis.Elements[idx].Item1, sis.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelCore(sis.Index, sis.XValueIndex, sis.XValueOffset, sis2);
					yield return sivc;
				}
				else if(state is SeriesItemState_Custom sisc) {
					var sis2 = new ISeriesItemValue[sisc.Elements.Length];
					for (int idx = 0; idx < sisc.Elements.Length; idx++) {
						sis2[idx] = new ItemStateCustom<PathFigure>(sisc.Index, sisc.XValueIndex, sisc.XValueOffset, sisc.Elements[idx].Item1, sisc.CustomValue, sisc.Elements[idx].Item2, idx);
					}
					var sivc = new ItemStateMultiChannelCore(sisc.Index, sisc.XValueIndex, sisc.XValueOffset, sis2);
					yield return sivc;
				}
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
			var matx = MatrixSupport.TransformFor(icrc.SeriesArea, CategoryAxis, ValueAxis);
			var mt = new MatrixTransform() { Matrix = matx };
			foreach (var state in ItemState) {
				state.Element.Data.Transform = mt;
				if (ClipToDataRegion) {
					state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
			}
			_trace.Verbose($"{Name} mat:{matx} clip:{icrc.SeriesArea}");
		}
		#endregion
		#region IDataSourceRenderer
		class State : RenderStateCore<ItemState<Path>, Path> {
			// category and label
			internal readonly BindingEvaluator bx;
			internal readonly BindingEvaluator bl;
			// values
			internal readonly BindingEvaluator bopen;
			internal readonly BindingEvaluator bhigh;
			internal readonly BindingEvaluator blow;
			internal readonly BindingEvaluator bclose;
			// value label
			internal readonly BindingEvaluator bvl;
			internal State(List<ItemState<Path>> sis, Recycler<Path> rc, params BindingEvaluator[] bes) :base(sis, rc) {
				bx = bes[0];
				bl = bes[1];
				bopen = bes[2];
				bhigh = bes[3];
				blow = bes[4];
				bclose = bes[5];
				bvl = bes[6];
			}
		}
		/// <summary>
		/// Path factory for recycler.
		/// </summary>
		/// <returns></returns>
		Path CreatePath() {
			var path = new Path();
			return path;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(OpenValuePath)) return null;
			if (String.IsNullOrEmpty(HighValuePath)) return null;
			if (String.IsNullOrEmpty(LowValuePath)) return null;
			if (String.IsNullOrEmpty(CloseValuePath)) return null;
			var bopen = new BindingEvaluator(OpenValuePath);
			var bhigh = new BindingEvaluator(HighValuePath);
			var blow = new BindingEvaluator(LowValuePath);
			var bclose = new BindingEvaluator(CloseValuePath);
			// TODO report the binding error
			if (bopen == null) return null;
			if (bhigh == null) return null;
			if (blow == null) return null;
			if (bclose == null) return null;
			ResetLimits();
			var paths = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<Path>(paths, CreatePath);
			return new State(new List<ItemState<Path>>(), recycler,
				!String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				!String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				bopen, bhigh, blow, bclose,
				!String.IsNullOrEmpty(ValueLabelPath) ? new BindingEvaluator(ValueLabelPath) : null);
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			// "raw" values
			var valueO = (double)st.bopen.For(item);
			var valueH = (double)st.bhigh.For(item);
			var valueL = (double)st.blow.For(item);
			var valueC = (double)st.bclose.For(item);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			UpdateLimits(valuex, valueO, valueH, valueL, valueC);
			st.ix = index;
			// short-circuit if any are NaN
			if (double.IsNaN(valueO) || double.IsNaN(valueH) || double.IsNaN(valueL) || double.IsNaN(valueC)) {
				if (st.bl != null) {
					// still map the X
					CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()));
				}
				return;
			}
			// map through axes
			var y1 = ValueAxis.For(valueO);
			var y2 = ValueAxis.For(valueC);
			var y3 = ValueAxis.For(valueH);
			var y4 = ValueAxis.For(valueL);
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString())));
			var barx = leftx + BarOffset;
			var rightx = barx + BarWidth;
			// force them to be a min/max
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var highy = Math.Max(y3, y4);
			var lowy = Math.Min(y3, y4);
			_trace.Verbose($"{Name}[{index}] {valueO}/{valueH}/{valueL}/{valueC} ({barx},{topy}) ({rightx},{bottomy})");
			// create geometry
			var path = st.NextElement();
			if (path == null) return;
			var pg = new PathGeometry();
			// body (open/close)
			var pf = PathHelper.Rectangle(barx, topy, rightx, bottomy);
			pg.Figures.Add(pf);
			// upper shadow (high)
			var centerx = barx + (rightx - barx) / 2;
			var upper = PathHelper.Line(centerx, topy, centerx, highy);
			pg.Figures.Add(upper);
			// lower shadow (low)
			var lower = PathHelper.Line(centerx, bottomy, centerx, lowy);
			pg.Figures.Add(lower);
			path.Item2.Data = pg;
			// establish the style for "forward" or "reverse" polarity
			BindTo(this, valueO < valueC ? nameof(PathStyle) : nameof(ReversePathStyle), path.Item2, Path.StyleProperty);
			var figs = new Tuple<double, PathFigure>[4];
			figs[0] = new Tuple<double, PathFigure>(y1, pf);
			figs[1] = new Tuple<double, PathFigure>(y2, pf);
			figs[2] = new Tuple<double, PathFigure>(y3, upper);
			figs[3] = new Tuple<double, PathFigure>(y4, lower);
			if (st.bvl == null) {
				st.itemstate.Add(new SeriesItemState(index, leftx, barx, y1, path.Item2, figs));
			} else {
				var cs = st.bvl.For(item);
				st.itemstate.Add(new SeriesItemState_Custom(index, leftx, barx, y1, cs, path.Item2, figs));
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
			ItemState = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			Dirty = false;
		}
		#endregion
	}
}
