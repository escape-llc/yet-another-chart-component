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
	#region IValueAxisLabelSelectorContext
	/// <summary>
	/// Context passed to the <see cref="IValueConverter"/> for value axis <see cref="Style"/> selection etc.
	/// </summary>
	public interface IValueAxisLabelSelectorContext : IAxisLabelSelectorContext {
		/// <summary>
		/// List of all tick values, in order of layout.
		/// MAY NOT be in sorted order!
		/// </summary>
		double[] AllTicks { get; }
		/// <summary>
		/// The computed tick interval.
		/// </summary>
		double TickInterval { get; }
		/// <summary>
		/// List of previously-generated ticks, in order of layout.
		/// MAY NOT be in sorted order!
		/// </summary>
		List<double> GeneratedTicks { get; }
	}
	#endregion
	#region ValueAxis
	/// <summary>
	/// Value axis is a "vertical" axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static readonly LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Error);
		#region ItemState
		/// <summary>
		/// The item state for this component.
		/// </summary>
		protected class ItemState {
			internal TextBlock tb;
			internal double value;
			internal void SetLocation(double left, double top) {
				tb.SetValue(Canvas.LeftProperty, left);
				tb.SetValue(Canvas.TopProperty, top);
			}
		}
		#endregion
		#region SelectorContext
		/// <summary>
		/// Internal context for selector.
		/// </summary>
		protected class SelectorContext : IValueAxisLabelSelectorContext {
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Index"/>.
			/// </summary>
			public int Index { get; private set; }
			/// <summary>
			/// <see cref="IValueAxisLabelSelectorContext.AllTicks"/>.
			/// </summary>
			public double[] AllTicks { get; private set; }
			/// <summary>
			/// <see cref="IValueAxisLabelSelectorContext.TickInterval"/>.
			/// </summary>
			public double TickInterval { get; private set; }
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Axis"/>.
			/// </summary>
			public IChartAxis Axis { get; private set; }
			/// <summary>
			/// <see cref="IAxisLabelSelectorContext.Area"/>.
			/// </summary>
			public Rect Area { get; private set; }
			/// <summary>
			/// <see cref="IValueAxisLabelSelectorContext.GeneratedTicks"/>.
			/// </summary>
			public List<double> GeneratedTicks { get; private set; }
			/// <summary>
			/// Ctor.
			/// </summary>
			/// <param name="ica"></param>
			/// <param name="rc"></param>
			/// <param name="ticks"></param>
			/// <param name="ti"></param>
			public SelectorContext(IChartAxis ica, Rect rc, double[] ticks, double ti) { Axis = ica; Area = rc; AllTicks = ticks; TickInterval = ti; GeneratedTicks = new List<double>(); }
			/// <summary>
			/// Set the current index.
			/// </summary>
			/// <param name="idx"></param>
			public void SetTick(int idx) { Index = idx; }
			/// <summary>
			/// Add to the list of generated ticks.
			/// </summary>
			/// <param name="dx"></param>
			public void Generated(double dx) { GeneratedTicks.Add(dx); }
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
		/// Path for the axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Geometry for the axis bar.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
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
		/// Creates Value/Left/Vertical axis.
		/// </summary>
		public ValueAxis() : base(AxisType.Value, AxisOrientation.Vertical, Side.Left) {
			CommonInit();
		}
		#endregion
		#region helpers
		private void CommonInit() {
			TickLabels = new List<ItemState>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinWidth = 32;
		}
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, nameof(PathStyle), Axis, FrameworkElement.StyleProperty);
		}
		void DoTickLabels(IChartRenderContext icrc) {
			var tc = new TickCalculator(Minimum, Maximum);
			_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			// TODO may want to include the LabelStyle's padding if defined
			var padding = AxisLineThickness + 2 * AxisMargin;
			var tbr = new Recycler<TextBlock, ItemState>(TickLabels.Select(tl => tl.tb), (ist) => {
				var tb = Theme.TextBlockTemplate.LoadContent() as TextBlock;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), tb, FrameworkElement.StyleProperty);
				} else {
					// already reported this, but need to do something
					tb.FontSize = 10;
					tb.Foreground = Axis.Fill;
					tb.VerticalAlignment = VerticalAlignment.Center;
					tb.HorizontalAlignment = Side == Side.Right ? HorizontalAlignment.Left : HorizontalAlignment.Right;
					tb.TextAlignment = Side == Side.Right ? TextAlignment.Left : TextAlignment.Right;
				}
				tb.Width = icrc.Area.Width - padding;
				tb.Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0);
				return tb;
			});
			var itemstate = new List<ItemState>();
			// materialize the ticks
			var lx = tc.GetTicks().ToArray();
			var sc = new SelectorContext(this, icrc.Area, lx, tc.TickInterval);
			for (int ix = 0; ix < lx.Length; ix++) {
				//_trace.Verbose($"grid vx:{tick}");
				sc.SetTick(ix);
				var createit = true;
				if (LabelSelector != null) {
					// ask the label selector
					var ox = LabelSelector.Convert(sc, typeof(bool), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
					if (ox is bool bx) {
						createit = bx;
					} else {
						createit = ox != null;
					}
				}
				if (!createit) continue;
				var current = tbr.Next(null);
				var tick = lx[ix];
				if (!current.Item1) {
					// restore binding if we are using a LabelFormatter
					if (LabelFormatter != null && LabelStyle != null) {
						BindTo(this, nameof(LabelStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				// default text
				var text = tick.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
				if (LabelFormatter != null) {
					// call for Style, String override
					var format = LabelFormatter.Convert(sc, typeof(Tuple<Style, String>), null, System.Globalization.CultureInfo.CurrentUICulture.Name);
					if (format is Tuple<Style, String> ovx) {
						if (ovx.Item1 != null) {
							current.Item2.Style = ovx.Item1;
						}
						if (ovx.Item2 != null) {
							text = ovx.Item2;
						}
					}
				}
				var state = new ItemState() { tb = current.Item2, value = tick };
				state.tb.Text = text;
				state.SetLocation(icrc.Area.Left, tick);
				sc.Generated(tick);
				itemstate.Add(state);
			}
			// VT and internal bookkeeping
			TickLabels = itemstate;
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			foreach (var xx in TickLabels) {
				// force it to measure; needed for Transforms
				xx.tb.Measure(icrc.Dimensions);
			}
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			ApplyLabelStyle(icelc as IChartErrorInfo);
			AssignFromRef(icelc as IChartErrorInfo, NameOrType(), nameof(PathStyle), nameof(Theme.PathAxisValue),
				PathStyle == null, Theme != null, Theme.PathAxisValue != null,
				() => PathStyle = Theme.PathAxisValue
			);
			if (Theme?.TextBlockTemplate == null) {
				if (icelc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"No {nameof(Theme.TextBlockTemplate)} was not found", new[] { nameof(Theme.TextBlockTemplate) }));
				}
			}
			DoBindings(icelc);
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
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
			var space = AxisMargin + AxisLineThickness + MinWidth;
			iclc.ClaimSpace(this, Side, space);
		}
		#endregion
		#region IRequireRender
		/// <summary>
		/// Layout axis components (bar, grid, labels).
		/// Each component has a corresponding transform (applied in Transforms()).  Right and Left are DUALs of each other wrt to horizontal axis.
		/// Axis "bar" and Tick marks:
		///		x: PX (scale 1)
		///		y: "axis" scale
		/// Tick labels:
		///		x, y: PX
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (Theme?.TextBlockTemplate == null) {
				// already reported an error so this should be no surprise
				return;
			}
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			// axis and tick marks
			AxisGeometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Side == Side.Right ? 0 : icrc.Area.Width, Minimum, Side == Side.Right ? AxisLineThickness : icrc.Area.Width - AxisLineThickness, Maximum);
			AxisGeometry.Figures.Add(pf);
			if(!double.IsNaN(Minimum) && !double.IsNaN(Maximum)) {
				// recycle and layout
				DoTickLabels(icrc);
			}
			Dirty = false;
		}
		#endregion
		#region IRequireTransforms
		/// <summary>
		/// X-coordinates	"px"
		/// Y-coordinates	[0..1]
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scaley = icrc.Area.Height / Range;
			var matx = new Matrix(1, 0, 0, -scaley, icrc.Area.Left + AxisMargin * (Side == Side.Right ? 1 : -1), icrc.Area.Top + Maximum * scaley);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sy:{scaley:F3} matx:{matx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			foreach (var state in TickLabels) {
				var adj = state.tb.ActualHeight / 2;
				var top = icrc.Area.Bottom - (state.value - Minimum) * scaley - adj;
				state.SetLocation(icrc.Area.Left, top);
			}
		}
		#endregion
	}
	#endregion
}
