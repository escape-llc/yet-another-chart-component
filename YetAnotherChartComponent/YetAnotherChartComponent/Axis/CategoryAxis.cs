using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ICategoryAxisLabelSelectorContext
	public class CategoryLabelState {
		public int Index { get; private set; }
		public double Value { get; private set; }
		public object Label { get; private set; }
		public CategoryLabelState(int idx, double vx, object lx) { Index = idx; Value = vx; Label = lx; }
	}
	/// <summary>
	/// Context passed to the <see cref="IValueConverter"/> for category axis <see cref="Style"/> selection etc.
	/// </summary>
	public interface ICategoryAxisLabelSelectorContext : IAxisLabelSelectorContext {
		/// <summary>
		/// The list of all labels and their indices.
		/// </summary>
		List<CategoryLabelState> AllLabels { get; }
		/// <summary>
		/// The list of previously-generated labels up to this point.
		/// </summary>
		List<CategoryLabelState> GeneratedLabels { get; }
	}
	#endregion
	#region CategoryAxis
	/// <summary>
	/// Horizontal Category axis.
	/// Category axis cells start on the left and extend rightward (in device X-units).
	/// The discrete category axis is a simple "positional-index" axis [0..N-1].  Each index defines a "cell" that allows "normalized" positioning [0..1) within the cell.
	/// Certain series types MAY extend the discrete axis by ONE cell, to draw the "last" elements there.
	/// </summary>
	public class CategoryAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static readonly LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Error);
		#region ItemState
		/// <summary>
		/// The item state for this component.
		/// </summary>
		protected class ItemState {
			internal TextBlock tb;
			internal String label;
			internal double value;
			// these are used for JIT re-positioning
			internal double scalex;
			internal bool usexau;
			internal double yy;
			internal double aleft;
			/// <summary>
			/// Configure <see cref="Canvas"/> attached properties.
			/// </summary>
			/// <param name="loc"></param>
			internal void Locate(Point loc) {
				if(usexau) {
					tb.Width = scalex;
				}
				tb.SetValue(Canvas.LeftProperty, loc.X);
				tb.SetValue(Canvas.TopProperty, loc.Y);
			}
			/// <summary>
			/// Calculate position based on <see cref="FrameworkElement.ActualWidth"/> or XAU as appropriate.
			/// </summary>
			/// <returns></returns>
			internal Point GetLocation() {
				if (usexau) {
					// place it at XAU zero point
					return new Point(aleft + value * scalex, yy);
				} else {
					// place it centered in cell
					return new Point(aleft + value * scalex + scalex / 2 - tb.ActualWidth / 2, yy);
				}
			}
		}
		#endregion
		#region SelectorContext
		/// <summary>
		/// Internal context for selector.
		/// </summary>
		protected class SelectorContext : ICategoryAxisLabelSelectorContext {
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Index"/>.
			/// </summary>
			public int Index { get; private set; }
			/// <summary>
			/// <see cref="ICategoryAxisLabelSelectorContext.AllLabels"/>.
			/// </summary>
			public List<CategoryLabelState> AllLabels { get; private set; }
			/// <summary>
			/// <see cref="ICategoryAxisLabelSelectorContext.GeneratedLabels"/>.
			/// </summary>
			public List<CategoryLabelState> GeneratedLabels { get; private set; }
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Axis"/>.
			/// </summary>
			public IChartAxis Axis { get; private set; }
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Area"/>.
			/// </summary>
			public Rect Area { get; private set; }
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="ica"></param>
			/// <param name="rc"></param>
			/// <param name="labels"></param>
			public SelectorContext(IChartAxis ica, Rect rc, List<CategoryLabelState> labels) { Axis = ica; Area = rc; AllLabels = labels; GeneratedLabels = new List<CategoryLabelState>(); }
			/// <summary>
			/// Set the current index and value.
			/// </summary>
			/// <param name="idx">Current index.</param>
			public void SetTick(int idx) { Index = idx; }
			public void Generated(CategoryLabelState cls) { GeneratedLabels.Add(cls); }
		}
		#endregion
		#region properties
		/// <summary>
		/// Converter to use as the element <see cref="FrameworkElement.Style"/> and <see cref="TextBlock.Text"/> selector.
		/// These are already set to their "standard" values before this is called, so it MAY selectively opt out of setting them.
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is used to determine which value is requested.
		/// Uses <see cref="String"/> for label override.  Return a new label or NULL to opt out.
		/// Uses <see cref="Style"/> for style override.  Return a style or NULL to opt out.
		/// </summary>
		public IValueConverter LabelFormatter { get; set; }
		/// <summary>
		/// Converter to use as the label creation selector.
		/// If it returns True, the label is created.
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is <see cref="bool"/>.
		/// SHOULD return a <see cref="bool"/> but MAY return NULL/not-NULL.
		/// </summary>
		public IValueConverter LabelSelector { get; set; }
		/// <summary>
		/// Path for axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Axis bar geometry.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
		/// <summary>
		/// Manage labels.
		/// </summary>
		protected Dictionary<int, Tuple<double, object>> LabelMap { get; set; } = new Dictionary<int, Tuple<double, object>>();
		/// <summary>
		/// List of active TextBlocks for labels.
		/// </summary>
		protected List<ItemState> TickLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal, Side.Bottom) {
			CommonInit();
		}
		private void CommonInit() {
			TickLabels = new List<ItemState>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinHeight = 24;
		}
		#endregion
		#region helpers
		/// <summary>
		/// Just-in-time re-position of label element at exactly the right spot after it's done with (asynchronous) measure/arrange.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Element_SizeChanged(object sender, SizeChangedEventArgs e) {
