using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Decoration that creates series value labels.
	/// </summary>
	public class SeriesValueLabels : ChartComponent, IRequireChartTheme, IRequireEnterLeave, IRequireRender, IRequireTransforms {
		static LogTools.Flag _trace = LogTools.Add("SeriesValueLabels", LogTools.Level.Error);
		#region SeriesItemState
		/// <summary>
		/// Shorthand for item state.
		/// </summary>
		protected class SeriesItemState : ItemState<TextBlock> {
			internal Point Direction { get; set; }
			internal SeriesItemState(int idx, double xv, double yv, TextBlock ele, int ch) : base(idx, xv, yv, ele, ch) { }
		}
		#endregion
		#region properties
		/// <summary>
		/// Holder for IRequireChartTheme interface.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The name of the series in the Components collection.
		/// The axes are obtained from this series.
		/// </summary>
		public String SeriesName { get { return (String)GetValue(SeriesNameProperty); } set { SetValue(SeriesNameProperty, value); } }
		/// <summary>
		/// The value channel to display values for.
		/// </summary>
		public int ValueChannel { get { return (int)GetValue(ValueChannelProperty); } set { SetValue(ValueChannelProperty, value); } }
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
		/// Units are PX coordinates, in Half-dimension based on TextBlock size.
		/// Y-up is negative.
		/// Default value is (0,0).
		/// </summary>
		public Point LabelOffset { get; set; } = new Point(0, 0);
		/// <summary>
		/// Placment offset is translation from "center" of a region.
		/// Units are WORLD coordinates.
		/// Y-up is positive.
		/// Default value is (0,0).
		/// </summary>
		public Point PlacementOffset { get; set; } = new Point(0, 0);
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		/// <summary>
		/// Dereferenced data series to interrogate for values.
		/// </summary>
		protected DataSeries Source { get; set; }
		/// <summary>
		/// The layer for components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		/// <summary>
		/// Current item state.
		/// </summary>
		protected List<SeriesItemState> ItemState { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValueChannel"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueChannelProperty = DependencyProperty.Register(
			nameof(ValueChannel), typeof(int), typeof(SeriesValueLabels), new PropertyMetadata(0, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="SeriesName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty SeriesNameProperty = DependencyProperty.Register(
			nameof(SeriesName), typeof(string), typeof(SeriesValueLabels), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(
			nameof(LabelStyle), typeof(Style), typeof(SeriesValueLabels), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		public SeriesValueLabels() {
			ItemState = new List<SeriesItemState>();
		}
		#endregion
		#region helpers
		/// <summary>
		/// Resolve series and axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureComponents(IChartComponentContext icrc) {
			if(Source == null && !String.IsNullOrEmpty(SeriesName)) {
				Source = icrc.Find(SeriesName) as DataSeries;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Series '{SeriesName}' was not found", new[] { nameof(Source), nameof(SeriesName) }));
				}
			}
			if (Source == null) {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Series lookup failed; no other components can resolve", new[] { nameof(Source), nameof(SeriesName) }));
				}
				return;
			}
			else {
				if(!(Source is IProvideSeriesItemValues)) {
					if (icrc is IChartErrorInfo icei) {
						icei.Report(new ChartValidationResult(Source.NameOrType(), $"Series does not provide values; no labels will generate", new[] { nameof(Source), nameof(SeriesName) }));
					}
					return;
				}
			}
			if (ValueAxis == null && !String.IsNullOrEmpty(Source.ValueAxisName)) {
				ValueAxis = icrc.Find(Source.ValueAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(Source.NameOrType(), $"Value axis '{Source.ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(Source.ValueAxisName) }));
				}
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(Source.CategoryAxisName)) {
				CategoryAxis = icrc.Find(Source.CategoryAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(Source.NameOrType(), $"Category axis '{Source.CategoryAxisName}' was not found", new[] { nameof(CategoryAxis), nameof(Source.CategoryAxisName) }));
				}
			}
		}
		/// <summary>
		/// Element factory for recycler.
		/// TODO should come from a <see cref="DataTemplate"/>.
		/// </summary>
		/// <returns></returns>
		TextBlock CreateElement() {
			var tb = new TextBlock();
			if (LabelStyle != null) {
				tb.Style = LabelStyle;
			}
			return tb;
		}
		/// <summary>
		/// Advance the recycler's iterator.
		/// </summary>
		/// <param name="elements"></param>
		/// <returns></returns>
		TextBlock NextElement(IEnumerator<TextBlock> elements) {
			if (elements.MoveNext()) return elements.Current;
			else return null;
		}
		#endregion
		#region IRequireEnterLeave
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			EnsureComponents(icelc as IChartComponentContext);
			Layer = icelc.CreateLayer();
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(LabelStyle), nameof(Theme.LabelAxisTop),
				LabelStyle == null, Theme != null, Theme.LabelAxisTop != null,
				() => LabelStyle = Theme.LabelAxisTop
			);
			_trace.Verbose($"{Name} enter s:{SeriesName} {Source} v:{ValueAxis} c:{CategoryAxis}");
		}
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			_trace.Verbose($"{Name} leave");
			ValueAxis = null;
			CategoryAxis = null;
			Source = null;
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireRender
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (Source == null || ValueAxis == null || CategoryAxis == null) return;
			if(Source is IProvideSeriesItemValues ipsiv) {
				// preamble
				var elements = ItemState.Select(ms => ms.Element);
				var recycler = new Recycler<TextBlock>(elements, CreateElement);
				var itemstate = new List<SeriesItemState>();
				var elenum = recycler.Items().GetEnumerator();
				// render
				foreach (var siv in ipsiv.SeriesItemValues) {
					ISeriesItemValue target = null;
					if (siv is ISeriesItemValue isiv) {
						if (isiv.Channel == ValueChannel) {
							target = isiv;
						}
					}
					else if(siv is ISeriesItemValues isivs) {
						target = isivs.YValues.SingleOrDefault((yv) => yv.Channel == ValueChannel);
					}
					if(target != null && !double.IsNaN(target.YValue)) {
						var tb = NextElement(elenum);
						if (tb == null) continue;
						tb.Text = target.YValue.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
						var mappedx = CategoryAxis.For(siv.Index);
						var pmt = (target as IProvidePlacement)?.Placement;
						// provide customized placement according to info
						switch(pmt) {
						case RectanglePlacement rp:
							// use the PlacementOffset
							var pt = rp.Transform(PlacementOffset);
							_trace.Verbose($"rp c:{rp.Center} d:{rp.Direction} hd:{rp.HalfDimension} pt:{pt}");
							var sis = new SeriesItemState(siv.Index, mappedx + CategoryAxisOffset, pt.Y, tb, target.Channel) { Direction = rp.Direction };
							itemstate.Add(sis);
							break;
						default:
							var sis2 = new SeriesItemState(siv.Index, mappedx + CategoryAxisOffset, target.YValue, tb, target.Channel) { Direction = Placement.UP_RIGHT };
							itemstate.Add(sis2);
							break;
						}
					}
				}
				// postamble
				ItemState = itemstate;
				Layer.Remove(recycler.Unused);
				Layer.Add(recycler.Created);
				foreach (var xx in ItemState) {
					// force everything to measure; needed for Transforms
					xx.Element.Measure(icrc.Dimensions);
				}
				Dirty = false;
			}
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
				state.Element.SetValue(Canvas.LeftProperty, dcc.X - hw + hw*LabelOffset.X*state.Direction.X);
				state.Element.SetValue(Canvas.TopProperty, dcc.Y - hh + hh*LabelOffset.Y*state.Direction.Y);
#if false
				if (ClipToDataRegion) {
					// TODO this does not work "correctly" the TB gets clipped no matter what
					// this is because the clip coordinate system is for "inside" the text block (gotta verify this)
					// must find intersection of the TB bounds and the icrc.SeriesArea, and make that the clip.
					//state.Element.Clip = new RectangleGeometry() { Rect = icrc.SeriesArea };
				}
#endif
				_trace.Verbose($"{Name} matx:{matx} pt:({state.XValue},{state.YValue}) dcc:{dcc} tbsz:{state.Element.ActualWidth},{state.Element.ActualHeight}");
			}
		}
		#endregion
	}
}
