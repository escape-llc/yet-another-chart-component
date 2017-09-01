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

namespace eScapeLLC.UWP.Charts {
	public interface IChartRenderContext {
	}
	public delegate void RefreshRequestEventHandler(ChartComponent cc);
	public abstract class ChartComponent : FrameworkElement {
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
		private static void OnDataSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			DataSeries ds = d as DataSeries;
			DetachOldDataSourceCollectionChangedListener(ds, e.OldValue);
			AttachDataSourceCollectionChangedListener(ds, e.NewValue);
			ds.ProcessData();
		}
		private static void DetachOldDataSourceCollectionChangedListener(DataSeries chart, object dataSource) {
			if (dataSource != null && dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= chart.OnDataSourceCollectionChanged;
			}
		}
		private static void AttachDataSourceCollectionChangedListener(DataSeries chart, object dataSource) {
			if (dataSource != null && dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(chart.OnDataSourceCollectionChanged);
			}
		}
		private void OnDataSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			// TODO: implement intelligent mechanism to hanlde multiple changes in one batch
			ProcessData();
		}
		#endregion
		#region extensions
		protected abstract void ProcessData();
		#endregion
	}
	public class ValueAxis : ChartComponent {
		public override void Enter() {
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
	}
	public class CategoryAxis: ChartComponent {
		public override void Enter() {
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
	}
	public class LineSeries : DataSeries {
		public override void Enter() {
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
		protected override void ProcessData() {
			Refresh();
		}
	}
	public class ColumnSeries : DataSeries {
		public override void Enter() {
		}
		public override void Leave() {
		}
		public override void Render(IChartRenderContext icrc) {
		}
		protected override void ProcessData() {
			Refresh();
		}
	}
	/// <summary>
	/// The chart.
	/// </summary>
	public class Chart : Control, IChartRenderContext {
		static LogTools.Flag _trace = LogTools.Add("Chart", LogTools.Level.Verbose);
		#region properties
		public ObservableCollection<ChartComponent> Components { get; private set; }
		#endregion
		#region ctor
		public Chart() {
			DefaultStyleKey = typeof(Chart);
			Components = new ObservableCollection<ChartComponent>();
			Components.CollectionChanged += new NotifyCollectionChangedEventHandler(OnComponentsChanged);
			LayoutUpdated += new EventHandler<object>(OnLayoutUpdated);
			Loaded += Chart_Loaded;
		}

		private void Chart_Loaded(object sender, RoutedEventArgs e) {
			_trace.Verbose($"OnLoaded {Width}x{Height}  {Components.Count}");
		}
		#endregion
		#region evhs
		private void OnLayoutUpdated(object sender, object e) {
			_trace.Verbose($"OnLayoutUpdated {Width}x{Height}");
			RenderComponents();
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
			foreach (ChartComponent cc in Components) {
				_trace.Verbose($"render {cc}");
				cc.Render(this);
			}
		}
		#endregion
	}
}
