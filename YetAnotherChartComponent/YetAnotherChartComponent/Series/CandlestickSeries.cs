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
	public class CandlestickSeries : DataSeries, IDataSourceRenderer, IProvideLegend, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("CandlestickSeries", LogTools.Level.Error);
		/// <summary>
		/// Shorthand for marker state.
		/// </summary>
		protected class SeriesItemState : ItemState_Matrix<Path> { }
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
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Data needed for current markers
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
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
			nameof(OpenValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Identifies <see cref="HighValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty HighValuePathProperty = DependencyProperty.Register(
			nameof(HighValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Identifies <see cref="LowValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LowValuePathProperty = DependencyProperty.Register(
			nameof(LowValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Identifies <see cref="CloseValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CloseValuePathProperty = DependencyProperty.Register(
			nameof(CloseValuePath), typeof(string), typeof(CandlestickSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public CandlestickSeries()  {
			ItemState = new List<SeriesItemState>();
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
				PathStyle == null && Theme != null,
				Theme.PathMarkerSeries != null,
				() => PathStyle = Theme.PathMarkerSeries
			);
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(ReversePathStyle), nameof(PathStyle),
				ReversePathStyle == null,
				PathStyle != null,
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
		class State : RenderStateCore<SeriesItemState, Path> {
			internal BindingEvaluator bx;
			internal BindingEvaluator bl;
			internal BindingEvaluator bopen;
			internal BindingEvaluator bhigh;
			internal BindingEvaluator blow;
			internal BindingEvaluator bclose;
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
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				bl = !String.IsNullOrEmpty(CategoryLabelPath) ? new BindingEvaluator(CategoryLabelPath) : null,
				bopen = bopen,
				bhigh = bhigh,
				blow = blow,
				bclose = bclose,
				itemstate = new List<SeriesItemState>(),
				recycler = recycler,
				elements = recycler.Items().GetEnumerator()
			};
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
			var leftx = (st.bl == null ? CategoryAxis.For(valuex) : CategoryAxis.For(new Tuple<double, String>(valuex, st.bl.For(item).ToString()))) + BarOffset;
			var rightx = leftx + BarWidth;
			// force them to be a min/max
			var topy = Math.Max(y1, y2);
			var bottomy = Math.Min(y1, y2);
			var highy = Math.Max(y3, y4);
			var lowy = Math.Min(y3, y4);
			_trace.Verbose($"{Name}[{index}] {valueO}/{valueH}/{valueL}/{valueC} ({leftx},{topy}) ({rightx},{bottomy})");
			// create geometry
			var path = st.NextElement();
			if (path == null) return;
			var pg = new PathGeometry();
			// body (open/close)
			var pf = PathHelper.Rectangle(leftx, topy, rightx, bottomy);
			pg.Figures.Add(pf);
			// upper shadow (high)
			var centerx = leftx + (rightx - leftx) / 2;
			var upper = PathHelper.Line(centerx, topy, centerx, highy);
			pg.Figures.Add(upper);
			// lower shadow (low)
			var lower = PathHelper.Line(centerx, bottomy, centerx, lowy);
			pg.Figures.Add(lower);
			path.Data = pg;
			// establish the style for "forward" or "reverse" polarity
			BindTo(this, valueO < valueC ? nameof(PathStyle) : nameof(ReversePathStyle), path, Path.StyleProperty);
			st.itemstate.Add(new SeriesItemState() { Index = index, XValue = leftx, YValue = y1, Element = path });
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
