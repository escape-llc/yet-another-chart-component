using eScape.Core;
using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
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
			"DataSource", typeof(System.Collections.IEnumerable), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSourcePropertyChanged))
		);
		private static void DataSourcePropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = dobj as DataSeries;
			DetachDataSourceCollectionChangedListener(ds, dpcea.OldValue);
			AttachDataSourceCollectionChangedListener(ds, dpcea.NewValue);
			ds.ProcessData(dpcea.Property);
		}
		private static void DetachDataSourceCollectionChangedListener(DataSeries ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= ds.DataSourceCollectionChanged;
			}
		}
		private static void AttachDataSourceCollectionChangedListener(DataSeries ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ds.DataSourceCollectionChanged);
			}
		}
		private void DataSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			ProcessData(DataSourceProperty);
		}
		#endregion
		#region category member path
		public static readonly DependencyProperty CategoryMemberPathProperty = DependencyProperty.Register(
			"CategoryMemberPath", typeof(string), typeof(DataSeries), new PropertyMetadata(new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		private static void DataSeriesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = d as DataSeries;
			ds.ProcessData(dpcea.Property);
		}
		#endregion
		#region value member path
		public static readonly DependencyProperty ValueMemberPathProperty = DependencyProperty.Register(
			"ValueMemberPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		#endregion
		#region properties
		/// <summary>
		/// Data source for the series.
		/// </summary>
		public System.Collections.IEnumerable DataSource { get { return (System.Collections.IEnumerable)GetValue(DataSourceProperty); } set { SetValue(DataSourceProperty, value); } }
		/// <summary>
		/// Binding path to the category axis value.
		/// </summary>
		public String CategoryMemberPath { get { return (String)GetValue(CategoryMemberPathProperty); } set { SetValue(CategoryMemberPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValueMemberPath { get { return (String)GetValue(ValueMemberPathProperty); } set { SetValue(ValueMemberPathProperty, value); } }
		/// <summary>
		/// Component name of value axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Component name of category axis.
		/// Referenced component MUST implement IChartAxis.
		/// </summary>
		public String CategoryAxisName { get; set; }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		#endregion
		#region helpers
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(CategoryAxisName)) {
				CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
			}
		}
		#endregion
		#region extensions
		/// <summary>
		/// Iterate through the DataSource and compute visuals.
		/// </summary>
		/// <param name="dp">Triggering DP; if unknown, SHOULD be DataSourceProperty.</param>
		protected abstract void ProcessData(DependencyProperty dp);
		#endregion
	}
	#endregion
	#region LineSeries
	/// <summary>
	/// Data series that generates a Polyline visual.
	/// </summary>
	public class LineSeries : DataSeries {
		static LogTools.Flag _trace = LogTools.Add("LineSeries", LogTools.Level.Verbose);
		public Polyline Segments { get; set; }
		public override void Enter() {
			_trace.Verbose($"enter v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
			Segments = new Polyline();
		}
		public override void Leave() {
			_trace.Verbose($"leave v:{ValueAxisName} c:{ValueAxisName} d:{DataSource}");
		}
		public override void Render(IChartRenderContext icrc) {
			EnsureAxes(icrc);
			_trace.Verbose($"render v:{ValueAxis} c:{CategoryAxis} d:{DataSource}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			ProcessData(DataSourceProperty);
		}
		protected override void ProcessData(DependencyProperty dp) {
			_trace.Verbose($"process-data v:{ValueAxis} c:{CategoryAxis} {dp}");
			if (ValueAxis == null || CategoryAxis == null || DataSource == null) return;
			if (String.IsNullOrEmpty(ValueMemberPath)) return;
			var by = new BindingEvaluator(ValueMemberPath);
			var bx = !String.IsNullOrEmpty(CategoryMemberPath) ? new BindingEvaluator(CategoryMemberPath) : null;
			int ix = 0;
			Segments.Points.Clear();
			foreach (var vx in DataSource) {
				// TODO handle datetime et al values that aren't double
				var valuey = (double)by.Eval(vx);
				var valuex = bx != null ? (double)bx.Eval(vx) : ix;
				var mappedy = ValueAxis.For(valuey);
				var mappedx = CategoryAxis.For(valuex);
				_trace.Verbose($"[{ix}:{mappedx}] {valuey}:{mappedy}");
				Segments.Points.Add(new Point(mappedx, mappedy));
				ix++;
			}
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
}
