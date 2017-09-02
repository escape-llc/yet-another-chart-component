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
		#endregion
		#region ctor
		public Chart() :base() {
			DefaultStyleKey = typeof(Chart);
			Components = new ObservableCollection<ChartComponent>();
			Components.CollectionChanged += new NotifyCollectionChangedEventHandler(OnComponentsChanged);
			LayoutUpdated += new EventHandler<object>(OnLayoutUpdated);
			DataContextChanged += Chart_DataContextChanged;
		}
		#endregion
		#region evhs
		private void Chart_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {
			foreach(var cc in Components) {
				cc.DataContext = args.NewValue;
			}
		}
		protected override void OnApplyTemplate() {
			Surface = (Canvas)TreeHelper.TemplateFindName("PART_Canvas", this);
			_trace.Verbose($"OnApplyTemplate {Width}x{Height} {Surface}");
		}
		private void OnLayoutUpdated(object sender, object e) {
			_trace.Verbose($"OnLayoutUpdated {ActualWidth}x{ActualHeight}");
			if (!double.IsNaN(ActualWidth) && !double.IsNaN(ActualHeight)) {
				RenderComponents();
			}
		}
		void OnComponentsChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.OldItems != null) {
				foreach (ChartComponent cc in e.OldItems) {
					_trace.Verbose($"leave {cc}");
					cc.RefreshRequest -= Cc_RefreshRequest;
					cc.Leave();
				}
			}
			if (e.NewItems != null) {
				foreach (ChartComponent cc in e.NewItems) {
					_trace.Verbose($"enter {cc}");
					cc.RefreshRequest += Cc_RefreshRequest;
					cc.DataContext = DataContext;
					cc.Enter();
				}
			}
			InvalidateMeasure();
		}
		private void Cc_RefreshRequest(ChartComponent cc) {
			_trace.Verbose($"refresh-request {cc}");
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
		private void RenderComponents() {
			var ctx = new DefaultRenderContext(Components) { Dimensions = new Size(ActualWidth, ActualHeight), DataContext = DataContext };
			foreach (ChartComponent cc in Components) {
				_trace.Verbose($"render {cc}");
				if(cc is IChartAxis) {
					(cc as IChartAxis).ResetLimits();
				}
				cc.Render(ctx);
			}
		}
		#endregion
	}
	#endregion
}
