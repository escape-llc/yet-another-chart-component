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

namespace eScapeLLC.UWP.Charts {
	public interface IChartAxis {
		double For(double value);
	}
	public interface IChartRenderContext {
		Size Dimensions { get; }
		ChartComponent Find(String name);
	}
	public class DefaultRenderContext : IChartRenderContext {
		ObservableCollection<ChartComponent> Components { get; set; }
		public DefaultRenderContext(ObservableCollection<ChartComponent> components) { Components = components; }
		public Size Dimensions { get; set; }
		public ChartComponent Find(string name) {
			return Components.SingleOrDefault((cx) => cx.Name == name);
		}
	}
	public delegate void RefreshRequestEventHandler(ChartComponent cc);
	#region ChartComponent
	public abstract class ChartComponent : FrameworkElement {
		protected ChartComponent() { }
		/// <summary>
		/// Render the component.
		/// </summary>
		/// <param name="icrc"></param>
		public abstract void Render(IChartRenderContext icrc);
		/// <summary>
		/// Component is entering the chart.
		/// </summary>
		public abstract void Enter();
		/// <summary>
		/// Component is leaving the chart.
		/// </summary>
		public abstract void Leave();
		/// <summary>
		/// Listen for requests to update this component.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		/// <summary>
		/// Invoke the refresh request event.
		/// </summary>
		protected void Refresh() { RefreshRequest?.Invoke(this); }
	}
	#endregion
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// </summary>
	public abstract class DataSeries : ChartComponent {
		#region data source
		/// <summary>
		/// Identifies <see cref="DataSource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
			"DataSource", typeof(System.Collections.IEnumerable), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(OnDataSourcePropertyChanged))
		);
		/// <summary>
		/// Gets or sets data source for the chart.
		/// This is a dependency property.
		/// </summary>
		public System.Collections.IEnumerable DataSource {
			get { return (System.Collections.IEnumerable)GetValue(DataSourceProperty); }
			set { SetValue(DataSourceProperty, value); }
		}
		private static void OnDataSourcePropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = dobj as DataSeries;
			DetachOldDataSourceCollectionChangedListener(ds, dpcea.OldValue);
			AttachDataSourceCollectionChangedListener(ds, dpcea.NewValue);
			ds.ProcessData(dpcea.Property);
		}
		private static void DetachOldDataSourceCollectionChangedListener(DataSeries ds, object dataSource) {
			if (dataSource != null && dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= ds.OnDataSourceCollectionChanged;
			}
		}
		private static void AttachDataSourceCollectionChangedListener(DataSeries ds, object dataSource) {
			if (dataSource != null && dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ds.OnDataSourceCollectionChanged);
			}
		}
		private void OnDataSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			// TODO: implement intelligent mechanism to hanlde multiple changes in one batch
			ProcessData(null);
		}
		public String ValueMemberPath { get; set; }
		#endregion
		#region properties
		public String ValueAxisName { get; set; }
		public String CategoryAxisName { get; set; }
		protected IChartAxis ValueAxis { get; set; }
		protected IChartAxis CategoryAxis { get; set; }
		#endregion
		#region extensions
		protected abstract void ProcessData(DependencyProperty dp);
		#endregion
	}
	#endregion
	#region ValueAxis
	public class ValueAxis : ChartComponent, IChartAxis {
		public override void Enter() {
		}
		public double For(double value) {
			throw new NotImplementedException();
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
	}
	#endregion
	#region CategoryAxis
	public class CategoryAxis: ChartComponent, IChartAxis {
		public override void Enter() {
		}
		public double For(double value) {
			throw new NotImplementedException();
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
	}
	#endregion
	#region LineSeries
	public class LineSeries : DataSeries {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Verbose);
		public Polyline Segments{ get; set; }
		public override void Enter() {
			_trace.Verbose($"enter v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			Segments = new Polyline();
		}
		public override void Leave() {
			_trace.Verbose($"leave v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
		}
		public override void Render(IChartRenderContext icrc) {
			if(ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)){
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(CategoryAxisName)) {
				CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
			}
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			_trace.Verbose($"render v:{ValueAxis} c:{ValueAxis} d:{DataSource}");
		}
		protected override void ProcessData(DependencyProperty dp) {
			_trace.Verbose($"process-data v:{ValueAxisName} c:{CategoryAxisName} {dp}");
			Refresh();
		}
	}
	#endregion
	#region ColumnSeries
	public class ColumnSeries : DataSeries {
		public Path Segments { get; set; }
		public override void Enter() {
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
		protected override void ProcessData(DependencyProperty dp) {
			Refresh();
		}
	}
	#endregion
	#region TreeHelper
	public static class TreeHelper {
		/// <summary>
		/// Finds object in control's template by it's name.
		/// </summary>
		/// <param name="name">Objects name.</param>
		/// <param name="templatedParent">Templated parent.</param>
		/// <returns>Object reference if found, null otherwise.</returns>
		public static object TemplateFindName(string name, FrameworkElement templatedParent) {
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(templatedParent); i++) {
				DependencyObject child = VisualTreeHelper.GetChild(templatedParent, i);
				if (child is FrameworkElement) {
					if (((FrameworkElement)child).Name == name) {
						return child;
					} else {
						object subChild = TreeHelper.TemplateFindName(name, (FrameworkElement)child);
						if (subChild != null && subChild is FrameworkElement && ((FrameworkElement)subChild).Name == name) {
							return subChild;
						}
					}
				}
			}
			return null;
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
		}
		#endregion
		#region evhs
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
			var ctx = new DefaultRenderContext(Components) { Dimensions = new Size(ActualWidth, ActualHeight) };
			foreach (ChartComponent cc in Components) {
				_trace.Verbose($"render {cc}");
				cc.Render(ctx);
			}
		}
		#endregion
	}
	#endregion
}
