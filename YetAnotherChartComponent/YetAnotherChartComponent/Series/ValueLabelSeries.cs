using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Series that creates value labels.
	/// </summary>
	public class ValueLabelSeries : DataSeriesWithValue, IDataSourceRenderer, IRequireChartTheme, IRequireEnterLeave, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("ValueLabelSeries", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// Shorthand for label state.
		/// </summary>
		protected class SeriesItemState : ItemState<TextBlock> { }
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The style to apply to labels.
		/// </summary>
		public Style LabelStyle { get { return (Style)GetValue(LabelStyleProperty); } set { SetValue(LabelStyleProperty, value); } }
		/// <summary>
		/// Alternate format string for labels.
		/// </summary>
		public String LabelFormatString { get; set; }
		/// <summary>
		/// Offset in Category axis offset in [0..1].
		/// Use with ColumnSeries to get the "points" to align with the column(s) layout in their cells.
		/// </summary>
		public double CategoryAxisOffset { get; set; }
		/// <summary>
		/// LabelOffset is translation from the "center" of the TextBlock.
		/// Units are Half-dimension based on TextBlock size.
		/// Default value is (0,0)
		/// </summary>
		public Point LabelOffset { get; set; } = new Point(0, 0);
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
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(nameof(LabelStyle), typeof(Style), typeof(ValueLabelSeries), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public ValueLabelSeries() {
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region IRequireEnterLeave
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureAxes(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(LabelStyle), nameof(Theme.LabelAxisTop),
				LabelStyle == null && Theme != null,
				Theme.LabelAxisTop != null,
				() => LabelStyle = Theme.LabelAxisTop
			);
			_trace.Verbose($"{Name} enter v:{ValueAxisName} {ValueAxis} c:{CategoryAxisName} {CategoryAxis} d:{DataSourceName}");
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// Adjust transforms for the current element state.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			if (CategoryAxis == null || ValueAxis == null) return;
			if (ItemState.Count == 0) return;
			_trace.Verbose($"{Name} transforms a:{icrc.Area} rx:{CategoryAxis.Range} ry:{ValueAxis.Range}");
			var matx = MatrixSupport.TransformFor(icrc.Area, CategoryAxis, ValueAxis);
			foreach (var state in ItemState) {
				var dcc = matx.Transform(new Point(state.XValue, state.YValue));
				// get half-dimensions of the TextBlock
				// IST elements must have had measure-pass before we get to here!
				var hw = state.Element.ActualWidth / 2;
				var hh = state.Element.ActualHeight / 2;
				state.Element.SetValue(Canvas.LeftProperty, dcc.X - hw + state.Element.ActualWidth*LabelOffset.X);
				state.Element.SetValue(Canvas.TopProperty, dcc.Y - hh + state.Element.ActualHeight*LabelOffset.Y);
				if (ClipToDataRegion) {
					// TODO this does not work "correctly" the TB gets clipped no matter what
					// this is because the clip coordinate system is for "inside" the text block (gotta verify this)
					// must find intersection of the TB bounds and the icrc.SeriesArea, and make that the clip.
					//state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
				_trace.Verbose($"{Name} matx:{matx} pt:({state.XValue},{state.YValue}) dcc:{dcc} tbsz:{state.Element.ActualWidth},{state.Element.ActualHeight}");
			}
		}
		#endregion
		#region IDataSourceRenderer
		class State :RenderState_ValueAndLabel<SeriesItemState, TextBlock> { }
		/// <summary>
		/// Element factory for recycler.
		/// </summary>
		/// <returns></returns>
		TextBlock CreateElement() {
			var tb = new TextBlock();
			if (LabelStyle != null) {
				tb.Style = LabelStyle;
			}
			return tb;
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (ValueAxis == null || CategoryAxis == null) return null;
			if (String.IsNullOrEmpty(ValuePath)) return null;
			var by = new BindingEvaluator(ValuePath);
			// TODO report the binding error
			if (by == null) return null;
			ResetLimits();
			var elements = ItemState.Select(ms => ms.Element);
			var recycler = new Recycler<TextBlock>(elements, CreateElement);
			return new State() {
				bx = !String.IsNullOrEmpty(CategoryPath) ? new BindingEvaluator(CategoryPath) : null,
				by = by,
				itemstate = new List<SeriesItemState>(),
				recycler = recycler,
				elements = recycler.Items().GetEnumerator()
			};
		}
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			var valuey = CoerceValue(item, st.by);
			var valuex = st.bx != null ? (double)st.bx.For(item) : index;
			valuex += CategoryAxisOffset;
			st.ix = index;
			UpdateLimits(valuex, valuey);
			// short-circuit if it's NaN
			if (double.IsNaN(valuey)) {
				return;
			}
			var mappedy = ValueAxis.For(valuey);
			var mappedx = CategoryAxis.For(valuex);
			// finish up
			var tb = st.NextElement();
			if (tb == null) return;
			tb.Text = valuey.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
			var sis = new SeriesItemState() { Element = tb, YValue = mappedy, XValue = mappedx, Index = index };
			st.itemstate.Add(sis);
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
			var sz = new Size(1024, 1024);
			foreach(var xx in st.recycler.Created) {
				if (xx.DesiredSize.Width == 0 || xx.DesiredSize.Height == 0) {
					// force it to measure; needed for Transforms
					xx.Measure(sz);
				}
			}
			Dirty = false;
		}
		#endregion
	}
}
