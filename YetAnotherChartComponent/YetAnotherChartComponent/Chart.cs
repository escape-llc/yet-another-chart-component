using eScape.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region context implementations
	#region DefaultRenderContext
	/// <summary>
	/// Default impl for render context.
	/// </summary>
	public class DefaultRenderContext : IChartRenderContext {
		#region properties
		/// <summary>
		/// The surface.  SHOULD NOT be null.
		/// </summary>
		protected Canvas Surface { get; set; }
		/// <summary>
		/// The list of components to search for Find().
		/// </summary>
		ObservableCollection<ChartComponent> Components { get; set; }
		/// <summary>
		/// The overall size of the chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// The data context in effect.
		/// </summary>
		public object DataContext { get; protected set; }
		/// <summary>
		/// The area for this component.
		/// </summary>
		public Rect Area { get; protected set; }
		/// <summary>
		/// The remaining area for series.
		/// </summary>
		public Rect SeriesArea { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface">The hosting UI.</param>
		/// <param name="components">The list of components.</param>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		/// <param name="dc">The data context.</param>
		public DefaultRenderContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc) {
			Surface = surface;
			Components = components;
			Dimensions = sz;
			Area = rc;
			SeriesArea = sa;
			DataContext = dc;
		}
		#endregion
		#region public
		/// <summary>
		/// Search the components list by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>!NULL: found; NULL: not found.</returns>
		public ChartComponent Find(string name) {
			return Components.SingleOrDefault((cx) => cx.Name == name);
		}
		#endregion
	}
	#endregion
	#region DefaultLayoutCompleteContext
	/// <summary>
	/// Default impl for layout complete context.
	/// </summary>
	public class DefaultLayoutCompleteContext : IChartLayoutCompleteContext {
		#region properties
		/// <summary>
		/// The overall size of the chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// The area for this component.
		/// </summary>
		public Rect Area { get; protected set; }
		/// <summary>
		/// The remaining area for series.
		/// </summary>
		public Rect SeriesArea { get; protected set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		public DefaultLayoutCompleteContext(Size sz, Rect rc, Rect sa) {
			Dimensions = sz;
			Area = rc;
			SeriesArea = sa;
		}
		#endregion
	}
	#endregion
	#region DefaultDataSourceRenderContext
	/// <summary>
	/// Default implementation for IDataSourceRenderContext.
	/// </summary>
	public class DefaultDataSourceRenderContext : DefaultRenderContext, IDataSourceRenderContext {
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="components"></param>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		/// <param name="sa"></param>
		/// <param name="dc"></param>
		public DefaultDataSourceRenderContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc)
		:base(surface, components, sz, rc, sa, dc) {
		}
	}
	#endregion
	#region DefaultEnterLeaveContext
	/// <summary>
	/// Default impl of the enter/leave context.
	/// </summary>
	public class DefaultEnterLeaveContext : DefaultRenderContext, IChartEnterLeaveContext {
		#region properties
		/// <summary>
		/// The next Z-index to allocate.
		/// </summary>
		public int NextZIndex { get; set; }
		/// <summary>
		/// The list of layers.
		/// </summary>
		protected List<IChartLayer> Layers { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface">The hosting UI.</param>
		/// <param name="components">The list of components.</param>
		/// <param name="layers">The list of layers.</param>
		/// <param name="sz">Size of chart rectangle.</param>
		/// <param name="rc">The target rectangle.</param>
		/// <param name="sa">The series area rectangle.</param>
		/// <param name="dc">The data context.</param>
		public DefaultEnterLeaveContext(Canvas surface, ObservableCollection<ChartComponent> components, List<IChartLayer> layers, Size sz, Rect rc, Rect sa, object dc) :base(surface, components, sz, rc, sa, dc) { Surface = surface; Layers = layers; }
		#endregion
		#region IChartEnterLeaveContext
		/// <summary>
		/// Add given element to surface.
		/// </summary>
		IChartLayer IChartEnterLeaveContext.CreateLayer() {
			var ccl = new CommonCanvasLayer(Surface, NextZIndex++);
			Layers.Add(ccl);
			return ccl;
			//Surface.Children.Add(fe);
		}
		IChartLayer IChartEnterLeaveContext.CreateLayer(params FrameworkElement[] fes) {
			var icl = (this as IChartEnterLeaveContext).CreateLayer();
			icl.Add(fes);
			return icl;
		}
		void IChartEnterLeaveContext.DeleteLayer(IChartLayer icl) {
			icl.Clear();
			Layers.Remove(icl);
		}
		#endregion
	}
	#endregion
	#region DefaultLayoutContext
	/// <summary>
	/// Default impl of layout context.
	/// </summary>
	public class DefaultLayoutContext : IChartLayoutContext {
		/// <summary>
		/// Overall size of chart rectangle.
		/// </summary>
		public Size Dimensions { get; protected set; }
		/// <summary>
		/// Amount of space remaining after claims.
		/// Gets adjusted after each call to Claim().
		/// </summary>
		public Rect RemainingRect { get; protected set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		public DefaultLayoutContext(Size sz, Rect rc) { Dimensions = sz; RemainingRect = rc; }
		IDictionary<ChartComponent, Rect> ClaimedRects { get; set; } = new Dictionary<ChartComponent, Rect>();
		/// <summary>
		/// Return the rect mapped to this component, else RemainingRect.
		/// </summary>
		/// <param name="cc"></param>
		/// <returns></returns>
		public Rect For(ChartComponent cc) { return ClaimedRects.ContainsKey(cc) ? ClaimedRects[cc] : RemainingRect; }
		/// <summary>
		/// Trim axis rectangles to be "flush" with the RemainingRect.
		/// </summary>
		public void FinalizeRects() {
			var tx = new Dictionary<ChartComponent, Rect>();
			foreach(var kv in ClaimedRects) {
				if(kv.Key is IChartAxis) {
					var ica = kv.Key as IChartAxis;
					switch (ica.Orientation) {
					case AxisOrientation.Horizontal:
						switch (ica.Side) {
						case Side.Bottom:
							tx.Add(kv.Key, new Rect(RemainingRect.Left, kv.Value.Top, RemainingRect.Width, kv.Value.Height));
							break;
						case Side.Top:
							// TODO fix
							tx.Add(kv.Key, new Rect(RemainingRect.Left, kv.Value.Top, RemainingRect.Width, kv.Value.Height));
							break;
						}
						break;
					case AxisOrientation.Vertical:
						switch (ica.Side) {
						case Side.Right:
							tx.Add(kv.Key, new Rect(kv.Value.Left, kv.Value.Top, kv.Value.Width, RemainingRect.Height));
							break;
						case Side.Left:
							// TODO fix
							tx.Add(kv.Key, new Rect(kv.Value.Left, kv.Value.Top, kv.Value.Width, RemainingRect.Height));
							break;
						}
						break;
					}
				}
			}
			// apply dictionary updates
			foreach(var kv in tx) {
				ClaimedRects[kv.Key] = kv.Value;
			}
		}
		/// <summary>
		/// Claim the indicated space for given component.
		/// </summary>
		/// <param name="cc"></param>
		/// <param name="sd"></param>
		/// <param name="amt"></param>
		/// <returns></returns>
		public Rect ClaimSpace(ChartComponent cc, Side sd, double amt) {
			var ul = new Point();
			var sz = new Size();
			switch(sd) {
			case Side.Top:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Top;
				sz.Width = Dimensions.Width;
				sz.Height = amt;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top + amt, RemainingRect.Width, RemainingRect.Height - amt);
				break;
			case Side.Right:
				ul.X = RemainingRect.Right - amt;
				ul.Y = RemainingRect.Top;
				sz.Width = amt;
				sz.Height = Dimensions.Height;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top, RemainingRect.Width - amt, RemainingRect.Height);
				break;
			case Side.Bottom:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Bottom - amt;
				sz.Width = Dimensions.Width;
				sz.Height = amt;
				RemainingRect = new Rect(RemainingRect.Left, RemainingRect.Top, RemainingRect.Width, RemainingRect.Height - amt);
				break;
			case Side.Left:
				ul.X = RemainingRect.Left;
				ul.Y = RemainingRect.Top;
				sz.Width = amt;
				sz.Height = Dimensions.Height;
				RemainingRect = new Rect(RemainingRect.Left + amt, RemainingRect.Top, RemainingRect.Width - amt, RemainingRect.Height);
				break;
			}
			var rect = new Rect(ul, sz);
			ClaimedRects.Add(cc, rect);
			return rect;
		}
	}
	#endregion
	#endregion
	#region layer implementations
	#region CommonCanvasLayer
	/// <summary>
	/// Layer where all layers share a common canvas.
	/// </summary>
	public class CommonCanvasLayer : IChartLayer {
		#region data
		readonly Canvas canvas;
		readonly int zindex;
		readonly List<FrameworkElement> elements;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="canvas">Target canvas.</param>
		/// <param name="zindex">Z-index to assign to elements.</param>
		public CommonCanvasLayer(Canvas canvas, int zindex) {
			this.canvas = canvas;
			this.zindex = zindex;
			this.elements = new List<FrameworkElement>();
		}
		#endregion
		#region IChartLayer
		/// <summary>
		/// Add element with assign z-index.
		/// </summary>
		/// <param name="fe"></param>
		void IChartLayer.Add(FrameworkElement fe) {
			fe.SetValue(Canvas.ZIndexProperty, zindex);
			elements.Add(fe);
			canvas.Children.Add(fe);
		}
		/// <summary>
		/// Add elements with assign z-index.
		/// </summary>
		/// <param name="fes"></param>
		void IChartLayer.Add(IEnumerable<FrameworkElement> fes) {
			foreach (var fe in fes) {
				(this as IChartLayer).Add(fe);
			}
		}
		/// <summary>
		/// Does Not Respond, because one canvas owns everything.
		/// </summary>
		/// <param name="target"></param>
		void IChartLayer.Layout(Rect target) { }
		void IChartLayer.Remove(FrameworkElement fe) { canvas.Children.Remove(fe); elements.Remove(fe); }
		void IChartLayer.Remove(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) { (this as IChartLayer).Remove(fe); } }
		/// <summary>
		/// Remove the elements this layer is tracking in the common parent.
		/// </summary>
		void IChartLayer.Clear() { foreach (var fe in elements) { canvas.Children.Remove(fe); } elements.Clear(); }
		#endregion
	}
	#endregion
	#region CanvasLayer
	/// <summary>
	/// Layer where each layer is bound to a different Canvas (COULD be IPanel).
	/// </summary>
	public class CanvasLayer : IChartLayer {
		#region data
		readonly Canvas canvas;
		readonly int zindex;
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="canvas">Target canvas.</param>
		/// <param name="zindex">Z-index to assign to this canvas.</param>
		public CanvasLayer(Canvas canvas, int zindex) {
			this.canvas = canvas;
			this.zindex = zindex;
			canvas.SetValue(Canvas.ZIndexProperty, zindex);
		}
		#endregion
		#region IChartLayer
		/// <summary>
		/// Add element with assign z-index.
		/// </summary>
		/// <param name="fe"></param>
		void IChartLayer.Add(FrameworkElement fe) { canvas.Children.Add(fe); }
		/// <summary>
		/// Add elements with assign z-index.
		/// </summary>
		/// <param name="fes"></param>
		void IChartLayer.Add(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) { (this as IChartLayer).Add(fe); } }
		/// <summary>
		/// Set Canvas layout properties on the source canvas.
		/// </summary>
		/// <param name="target"></param>
		void IChartLayer.Layout(Rect target) {
			canvas.SetValue(Canvas.TopProperty, target.Top);
			canvas.SetValue(Canvas.LeftProperty, target.Left);
			canvas.SetValue(FrameworkElement.WidthProperty, target.Width);
			canvas.SetValue(FrameworkElement.HeightProperty, target.Height);
		}
		void IChartLayer.Remove(FrameworkElement fe) { canvas.Children.Remove(fe); }
		void IChartLayer.Remove(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) (this as IChartLayer).Remove(fe); }
		void IChartLayer.Clear() { canvas.Children.Clear(); }
		#endregion
	}
	#endregion
	#endregion
	#region ChartDataSourceCollection
	/// <summary>
	/// This is to appease the XAML infrastruction which eschews generic classes as property type.
	/// </summary>
	public class ChartDataSourceCollection : ObservableCollection<DataSource> { }
	#endregion
	#region ChartComponentCollection
	/// <summary>
	/// This is to appease the XAML infrastruction which eschews generic classes as property type.
	/// </summary>
	public class ChartComponentCollection : ObservableCollection<ChartComponent>{ }
	#endregion
	#region LayoutState
	/// <summary>
	/// Keeps track of layout state between refreshes.
	/// </summary>
	public class LayoutState {
		#region properties
		/// <summary>
		/// Current dimensions.
		/// MUST NOT be (NaN,NaN) or (0,0).
		/// </summary>
		public Size Dimensions { get; set; }
		/// <summary>
		/// The "starting" layout rectangle.
		/// MAY account for Padding.
		/// Initialized by <see cref="InitializeLayoutContext"/>
		/// </summary>
		public Rect LayoutRect { get; private set; }
		/// <summary>
		/// The size of LayoutRect.
		/// Initialized by <see cref="InitializeLayoutContext"/>
		/// </summary>
		public Size LayoutDimensions { get; private set; }
		/// <summary>
		/// Current layout context.
		/// Initialized by <see cref="InitializeLayoutContext"/>
		/// </summary>
		public DefaultLayoutContext Layout { get; set; }
		#endregion
		#region data
		/// <summary>
		/// Cache for render contexts.
		/// </summary>
		Dictionary<ChartComponent, DefaultRenderContext> rendercache = new Dictionary<ChartComponent, DefaultRenderContext>();
		#endregion
		#region public
		/// <summary>
		/// Whether the given dimensions are different from <see cref="Dimensions"/>
		/// </summary>
		/// <param name="sz">New dimensions.</param>
		/// <returns></returns>
		public bool IsSizeChanged(Size sz) {
			return (Dimensions.Width != sz.Width || Dimensions.Height != sz.Height) ;
		}
		/// <summary>
		/// Calculate the initial layout rect.
		/// </summary>
		/// <param name="padding">Amount to subtract from rect.</param>
		/// <returns>Rectangle minus padding.</returns>
		Rect Initial(Thickness padding) {
			return new Rect(padding.Left, padding.Top, Dimensions.Width - padding.Left - padding.Right, Dimensions.Height - padding.Top - padding.Bottom);
		}
		/// <summary>
		/// Recreate the layout context.
		/// Sets <see cref="LayoutRect"/>, <see cref="LayoutDimensions"/>, <see cref="Layout"/>.
		/// Clears <see cref="rendercache"/>.
		/// </summary>
		/// <param name="padding"></param>
		public void InitializeLayoutContext(Thickness padding) {
			LayoutRect = Initial(padding);
			LayoutDimensions = new Size(LayoutRect.Width, LayoutRect.Height);
			Layout = new DefaultLayoutContext(LayoutDimensions, LayoutRect);
			rendercache.Clear();
		}
		/// <summary>
		/// Provide a render context for given component.
		/// Created contexts are cached until <see cref="InitializeLayoutContext"/> is called.
		/// </summary>
		/// <param name="cc">Component to provide context for.</param>
		/// <param name="surf">For ctor.</param>
		/// <param name="ccs">For ctor.</param>
		/// <param name="dc">For ctor.</param>
		/// <returns>New or cached instance.</returns>
		public DefaultRenderContext RenderFor(ChartComponent cc, Canvas surf, ObservableCollection<ChartComponent> ccs, object dc) {
			if (rendercache.ContainsKey(cc)) return rendercache[cc];
			var rect = Layout.For(cc);
			var drc = new DefaultRenderContext(surf, ccs, LayoutDimensions, rect, Layout.RemainingRect, dc);
			rendercache.Add(cc, drc);
			return drc;
		}
		#endregion
	}
	#endregion
	#region Chart
	/// <summary>
	/// The chart.
	/// </summary>
	[TemplatePart(Name = PART_Canvas, Type = typeof(Canvas))]
	public class Chart : Control {
		static LogTools.Flag _trace = LogTools.Add("Chart", LogTools.Level.Error);
		/// <summary>
		/// Control template part: canvas.
		/// </summary>
		public const String PART_Canvas = "PART_Canvas";
		#region properties
		/// <summary>
		/// The list of data sources.
		/// </summary>
		public ChartDataSourceCollection DataSources { get; private set; }
		/// <summary>
		/// The chart's visual components.
		/// Obtained from the XAML and programmatic.
		/// </summary>
		public ChartComponentCollection Components { get; private set; }
		/// <summary>
		/// The list of Legend items.
		/// This is intended for data binding to an external UI to present the legend.
		/// </summary>
		public ObservableCollection<Legend> LegendItems { get; private set; }
		/// <summary>
		/// The style to use for the axis labels.
		/// If NULL, attempts to initialize from ChartComponent.Resources.
		/// If that fails, axis uses hard-coded styling.
		/// </summary>
		public Style AxisLabelStyle { get { return (Style)GetValue(AxisLabelStyleProperty); } set { SetValue(AxisLabelStyleProperty, value); } }
		/// <summary>
		/// Obtained from the templated parent.
		/// </summary>
		protected Canvas Surface { get; set; }
		/// <summary>
		/// Components that are IChartAxis.
		/// </summary>
		protected List<IChartAxis> Axes { get; set; }
		/// <summary>
		/// Components that are DataSeries.
		/// </summary>
		protected List<DataSeries> Series { get; set; }
		/// <summary>
		/// Components that entered before the Surface was ready (via XAML).
		/// </summary>
		protected List<ChartComponent> DeferredEnter{ get; set; }
		/// <summary>
		/// Last-computed layout state.
		/// LayoutUpdated gets called frequently, so it gets debounced.
		/// </summary>
		protected LayoutState CurrentLayout { get; set; }
		/// <summary>
		/// Current set of layers.
		/// </summary>
		protected List<IChartLayer> Layers { get; set; }
		#endregion
		#region DPs
		/// <summary>
		/// Deendency property for <see cref="AxisLabelStyle"/>.
		/// </summary>
		public static readonly DependencyProperty AxisLabelStyleProperty = DependencyProperty.Register("AxisLabelStyle", typeof(Style), typeof(Chart), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Establish default values.
		/// </summary>
		public Chart() :base() {
			DefaultStyleKey = typeof(Chart);
			LegendItems = new ObservableCollection<Legend>();
			DataSources = new ChartDataSourceCollection();
			DataSources.CollectionChanged += DataSources_CollectionChanged;
			Components = new ChartComponentCollection();
			Components.CollectionChanged += new NotifyCollectionChangedEventHandler(Components_CollectionChanged);
			Axes = new List<IChartAxis>();
			Series = new List<DataSeries>();
			DeferredEnter = new List<ChartComponent>();
			LayoutUpdated += new EventHandler<object>(Chart_LayoutUpdated);
			DataContextChanged += Chart_DataContextChanged;
			CurrentLayout = new LayoutState();
			Layers = new List<IChartLayer>();
		}
		#endregion
		#region evhs
		/// <summary>
		/// Propagate data context changes to data sources and components.
		/// The number of times this is called is non-deterministic.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void Chart_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
			if (args.NewValue != DataContext) {
				_trace.Verbose($"DataContextChanged {args.NewValue}");
				foreach (var cc in Components) {
					cc.DataContext = args.NewValue;
				}
				foreach(DataSource ds in DataSources) {
					ds.DataContext = args.NewValue;
				}
			}
			else {
				foreach (var cc in Components) {
					if (cc.DataContext != args.NewValue) {
						_trace.Verbose($"DataContextChanged {cc} {args.NewValue}");
						cc.DataContext = args.NewValue;
					}
				}
				foreach (DataSource ds in DataSources) {
					if (ds.DataContext != args.NewValue) {
						ds.DataContext = args.NewValue;
					}
				}
			}
			args.Handled = true;
		}
		/// <summary>
		/// Reconfigure components in response to layout change.
		/// Happens After OnApplyTemplate.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Chart_LayoutUpdated(object sender, object e) {
			// This is (NaN,NaN) if we haven't been sized yet
			var sz = new Size(ActualWidth, ActualHeight);
			_trace.Verbose($"LayoutUpdated ({sz.Width}x{sz.Height})");
			if (!double.IsNaN(sz.Width) && !double.IsNaN(sz.Height)) {
				// we are sized; see if dimensions actually changed
				if (sz.Width == 0 || sz.Height == 0) return;
				if (CurrentLayout.IsSizeChanged(sz)) {
					var ls = new LayoutState() { Dimensions = sz, Layout = CurrentLayout.Layout };
					RenderComponents(ls);
					CurrentLayout = ls;
				}
			}
		}
		/// <summary>
		/// Manage data source enter/leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DataSources_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			_trace.Verbose($"DataSourcesChanged {e}");
			if (e.OldItems != null) {
				foreach (DataSource ds in e.OldItems) {
					_trace.Verbose($"leave '{ds.Name}' {ds}");
					ds.RefreshRequest -= DataSource_RefreshRequest;
				}
			}
			if (e.NewItems != null) {
				foreach (DataSource ds in e.NewItems) {
					_trace.Verbose($"enter '{ds.Name}' {ds}");
					ds.RefreshRequest += DataSource_RefreshRequest;
					ds.DataContext = DataContext;
				}
			}
			if (Surface != null) {
				RenderComponents(CurrentLayout);
			}
		}
		/// <summary>
		/// Reconfigure components that enter and leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var celc = new DefaultEnterLeaveContext(Surface, Components, Layers, CurrentLayout.Dimensions, Rect.Empty, Rect.Empty, DataContext);
			if (e.OldItems != null) {
				foreach (ChartComponent cc in e.OldItems) {
					_trace.Verbose($"leave '{cc.Name}' {cc}");
					cc.RefreshRequest -= ChartComponent_RefreshRequest;
					LeaveComponent(celc, cc);
				}
			}
			if (e.NewItems != null) {
				foreach (ChartComponent cc in e.NewItems) {
					_trace.Verbose($"enter '{cc.Name}' {cc}");
					cc.RefreshRequest += ChartComponent_RefreshRequest;
					cc.DataContext = DataContext;
					if(Surface != null)  {
						EnterComponent(celc, cc);
					}
					else {
						DeferredEnter.Add(cc);
					}
				}
			}
			if (Surface != null) {
				InvalidateArrange();
			}
		}
		/// <summary>
		/// Data source is requesting a refresh.
		/// Render chart subject to current dirtiness.
		/// This method is invoke-safe; it MAY be called from a different thread.
		/// </summary>
		/// <param name="ds"></param>
		private async void DataSource_RefreshRequest(DataSource ds) {
			_trace.Verbose($"refresh-request-ds '{ds.Name}' {ds}");
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
				if (Surface != null) {
					RenderComponents(CurrentLayout);
				}
			});
		}
		/// <summary>
		/// Component is requesting a refresh.
		/// Mark the chart's data source dirty and render chart.
		/// TODO get the DS to just refresh this CC.
		/// This method is invoke-safe; it MAY be called from a different thread.
		/// </summary>
		/// <param name="cc">Component requesting refresh.</param>
		/// <param name="rrea">The request parameter.</param>
		private async void ChartComponent_RefreshRequest(ChartComponent cc, RefreshRequestEventArgs rrea) {
			_trace.Verbose($"refresh-request-cc '{cc.Name}' {cc} r:{rrea.Request} a:{rrea.Axis}");
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
				if (Surface != null) {
					if (cc is DataSeries) {
						var ds = DataSources.SingleOrDefault(dds => dds.Name == (cc as DataSeries).DataSourceName);
						if (ds != null) {
							ds.IsDirty = true;
						}
						RenderComponents(CurrentLayout);
					} else {
						// dispatch other kinds of refresh requests
						if (rrea.Request == RefreshRequestType.LayoutDirty && rrea.Component is IRequireLayout) {
							ComponentRender(CurrentLayout, rrea);
						} else if (rrea.Request == RefreshRequestType.ValueDirty && rrea.Component is IRequireRender) {
							ComponentRender(CurrentLayout, rrea);
						} else if (rrea.Request == RefreshRequestType.TransformsDirty && cc is IRequireTransforms) {
							ComponentTransforms(CurrentLayout, rrea);
						}
					}
				}
			});
		}
		#endregion
		#region extensions
		/// <summary>
		/// Obtain UI elements from the control template.
		/// Happens Before Chart_LayoutUpdated.
		/// </summary>
		protected override void OnApplyTemplate() {
			try {
				Surface = GetTemplateChild(PART_Canvas) as Canvas;
				_trace.Verbose($"OnApplyTemplate ({Width}x{Height}) {Surface} d:{DeferredEnter.Count}");
				var celc = new DefaultEnterLeaveContext(Surface, Components, Layers, CurrentLayout.Dimensions, Rect.Empty, Rect.Empty, DataContext);
				foreach (var cc in DeferredEnter) {
					EnterComponent(celc, cc);
				}
				DeferredEnter.Clear();
			} finally {
				base.OnApplyTemplate();
			}
		}
		#endregion
		#region preset brushes - not currently used
		private List<Brush> _presetBrushes = new List<Brush>() {
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x66, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xD2, 0x02)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xB0, 0xDE, 0x09)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x0D, 0x8E, 0xCF)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x0C, 0xD0)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCD, 0x0D, 0x74)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xCC, 0x00, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xCC, 0x00)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xCC)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x99, 0x99)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x33, 0x33)),
			new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x00, 0x00))
		};
		/// <summary>
		/// Gets a collection of preset brushes used for graphs when their Brush property isn't set explicitly.
		/// </summary>
		public List<Brush> PresetBrushes { get { return _presetBrushes; } }
		#endregion
		#region phase helpers
		/// <summary>
		/// Reset all axis extents.
		/// </summary>
		protected void Phase_ResetAxes() {
			foreach (var axis in Axes) {
				_trace.Verbose($"reset {(axis as ChartComponent).Name} {axis}");
				axis.ResetLimits();
			}
		}
		/// <summary>
		/// Update limits for all components except the excluded one.
		/// Assumed excluded one will update limits itself afterwards.
		/// </summary>
		/// <param name="pred">Component filter.</param>
		protected void Phase_AxisLimits(Func<ChartComponent,bool> pred) {
			foreach (var cc in Components.Where(pred)) {
				_trace.Verbose($"axis-limits '{cc.Name}' {cc}");
				if(cc is IProvideCategoryExtents ipce) {
					var axis = Axes.SingleOrDefault((ax) => ipce.CategoryAxisName == (ax as ChartComponent).Name);
					_trace.Verbose($"axis-limits x-axis:{axis}");
					if (axis != null) {
						axis.UpdateLimits(ipce.CategoryMaximum);
						axis.UpdateLimits(ipce.CategoryMinimum);
					}
				}
				if(cc is IProvideValueExtents ipve) {
					var axis = Axes.SingleOrDefault((ax) => ipve.ValueAxisName == (ax as ChartComponent).Name);
					_trace.Verbose($"axis-limits y-axis:{axis}");
					if (axis != null) {
						axis.UpdateLimits(ipve.Maximum);
						axis.UpdateLimits(ipve.Minimum);
					}
				}
			}
		}
		/// <summary>
		/// Phase: Layout.
		/// IRequireLayout, finalize rects, IRequireLayoutComplete.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_Layout(LayoutState ls) {
			foreach (IRequireLayout cc in Components.Where((cc2) => cc2 is IRequireLayout)) {
				_trace.Verbose($"layout {cc}");
				cc.Layout(ls.Layout);
			}
			// what's left is for the data series area
			_trace.Verbose($"remaining {ls.Layout.RemainingRect}");
			ls.Layout.FinalizeRects();
			foreach (IRequireLayoutComplete cc in Components.Where((cc2) => cc2 is IRequireLayoutComplete)) {
				_trace.Verbose($"layout-complete {cc}");
				var rect = ls.Layout.For(cc as ChartComponent);
				var ctx = new DefaultLayoutCompleteContext(ls.Layout.Dimensions,rect, ls.Layout.RemainingRect);
				cc.LayoutComplete(ctx);
			}
		}
		/// <summary>
		/// Phase: Data Source Render Pipeline.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderDataSources(LayoutState ls) {
			var dsctx = new DefaultDataSourceRenderContext(Surface, Components, ls.LayoutDimensions, Rect.Empty, ls.Layout.RemainingRect, DataContext);
			foreach (DataSource ds in DataSources) {
				ds.Render(dsctx);
			}
		}
		/// <summary>
		/// Phase: axes have seen all values let them render (IRequireRender)
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderAxes(LayoutState ls) {
			foreach (var axis in Axes.Where((cc2) => cc2 is IRequireRender)) {
				var acc = axis as ChartComponent;
				var ctx = ls.RenderFor(acc, Surface, Components, DataContext);
				_trace.Verbose($"limits {acc.Name} ({axis.Minimum},{axis.Maximum}) r:{axis.Range} rect:{ctx.Area}");
				if (axis is IRequireRender irr) {
					irr.Render(ctx);
				}
			}
		}
		/// <summary>
		/// Phase: after-axes-finalized.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_AxesFinalized(LayoutState ls) {
			foreach (var cc in Components.Where((cc2) => cc2 is IRequireAfterAxesFinalized)) {
				var ctx = ls.RenderFor(cc, Surface, Components, DataContext);
				_trace.Verbose($"axes-finalized {cc.Name} rect:{ctx.Area}");
				if (cc is IRequireAfterAxesFinalized iraaf) {
					iraaf.AxesFinalized(ctx);
				}
			}
		}
		/// <summary>
		/// Phase: render-components.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderComponents(LayoutState ls) {
			foreach (IRequireRender cc in Components.Where((cc2) => !(cc2 is IChartAxis) && (cc2 is IRequireRender))) {
				var ctx = ls.RenderFor(cc as ChartComponent, Surface, Components, DataContext);
				cc.Render(ctx);
			}
		}
		/// <summary>
		/// Phase: transforms.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_Transforms(LayoutState ls) {
			foreach (IRequireTransforms cc in Components.Where((cc2) => cc2 is IRequireTransforms)) {
				var ctx = ls.RenderFor(cc as ChartComponent, Surface, Components, DataContext);
				_trace.Verbose($"transforms {cc} {ctx.Area}");
				cc.Transforms(ctx);
			}
		}
		#endregion
		#region helpers
		/// <summary>
		/// Bookkeeping for registering IDataSourceRenderer.
		/// </summary>
		/// <param name="dsname">Data source name.</param>
		/// <param name="idsr">Instance to register.</param>
		protected void Register(String dsname, IDataSourceRenderer idsr) {
			var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == dsname);
			if (source != null) {
				source.Register(idsr);
			}
		}
		/// <summary>
		/// Bookkeeping for unregistering IDataSourceRenderer.
		/// </summary>
		/// <param name="dsname">Data source name.</param>
		/// <param name="idsr">Instance to unregister.</param>
		protected void Unregister(String dsname, IDataSourceRenderer idsr) {
			var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == dsname);
			if (source != null) {
				source.Unregister(idsr);
			}
		}
		/// <summary>
		/// Common logic for component entering the chart.
		/// </summary>
		/// <param name="icelc">The context.</param>
		/// <param name="cc">The component entering chart.</param>
		protected void EnterComponent(IChartEnterLeaveContext icelc, ChartComponent cc) {
			// pre-load resources
			if (AxisLabelStyle != null && !cc.Resources.ContainsKey(nameof(AxisLabelStyle))) {
				cc.Resources.Add(nameof(AxisLabelStyle), AxisLabelStyle);
			}
			// invoke IREL
			if (cc is IRequireEnterLeave irel) {
				irel.Enter(icelc);
			}
			// for now anything can provide a legend item
			if(cc is IProvideLegend ipl) {
				LegendItems.Add(ipl.Legend);
			}
			// axis and series are mutually-exclusive
			if (cc is IChartAxis ica) {
				Axes.Add(ica);
			} else if (cc is DataSeries ds) {
				Series.Add(ds);
				if (ds is IDataSourceRenderer idsr) {
					Register(ds.DataSourceName, idsr);
				} else if(ds is IProvideDataSourceRenderer ipdsr) {
					Register(ds.DataSourceName, ipdsr.Renderer);
				}
			}
		}
		/// <summary>
		/// Common logic for leaving the chart.
		/// </summary>
		/// <param name="icelc"></param>
		/// <param name="cc"></param>
		protected void LeaveComponent(IChartEnterLeaveContext icelc, ChartComponent cc) {
			if(cc is IProvideLegend ipl) {
				LegendItems.Remove(ipl.Legend);
			}
			if (cc is IChartAxis ica) {
				Axes.Remove(ica);
			} else if (cc is DataSeries ds) {
				if (ds is IDataSourceRenderer idsr) {
					Unregister(ds.DataSourceName, idsr);
				} else if (ds is IProvideDataSourceRenderer ipdsr) {
					Unregister(ds.DataSourceName, ipdsr.Renderer);
				}
				Series.Remove(ds);
			}
			if (cc is IRequireEnterLeave irel) {
				irel.Leave(icelc);
			}
			cc.Resources.Remove(nameof(AxisLabelStyle));
		}
		/// <summary>
		/// Transforms for single component.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		/// <param name="rrea">Refresh request.</param>
		protected void ComponentTransforms(LayoutState ls, RefreshRequestEventArgs rrea) {
			if (rrea.Component is IRequireTransforms irt) {
				var rect = ls.Layout.For(rrea.Component);
				_trace.Verbose($"component-transforms {rrea.Component} {rrea.Axis} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext);
				irt.Transforms(ctx);
			}
		}
		/// <summary>
		/// Render for single component.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		/// <param name="rrea">Refresh request.</param>
		protected void ComponentRender(LayoutState ls, RefreshRequestEventArgs rrea) {
			if (rrea.Component is IRequireRender irr) {
				var rect = ls.Layout.For(rrea.Component);
				_trace.Verbose($"component-render {rrea.Component} {rrea.Axis} {rect}");
				if (rrea.Axis != AxisUpdateState.None) {
					// put axis limits into correct state for IRenderRequest components
					Phase_ResetAxes();
					Phase_AxisLimits((cc2) => cc2 is DataSeries && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
				}
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext);
				irr.Render(ctx);
				if (rrea.Axis != AxisUpdateState.None) {
					// axes MUST be re-evaluated because this thing changed.
					Phase_AxisLimits((cc2) => !(cc2 is DataSeries) && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
					Phase_AxesFinalized(ls);
					Phase_RenderAxes(ls);
					Phase_Transforms(ls);
				} else {
					if (rrea.Component is IRequireTransforms irt) {
						irt.Transforms(ctx);
					}
				}
			}
		}
		/// <summary>
		/// Adjust layout and transforms based on size change.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void TransformsLayout(LayoutState ls) {
			ls.InitializeLayoutContext(Padding);
			_trace.Verbose($"transforms-only starting {ls.LayoutRect}");
			Phase_Layout(ls);
			Phase_Transforms(ls);
		}
		/// <summary>
		/// Perform a full layout and rendering pass.
		/// At least ONE component reported as dirty.
		/// The full rendering sequence is: axis-reset, layout, render, transforms.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void FullLayout(LayoutState ls) {
			ls.InitializeLayoutContext(Padding);
			_trace.Verbose($"full starting {ls.LayoutRect}");
			// Phase I: reset axes
			Phase_ResetAxes();
			// Phase II: claim space (IRequireLayout)
			Phase_Layout(ls);
			// Phase III: data source rendering pipeline (IDataSourceRenderer)
			Phase_RenderDataSources(ls);
			Phase_AxisLimits((cc2) => cc2 is DataSeries && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
			// Phase IV: render non-axis components (IRequireRender)
			Phase_RenderComponents(ls);
			Phase_AxisLimits((cc2) => !(cc2 is DataSeries) && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
			// Phase V: axes finalized
			Phase_AxesFinalized(ls);
			// Phase VI: render axes (IRequireRender)
			Phase_RenderAxes(ls);
			// Phase VII: configure all transforms
			Phase_Transforms(ls);
		}
		/// <summary>
		/// Determine what kind of render is required, and run it.
		/// If anything is dirty, full layout, else adjust transforms.
		/// Once all components are "clean" only the visual transforms are updated; no data traversal is done.
		/// </summary>
		/// <param name="ls">The current layout state.</param>
		protected void RenderComponents(LayoutState ls) {
			_trace.Verbose($"render-components {ls.Dimensions.Width}x{ls.Dimensions.Height}");
			if (DataSources.Cast<DataSource>().Any((ds) => ds.IsDirty)) {
				FullLayout(ls);
			} else {
				TransformsLayout(ls);
			}
		}
		#endregion
	}
	#endregion
}
