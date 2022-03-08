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
		TickState[] AllTicks { get; }
		/// <summary>
		/// The computed tick interval.
		/// </summary>
		double TickInterval { get; }
		/// <summary>
		/// List of previously-generated ticks, in order of layout.
		/// MAY NOT be in sorted order!
		/// </summary>
		List<TickState> GeneratedTicks { get; }
	}
	#endregion
	#region ValueAxisSelectorContext
	/// <summary>
	/// Context for value axis selectors.
	/// </summary>
	public class ValueAxisSelectorContext : IValueAxisLabelSelectorContext {
		/// <summary>
		/// <see cref="IAxisLabelSelectorContext.Index"/>.
		/// </summary>
		public int Index { get; private set; }
		/// <summary>
		/// <see cref="IValueAxisLabelSelectorContext.AllTicks"/>.
		/// </summary>
		public TickState[] AllTicks { get; private set; }
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
		public List<TickState> GeneratedTicks { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="ica"></param>
		/// <param name="rc"></param>
		/// <param name="ticks"></param>
		/// <param name="ti"></param>
		public ValueAxisSelectorContext(IChartAxis ica, Rect rc, TickState[] ticks, double ti) { Axis = ica; Area = rc; AllTicks = ticks; TickInterval = ti; GeneratedTicks = new List<TickState>(); }
		/// <summary>
		/// Set the current index.
		/// </summary>
		/// <param name="idx"></param>
		public void SetTick(int idx) { Index = idx; }
		/// <summary>
		/// Add to the list of generated ticks.
		/// </summary>
		/// <param name="dx"></param>
		public void Generated(TickState dx) { GeneratedTicks.Add(dx); }
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
		protected abstract class ItemState {
			internal FrameworkElement element;
			internal TickState tick;
			// these are used for JIT re-positioning
			internal double dim;
			internal double yorigin;
			internal double xorigin;
			#region extension points
			/// <summary>
			/// Size the element according to orientation.
			/// </summary>
			/// <param name="element">non-NULL.</param>
			protected abstract void SizeElement(FrameworkElement element);
			/// <summary>
			/// Calculate position in XAML coordinates based on <see cref="FrameworkElement.ActualWidth"/> or <see cref="FrameworkElement.ActualHeight"/> as appropriate.
			/// </summary>
			/// <returns></returns>
			protected abstract Point GetLocation(FrameworkElement element);
			#endregion
			/// <summary>
			/// Call <see cref="GetLocation"/> and configure <see cref="element"/>.
			/// </summary>
			/// <returns>The new location OR NULL.</returns>
			internal Point? UpdateLocation() {
				if (element != null) {
					SizeElement(element);
					var loc = GetLocation(element);
					element.SetValue(Canvas.LeftProperty, loc.X);
					element.SetValue(Canvas.TopProperty, loc.Y);
					return loc;
				}
				return null;
			}
		}
		/// <summary>
		/// Vertical value label layout.
		/// </summary>
		protected class ItemState_Vertical : ItemState {
			internal ItemState_Vertical(FrameworkElement element, TickState ts) { this.element = element; this.tick = ts; }
			/// <summary>
			/// <inheritdoc/>
			/// </summary>
			/// <returns></returns>
			protected override Point GetLocation(FrameworkElement element) {
				return new Point(xorigin, yorigin - element.ActualHeight / 2);
			}
			/// <summary>
			/// <inheritdoc/>
			/// </summary>
			/// <param name="element"></param>
			protected override void SizeElement(FrameworkElement element) {
				element.Width = dim;
			}
		}
		/// <summary>
		/// Horizontal value label layout.
		/// </summary>
		protected class ItemState_Horizontal : ItemState {
			internal ItemState_Horizontal(FrameworkElement element, TickState ts) { this.element = element; this.tick = ts; }
			/// <summary>
			/// <inheritdoc/>
			/// </summary>
			/// <returns></returns>
			protected override Point GetLocation(FrameworkElement element) {
				return new Point(xorigin - element.ActualWidth / 2, yorigin);
			}
			/// <summary>
			/// <inheritdoc/>
			/// </summary>
			/// <param name="element"></param>
			protected override void SizeElement(FrameworkElement element) {
				element.Height = dim;
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Converter to use as the element <see cref="FrameworkElement.Style"/> and <see cref="TextBlock.Text"/> selector.
		/// These are already set to their "standard" values before this is called, so it MAY selectively opt out of setting them.
		/// <para/>
		/// The <see cref="IValueConverter.Convert"/> targetType parameter is used to determine which value is requested.
		/// <para/>
		/// Uses <see cref="Tuple{Style,String}"/> for style/label override.  Return a new instance/NULL to opt in/out.
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
		/// If set, the template to use for labels.
		/// This overrides <see cref="AxisCommon.LabelStyle"/>.
		/// If this is not set, then <see cref="TextBlock"/>s are used and <see cref="AxisCommon.LabelStyle"/> applied to them.
		/// </summary>
		public DataTemplate LabelTemplate { get { return (DataTemplate)GetValue(LabelTemplateProperty); } set { SetValue(LabelTemplateProperty, value); } }
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
		protected List<ItemState> AxisLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="LabelTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.Register(
			nameof(LabelTemplate), typeof(DataTemplate), typeof(ValueAxis), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// Creates Value/Left/Vertical axis.
		/// </summary>
		public ValueAxis() : base(AxisType.Value, Side.Left) {
			CommonInit();
		}
		#endregion
		#region helpers
		private void CommonInit() {
			AxisLabels = new List<ItemState>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinWidth = 32;
		}
		void DoBindings(IChartEnterLeaveContext _1) {
			BindTo(this, nameof(PathStyle), Axis, FrameworkElement.StyleProperty);
		}
		FrameworkElement CreateElement(ItemState _1) {
			var fe = default(FrameworkElement);
			if (LabelTemplate != null) {
				fe = LabelTemplate.LoadContent() as FrameworkElement;
			} else if (Theme.TextBlockTemplate != null) {
				fe = Theme.TextBlockTemplate.LoadContent() as FrameworkElement;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), fe, FrameworkElement.StyleProperty);
				}
			}
			if (fe != null) {
				fe.SizeChanged += Element_SizeChanged;
			}
			return fe;
		}
		void DoTickLabels(IChartRenderContext icrc) {
			var tc = new TickCalculator(Minimum, Maximum);
			_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			// TODO may want to include the LabelStyle's padding if defined
			var padding = AxisLineThickness + 2 * AxisMargin;
			var tbr = new Recycler<FrameworkElement, ItemState>(AxisLabels.Select(tl => tl.element), (ist) => {
				var fe = CreateElement(ist);
				if (Orientation == AxisOrientation.Vertical) {
					fe.Width = icrc.Area.Width - padding;
					if (fe is TextBlock tbb) {
						tbb.Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0);
					}
				}
				else {
					fe.Height = icrc.Area.Height - padding;
					if (fe is TextBlock tbb) {
						tbb.Padding = Side == Side.Bottom ? new Thickness(0, padding, 0, 0) : new Thickness(0, 0, 0, padding);
					}
				}
				return fe;
			});
			var itemstate = new List<ItemState>();
			// materialize the ticks
			var tix = tc.GetTicks().OrderBy(x => x.Index).ToArray();
			var sc = new ValueAxisSelectorContext(this, icrc.Area, tix, tc.TickInterval);
			for (int ix = 0; ix < tix.Length; ix++) {
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
				var tick = tix[ix];
				if (!current.Item1) {
					// restore binding if we are using a LabelFormatter
					if (LabelFormatter != null && LabelStyle != null) {
						BindTo(this, nameof(LabelStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				// default text
				var text = tick.Value.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
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
				var shim = new TextShim() { Text = text };
				current.Item2.DataContext = shim;
				BindTo(shim, nameof(Visibility), current.Item2, UIElement.VisibilityProperty);
				var state = (Orientation == AxisOrientation.Vertical
					? new ItemState_Vertical(current.Item2, tick) as ItemState
					: new ItemState_Horizontal(current.Item2, tick) as ItemState);
				sc.Generated(tick);
				itemstate.Add(state);
			}
			// VT and internal bookkeeping
			AxisLabels = itemstate;
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			// for "horizontal" axis orientation it's important to get the TextBlocks sized
			// otherwise certain Style settings can resize it beyond the text bounds
			// use unparented element for text measuring
			var inf = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
			foreach (var al in AxisLabels) {
				al.element.Measure(inf);
			}
		}
		/// <summary>
		/// Calculate the value axis projection.
		/// NOTE: scale/origin sign is reversed for "vertical" <see cref="Side.Left"/> and <see cref="Side.Right"/> so we get "cartesian" layout not DC.
		/// </summary>
		/// <param name="area">Axis rectangle.</param>
		/// <param name="scale">Value axis px/unit.</param>
		/// <returns></returns>
		Matrix ProjectionForAxis(Rect area, double scale) {
			switch (Side) {
				case Side.Bottom:
					return new Matrix(scale, 0, 0, 1, area.Left - Minimum * scale, area.Top + AxisMargin);
				case Side.Top:
					return new Matrix(scale, 0, 0, 1, area.Left - Minimum * scale, area.Bottom - AxisMargin);
				case Side.Left:
					return new Matrix(1, 0, 0, -scale, area.Right - AxisMargin, area.Bottom + Minimum * scale);
				case Side.Right:
					return new Matrix(1, 0, 0, -scale, area.Left + AxisMargin, area.Bottom + Minimum * scale);
				default:
					throw new NotImplementedException($"Not Implemented: {Side}");
			}
		}
		#endregion
		#region evhs
		/// <summary>
		/// Layout pass size changed.
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
			if (fe.ActualWidth == 0 || fe.ActualHeight == 0) return;
			var state = AxisLabels.SingleOrDefault(sis => sis.element == fe);
			if (state != null) {
				var loc = state.UpdateLocation();
				_trace.Verbose($"{Name} sizeChanged[{state.tick.Index}] loc:{loc} yv:{state.tick.Value} o:({state.xorigin},{state.yorigin}) ns:{e.NewSize} ds:{fe.DesiredSize}");
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
			if(Layer is IChartLayerAnimation icla) {
			}
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
			// axis and tick labels
			AxisGeometry.Figures.Clear();
			var pf = Orientation == AxisOrientation.Horizontal
				? PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness)
				: PathHelper.Rectangle(0, Minimum, AxisLineThickness, Maximum);
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
			var scale = Orientation == AxisOrientation.Horizontal ? icrc.Area.Width / Range : icrc.Area.Height / Range;
			var matx = ProjectionForAxis(icrc.Area, scale);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms s:{scale:F3} matx:{matx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			foreach (var state in AxisLabels) {
				if (state.element == null) continue;
				var pt = matx.Transform(Orientation == AxisOrientation.Horizontal ? new Point(state.tick.Value, 0) : new Point(0, state.tick.Value));
				switch(state) {
					case ItemState_Vertical isv:
						isv.dim = icrc.Area.Width - AxisMargin;
						isv.xorigin = icrc.Area.Left;
						isv.yorigin = pt.Y;
						break;
					case ItemState_Horizontal ish:
						ish.dim = icrc.Area.Height - AxisMargin;
						ish.xorigin = pt.X;
						ish.yorigin = icrc.Area.Top + AxisMargin;
						break;
				}
				var loc = state.UpdateLocation();
				_trace.Verbose($"{Name} el {state.element.ActualWidth}x{state.element.ActualHeight} v:{state.tick.Value} @:({loc?.X},{loc?.Y})");
				if (icrc.Type != RenderType.TransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					state.element.InvalidateMeasure();
					state.element.InvalidateArrange();
				}
			}
		}
		#endregion
	}
	#endregion
}
