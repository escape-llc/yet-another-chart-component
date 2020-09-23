using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ICategoryLabelState
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
	public class CategoryAxis : AxisCommon, IRequireLayout, IDataSourceRenderer, IRequireDataSourceUpdates, IRequireTransforms, IRequireEnterLeave {
		static readonly LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Error);
		#region ItemState
		/// <summary>
		/// The item state for this component.
		/// </summary>
		protected abstract class ItemState : ICategoryLabelState {
			#region data
			// the index; SHOULD match position in array
			internal int index;
			// the value; same as index if discrete
			internal double value;
			// the label
			internal object label;
			// the display element
			internal FrameworkElement element;
			// these are used for JIT re-positioning
			internal double scale;
			internal bool usexau;
			internal double yorigin;
			internal double xorigin;
			#endregion
			#region ICategoryLabelState
			int ICategoryLabelState.Index => index;
			double ICategoryLabelState.Value => value;
			object ICategoryLabelState.Label => label;
			#endregion
			#region extension points
			/// <summary>
			/// Perform any sizing of the element according to orientation.
			/// </summary>
			/// <param name="element"></param>
			internal abstract void SizeElement(FrameworkElement element);
			/// <summary>
			/// Calculate position based on <see cref="FrameworkElement.ActualWidth"/> or XAU as appropriate.
			/// </summary>
			/// <returns></returns>
			internal abstract Point GetLocation();
			#endregion
			/// <summary>
			/// Configure <see cref="Canvas"/> attached properties.
			/// </summary>
			/// <param name="loc"></param>
			internal void Locate(Point loc) {
				SizeElement(element);
				element.SetValue(Canvas.LeftProperty, loc.X);
				element.SetValue(Canvas.TopProperty, loc.Y);
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
		/// <summary>
		/// Version for horizontal (Top/Bottom).
		/// </summary>
		protected class ItemState_Horizontal : ItemState {
			internal override void SizeElement(FrameworkElement element) {
				if (usexau) {
					element.Width = scale;
				}
			}
			internal override Point GetLocation() {
				if (usexau) {
					// place it at XAU zero point
					return new Point(xorigin + value * scale, yorigin);
				}
				else {
					// place it centered in cell
					return new Point(xorigin + value * scale + scale / 2 - element.ActualWidth / 2, yorigin);
				}
			}
		}
		/// <summary>
		/// Version for vertical (Left/Right).
		/// </summary>
		protected class ItemState_Vertical : ItemState {
			internal override void SizeElement(FrameworkElement element) {
				if (usexau) {
					element.Height = scale;
				}
			}
			internal override Point GetLocation() {
				if (usexau) {
					// place it at XAU zero point
					return new Point(xorigin, yorigin + value * scale);
				}
				else {
					// place it centered in cell
					return new Point(xorigin, yorigin + value * scale + scale / 2 - element.ActualHeight / 2);
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
		/// <summary>
		/// Identifies <see cref="LabelTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.Register(
			nameof(LabelTemplate), typeof(DataTemplate), typeof(CategoryAxis), new PropertyMetadata(null)
		);
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public CategoryAxis() : base(AxisType.Category, Side.Bottom) {
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
		/// Rebuild the axis geometry based on current extents.
		/// </summary>
		void RebuildAxisGeometry() {
			AxisGeometry.Figures.Clear();
			var pf = Orientation == AxisOrientation.Horizontal
				? PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness)
				: PathHelper.Rectangle(0, Minimum, AxisLineThickness, Maximum);
			AxisGeometry.Figures.Add(pf);
		}
		/// <summary>
		/// Factory method for <see cref="FrameworkElement"/> creation.
		/// </summary>
		/// <param name="state">Item state.</param>
		/// <returns>New element.</returns>
		FrameworkElement CreateElement(ItemState state) {
			var fe = default(FrameworkElement);
			if (LabelTemplate != null) {
				fe = LabelTemplate.LoadContent() as FrameworkElement;
			} else if (Theme.TextBlockTemplate != null) {
				fe = Theme.TextBlockTemplate.LoadContent() as TextBlock;
				if (LabelStyle != null) {
					BindTo(this, nameof(LabelStyle), fe, FrameworkElement.StyleProperty);
				}
			}
			if (fe != null) {
				fe.SizeChanged += Element_SizeChanged;
			}
			return fe;
		}
		/// <summary>
		/// Main flow of the render pipeline.
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="ist"></param>
		/// <param name="recycler"></param>
		void ElementPipeline(SelectorContext sc, ItemState ist, Recycler<FrameworkElement,ItemState> recycler) {
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
			if (!createit) return;
			var current = recycler.Next(ist);
			if (current == null) return;
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
			var shim = new TextShim() { Text = text };
			current.Item2.DataContext = shim;
			BindTo(shim, nameof(Visibility), current.Item2, UIElement.VisibilityProperty);
			ist.element = current.Item2;
			sc.Generated(ist);
		}
		Matrix ProjectionFor(Rect area, bool reverse = false) {
			switch(Side) {
				case Side.Bottom:
					return MatrixSupport.ProjectionFor(
						reverse ? area.Right : area.Left,
						area.Top + AxisLineThickness + 2 * AxisMargin,
						reverse ? -1 : 1,
						1
					);
				case Side.Top:
					return MatrixSupport.ProjectionFor(
						reverse ? area.Right : area.Left,
						area.Top + AxisLineThickness + 2 * AxisMargin,
						reverse ? -1 : 1,
						-1
					);
				case Side.Left:
					return MatrixSupport.ProjectionFor(
						area.Right,
						area.Top,
						-(area.Width - AxisLineThickness - 2*AxisMargin),
						reverse ? -1 : 1
					);
				case Side.Right:
					return MatrixSupport.ProjectionFor(
						area.Left + AxisLineThickness + 2 * AxisMargin,
						area.Top,
						1,
						reverse ? -1 : 1
					);
				default:
					throw new NotImplementedException($"Not Implemented: {Side}");
			}
		}
		Matrix ProjectionForAxis(Rect area, double scale) {
			switch (Side) {
				case Side.Bottom:
					return MatrixSupport.ProjectionFor(area.Left, area.Top + AxisMargin, scale, 1);
				case Side.Top:
					return MatrixSupport.ProjectionFor(area.Left, area.Bottom - AxisMargin, scale, 1);
				case Side.Left:
					return MatrixSupport.ProjectionFor(area.Right - AxisMargin, area.Top, 1, scale);
				case Side.Right:
					return MatrixSupport.ProjectionFor(area.Left + AxisMargin, area.Top, 1, scale);
				default:
					throw new NotImplementedException($"Not Implemented: {Side}");
			}
		}
		#endregion
		#region extensions
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
			var state = AxisLabels.SingleOrDefault((sis) => sis.element == fe);
			if (state != null) {
				var loc = state.UpdateLocation();
				_trace.Verbose($"{Name} sizeChanged[{state.index}] loc:{loc} yv:{state.value} ns:{e.NewSize}");
			}
		}
		#endregion
		#region IRequireEnterLeave
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			if (Layer is IChartLayerAnimation icla) {
			}
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
			var space = AxisMargin + AxisLineThickness + (Orientation == AxisOrientation.Horizontal ? MinHeight : MinWidth);
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
			var scale = Orientation == AxisOrientation.Horizontal ? icrc.Area.Width / Range : icrc.Area.Height / Range;
			var matx = ProjectionForAxis(icrc.Area, scale);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			var matxv = ProjectionFor(icrc.Area);
			_trace.Verbose($"{Name}:{Orientation}:{Side} transforms sx:{scale:F3} matx:{matx} matxv:{matxv} a:{icrc.Area}");
			foreach (var state in AxisLabels) {
				if (state.element == null) continue;
				var mapt = Orientation == AxisOrientation.Horizontal ? new Point(0, 1) : new Point(1, 0);
				var ptv = matxv.Transform(mapt);
				_trace.Verbose($"{Name} mapped mapt:{mapt} ptv:{ptv}");
				state.scale = scale;
				state.xorigin = ptv.X;
				state.yorigin = ptv.Y;
				var loc = state.UpdateLocation();
				_trace.Verbose($"{Name} el {state.element.ActualWidth}x{state.element.ActualHeight} v:{state.value} @:({loc.X},{loc.Y})");
				if (icrc.Type != RenderType.TransformsOnly) {
					// doing render so (try to) trigger the SizeChanged handler
					state.element.InvalidateMeasure();
					state.element.InvalidateArrange();
				}
			}
		}
		#endregion
		#region IDataSourceRenderer
		/// <summary>
		/// Internal render state.
		/// </summary>
		class State : RenderStateCore<ItemState, FrameworkElement> {
			/// <summary>
			/// Remember whether we are using x-axis units or auto.
			/// </summary>
			internal readonly bool usexau;
			/// <summary>
			/// Binds label; MAY be NULL.
			/// </summary>
			internal readonly BindingEvaluator bl;
			internal readonly IChartRenderContext icrc;
			internal State(List<ItemState> state, Recycler<FrameworkElement, ItemState> rc, IChartRenderContext icrc, bool xau, BindingEvaluator bl) : base(state, rc) {
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
			var recycler = new Recycler<FrameworkElement, ItemState>(AxisLabels.Where(tl => tl.element != null).Select(tl => tl.element), CreateElement);
			ResetLimits();
			var autox = LabelStyle?.Find(Orientation == AxisOrientation.Horizontal ? FrameworkElement.WidthProperty : FrameworkElement.HeightProperty);
			return new State(new List<ItemState>(), recycler, icrc, autox == null, bl);
		}
		ItemState MakeIt(int index, object label, bool xau) {
			if (Orientation == AxisOrientation.Horizontal) {
				return new ItemState_Horizontal() {
					index = index,
					value = index,
					label = label,
					usexau = xau
				};
			}
			else {
				return new ItemState_Vertical() {
					index = index,
					value = index,
					label = label,
					usexau = xau
				};
			}
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
			var istate = MakeIt(index, label, st.usexau);
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
				ElementPipeline(sc, ist, st.recycler);
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
			RebuildAxisGeometry();
			Dirty = false;
		}
		#endregion
		#region IRequireDataSourceUpdates
		string IRequireDataSourceUpdates.UpdateSourceName => DataSourceName;
		void IRequireDataSourceUpdates.Remove(IChartRenderContext icrc, int startAt, IList items) {
			var remove = new List<FrameworkElement>();
			for (int ix = 0; ix < items.Count; ix++) {
				// remove requested item
				if (AxisLabels[startAt].element != null) {
					remove.Add(AxisLabels[startAt].element);
				}
				AxisLabels.RemoveAt(startAt);
			}
			// re-sequence remaining items
			for (int ix = startAt; ix < AxisLabels.Count; ix++) {
				AxisLabels[ix].index = ix;
				AxisLabels[ix].value = ix;
			}
			// configure axis limits; just based on count-of-elements
			UpdateLimits(0);
			UpdateLimits(AxisLabels.Count);
			// finish up
			Layer.Remove(remove);
			RebuildAxisGeometry();
			Dirty = false;
		}
		void IRequireDataSourceUpdates.Add(IChartRenderContext icrc, int startAt, IList items) {
			// mimic the DSRP sequence
			var widx = LabelStyle?.Find(FrameworkElement.WidthProperty);
			var bl = new BindingEvaluator(LabelPath);
			// keep a separate list; easier at the end
			var reproc = new List<ItemState>();
			for (int ix = 0; ix < items.Count; ix++) {
				// add requested item
				var label = bl.For(items[ix]);
				var istate = MakeIt(startAt + ix, label, widx == null);
				AxisLabels.Insert(startAt + ix, istate);
				reproc.Add(istate);
			}
			// re-sequence remaining items
			for (int ix = startAt + reproc.Count; ix < AxisLabels.Count; ix++) {
				AxisLabels[ix].index = ix;
				AxisLabels[ix].value = ix;
			}
			// render new items
			// run the element pipeline on the added items
			var recycler = new Recycler<FrameworkElement, ItemState>(CreateElement);
			var labels = new List<ICategoryLabelState>(AxisLabels);
			var sc = new SelectorContext(this, icrc.SeriesArea, labels);
			foreach (var istate in reproc) {
				ElementPipeline(sc, istate, recycler);
			}
			// configure axis limits; just based on count-of-elements
			UpdateLimits(0);
			UpdateLimits(AxisLabels.Count);
			// finish up
			Layer.Add(recycler.Created);
			RebuildAxisGeometry();
			Dirty = false;
		}
		#endregion
	}
	#endregion
}
