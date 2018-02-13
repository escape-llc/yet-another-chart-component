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
		/// <summary>
		/// Value to provide for <see cref="IChartRenderContext.IsTransformsOnly"/>.
		/// </summary>
		public bool IsTransformsOnly { get; set; }
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
			drc.IsTransformsOnly = IsTransformsOnly;
			return drc;
		}
		#endregion
	}
	#endregion
	#region ChartErrorEventArgs
	/// <summary>
	/// Represents the error event args.
	/// </summary>
	public class ChartErrorEventArgs : EventArgs {
		/// <summary>
		/// The validation results array.
		/// </summary>
		public ChartValidationResult[] Results { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="cvr"></param>
		public ChartErrorEventArgs(params ChartValidationResult[] cvr) { Results = cvr; }
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
		/// The THEME to use for this chart.
		/// This MUST be set from GENERIC.XAML.
		/// If that fails, use hard-coded theme.
		/// </summary>
		public ChartTheme Theme { get { return (ChartTheme)GetValue(ThemeProperty); } set { SetValue(ThemeProperty, value); } }
		/// <summary>
		/// Obtained from the templated parent.
		/// </summary>
		protected Canvas Surface { get; set; }
		/// <summary>
		/// Components that are IChartAxis.
		/// </summary>
		protected List<IChartAxis> Axes { get; set; }
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
		#region events
		/// <summary>
		/// Event to receive notification of error info.
		/// This can help detect configuration or other runtime chart processing errors.
		/// </summary>
		public event TypedEventHandler<Chart, ChartErrorEventArgs> ChartError;
		#endregion
		#region DPs
		/// <summary>
		/// Deendency property for <see cref="Theme"/>.
		/// </summary>
		public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register (
			nameof(Theme), typeof(ChartTheme), typeof(Chart), new PropertyMetadata(null)
		);
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
					if (ds.Items != null && !ds.IsDirty && ds.Items.GetEnumerator().MoveNext()) {
						// force this dirty so it refreshes
						ds.IsDirty = true;
					}
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
			var celc = new DefaultEnterLeaveContext(Surface, Components, Layers, DataContext);
			if (e.OldItems != null) {
				foreach (ChartComponent cc in e.OldItems) {
					_trace.Verbose($"leave '{cc.Name}' {cc}");
					cc.RefreshRequest -= ChartComponent_RefreshRequest;
					ComponentLeave(celc, cc);
				}
			}
			if (e.NewItems != null) {
				foreach (ChartComponent cc in e.NewItems) {
					_trace.Verbose($"enter '{cc.Name}' {cc}");
					cc.RefreshRequest += ChartComponent_RefreshRequest;
					cc.DataContext = DataContext;
					if(Surface != null)  {
						ComponentEnter(celc, cc);
					}
					else {
						DeferredEnter.Add(cc);
					}
				}
			}
			if(celc.Errors.Count > 0) {
				Report(celc.Errors.ToArray());
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
				if (Surface == null) return;
				if (cc is IDataSourceRenderer idsr) {
					// TODO account for IProvideDataSourceRenderer
					var ds = DataSources.SingleOrDefault(dds => dds.Name == idsr.DataSourceName);
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
			});
		}
		/// <summary>
		/// Manage dynamic legend updates.
		/// </summary>
		/// <param name="sender">Component sending update.</param>
		/// <param name="args">Current state of legend.</param>
		private void Ipld_LegendChanged(ChartComponent sender, LegendDynamicEventArgs args) {
			foreach (var ldea in args.PreviousItems) {
				if (!args.CurrentItems.Contains(ldea))
					LegendItems.Remove(ldea);
			}
			foreach (var ldea in args.CurrentItems) {
				if (!LegendItems.Contains(ldea))
					LegendItems.Add(ldea);
			}
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
				var celc = new DefaultEnterLeaveContext(Surface, Components, Layers, DataContext);
				foreach (var cc in DeferredEnter) {
					ComponentEnter(celc, cc);
				}
				DeferredEnter.Clear();
				if (celc.Errors.Count > 0) {
					Report(celc.Errors.ToArray());
				}
			} finally {
				base.OnApplyTemplate();
			}
		}
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
		/// Report event(s).
		/// MUST be on Dispatcher thread!
		/// </summary>
		/// <param name="cvr">The event(s) to report.</param>
		protected void Report(params ChartValidationResult[] cvr) {
			ChartError?.Invoke(this, new ChartErrorEventArgs(cvr));
		}
		/// <summary>
		/// Bookkeeping for registering IDataSourceRenderer.
		/// </summary>
		/// <param name="idsr">Instance to register.</param>
		protected void Register(IDataSourceRenderer idsr) {
			var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == idsr.DataSourceName);
			if (source != null) {
				source.Register(idsr);
			}
		}
		/// <summary>
		/// Bookkeeping for unregistering IDataSourceRenderer.
		/// </summary>
		/// <param name="idsr">Instance to unregister.</param>
		protected void Unregister(IDataSourceRenderer idsr) {
			var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == idsr.DataSourceName);
			if (source != null) {
				source.Unregister(idsr);
			}
		}
		/// <summary>
		/// Common logic for entering the chart.
		/// </summary>
		/// <param name="icelc">The context.</param>
		/// <param name="cc">The component entering chart.</param>
		protected void ComponentEnter(IChartEnterLeaveContext icelc, ChartComponent cc) {
			// pre-load resources
			if (cc is IRequireChartTheme irct) {
				if (Theme == null) {
					Report(new ChartValidationResult("Chart", $"The {nameof(Theme)} property is NULL, chart elements may not be visible", new[] { cc.NameOrType(), nameof(Theme) }));
				} else {
					irct.Theme = Theme;
				}
			}
			// invoke IREL
			if (cc is IRequireEnterLeave irel) {
				irel.Enter(icelc);
			}
			// for now anything can provide legend items
			if (cc is IProvideLegend ipl) {
				foreach (var li in ipl.LegendItems) {
					LegendItems.Add(li);
				}
			}
			if(cc is IProvideLegendDynamic ipld) {
				// attach the event
				ipld.LegendChanged += Ipld_LegendChanged;
			}
			// axis and DSRP are mutually-exclusive
			if (cc is IChartAxis ica) {
				Axes.Add(ica);
			} else if (cc is IProvideDataSourceRenderer ipdsr) {
				Register(ipdsr.Renderer);
			} else if (cc is IDataSourceRenderer idsr) {
				Register(idsr);
			}
		}
		/// <summary>
		/// Common logic for leaving the chart.
		/// SHOULD be strict dual of ComponentEnter sequence.
		/// </summary>
		/// <param name="icelc">The context.</param>
		/// <param name="cc">The component leaving chart.</param>
		protected void ComponentLeave(IChartEnterLeaveContext icelc, ChartComponent cc) {
			if (cc is IChartAxis ica) {
				Axes.Remove(ica);
			} else if (cc is IProvideDataSourceRenderer ipdsr) {
				Unregister(ipdsr.Renderer);
			} else if (cc is IDataSourceRenderer idsr) {
				Unregister(idsr);
			}
			if (cc is IProvideLegendDynamic ipld) {
				// detach the event
				ipld.LegendChanged -= Ipld_LegendChanged;
			}
			if (cc is IProvideLegend ipl) {
				foreach (var li in ipl.LegendItems) {
					LegendItems.Remove(li);
				}
			}
			if (cc is IRequireEnterLeave irel) {
				irel.Leave(icelc);
			}
			if(cc is IRequireChartTheme irct) {
				irct.Theme = null;
			}
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
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext) { IsTransformsOnly = true };
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
					// put axis limits into correct state for IRequireRender components
					Phase_ResetAxes();
					Phase_AxisLimits((cc2) => cc2 is DataSeries && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
				}
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext) { IsTransformsOnly = false };
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
		/// SETs <see cref="LayoutState.IsTransformsOnly"/> to TRUE.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void TransformsLayout(LayoutState ls) {
			ls.IsTransformsOnly = true;
			ls.InitializeLayoutContext(Padding);
			_trace.Verbose($"transforms-only starting {ls.LayoutRect}");
			Phase_Layout(ls);
			Phase_Transforms(ls);
		}
		/// <summary>
		/// Perform a full layout and rendering pass.
		/// At least ONE component reported as dirty.
		/// The full rendering sequence is: axis-reset, layout, render, transforms.
		/// SETs <see cref="LayoutState.IsTransformsOnly"/> to FALSE.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void FullLayout(LayoutState ls) {
			ls.IsTransformsOnly = false;
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
			if(ls.Dimensions.Width == 0 || ls.Dimensions.Height == 0) {
				return;
			}
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
