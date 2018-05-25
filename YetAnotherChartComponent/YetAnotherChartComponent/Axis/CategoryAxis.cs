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
	#region CategoryLabelState
	/// <summary>
	/// Common state for category axis labels.
	/// </summary>
	public interface ICategoryLabelState {
		/// <summary>
		/// Index on category axis.
		/// </summary>
		int Index { get; }
		/// <summary>
		/// The value.
		/// SHOULD be <see cref="Double.NaN"/> if this index is a "hole".
		/// </summary>
		double Value { get; }
		/// <summary>
		/// The label "object".
		/// SHOULD be NULL if this index is a "hole".
		/// </summary>
		object Label { get; }
	}
	#endregion
	#region ICategoryAxisLabelSelectorContext
	/// <summary>
	/// Context passed to the <see cref="IValueConverter"/> for category axis <see cref="Style"/> selection etc.
	/// </summary>
	public interface ICategoryAxisLabelSelectorContext : IAxisLabelSelectorContext {
		/// <summary>
		/// The list of all potential label value data.
		/// </summary>
		IList<ICategoryLabelState> AllLabels { get; }
		/// <summary>
		/// The list of previously-generated labels up to this point.
		/// </summary>
		IList<ICategoryLabelState> GeneratedLabels { get; }
	}
	#endregion
	#region CategoryAxis
	/// <summary>
	/// Horizontal (Discrete) Category axis.
	/// Category axis cells start on the left and extend rightward (in device X-units).
	/// The discrete category axis is a simple "positional-index" axis [0..N-1].  Each index defines a "cell" that allows "normalized" positioning [0..1) within the cell.
	/// </summary>
	public class CategoryAxis : AxisCommon, IRequireLayout, IDataSourceRenderer, IRequireTransforms, IRequireEnterLeave {
		static readonly LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Error);
		#region ItemState
		/// <summary>
		/// The item state for this component.
		/// </summary>
		protected class ItemState : ICategoryLabelState {
			#region data
			// the index; SHOULD match position in array
			internal int index;
			// the value; same as index if discrete
			internal double value;
			// the label
			internal object label;
			// the display element
			internal TextBlock tb;
			// these are used for JIT re-positioning
			internal double scalex;
			internal bool usexau;
			internal double top;
			internal double left;
			#endregion
			#region ICategoryLabelState
			int ICategoryLabelState.Index => index;
			double ICategoryLabelState.Value => value;
			object ICategoryLabelState.Label => label;
			#endregion
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
					return new Point(left + value * scalex, top);
				} else {
					// place it centered in cell
					return new Point(left + value * scalex + scalex / 2 - tb.ActualWidth / 2, top);
				}
			}
			/// <summary>
			/// Combine <see cref="GetLocation"/> and <see cref="Locate"/>.
			/// </summary>
			/// <returns>The new location.</returns>
			internal Point UpdateLocation() {
				var loc = GetLocation();
				Locate(loc);
				return loc;
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
			public IList<ICategoryLabelState> AllLabels { get; private set; }
			/// <summary>
			/// <see cref="ICategoryAxisLabelSelectorContext.GeneratedLabels"/>.
			/// </summary>
			public IList<ICategoryLabelState> GeneratedLabels { get; private set; }
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
			public SelectorContext(IChartAxis ica, Rect rc, List<ICategoryLabelState> labels) { Axis = ica; Area = rc; AllLabels = labels; GeneratedLabels = new List<ICategoryLabelState>(); }
			/// <summary>
			/// Set the current index and value.
			/// </summary>
			/// <param name="idx">Current index.</param>
			public void SetTick(int idx) { Index = idx; }
			/// <summary>
			/// Add to the generated labels list.
			/// </summary>
			/// <param name="cls">The state.</param>
			public void Generated(ICategoryLabelState cls) { GeneratedLabels.Add(cls); }
		}
		#endregion
		#region properties
		/// <summary>
		/// Name of the data source we are attached to.
		/// </summary>
		public String DataSourceName { get { return (String)GetValue(DataSourceNameProperty); } set { SetValue(DataSourceNameProperty, value); } }
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
		/// Binding path to the axis label.
		/// </summary>
		public String LabelPath { get { return (String)GetValue(LabelPathProperty); } set { SetValue(LabelPathProperty, value); } }
		/// <summary>
		/// Path for axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Axis bar geometry.
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
		/// Identifies <see cref="DataSourceName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			nameof(DataSourceName), typeof(String), typeof(CategoryAxis), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="LabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelPathProperty = DependencyProperty.Register(
			nameof(LabelPath), typeof(String), typeof(CategoryAxis), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal, Side.Bottom) {
			CommonInit();
		}
		private void CommonInit() {
			AxisLabels = new List<ItemState>();
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
			var state = AxisLabels.SingleOrDefault((sis) => sis.tb == fe);
			if (state != null) {
				var loc = state.UpdateLocation();
				_trace.Verbose($"{Name} sizeChanged loc:{loc} yv:{state.value} ns:{e.NewSize}");
			}
		}
		#endregion
		#region extensions
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
			foreach (var state in AxisLabels) {
				if (state.tb == null) continue;
				state.scalex = scalex;
				state.top = icrc.Area.Top + AxisLineThickness + 2 * AxisMargin;
				state.left = icrc.Area.Left;
				var loc = state.UpdateLocation();
				_trace.Verbose($"tb {state.tb.ActualWidth}x{state.tb.ActualHeight} v:{state.value} @:({loc.X},{loc.Y})");
				if (!icrc.IsTransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					state.tb.InvalidateMeasure();
					state.tb.InvalidateArrange();
				}
			}
		}
		#endregion
		#region IDataSourceRenderer
		/// <summary>
		/// Internal render state.
		/// </summary>
		class State : RenderStateCore2<ItemState, TextBlock> {
			/// <summary>
			/// Remember whether we are using x-axis units or auto.
			/// </summary>
			internal readonly bool usexau;
			/// <summary>
			/// Binds label; MAY be NULL.
			/// </summary>
			internal readonly BindingEvaluator bl;
			internal readonly IChartRenderContext icrc;
			internal State(List<ItemState> state, Recycler2<TextBlock, ItemState> rc, IChartRenderContext icrc, bool xau, BindingEvaluator bl) : base(state, rc) {
				this.icrc = icrc;
				usexau = xau;
				this.bl = bl;
			}
		}
		object IDataSourceRenderer.Preamble(IChartRenderContext icrc) {
			if (Theme?.TextBlockTemplate == null) {
				// already reported an error so this should be no surprise
				return null;
			}
			if (String.IsNullOrEmpty(LabelPath)) return null;
			var bl = new BindingEvaluator(LabelPath);
			if (bl == null) return null;
			var recycler = new Recycler2<TextBlock, ItemState>(AxisLabels.Where(tl=>tl.tb != null).Select(tl => tl.tb), (tpx) => {
				var tb = Theme.TextBlockTemplate.LoadContent() as TextBlock;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), tb, FrameworkElement.StyleProperty);
				} else {
					// already reported this, but need to do something
					tb.FontSize = 10;
					tb.Foreground = Axis.Fill;
					tb.VerticalAlignment = VerticalAlignment.Center;
					tb.HorizontalAlignment = HorizontalAlignment.Center;
					tb.TextAlignment = TextAlignment.Center;
				}
				tb.SizeChanged += Element_SizeChanged;
				return tb;
			});
			ResetLimits();
			var widx = LabelStyle?.Find(FrameworkElement.WidthProperty);
			return new State(new List<ItemState>(), recycler, icrc, widx == null, bl);
		}
		/// <summary>
		/// MUST do "data-only" layout here, we don't know all the values yet.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="item"></param>
		void IDataSourceRenderer.Render(object state, int index, object item) {
			var st = state as State;
			st.ix = index;
			var label = st.bl.For(item);
			var istate = new ItemState() {
				index = index,
				value = index,
				label = label,
				usexau = st.usexau
			};
			st.itemstate.Add(istate);
		}
		/// <summary>
		/// Saw everything.  Set axis limits, finish up render process.
		/// </summary>
		/// <param name="state"></param>
		void IDataSourceRenderer.RenderComplete(object state) {
			var st = state as State;
			var labels = new List<ICategoryLabelState>(st.itemstate);
			// materialize current state
			var sc = new SelectorContext(this, st.icrc.SeriesArea, labels);
			// configure axis limits; just based on count-of-elements
			UpdateLimits(0);
			UpdateLimits(st.itemstate.Count);
			// finish up layout process by checking with selector/formatter
			foreach (var ist in st.itemstate) {
				sc.SetTick(ist.index);
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
				var current = st.recycler.Next(ist);
				if (current == null) break;
				if (!current.Item1) {
					// recycled: restore binding if we are using a LabelFormatter
					if (LabelFormatter != null && LabelStyle != null) {
						BindTo(this, nameof(LabelStyle), current.Item2, FrameworkElement.StyleProperty);
					}
				}
				// default text
				var text = ist.label == null
					? String.Empty
					: (String.IsNullOrEmpty(LabelFormatString)
						? ist.label.ToString()
						: String.Format(LabelFormatString, ist.label)
						);
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
				// back-fill values
				ist.tb = current.Item2;
				ist.tb.Text = text;
				sc.Generated(ist);
			}
		}
		/// <summary>
		/// Transfer render state to the component.
		/// </summary>
		/// <param name="state"></param>
		void IDataSourceRenderer.Postamble(object state) {
			var st = state as State;
			AxisLabels = st.itemstate;
			Layer.Remove(st.recycler.Unused);
			Layer.Add(st.recycler.Created);
			AxisGeometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness);
			AxisGeometry.Figures.Add(pf);
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