#if false
			var vm = fe.DataContext as DataTemplateShim;
			_trace.Verbose($"{Name} sizeChanged ps:{e.PreviousSize} ns:{e.NewSize} text:{vm?.Text}");
#endif
			var fe = sender as FrameworkElement;
			var state = TickLabels.SingleOrDefault((sis) => sis.tb == fe);
			if (state != null) {
				var loc = state.GetLocation();
				_trace.Verbose($"{Name} sizeChanged loc:{loc} yv:{state.value} ns:{e.NewSize}");
				state.Locate(loc);
			}
		}
		#endregion
		#region extensions
		/// <summary>
		/// Clear the label map in addition to default impl.
		/// </summary>
		public override void ResetLimits() {
			LabelMap.Clear();
			base.ResetLimits();
		}
		/// <summary>
		/// Labels are cached for presentation.
		/// </summary>
		/// <param name="valueWithLabel"></param>
		/// <returns>base.For(double)</returns>
		public override double For(Tuple<double, object> valueWithLabel) {
			var mv = base.For(valueWithLabel.Item1);
			int key = (int)mv;
			if (LabelMap.ContainsKey(key)) {
				// should be an error but just overwrite it
				LabelMap[key] = valueWithLabel;
			} else {
				LabelMap.Add(key, valueWithLabel);
			}
			return mv;
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			ApplyLabelStyle(icelc as IChartErrorInfo);
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathAxisCategory),
				PathStyle == null, Theme != null, Theme.PathAxisCategory != null,
				() => PathStyle = Theme.PathAxisCategory
			);
			if (Theme?.TextBlockTemplate == null) {
				if (icelc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"No {nameof(Theme.TextBlockTemplate)} was not found", new[] { nameof(Theme.TextBlockTemplate) }));
				}
			}
			BindTo(this, nameof(PathStyle), Axis, FrameworkElement.StyleProperty);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		#endregion
		#region IRequireLayout
		/// <summary>
		/// Claim the space indicated by properties.
		/// </summary>
		/// <param name="iclc"></param>
		void IRequireLayout.Layout(IChartLayoutContext iclc) {
			var space = AxisMargin + AxisLineThickness + MinHeight;
			iclc.ClaimSpace(this, Side, space);
		}
		#endregion
		#region IRequireRender
		/// <summary>
		/// Compute axis visual elements.
		/// This is an AXIS, so the extents are already finalized and safe to use here.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (Theme?.TextBlockTemplate == null) {
				// already reported an error so this should be no surprise
				return;
			}
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			AxisGeometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness);
			AxisGeometry.Figures.Add(pf);
			var i1 = (int)Minimum;
			var i2 = (int)Maximum;
			var scalex = icrc.Area.Width / Range;
			// recycle and lay out tick labels
			// see if style wants to override width
			var widx = LabelStyle?.Find(FrameworkElement.WidthProperty);
			var recycler = new Recycler2<TextBlock, CategoryLabelState>(TickLabels.Select(tl => tl.tb), (tpx) => {
				var tb = Theme.TextBlockTemplate.LoadContent() as TextBlock;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), tb, FrameworkElement.StyleProperty);
					if (widx == null) {
						tb.Width = scalex;
					}
				} else {
					// already reported this, but need to do something
					tb.FontSize = 10;
					tb.Foreground = Axis.Fill;
					tb.VerticalAlignment = VerticalAlignment.Center;
					tb.HorizontalAlignment = HorizontalAlignment.Center;
					tb.TextAlignment = TextAlignment.Center;
					tb.Width = scalex;
				}
				tb.SizeChanged += Element_SizeChanged;
				return tb;
			});
			var itemstate = new List<ItemState>();
			// materialize the labels into a list
			var labels = new List<CategoryLabelState>();
			for(var ix = i1; ix <= i2; ix++) {
				if(LabelMap.ContainsKey(ix)) {
					labels.Add(new CategoryLabelState(ix, LabelMap[ix].Item1, LabelMap[ix].Item2));
				} else {
					labels.Add(new CategoryLabelState(ix, double.NaN, null));
				}
			}
			var sc = new SelectorContext(this, icrc.SeriesArea, labels);
			for(var ix = 0; ix < labels.Count; ix++) {
				var cls = labels[ix];
				if (cls.Label == null) continue;
				// create a label for this entry
				_trace.Verbose($"key {ix} label {cls.Label}");
				sc.SetTick(ix);
				var createit = true;
				if (LabelSelector != null) {
					var ox = LabelSelector.Convert(sc, typeof(bool), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
					if (ox is bool bx) {
						createit = bx;
					} else {
						createit = ox != null;
					}
				}
				if (!createit) continue;
				var current = recycler.Next(cls);
				if (current == null) break;
				if (!current.Item1) {
					// recycled: restore binding if we are using a LabelFormatter
					if (LabelFormatter != null && LabelStyle != null) {
						BindTo(this, nameof(LabelStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				// default text
				var text = cls.Label?.ToString();
				if (LabelFormatter != null) {
					// call for Style, String override
					var format = LabelFormatter.Convert(sc, typeof(Tuple<Style, String>), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
					if(format is Tuple<Style, String> ovx) {
						if (ovx.Item1 != null) {
							current.Item2.Style = ovx.Item1;
						}
						if (ovx.Item2 != null) {
							text = ovx.Item2;
						}
					}
				}
				var state = new ItemState() {
					tb = current.Item2,
					value = cls.Value,
					label = text,
					usexau = widx == null
				};
				state.tb.Text = text;
				sc.Generated(cls);
				itemstate.Add(state);
			}
			// VT and internal bookkeeping
			TickLabels = itemstate;
			Layer.Remove(recycler.Unused);
			Layer.Add(recycler.Created);
			Dirty = false;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// X-coordinates	axis
		/// Y-coordinates	"px"
		/// Grid-coordinates (x:axis, y:[0..1])
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scalex = icrc.Area.Width / Range;
			var matx = new Matrix(scalex, 0, 0, 1, icrc.Area.Left, icrc.Area.Top + AxisMargin);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sx:{scalex:F3} matx:{matx} a:{icrc.Area}");
			foreach (var state in TickLabels) {
				state.scalex = scalex;
				state.yy = icrc.Area.Top + AxisLineThickness + 2 * AxisMargin;
				state.aleft = icrc.Area.Left;
				var loc = state.GetLocation();
				_trace.Verbose($"tb {state.tb.ActualWidth}x{state.tb.ActualHeight} v:{state.value} @:({loc.X},{loc.Y})");
				state.Locate(loc);
				if (!icrc.IsTransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					state.tb.InvalidateMeasure();
					state.tb.InvalidateArrange();
				}
			}
		}
		#endregion
	}
	#endregion
}
