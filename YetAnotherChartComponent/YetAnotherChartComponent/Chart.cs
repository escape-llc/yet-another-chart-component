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
	#region DefaultRenderContext
	/// <summary>
	/// Default impl for render context.
	/// </summary>
	public class DefaultRenderContext : IChartRenderContext {
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
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="components"></param>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		/// <param name="sa"></param>
		/// <param name="dc"></param>
		public DefaultRenderContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc) {
			Surface = surface;
			Components = components;
			Dimensions = sz;
			Area = rc;
			SeriesArea = sa;
			DataContext = dc;
		}
		/// <summary>
		/// Search the components list by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>!NULL: found; NULL: not found.</returns>
		public ChartComponent Find(string name) {
			return Components.SingleOrDefault((cx) => cx.Name == name);
		}
		/// <summary>
		/// Add elements to the surface.
		/// </summary>
		/// <param name="fes"></param>
		public void Add(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) Surface.Children.Add(fe); }
		/// <summary>
		/// Remove elements from the surface.
		/// </summary>
		/// <param name="fes"></param>
		public void Remove(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) Surface.Children.Remove(fe); }
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
		/// <summary>
		/// Ctor.
		/// Initialize.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="components"></param>
		/// <param name="sz"></param>
		/// <param name="rc"></param>
		/// <param name="sa"></param>
		/// <param name="dc"></param>
		public DefaultEnterLeaveContext(Canvas surface, ObservableCollection<ChartComponent> components, Size sz, Rect rc, Rect sa, object dc) :base(surface, components, sz, rc, sa, dc) { Surface = surface; }
		/// <summary>
		/// Add given element to surface.
		/// </summary>
		/// <param name="fe"></param>
		public void Add(FrameworkElement fe) {
			Surface.Children.Add(fe);
		}
		/// <summary>
		/// Remove given element from surface.
		/// </summary>
		/// <param name="fe"></param>
		public void Remove(FrameworkElement fe) {
			Surface.Children.Remove(fe);
		}
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
	#region CanvasLayer
	public class CanvasLayer : IChartLayer {
		readonly Canvas canvas;
		public CanvasLayer(Canvas canvas) {
			this.canvas = canvas;
		}
		void IChartLayer.Add(FrameworkElement fe) { canvas.Children.Add(fe); }
		void IChartLayer.Add(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) canvas.Children.Add(fe); }
		void IChartLayer.Layout(Rect target) {
			canvas.SetValue(Canvas.TopProperty, target.Top);
			canvas.SetValue(Canvas.LeftProperty, target.Left);
			canvas.SetValue(FrameworkElement.WidthProperty, target.Width);
			canvas.SetValue(FrameworkElement.HeightProperty, target.Height);
		}
		void IChartLayer.Remove(FrameworkElement fe) { canvas.Children.Remove(fe); }
		void IChartLayer.Remove(IEnumerable<FrameworkElement> fes) { foreach (var fe in fes) canvas.Children.Remove(fe); }
	}
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
		/// Last-seen value during LayoutUpdated.
		/// LayoutUpdated gets called frequently, so it gets debounced.
		/// </summary>
		protected Size LastLayout { get; set; }
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
				if (LastLayout.Width == sz.Width && LastLayout.Height == sz.Height) return;
				RenderComponents(sz);
				LastLayout = sz;
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
				RenderComponents(LastLayout);
			}
		}
		/// <summary>
		/// Reconfigure components that enter and leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var celc = new DefaultEnterLeaveContext(Surface, Components, LastLayout, Rect.Empty, Rect.Empty, DataContext);
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
					RenderComponents(LastLayout);
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
						RenderComponents(LastLayout);
					} else {
						// dispatch other kinds of refresh requests
						if (rrea.Request == RefreshRequestType.LayoutDirty && rrea.Component is IRequireLayout) {
							ComponentRender(LastLayout, rrea);
						} else if (rrea.Request == RefreshRequestType.ValueDirty && rrea.Component is IRequireRender) {
							ComponentRender(LastLayout, rrea);
						} else if (rrea.Request == RefreshRequestType.TransformsDirty && cc is IRequireTransforms) {
							ComponentTransforms(LastLayout, rrea);
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
				var celc = new DefaultEnterLeaveContext(Surface, Components, LastLayout, Rect.Empty, Rect.Empty, DataContext);
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
		protected void Phase_Layout(DefaultLayoutContext dlc) {
			foreach (IRequireLayout cc in Components.Where((cc2) => cc2 is IRequireLayout)) {
				_trace.Verbose($"layout {cc}");
				cc.Layout(dlc);
			}
			// what's left is for the data series area
			_trace.Verbose($"remaining {dlc.RemainingRect}");
			dlc.FinalizeRects();
		}
		protected void Phase_RenderDataSources(DefaultLayoutContext dlc, Size inner) {
			var dsctx = new DefaultDataSourceRenderContext(Surface, Components, inner, Rect.Empty, dlc.RemainingRect, DataContext);
			foreach (DataSource ds in DataSources) {
				ds.Render(dsctx);
			}
		}
		/// <summary>
		/// Phase: axes have seen all values let them render (IRequireRender)
		/// </summary>
		/// <param name="dlc"></param>
		/// <param name="inner"></param>
		protected void Phase_RenderAxes(DefaultLayoutContext dlc, Size inner) {
			foreach (var axis in Axes.Where((cc2) => cc2 is IRequireRender)) {
				var acc = axis as ChartComponent;
				var rect = dlc.For(acc);
				_trace.Verbose($"limits {acc.Name} ({axis.Minimum},{axis.Maximum}) r:{axis.Range} rect:{rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				if (axis is IRequireRender irr) {
					irr.Render(ctx);
				}
			}
		}
		protected void Phase_AxesFinalized(DefaultLayoutContext dlc, Size inner) {
			foreach (var cc in Components.Where((cc2) => cc2 is IRequireAfterAxesFinalized)) {
				var rect = dlc.For(cc);
				_trace.Verbose($"axes-finalized {cc.Name} rect:{rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				if (cc is IRequireAfterAxesFinalized iraaf) {
					iraaf.AxesFinalized(ctx);
				}
			}
		}
		protected void Phase_RenderComponents(DefaultLayoutContext dlc, Size inner) {
			foreach (IRequireRender cc in Components.Where((cc2) => !(cc2 is IChartAxis) && (cc2 is IRequireRender))) {
				var rect = dlc.For(cc as ChartComponent);
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				cc.Render(ctx);
			}
		}
		protected void Phase_Transforms(DefaultLayoutContext dlc, Size inner) {
			foreach (IRequireTransforms cc in Components.Where((cc2) => cc2 is IRequireTransforms)) {
				var rect = dlc.For(cc as ChartComponent);
				_trace.Verbose($"transforms {cc} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				cc.Transforms(ctx);
			}
		}
		#endregion
		#region helpers
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
				var leg = ipl.Legend();
				LegendItems.Add(leg);
			}
			// axis and series are mutually-exclusive
			if (cc is IChartAxis ica) {
				Axes.Add(ica);
			} else if (cc is DataSeries ds) {
				Series.Add(ds);
				if (ds is IDataSourceRenderer idsr) {
					var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == ds.DataSourceName);
					if (source != null) {
						source.Register(idsr);
					}
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
			// TODO pull out the legend item from collection.
			}
			if (cc is IChartAxis ica) {
				Axes.Remove(ica);
			} else if (cc is DataSeries ds) {
				if (ds is IDataSourceRenderer idsr) {
					var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == ds.DataSourceName);
					if (source != null) {
						source.Unregister(idsr);
					}
				}
				Series.Remove(ds);
			}
			if (cc is IRequireEnterLeave irel) {
				irel.Leave(icelc);
			}
			cc.Resources.Remove(nameof(AxisLabelStyle));
		}
		/// <summary>
		/// Account for padding.
		/// </summary>
		/// <param name="sz">Size.</param>
		/// <returns>Adjusted rectangle.</returns>
		protected Rect CalculateInitialRect(Size sz) {
			return new Rect(Padding.Left, Padding.Top, sz.Width - Padding.Left - Padding.Right, sz.Height - Padding.Top - Padding.Bottom);
		}
		/// <summary>
		/// Transforms for single component.
		/// </summary>
		/// <param name="sz">Current size.</param>
		/// <param name="rrea">Refresh request.</param>
		protected void ComponentTransforms(Size sz, RefreshRequestEventArgs rrea) {
			if (sz.Width == 0 || sz.Height == 0) return;
			if (rrea.Component is IRequireTransforms irt) {
				var initialRect = CalculateInitialRect(sz);
				var inner = new Size(initialRect.Width, initialRect.Height);
				// FOR NOW just re-run this, but it SHOULD be cached (when LastLayout is)
				var dlc = new DefaultLayoutContext(inner, initialRect);
				_trace.Verbose($"cr.starting {initialRect}");
				Phase_Layout(dlc);
				var rect = dlc.For(rrea.Component);
				_trace.Verbose($"component-render {rrea.Component} {rrea.Axis} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				irt.Transforms(ctx);
			}
		}
		/// <summary>
		/// Render for single component.
		/// </summary>
		/// <param name="sz">Current size.</param>
		/// <param name="rrea">Refresh request.</param>
		protected void ComponentRender(Size sz, RefreshRequestEventArgs rrea) {
			if (sz.Width == 0 || sz.Height == 0) return;
			if (rrea.Component is IRequireRender irr) {
				var initialRect = CalculateInitialRect(sz);
				var inner = new Size(initialRect.Width, initialRect.Height);
				// FOR NOW just re-run this, but it SHOULD be cached (when LastLayout is)
				var dlc = new DefaultLayoutContext(inner, initialRect);
				_trace.Verbose($"cr.starting {initialRect}");
				Phase_Layout(dlc);
				var rect = dlc.For(rrea.Component);
				_trace.Verbose($"component-render {rrea.Component} {rrea.Axis} {rect}");
				if (rrea.Axis != AxisUpdateState.None) {
					// put axis limits into correct state for IRenderRequest components
					Phase_ResetAxes();
					Phase_AxisLimits((cc2) => cc2 is DataSeries && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
				}
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				irr.Render(ctx);
				if (rrea.Axis != AxisUpdateState.None) {
					// axes MUST be re-evaluated because this thing changed.
					Phase_AxisLimits((cc2) => !(cc2 is DataSeries) && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
					Phase_AxesFinalized(dlc, inner);
					Phase_RenderAxes(dlc, inner);
					Phase_Transforms(dlc, inner);
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
		/// <param name="sz">Dimensions.</param>
		protected void TransformsOnly(Size sz) {
			var initialRect = CalculateInitialRect(sz);
			var inner = new Size(initialRect.Width, initialRect.Height);
			var dlc = new DefaultLayoutContext(inner, initialRect);
			_trace.Verbose($"transforms-only starting {initialRect}");
			Phase_Layout(dlc);
			Phase_Transforms(dlc, inner);
		}
		/// <summary>
		/// Perform a full layout and rendering pass.
		/// At least ONE component reported as dirty.
		/// The full rendering sequence is: axis-reset, layout, render, transforms.
		/// </summary>
		/// <param name="sz">Dimensions.</param>
		protected void FullLayout(Size sz) {
			var initialRect = CalculateInitialRect(sz);
			var inner = new Size(initialRect.Width, initialRect.Height);
			// Phase I: reset axes
			Phase_ResetAxes();
			// Phase II: claim space (IRequireLayout)
			var dlc = new DefaultLayoutContext(inner, initialRect);
			_trace.Verbose($"full starting {initialRect}");
			Phase_Layout(dlc);
			// Phase III: data source rendering pipeline (IDataSourceRenderer)
			Phase_RenderDataSources(dlc, inner);
			Phase_AxisLimits((cc2) => cc2 is DataSeries && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
			// Phase IV: render non-axis components (IRequireRender)
			Phase_RenderComponents(dlc, inner);
			Phase_AxisLimits((cc2) => !(cc2 is DataSeries) && (cc2 is IProvideCategoryExtents || cc2 is IProvideValueExtents));
			// Phase V: axes finalized
			Phase_AxesFinalized(dlc, inner);
			// Phase VI: render axes (IRequireRender)
			Phase_RenderAxes(dlc, inner);
			// Phase VII: configure all transforms
			Phase_Transforms(dlc, inner);
		}
		/// <summary>
		/// Determine what kind of render is required, and run it.
		/// If anything is dirty, full layout, else adjust transforms.
		/// Once all components are "clean" only the visual transforms are updated; no data traversal is done.
		/// </summary>
		/// <param name="sz">The dimensions.</param>
		private void RenderComponents(Size sz) {
			_trace.Verbose($"render-components {sz.Width}x{sz.Height}");
			if (DataSources.Cast<DataSource>().Any((ds) => ds.IsDirty)) {
				FullLayout(sz);
			} else {
				TransformsOnly(sz);
			}
		}
		#endregion
	}
	#endregion
}
