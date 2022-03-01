﻿using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace eScapeLLC.UWP.Charts {
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
		/// Value to provide for <see cref="IChartRenderContext.Type"/>.
		/// </summary>
		public RenderType Type { get; set; }
		#endregion
		#region data
		/// <summary>
		/// Cache for render contexts.
		/// </summary>
		readonly Dictionary<ChartComponent, DefaultRenderContext> rendercache = new Dictionary<ChartComponent, DefaultRenderContext>();
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
			// ensure w/h are GE zero
			var wid = padding.Left + padding.Right >= Dimensions.Width ? Dimensions.Width: Dimensions.Width - padding.Left - padding.Right;
			var hgt = padding.Top + padding.Bottom >= Dimensions.Height ? Dimensions.Height : Dimensions.Height - padding.Top - padding.Bottom;
			return new Rect(padding.Left, padding.Top, wid, hgt);
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
		/// <para/>
		/// Sets the <see cref="DefaultRenderContext.Type"/> to the current value of <see cref="Type"/>.
		/// </summary>
		/// <param name="cc">Component to provide context for.</param>
		/// <param name="surf">For ctor.</param>
		/// <param name="ccs">For ctor.</param>
		/// <param name="dc">For ctor.</param>
		/// <returns>New or cached instance.</returns>
		public DefaultRenderContext RenderFor(ChartComponent cc, Canvas surf, ObservableCollection<ChartComponent> ccs, object dc) {
			if (rendercache.ContainsKey(cc)) {
				var rc = rendercache[cc];
				rc.Type = Type;
				return rc;
			}
			var rect = Layout.For(cc);
			var drc = new DefaultRenderContext(surf, ccs, LayoutDimensions, rect, Layout.RemainingRect, dc);
			rendercache.Add(cc, drc);
			drc.Type = Type;
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
	#region Phase<T>
	/// <summary>
	/// Base impl.
	/// </summary>
	public abstract class PhaseBase {
		/// <summary>
		/// Enter list if passing checks.
		/// </summary>
		/// <param name="cc">Candidate.</param>
		public abstract void Enter(ChartComponent cc);
		/// <summary>
		/// Remove element.
		/// </summary>
		/// <param name="cc">Candidate.</param>
		public abstract void Leave(ChartComponent cc);
		/// <summary>
		/// Reset the list.
		/// </summary>
		public abstract void Clear();
	}
	/// <summary>
	/// No checks beyond "is T".
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	public class PhaseOnly<T> : PhaseBase where T : class {
		/// <summary>
		/// The list.
		/// </summary>
		protected readonly List<T> items = new List<T>();
		/// <summary>
		/// Iterate the items.
		/// </summary>
		public IEnumerable<T> Items => items;
		/// <summary>
		/// Reset the list.
		/// </summary>
		public override void Clear() { items.Clear(); }
		/// <summary>
		/// Enter list if passing checks.
		/// </summary>
		/// <param name="ex">Candidate.</param>
		public override void Enter(ChartComponent ex) { if (ex is T tx) { items.Add(tx); } }
		/// <summary>
		/// Remove element.
		/// </summary>
		/// <param name="ex">Candidate.</param>
		public override void Leave(ChartComponent ex) { items.Remove(ex as T); }
	}
	/// <summary>
	/// Additional entry criteria.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	public class Phase<T> : PhaseOnly<T> where T : class {
		readonly Func<ChartComponent, bool> enter;
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="enter">Additional entry criteria.</param>
		public Phase(Func<ChartComponent, bool> enter) {
			this.enter = enter ?? throw new ArgumentNullException(nameof(enter));
		}
		/// <summary>
		/// Enter list if passing checks.
		/// </summary>
		/// <param name="ex">Candidate.</param>
		public override void Enter(ChartComponent ex) { if (ex is T tx && enter(ex)) { items.Add(tx); } }
	}
	#endregion
	#region Chart
	/// <summary>
	/// The chart.
	/// </summary>
	[TemplatePart(Name = PART_Canvas, Type = typeof(Canvas))]
	public class Chart : Control {
		static readonly LogTools.Flag _trace = LogTools.Add("Chart", LogTools.Level.Error);
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
		public ObservableCollection<LegendBase> LegendItems { get; private set; }
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
		protected List<ChartComponent> DeferredEnter { get; set; }
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
		public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
			nameof(Theme), typeof(ChartTheme), typeof(Chart), new PropertyMetadata(null)
		);
		#endregion
		#region Phases
		/// <summary>
		/// Has <see cref="IRequireLayout"/>.
		/// </summary>
		protected PhaseOnly<IRequireLayout> Layout_All { get; set; } = new PhaseOnly<IRequireLayout>();
		/// <summary>
		/// Has <see cref="IRequireLayoutComplete"/>.
		/// </summary>
		protected PhaseOnly<IRequireLayoutComplete> LayoutComplete_All { get; set; } = new PhaseOnly<IRequireLayoutComplete>();
		/// <summary>
		/// Has <see cref="IRequireAfterAxesFinalized"/>.
		/// </summary>
		protected PhaseOnly<IRequireAfterAxesFinalized> AfterAxesFinalized { get; set; } = new PhaseOnly<IRequireAfterAxesFinalized>();
		/// <summary>
		/// Has <see cref="IRequireRender"/> AND is <see cref="IChartAxis"/>.
		/// </summary>
		protected Phase<IRequireRender> Render_AllAxes { get; set; } = new Phase<IRequireRender>(cc => cc is IChartAxis);
		/// <summary>
		/// Has <see cref="IRequireRender"/> AND is NOT <see cref="IChartAxis"/>.
		/// </summary>
		protected Phase<IRequireRender> Render_NotAnAxis { get; set; } = new Phase<IRequireRender>(cc => !(cc is IChartAxis));
		/// <summary>
		/// Has <see cref="IRequireRender"/> AND is NOT <see cref="IChartAxis"/> AND is NOT <see cref="IRequireRenderPostAxesFinalized"/>.
		/// </summary>
		protected Phase<IRequireRender> Render_Components { get; set; } = new Phase<IRequireRender>(cc => !(cc is IChartAxis) && !(cc is IRequireRenderPostAxesFinalized));
		/// <summary>
		/// Has <see cref="IRequireRender"/> AND is NOT <see cref="IChartAxis"/> AND is <see cref="IRequireRenderPostAxesFinalized"/>.
		/// </summary>
		protected Phase<IRequireRender> Render_Components_PostAxis { get; set; } = new Phase<IRequireRender>(cc => !(cc is IChartAxis) && (cc is IRequireRenderPostAxesFinalized));
		/// <summary>
		/// Has <see cref="IRequireTransforms"/>.
		/// </summary>
		protected PhaseOnly<IRequireTransforms> Transforms_All { get; set; } = new PhaseOnly<IRequireTransforms>();
		/// <summary>
		/// Has <see cref="IProvideValueExtents"/> AND is <see cref="DataSeries"/>.
		/// </summary>
		protected Phase<IProvideValueExtents> ValueExtents_DataSeries { get; set; } = new Phase<IProvideValueExtents>(cc => cc is DataSeries);
		/// <summary>
		/// Has <see cref="IProvideValueExtents"/> AND is NOT <see cref="DataSeries"/>.
		/// </summary>
		protected Phase<IProvideValueExtents> ValueExtents_NotDataSeries { get; set; } = new Phase<IProvideValueExtents>(cc => !(cc is DataSeries));
		/// <summary>
		/// Has <see cref="IRequireDataSourceUpdates"/>.
		/// </summary>
		protected PhaseOnly<IRequireDataSourceUpdates> DataSourceUpdates_All { get; set; } = new PhaseOnly<IRequireDataSourceUpdates>();
		/// <summary>
		/// All the phases in one list.
		/// </summary>
		protected List<PhaseBase> AllPhases { get; set; } = new List<PhaseBase>();
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Establish default values.
		/// </summary>
		public Chart() :base() {
			DefaultStyleKey = typeof(Chart);
			LegendItems = new ObservableCollection<LegendBase>();
			DataSources = new ChartDataSourceCollection();
			DataSources.CollectionChanged += DataSources_CollectionChanged;
			Components = new ChartComponentCollection();
			Components.CollectionChanged += new NotifyCollectionChangedEventHandler(Components_CollectionChanged);
			Axes = new List<IChartAxis>();
			DeferredEnter = new List<ChartComponent>();
			LayoutUpdated += new EventHandler<object>(Chart_LayoutUpdated);
			SizeChanged += Chart_SizeChanged;
			DataContextChanged += Chart_DataContextChanged;
			CurrentLayout = new LayoutState();
			Layers = new List<IChartLayer>();
			AllPhases.Add(Layout_All);
			AllPhases.Add(LayoutComplete_All);
			AllPhases.Add(AfterAxesFinalized);
			AllPhases.Add(Render_AllAxes);
			AllPhases.Add(Render_NotAnAxis);
			AllPhases.Add(Render_Components);
			AllPhases.Add(Render_Components_PostAxis);
			AllPhases.Add(Transforms_All);
			AllPhases.Add(DataSourceUpdates_All);
			AllPhases.Add(ValueExtents_DataSeries);
			AllPhases.Add(ValueExtents_NotDataSeries);
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
		private void Chart_SizeChanged(object sender, SizeChangedEventArgs e) {
		#if false
			_trace.Verbose($"SizeChanged prev({e.PreviousSize.Width}x{e.PreviousSize.Height}) new({e.NewSize.Width}x{e.NewSize.Height})");
			if (e.NewSize.Width == 0 || e.NewSize.Height == 0) return;
			if (CurrentLayout.IsSizeChanged(e.NewSize)) {
				var ls = new LayoutState() { Dimensions = e.NewSize, Layout = CurrentLayout.Layout };
				RenderComponents(ls);
				CurrentLayout = ls;
			}
		#endif
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
			//_trace.Verbose($"LayoutUpdated ({sz.Width}x{sz.Height})");
			if (!double.IsNaN(sz.Width) && !double.IsNaN(sz.Height)) {
				// we are sized; see if dimensions actually changed
				if (sz.Width == 0 || sz.Height == 0) return;
				if (CurrentLayout.IsSizeChanged(sz)) {
					_trace.Verbose($"LayoutUpdated.trigger ({sz.Width}x{sz.Height})");
					var ls = new LayoutState() { Dimensions = sz, Layout = CurrentLayout.Layout };
					try {
						RenderComponents(ls);
					}
					catch(Exception ex) {
						_trace.Error($"{ex}");
					}
					finally {
						CurrentLayout = ls;
					}
				}
			}
		}
		/// <summary>
		/// Manage data source enter/leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="nccea"></param>
		private void DataSources_CollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			_trace.Verbose($"DataSourcesChanged {nccea}");
			try {
				if (nccea.OldItems != null) {
					foreach (DataSource ds in nccea.OldItems) {
						_trace.Verbose($"leave '{ds.Name}' {ds}");
						ds.RefreshRequest -= DataSource_RefreshRequest;
					}
				}
				if (nccea.NewItems != null) {
					foreach (DataSource ds in nccea.NewItems) {
						_trace.Verbose($"enter '{ds.Name}' {ds}");
						if (ds.Items != null && !ds.IsDirty && ds.Items.GetEnumerator().MoveNext()) {
							// force this dirty so it refreshes
							ds.IsDirty = true;
						}
						ds.RefreshRequest -= DataSource_RefreshRequest;
						ds.RefreshRequest += DataSource_RefreshRequest;
						ds.DataContext = DataContext;
					}
				}
			}
			catch(Exception ex) {
				_trace.Error($"{Name} DataSources_CollectionChanged.unhandled: {ex}");
			}
			if (Surface != null) {
				RenderComponents(CurrentLayout);
			}
		}
		/// <summary>
		/// Reconfigure components that enter and leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="nccea"></param>
		void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			var celc = new DefaultEnterLeaveContext(Surface, Components, Layers, DataContext);
			try {
				if (nccea.OldItems != null) {
					foreach (ChartComponent cc in nccea.OldItems) {
						_trace.Verbose($"leave '{cc.Name}' {cc}");
						cc.RefreshRequest -= ChartComponent_RefreshRequest;
						ComponentLeave(celc, cc);
					}
				}
				if (nccea.NewItems != null) {
					foreach (ChartComponent cc in nccea.NewItems) {
						_trace.Verbose($"enter '{cc.Name}' {cc}");
						cc.RefreshRequest -= ChartComponent_RefreshRequest;
						cc.RefreshRequest += ChartComponent_RefreshRequest;
						cc.DataContext = DataContext;
						if (Surface != null) {
							ComponentEnter(celc, cc);
						}
						else {
							DeferredEnter.Add(cc);
						}
					}
				}
			}
			catch(Exception ex) {
				_trace.Error($"{Name} Components_CollectionChanged.unhandled: {ex}");
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
		/// <param name="ds">The data source.</param>
		/// <param name="nccea">Collection change args.</param>
		private async void DataSource_RefreshRequest(DataSource ds, NotifyCollectionChangedEventArgs nccea) {
			_trace.Verbose($"refresh-request-ds '{ds.Name}' {ds}");
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
				if (Surface != null) {
					try {
						switch (nccea.Action) {
							case NotifyCollectionChangedAction.Add:
								IncrementalUpdate(NotifyCollectionChangedAction.Add, CurrentLayout, ds, nccea.NewStartingIndex, nccea.NewItems);
								break;
							case NotifyCollectionChangedAction.Remove:
								IncrementalUpdate(NotifyCollectionChangedAction.Remove, CurrentLayout, ds, nccea.OldStartingIndex, nccea.OldItems);
								break;
							case NotifyCollectionChangedAction.Move:
							case NotifyCollectionChangedAction.Replace:
							case NotifyCollectionChangedAction.Reset:
							default:
								RenderComponents(CurrentLayout);
								break;
						}
					}
					catch(Exception ex) {
						_trace.Error($"{Name} DataSource_RefreshRequest.unhandled: {ex}");
					}
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
				try {
					if (cc is IProvideDataSourceRenderer ipdsr) {
						var ds = DataSources.SingleOrDefault(dds => dds.Name == ipdsr.Renderer.DataSourceName);
						if (ds != null) {
							ds.IsDirty = true;
						}
						RenderComponents(CurrentLayout);
					}
					else if (cc is IDataSourceRenderer idsr) {
						var ds = DataSources.SingleOrDefault(dds => dds.Name == idsr.DataSourceName);
						if (ds != null) {
							ds.IsDirty = true;
						}
						RenderComponents(CurrentLayout);
					}
					else {
						// dispatch other kinds of refresh requests
						if (rrea.Request == RefreshRequestType.LayoutDirty && rrea.Component is IRequireLayout) {
							ComponentRender(CurrentLayout, rrea);
						}
						else if (rrea.Request == RefreshRequestType.ValueDirty && rrea.Component is IRequireRender) {
							ComponentRender(CurrentLayout, rrea);
						}
						else if (rrea.Request == RefreshRequestType.TransformsDirty && cc is IRequireTransforms) {
							ComponentTransforms(CurrentLayout, rrea);
						}
					}
				}
				catch(Exception ex) {
					_trace.Verbose($"{Name} ChartComponent_RefreshRequest.unhandled: {ex}");
				}
			});
		}
		/// <summary>
		/// Manage dynamic legend updates.
		/// </summary>
		/// <param name="sender">Component sending update.</param>
		/// <param name="ldea">Current state of legend.</param>
		private void Ipld_LegendChanged(ChartComponent sender, LegendDynamicEventArgs ldea) {
			foreach (var prev in ldea.PreviousItems) {
				if (!ldea.CurrentItems.Contains(prev))
					LegendItems.Remove(prev);
			}
			foreach (var curr in ldea.CurrentItems) {
				if (!LegendItems.Contains(curr))
					LegendItems.Add(curr);
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
				_trace.Verbose($"OnApplyTemplate ({Width}x{Height}) {Surface} d:{DeferredEnter.Count} composition:{UniversalApiContract.v3.CompositionSupport.IsSupported}");
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
		/// Update limits for all elements passing the filter.
		/// </summary>
		/// <param name="pred">Component filter.</param>
		protected void Phase_AxisLimits(Func<ChartComponent,bool> pred) {
			Phase_AxisLimits(Components.Where(pred).Cast<IProvideValueExtents>());
		}
		/// <summary>
		/// Update limits of all enumerated elements.
		/// </summary>
		/// <param name="items"></param>
		protected void Phase_AxisLimits(IEnumerable<IProvideValueExtents> items) {
			foreach (var ipve in items) {
				var cc = ipve as ChartComponent;
				_trace.Verbose($"axis-limits '{cc.Name}' {cc}");
				var axis = Axes.SingleOrDefault((ax) => ipve.ValueAxisName == (ax as ChartComponent).Name);
				_trace.Verbose($"axis-limits y-axis:{axis} min:{ipve.Minimum:F3} max:{ipve.Maximum:F3}");
				if (axis != null) {
					axis.UpdateLimits(ipve.Maximum);
					axis.UpdateLimits(ipve.Minimum);
				}
			}
		}
		/// <summary>
		/// Phase: Layout.
		/// IRequireLayout, finalize rects, IRequireLayoutComplete.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_Layout(LayoutState ls) {
			foreach (IRequireLayout irl in Layout_All.Items /*Components.Where((cc2) => cc2 is IRequireLayout)*/) {
				_trace.Verbose($"layout {irl}");
				irl.Layout(ls.Layout);
			}
			// what's left is for the data series area
			_trace.Verbose($"remaining {ls.Layout.RemainingRect}");
			ls.Layout.FinalizeRects();
			foreach (IRequireLayoutComplete irlc in LayoutComplete_All.Items /*Components.Where((cc2) => cc2 is IRequireLayoutComplete)*/) {
				_trace.Verbose($"layout-complete {irlc}");
				var rect = ls.Layout.For(irlc as ChartComponent);
				var ctx = new DefaultLayoutCompleteContext(ls.Layout.Dimensions,rect, ls.Layout.RemainingRect);
				irlc.LayoutComplete(ctx);
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
		/// Phase: axes have seen all values let them render (IRequireRender).
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderAxes(LayoutState ls) {
			foreach (var irr in Render_AllAxes.Items /*Axes.Where((cc2) => cc2 is IRequireRender)*/) {
				var ctx = ls.RenderFor(irr as ChartComponent, Surface, Components, DataContext);
				_trace.Verbose(() => {
					var axis = irr as IChartAxis;
					return $"limits {(irr as ChartComponent).Name} ({axis.Minimum},{axis.Maximum}) r:{axis.Range} rect:{ctx.Area}";
				});
				irr.Render(ctx);
			}
		}
		/// <summary>
		/// Phase: after-axes-finalized.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_AxesFinalized(LayoutState ls) {
			foreach (var iraaf in AfterAxesFinalized.Items) {
				var cc = iraaf as ChartComponent;
				var ctx = ls.RenderFor(cc, Surface, Components, DataContext);
				_trace.Verbose($"axes-finalized {cc.Name} rect:{ctx.Area}");
				iraaf.AxesFinalized(ctx);
			}
		}
		/// <summary>
		/// Phase: render-components.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderComponents(LayoutState ls) {
			foreach (IRequireRender irr in Render_Components.Items /*Components.Where((cc2) => !(cc2 is IChartAxis) && (cc2 is IRequireRender) && !(cc2 is IRequireRenderPostAxesFinalized))*/) {
				var ctx = ls.RenderFor(irr as ChartComponent, Surface, Components, DataContext);
				irr.Render(ctx);
			}
		}
		/// <summary>
		/// Phase: transforms.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_Transforms(LayoutState ls) {
			foreach (IRequireTransforms irt in Transforms_All.Items /*Components.Where((cc2) => cc2 is IRequireTransforms)*/) {
				var ctx = ls.RenderFor(irt as ChartComponent, Surface, Components, DataContext);
				_trace.Verbose($"transforms {irt} {ctx.Area}");
				irt.Transforms(ctx);
			}
		}
		/// <summary>
		/// Phase: render post-axes-finalized.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void Phase_RenderPostAxesFinalized(LayoutState ls) {
			foreach (IRequireRender irr in Render_Components_PostAxis.Items /*Components.Where((cc2) => !(cc2 is IChartAxis) && (cc2 is IRequireRender) && (cc2 is IRequireRenderPostAxesFinalized))*/) {
				var ctx = ls.RenderFor(irr as ChartComponent, Surface, Components, DataContext);
				_trace.Verbose($"render-post-axes-finalized {(irr as ChartComponent).Name} rect:{ctx.Area}");
				irr.Render(ctx);
			}
		}
		#endregion
		#region incremental helpers
		/// <summary>
		/// Process incremental updates to a <see cref="DataSource"/>.
		/// </summary>
		/// <param name="ncca">The operation.  Only <see cref="NotifyCollectionChangedAction.Add"/> and <see cref="NotifyCollectionChangedAction.Remove"/> are supported.</param>
		/// <param name="ls">Current layout state.</param>
		/// <param name="ds">The <see cref="DataSource"/> that changed.</param>
		/// <param name="startIndex">Starting index of update.</param>
		/// <param name="items">Item(s) involved in the update.</param>
		protected void IncrementalUpdate(NotifyCollectionChangedAction ncca, LayoutState ls, DataSource ds, int startIndex, IList items) {
			_trace.Verbose($"incr-update {ncca} '{ds.Name}' {ds} @{startIndex}+{items.Count}");
			ls.Type = RenderType.Incremental;
			// Phase I: reset axes
			Phase_ResetAxes();
			// Phase II: Phase_Layout (skipped)
			// Phase III: this loop comprises the DSRP
			foreach (var irdsu in DataSourceUpdates_All.Items.Where(irdsu2 => irdsu2.UpdateSourceName == ds.Name) /* Components.Where(xx => xx is IRequireDataSourceUpdates irdsu2 && irdsu2.UpdateSourceName == ds.Name)*/) {
				var cc = irdsu as ChartComponent;
				_trace.Verbose($"incr {ncca} '{cc.Name}' {cc}");
				var ctx = ls.RenderFor(cc, Surface, Components, DataContext);
				switch (ncca) {
				case NotifyCollectionChangedAction.Add:
					irdsu.Add(ctx, startIndex, items);
					break;
				case NotifyCollectionChangedAction.Remove:
					irdsu.Remove(ctx, startIndex, items);
					break;
				}
			}
			// TODO above stage MAY generate additional update events, e.g. to ISeriesItemValues, that MUST be collected and distributed
			// TODO do it here and not allow things to directly connect to each other, so render pipeline stays under control
			Phase_AxisLimits(cc2 => cc2 is IRequireDataSourceUpdates irdsu2 && irdsu2.UpdateSourceName == ds.Name && cc2 is IProvideValueExtents);
			// Phase IV: render non-axis components (IRequireRender)
			// trigger render on other components since values they track may have changed
			foreach (IRequireRender irr in Render_NotAnAxis.Items.Where(cc2 => !(cc2 is IRequireDataSourceUpdates irdsu2 && irdsu2.UpdateSourceName == ds.Name)) /*Components.Where((cc2) => !(cc2 is IChartAxis) && !(cc2 is IRequireDataSourceUpdates irdsu2 && irdsu2.UpdateSourceName == ds.Name) && (cc2 is IRequireRender))*/) {
				var ctx = ls.RenderFor(irr as ChartComponent, Surface, Components, DataContext);
				irr.Render(ctx);
			}
			Phase_AxisLimits(cc2 => !(cc2 is IRequireDataSourceUpdates irdsu2 && irdsu2.UpdateSourceName == ds.Name) && cc2 is IProvideValueExtents);
			// Phase V: axis-finalized
			Phase_AxesFinalized(ls);
			// Phase VI: render axes
			Phase_RenderAxes(ls);
			// Phase VII: transforms
			Phase_Transforms(ls);
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
				ipld.LegendChanged -= Ipld_LegendChanged;
				ipld.LegendChanged += Ipld_LegendChanged;
			}
			// axis and DSRP are mutually-exclusive
			if (cc is IChartAxis ica) {
				Axes.Add(ica);
			}
			if (cc is IProvideDataSourceRenderer ipdsr) {
				Register(ipdsr.Renderer);
			} else if (cc is IDataSourceRenderer idsr) {
				Register(idsr);
			}
			foreach (var px in AllPhases) px.Enter(cc);
		}
		/// <summary>
		/// Common logic for leaving the chart.
		/// SHOULD be strict dual of ComponentEnter sequence.
		/// </summary>
		/// <param name="icelc">The context.</param>
		/// <param name="cc">The component leaving chart.</param>
		protected void ComponentLeave(IChartEnterLeaveContext icelc, ChartComponent cc) {
			foreach (var px in AllPhases) px.Leave(cc);
			if (cc is IProvideDataSourceRenderer ipdsr) {
				Unregister(ipdsr.Renderer);
			} else if (cc is IDataSourceRenderer idsr) {
				Unregister(idsr);
			}
			if (cc is IChartAxis ica) {
				Axes.Remove(ica);
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
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext) { Type = RenderType.TransformsOnly };
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
					Phase_AxisLimits(ValueExtents_DataSeries.Items);
				}
				var ctx = new DefaultRenderContext(Surface, Components, ls.LayoutDimensions, rect, ls.Layout.RemainingRect, DataContext) { Type = RenderType.Component };
				irr.Render(ctx);
				if (rrea.Axis != AxisUpdateState.None) {
					// axes MUST be re-evaluated because this thing changed.
					Phase_AxisLimits(ValueExtents_NotDataSeries.Items);
					Phase_AxesFinalized(ls);
					Phase_RenderPostAxesFinalized(ls);
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
		/// SETs <see cref="LayoutState.Type"/> to <see cref="RenderType.TransformsOnly"/>.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void TransformsLayout(LayoutState ls) {
			ls.Type = RenderType.TransformsOnly;
			ls.InitializeLayoutContext(Padding);
			_trace.Verbose($"transforms-only starting {ls.LayoutRect}");
			Phase_Layout(ls);
			Phase_Transforms(ls);
		}
		/// <summary>
		/// Perform a full layout and rendering pass.
		/// At least ONE component reported as dirty.
		/// The full rendering sequence is: axis-reset, layout, render, transforms.
		/// SETs <see cref="LayoutState.Type"/> to FALSE.
		/// </summary>
		/// <param name="ls">Layout state.</param>
		protected void FullLayout(LayoutState ls) {
			ls.Type = RenderType.Full;
			ls.InitializeLayoutContext(Padding);
			_trace.Verbose($"full starting {ls.LayoutRect}");
			// Phase I: reset axes
			Phase_ResetAxes();
			// Phase II: claim space (IRequireLayout)
			Phase_Layout(ls);
			// Phase III: data source rendering pipeline (IDataSourceRenderer)
			Phase_RenderDataSources(ls);
			Phase_AxisLimits(ValueExtents_DataSeries.Items);
			// Phase IV: render non-axis components (IRequireRender)
			Phase_RenderComponents(ls);
			Phase_AxisLimits(ValueExtents_NotDataSeries.Items);
			// Phase V: axes finalized
			Phase_AxesFinalized(ls);
			// Phase VI: post-axes finalized
			Phase_RenderPostAxesFinalized(ls);
			// Phase VII: render axes (IRequireRender)
			Phase_RenderAxes(ls);
			// Phase VIII: configure all transforms
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
