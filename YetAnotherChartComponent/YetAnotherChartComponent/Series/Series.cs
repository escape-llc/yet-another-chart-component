using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// </summary>
	public abstract class DataSeries : ChartComponent, IProvideValueExtents, IProvideCategoryExtents {
		#region DPs
		/// <summary>
		/// DataSourceName DP.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			"DataSourceName", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			"ValuePath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// CategoryPath DP.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			"CategoryPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// CategoryLabelPath DP.
		/// </summary>
		public static readonly DependencyProperty CategoryLabelPathProperty = DependencyProperty.Register(
			"CategoryLabelPath", typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(DataSeriesPropertyChanged))
		);
		/// <summary>
		/// Generic DP property change handler.
		/// Calls DataSeries.ProcessData().
		/// </summary>
		/// <param name="d"></param>
		/// <param name="dpcea"></param>
		protected static void DataSeriesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs dpcea) {
			DataSeries ds = d as DataSeries;
			ds.Dirty = true;
			ds.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
		}
		#endregion
		#region properties
		/// <summary>
		/// The name of the data source in the DataSources collection.
		/// </summary>
		public String DataSourceName { get { return (String)GetValue(DataSourceNameProperty); } set { SetValue(DataSourceNameProperty, value); } }
		/// <summary>
		/// Binding path to the category axis value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryPath { get { return (String)GetValue(CategoryPathProperty); } set { SetValue(CategoryPathProperty, value); } }
		/// <summary>
		/// Binding path to the category axis label.
		/// If multiple series are presenting the same data source, only one MUST HAVE this property set.
		/// If CategoryMemberPath is NULL, the data-index is used.
		/// MAY be NULL, in which case no labels are used on category axis.
		/// </summary>
		public String CategoryLabelPath { get { return (String)GetValue(CategoryLabelPathProperty); } set { SetValue(CategoryLabelPathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
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
		/// The minimum value seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double Minimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum value seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double Maximum { get; protected set; } = double.NaN;
		/// <summary>
		/// The minimum category (value) seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double CategoryMinimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum category (value) seen.
		/// Only valid after ProcessData() is called.
		/// </summary>
		public double CategoryMaximum { get; protected set; } = double.NaN;
		/// <summary>
		/// Range of the values or NaN if ProcessData() was never called.
		/// </summary>
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum + 1; } }
		/// <summary>
		/// Whether to clip geometry to the data region.
		/// Default value is true.
		/// </summary>
		public bool ClipToDataRegion { get; set; } = true;
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
		/// Provide a readable name for DP update diagnostics.
		/// </summary>
		/// <param name="dp"></param>
		/// <returns></returns>
		protected virtual String DPName(DependencyProperty dp) {
			if (dp == ValuePathProperty) return "ValuePath";
			else if (dp == CategoryPathProperty) return "CategoryPath";
			else if (dp == CategoryLabelPathProperty) return "CategoryLabelPath";
			else if (dp == DataSourceNameProperty) return "DataSourceName";
			return dp.ToString();
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartRenderContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			}
			else {
				if(icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
			if (CategoryAxis == null && !String.IsNullOrEmpty(CategoryAxisName)) {
				CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"Category axis '{CategoryAxisName}' was not found", new[] { nameof(CategoryAxis), nameof(CategoryAxisName) }));
				}
			}
		}
		/// <summary>
		/// Update value and category limits.
		/// If a value is NaN, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="vx">Category. MAY be NaN.</param>
		/// <param name="vy">Value.  MAY be NaN.</param>
		protected void UpdateLimits(double vx, double vy) {
			if (double.IsNaN(Minimum) || vy < Minimum) { Minimum = vy; }
			if (double.IsNaN(Maximum) || vy > Maximum) { Maximum = vy; }
			if (double.IsNaN(CategoryMinimum) || vx < CategoryMinimum) { CategoryMinimum = vx; }
			if (double.IsNaN(CategoryMaximum) || vx > CategoryMaximum) { CategoryMaximum = vx; }
		}
		/// <summary>
		/// Update value and category limits.
		/// Optimized for multiple y-axis values.
		/// If a value is NaN, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="vx"></param>
		/// <param name="vys"></param>
		protected void UpdateLimits(double vx, params double[] vys) {
			if (double.IsNaN(CategoryMinimum) || vx < CategoryMinimum) { CategoryMinimum = vx; }
			if (double.IsNaN(CategoryMaximum) || vx > CategoryMaximum) { CategoryMaximum = vx; }
			foreach(var vy in vys) {
				if (double.IsNaN(Minimum) || vy < Minimum) { Minimum = vy; }
				if (double.IsNaN(Maximum) || vy > Maximum) { Maximum = vy; }
			}
		}
		/// <summary>
		/// Reset the value and category limits.
		/// Sets Dirty = true.
		/// </summary>
		protected void ResetLimits() {
			Minimum = double.NaN; Maximum = double.NaN;
			CategoryMinimum = double.NaN; CategoryMaximum = double.NaN;
			Dirty = true;
		}
		#endregion
	}
	#endregion
	#region ItemState
	/// <summary>
	/// Basic item state.
	/// This is used when there are multiple paths generated, so they can be re-adjusted in Transforms et al.
	/// </summary>
	public class ItemState {
		/// <summary>
		/// The generated path.
		/// </summary>
		public Path Path { get; set; }
		/// <summary>
		/// The index of this value from data source.
		/// </summary>
		public int Index { get; set; }
		/// <summary>
		/// The x value.
		/// </summary>
		public double XValue { get; set; }
		/// <summary>
		/// The y value.
		/// </summary>
		public double YValue { get; set; }
	}
	/// <summary>
	/// Item state with transformation matrix.
	/// </summary>
	public class ItemState_Matrix : ItemState {
		/// <summary>
		/// Alternate matrix for the M matrix
		/// </summary>
		public Matrix World { get; set; }
	}
	/// <summary>
	/// Item state with matrix and geometry.
	/// </summary>
	/// <typeparam name="G">Type of geometry.</typeparam>
	public class ItemState_MatrixAndGeometry<G> : ItemState_Matrix where G: Geometry {
		/// <summary>
		/// The geometry.
		/// If you can use Path.Data to reference geometry, choose <see cref="ItemState_Matrix"/> or <see cref="ItemState"/> instead.
		/// </summary>
		public G Geometry { get; set; }
	}
	#endregion
}
