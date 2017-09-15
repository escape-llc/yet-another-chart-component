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
	public class Chart : Control {
		static LogTools.Flag _trace = LogTools.Add("Chart", LogTools.Level.Verbose);
		#region properties
		/// <summary>
		/// The list of data sources.
		/// </summary>
		public DependencyObjectCollection DataSources { get; private set; }
		/// <summary>
		/// The chart's visual components.
		/// Obtained from the XAML and programmatic.
		/// </summary>
		public ChartComponentCollection Components { get; private set; }
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
		#region ctor
		/// <summary>
		/// Ctor.
		/// Establish default values.
		/// </summary>
		public Chart() :base() {
			DefaultStyleKey = typeof(Chart);
			DataSources = new DependencyObjectCollection();
			DataSources.VectorChanged += DataSources_VectorChanged;
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
		private void DataSources_VectorChanged(Windows.Foundation.Collections.IObservableVector<DependencyObject> sender, Windows.Foundation.Collections.IVectorChangedEventArgs ivcea) {
			_trace.Verbose($"DataSourcesChanged {ivcea.CollectionChange} {ivcea.Index}");
			if(Surface != null) {
				RenderComponents(LastLayout);
			}
		}
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
		/// Obtain UI elements from the control template.
		/// Happens Before Chart_LayoutUpdated.
		/// </summary>
		protected override void OnApplyTemplate() {
			Surface = (Canvas)TreeHelper.TemplateFindName("PART_Canvas", this);
			_trace.Verbose($"OnApplyTemplate ({Width}x{Height}) {Surface} d:{DeferredEnter.Count}");
			var celc = new DefaultEnterLeaveContext(Surface, Components, LastLayout, Rect.Empty, Rect.Empty, DataContext);
			foreach(var cc in DeferredEnter) {
				EnterComponent(celc, cc);
			}
			DeferredEnter.Clear();
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
				if (LastLayout.Width == sz.Width && LastLayout.Height == sz.Height) return;
				RenderComponents(sz);
				LastLayout = sz;
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
					cc.RefreshRequest -= Cc_RefreshRequest;
					LeaveComponent(celc, cc);
				}
			}
			if (e.NewItems != null) {
				foreach (ChartComponent cc in e.NewItems) {
					_trace.Verbose($"enter '{cc.Name}' {cc}");
					cc.RefreshRequest += Cc_RefreshRequest;
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
		private void Cc_RefreshRequest(ChartComponent cc) {
			_trace.Verbose($"refresh-request '{cc.Name}' {cc}");
		}
		#endregion
		#region preset brushes
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
		#region helpers
		/// <summary>
		/// Common logic for component entering the chart.
		/// </summary>
		/// <param name="icelc"></param>
		/// <param name="cc"></param>
		protected void EnterComponent(IChartEnterLeaveContext icelc, ChartComponent cc) {
			cc.Enter(icelc);
			if (cc is IChartAxis) {
				Axes.Add(cc as IChartAxis);
			} else if (cc is DataSeries) {
				var ds = cc as DataSeries;
				Series.Add(ds);
				if (ds is IDataSourceRenderer) {
					var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == ds.DataSourceName);
					if (source != null) {
						source.Register(ds as IDataSourceRenderer);
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
			if (cc is IChartAxis) {
				Axes.Remove(cc as IChartAxis);
			} else if (cc is DataSeries) {
				var ds = cc as DataSeries;
				if (ds is IDataSourceRenderer) {
					var source = DataSources.Cast<DataSource>().SingleOrDefault<DataSource>((dds) => dds.Name == ds.DataSourceName);
					if (source != null) {
						source.Unregister(ds as IDataSourceRenderer);
					}
				}
				Series.Remove(ds);
			}
			cc.Leave(icelc);
		}
		/// <summary>
		/// Adjust layout and transforms based on size change.
		/// </summary>
		/// <param name="sz">Dimensions.</param>
		protected void TransformsOnly(Size sz) {
			var initialRect = new Rect(Padding.Left, Padding.Top, sz.Width - Padding.Left - Padding.Right, sz.Height - Padding.Top - Padding.Bottom);
			var inner = new Size(initialRect.Width, initialRect.Height);
			var dlc = new DefaultLayoutContext(inner, initialRect);
			_trace.Verbose($"transforms-only starting {initialRect}");
			foreach (var cc in Components) {
				_trace.Verbose($"layout {cc}");
				cc.Layout(dlc);
			}
			_trace.Verbose($"transforms-only remaining:{dlc.RemainingRect}");
			dlc.FinalizeRects();
			foreach (var axis in Axes) {
				var cc = axis as ChartComponent;
				var rect = dlc.For(cc);
				_trace.Verbose($"transforms-only {cc.Name} {axis} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				cc.Transforms(ctx);
			}
			foreach (DataSeries cc in Series) {
				var rect = dlc.For(cc);
				_trace.Verbose($"transforms-only {cc} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				cc.Transforms(ctx);
			}
		}
		/// <summary>
		/// Perform a full layout and rendering pass.
		/// At least ONE component reported as dirty.
		/// The full rendering sequence is: axis-reset, layout, render, transforms.
		/// </summary>
		/// <param name="sz">Dimensions.</param>
		protected void FullLayout(Size sz) {
			foreach (var axis in Axes) {
				_trace.Verbose($"reset {(axis as ChartComponent).Name} {axis}");
				axis.ResetLimits();
			}
			var initialRect = new Rect(Padding.Left, Padding.Top, sz.Width - Padding.Left - Padding.Right, sz.Height - Padding.Top - Padding.Bottom);
			var inner = new Size(initialRect.Width, initialRect.Height);
			var dlc = new DefaultLayoutContext(inner, initialRect);
			_trace.Verbose($"starting {initialRect}");
			foreach (var cc in Components) {
				_trace.Verbose($"layout {cc}");
				cc.Layout(dlc);
			}
			// what's left is for the data series area
			_trace.Verbose($"remaining {dlc.RemainingRect}");
			dlc.FinalizeRects();
			// render the data sources -> series -> axes
			foreach(DataSource ds in DataSources) {
				ds.Render();
			}
			// axes have seen all values; render+transform them now
			foreach (var axis in Axes) {
				var acc = axis as ChartComponent;
				var scale = (axis.Type == AxisType.Value ? inner.Height : inner.Width) / axis.Range;
				var rect = dlc.For(acc);
				_trace.Verbose($"limits {acc.Name} ({axis.Minimum},{axis.Maximum}) r:{axis.Range} rect:{rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				acc.Render(ctx);
				acc.Transforms(ctx);
			}
			// configure series transforms now axes are configured
			foreach (DataSeries cc in Series) {
				var rect = dlc.For(cc);
				_trace.Verbose($"transforms {cc} {rect}");
				var ctx = new DefaultRenderContext(Surface, Components, inner, rect, dlc.RemainingRect, DataContext);
				cc.Transforms(ctx);
			}
		}
		/// <summary>
		/// Determine what kind of render is required, and run it.
		/// If anything is dirty, full layout, else adjust transforms.
		/// Once all components are "clean" only the visual transforms are updated; no data traversal is done.
		/// </summary>
		private void RenderComponents(Size sz) {
			_trace.Verbose($"render-components {sz.Width}x{sz.Height}");
			if (DataSources.Cast<DataSource>().Any((cx) => cx.IsDirty)) {
				FullLayout(sz);
			} else {
				TransformsOnly(sz);
			}
		}
		#endregion
	}
	#endregion
}
