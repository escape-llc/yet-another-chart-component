using eScape.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Data;

namespace eScapeLLC.UWP.Charts {
	#region DefaultRenderContext
	public class DefaultRenderContext : IChartRenderContext {
		ObservableCollection<ChartComponent> Components { get; set; }
		public DefaultRenderContext(ObservableCollection<ChartComponent> components) { Components = components; }
		public Size Dimensions { get; set; }
		public object DataContext { get; set; }
		public ChartComponent Find(string name) {
			return Components.SingleOrDefault((cx) => cx.Name == name);
		}
	}
	public class DefaultEnterLeaveContext : DefaultRenderContext, IChartEnterLeaveContext {
		Canvas Surface { get; set; }
		public DefaultEnterLeaveContext(Canvas surface, ObservableCollection<ChartComponent> components) :base(components){ Surface = surface; }
		public void Add(FrameworkElement fe) {
			Surface.Children.Add(fe);
		}
		public void Remove(FrameworkElement fe) {
			Surface.Children.Remove(fe);
		}
	}
	#endregion
	#region Chart
	/// <summary>
	/// The chart.
	/// </summary>
	public class Chart : Control {
		static LogTools.Flag _trace = LogTools.Add("Chart", LogTools.Level.Verbose);
		#region properties
		public ObservableCollection<ChartComponent> Components { get; private set; }
		protected Canvas Surface { get; set; }
		protected List<IChartAxis> Axes { get; set; }
		protected List<DataSeries> Series { get; set; }
		protected List<ChartComponent> DeferredEnter{ get; set; }
		#endregion
		#region ctor
		public Chart() :base() {
			DefaultStyleKey = typeof(Chart);
			Components = new ObservableCollection<ChartComponent>();
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
		/// Propagate data context changes to components.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void Chart_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
			if (args.NewValue != DataContext) {
				_trace.Verbose($"DataContextChanged {args.NewValue}");
				foreach (var cc in Components) {
					cc.DataContext = args.NewValue;
				}
			}
			else {
				foreach (var cc in Components) {
					if (cc.DataContext != args.NewValue) {
						_trace.Verbose($"DataContextChanged {cc} {args.NewValue}");
						cc.DataContext = args.NewValue;
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
			var celc = new DefaultEnterLeaveContext(Surface, Components);
			foreach(var cc in DeferredEnter) {
				EnterComponent(celc, cc);
			}
		}
		/// <summary>
		/// Reconfigure components in response to layout change.
		/// Happens After OnApplyTemplate.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Chart_LayoutUpdated(object sender, object e) {
			_trace.Verbose($"LayoutUpdated ({ActualWidth}x{ActualHeight})");
			if (!double.IsNaN(ActualWidth) && !double.IsNaN(ActualHeight)) {
				// TODO will need to recalculate axes if the dimensions have changed
				if (Components.Any((cx) => cx.Dirty)) {
					RenderComponents();
				}
			}
		}
		void EnterComponent(IChartEnterLeaveContext icelc, ChartComponent cc) {
			cc.Enter(icelc);
			if (cc is IChartAxis) {
				Axes.Add(cc as IChartAxis);
			} else if (cc is DataSeries) {
				Series.Add(cc as DataSeries);
			}
		}
		/// <summary>
		/// Reconfigure components that enter and leave.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var celc = new DefaultEnterLeaveContext(Surface, Components);
			if (e.OldItems != null) {
				foreach (ChartComponent cc in e.OldItems) {
					_trace.Verbose($"leave '{cc.Name}' {cc}");
					cc.RefreshRequest -= Cc_RefreshRequest;
					cc.Leave(celc);
					if(cc is IChartAxis) {
						Axes.Remove(cc as IChartAxis);
					}
					else if(cc is DataSeries) {
						Series.Remove(cc as DataSeries);
					}
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
		/// Iterate the components for rendering.
		/// </summary>
		private void RenderComponents() {
			var ctx = new DefaultRenderContext(Components) { Dimensions = new Size(ActualWidth, ActualHeight), DataContext = DataContext };
			foreach(var axis in Axes) {
				_trace.Verbose($"reset {(axis as ChartComponent).Name} {axis}");
				axis.ResetLimits();
			}
			foreach (DataSeries cc in Series) {
				_trace.Verbose($"render {cc}");
				cc.Render(ctx);
			}
			// TODO reconfigure series transforms now axes have limits built
			foreach (var axis in Axes) {
				var scale = (axis.Type == AxisType.Value ? ActualHeight : ActualWidth) / axis.Range;
				_trace.Verbose($"limits {(axis as ChartComponent).Name} ({axis.Minimum},{axis.Maximum}) r:{axis.Range} s:{scale:F3}");
				(axis as ChartComponent).Render(ctx);
			}
		}
		#endregion
	}
	#endregion
}
