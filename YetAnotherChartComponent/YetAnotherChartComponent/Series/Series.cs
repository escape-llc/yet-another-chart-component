﻿using eScape.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// <para/>
	/// This class commits to a <see cref="DataSourceName"/> only.
	/// <para/>
	/// The <see cref="Chart"/> class keys on this class for certain render pipeline phases.
	/// </summary>
	public abstract class DataSeries : ChartComponent {
		static LogTools.Flag _trace = LogTools.Add("DataSeries", LogTools.Level.Error);
		#region DPs
		/// <summary>
		/// Identifies <see cref="DataSourceName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			nameof(DataSourceName), typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region properties
		/// <summary>
		/// The name of the data source in the DataSources collection.
		/// </summary>
		public String DataSourceName { get { return (String)GetValue(DataSourceNameProperty); } set { SetValue(DataSourceNameProperty, value); } }
		/// <summary>
		/// Whether to clip geometry to the data region.
		/// Default value is true.
		/// </summary>
		public bool ClipToDataRegion { get; set; } = true;
		#endregion
		#region helpers
		/// <summary>
		/// Invoke the items update event, trapping and reporting any <see cref="Exception"/> via <see cref="IChartErrorInfo"/>.
		/// </summary>
		/// <param name="eh">The event handler to invoke.</param>
		/// <param name="icrc">Passed to args.</param>
		/// <param name="ncca">Passed to args.</param>
		/// <param name="startAt">Passed to args.</param>
		/// <param name="reproc">Passed to args.</param>
		protected void RaiseItemsUpdated(EventHandler<SeriesItemUpdateEventArgs> eh, IChartRenderContext icrc, NotifyCollectionChangedAction ncca, int startAt, IEnumerable<ISeriesItem> reproc) {
			try {
				var args = new SeriesItemUpdateEventArgs(icrc, ncca, startAt, reproc);
				eh?.Invoke(this, args);
			} catch (Exception ex) {
				_trace.Error($"{NameOrType()} SeriesItemUpdate({ncca}).unhandled: {ex.Message}");
				if (icrc is IChartErrorInfo icei) {
					icei.Report(new ChartValidationResult(NameOrType(), $"SeriesItemUpdate({ncca}) failed: {ex.Message}"));
				}
			}
		}
		/// <summary>
		/// Provide a readable name for DP update diagnostics.
		/// </summary>
		/// <param name="dp"></param>
		/// <returns></returns>
		protected virtual String DPName(DependencyProperty dp) {
			if (dp == DataSourceNameProperty) return nameof(DataSourceName);
			return dp.ToString();
		}
		/// <summary>
		/// Take the actual value from the source and coerce it to the double type, until we get full polymorphism on the y-value.
		/// <para/>
		/// Currently handles <see cref="double"/>, <see cref="int"/>, <see cref="short"/>,and Nullable{double,int,short} types.
		/// </summary>
		/// <param name="item">Source instance.</param>
		/// <param name="be">Evaluator or NULL.  If NULL returns <see cref="Double.NaN"/>.</param>
		/// <returns>Coerced value or THROWs.</returns>
		public static double CoerceValue(object item, BindingEvaluator be) {
			if (be == null) return double.NaN;
			var ox = be.For(item);
			if (ox is short sx) return (double)sx;
			if (ox is int ix) return (double)ix;
			if (ox is long lx) return (double)lx;
			if (ox is DateTime dt)
				return dt == default(DateTime) ? double.NaN : (double)dt.Ticks;
			// now nullable types
			if (ox is double?) {
				double? ddx = (double?)ox;
				return ddx ?? double.NaN;
			}
			if (ox is int?) {
				int? ddx = (int?)ox;
				return ddx ?? double.NaN;
			}
			if (ox is short?) {
				short? ddx = (short?)ox;
				return ddx ?? double.NaN;
			}
			return (double)ox;
		}
		#endregion
	}
	#endregion
	#region DataSeriesWithAxes
	/// <summary>
	/// This class commits to a <see cref="CategoryAxis"/> and <see cref="ValueAxis"/>, but no values.
	/// </summary>
	public abstract class DataSeriesWithAxes : DataSeries, IProvideValueExtents, IRequireCategoryAxis {
		#region DPs
		/// <summary>
		/// Identifies <see cref="CategoryPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			nameof(CategoryPath), typeof(string), typeof(DataSeriesWithAxes), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		#endregion
		#region properties
		/// <summary>
		/// Binding path to the <see cref="CategoryAxis"/> value.
		/// MAY be NULL, in which case the data-index is used instead.
		/// </summary>
		public String CategoryPath { get { return (String)GetValue(CategoryPathProperty); } set { SetValue(CategoryPathProperty, value); } }
		/// <summary>
		/// Component name of <see cref="ValueAxis"/>.
		/// Referenced component MUST implement <see cref="IChartAxis"/>.
		/// </summary>
		public String ValueAxisName { get; set; }
		/// <summary>
		/// Component name of <see cref="CategoryAxis"/>.
		/// Referenced component MUST implement <see cref="IChartAxis"/>.
		/// </summary>
		public String CategoryAxisName { get; set; }
		/// <summary>
		/// The minimum value seen.
		/// </summary>
		public double Minimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum value seen.
		/// </summary>
		public double Maximum { get; protected set; } = double.NaN;
		/// <summary>
		/// The minimum category (value) seen.
		/// </summary>
		public double CategoryMinimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The maximum category (value) seen.
		/// </summary>
		public double CategoryMaximum { get; protected set; } = double.NaN;
		/// <summary>
		/// Range of the values or <see cref="double.NaN"/> if <see cref="UpdateLimits(double, double)"/>or <see cref="UpdateLimits(double, double[])"/> was never called.
		/// </summary>
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		/// <summary>
		/// Dereferenced value axis.
		/// </summary>
		protected IChartAxis ValueAxis { get; set; }
		/// <summary>
		/// Dereferenced category axis.
		/// </summary>
		protected IChartAxis CategoryAxis { get; set; }
		/// <summary>
		/// Always maps to the x-component (horz) axis or NULL.
		/// </summary>
		protected IChartAxis XAxis =>
			CategoryAxis != null && CategoryAxis.Orientation == AxisOrientation.Horizontal
			? CategoryAxis
			: (ValueAxis != null && ValueAxis.Orientation == AxisOrientation.Horizontal
				? ValueAxis
				: null);
		/// <summary>
		/// Always maps to the y-component (vert) axis or NULL.
		/// </summary>
		protected IChartAxis YAxis =>
			ValueAxis != null && ValueAxis.Orientation == AxisOrientation.Vertical
			? ValueAxis
			: (CategoryAxis != null && CategoryAxis.Orientation == AxisOrientation.Vertical
				? CategoryAxis
				: null);
		#endregion
		#region extension points
		/// <summary>
		/// Reset and recalculate series limits.
		/// </summary>
		protected abstract void ReconfigureLimits();
		#endregion
		#region helpers
		/// <summary>
		/// Provide a readable name for DP update diagnostics.
		/// </summary>
		/// <param name="dp"></param>
		/// <returns></returns>
		protected override String DPName(DependencyProperty dp) {
			if (dp == CategoryPathProperty) return nameof(CategoryPath);
			return dp.ToString();
		}
		/// <summary>
		/// Resolve axis references with error info.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartComponentContext icrc) {
			IChartErrorInfo icei = icrc as IChartErrorInfo;
			if (ValueAxis == null) {
				if (!string.IsNullOrEmpty(ValueAxisName)) {
					ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
					if (ValueAxis == null) {
						icei?.Report(new ChartValidationResult(NameOrType(), $"Value axis '{ValueAxisName}' was not found", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
					}
				} else {
					icei?.Report(new ChartValidationResult(NameOrType(), $"Property '{nameof(ValueAxisName)}' was not set", new[] { nameof(ValueAxis), nameof(ValueAxisName) }));
				}
			}
			if (CategoryAxis == null) {
				if (!string.IsNullOrEmpty(CategoryAxisName)) {
					CategoryAxis = icrc.Find(CategoryAxisName) as IChartAxis;
					if (CategoryAxis == null) {
						icei?.Report(new ChartValidationResult(NameOrType(), $"Value axis '{CategoryAxisName}' was not found", new[] { nameof(CategoryAxis), nameof(CategoryAxisName) }));
					}
				} else {
					icei?.Report(new ChartValidationResult(NameOrType(), $"Property '{nameof(CategoryAxisName)}' was not set", new[] { nameof(CategoryAxis), nameof(CategoryAxisName) }));
				}
			}
			if (ValueAxis != null && CategoryAxis != null && ValueAxis.Orientation == CategoryAxis.Orientation) {
				icei?.Report(new ChartValidationResult(NameOrType(), $"Category axis '{(CategoryAxis as ChartComponent).Name}' and Value axis '{(ValueAxis as ChartComponent).Name}' MUST have different orientations", new[] { nameof(ValueAxis), nameof(CategoryAxis) }));
			}
		}
		/// <summary>
		/// Update value and category limits.
		/// If a value is <see cref="double.NaN"/>, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="value_cat">Category. MAY be NaN.</param>
		/// <param name="value_val">Value.  MAY be NaN.</param>
		protected void UpdateLimits(double value_cat, double value_val) {
			if (double.IsNaN(CategoryMinimum) || value_cat < CategoryMinimum) { CategoryMinimum = value_cat; }
			if (double.IsNaN(CategoryMaximum) || value_cat > CategoryMaximum) { CategoryMaximum = value_cat; }
			if (double.IsNaN(Minimum) || value_val < Minimum) { Minimum = value_val; }
			if (double.IsNaN(Maximum) || value_val > Maximum) { Maximum = value_val; }
		}
		/// <summary>
		/// Update value and category limits.
		/// Syntactic convenience for multiple y-axis values.
		/// If a value is <see cref="double.NaN"/>, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="value_cat">Category. MAY be NaN.</param>
		/// <param name="value_vals">Values.  MAY contain NaN.</param>
		protected void UpdateLimits(double value_cat, params double[] value_vals) {
			UpdateLimits(value_cat, (IEnumerable<double>)value_vals);
		}
		/// <summary>
		/// Update value and category limits.
		/// If a value is <see cref="double.NaN"/>, it is effectively ignored because NaN is NOT GT/LT ANY number, even itself.
		/// </summary>
		/// <param name="value_cat">Category. MAY be NaN.</param>
		/// <param name="value_vals">Values.  MAY contain NaN.</param>
		protected void UpdateLimits(double value_cat, IEnumerable<double> value_vals) {
			if (double.IsNaN(CategoryMinimum) || value_cat < CategoryMinimum) { CategoryMinimum = value_cat; }
			if (double.IsNaN(CategoryMaximum) || value_cat > CategoryMaximum) { CategoryMaximum = value_cat; }
			foreach (var vy in value_vals) {
				if (double.IsNaN(Minimum) || vy < Minimum) { Minimum = vy; }
				if (double.IsNaN(Maximum) || vy > Maximum) { Maximum = vy; }
			}
		}
		/// <summary>
		/// Reset the value and category limits to <see cref="double.NaN"/>.
		/// Sets <see cref="ChartComponent.Dirty"/> = true.
		/// </summary>
		protected void ResetLimits() {
			Minimum = double.NaN; Maximum = double.NaN;
			CategoryMinimum = double.NaN; CategoryMaximum = double.NaN;
			Dirty = true;
		}
		#endregion
	}
	#endregion
	#region DataSeriesWithValue
	/// <summary>
	/// Derive from this series type when the series has a single value binding, e.g. <see cref="LineSeries"/>, <see cref="ColumnSeries"/>, <see cref="MarkerSeries"/>.
	/// <para/>
	/// This class commits to the <see cref="ValuePath"/> and <see cref="PathStyle"/> of those elements.
	/// <para/>
	/// Series type with multiple value bindings SHOULD use <see cref="DataSeriesWithAxes"/> instead.
	/// </summary>
	public abstract class DataSeriesWithValue : DataSeriesWithAxes, IProvideSeriesItemValues {
		#region DPs
		/// <summary>
		/// Identifies <see cref="ValuePath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(DataSeriesWithValue), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="ValueLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ValueLabelPathProperty = DependencyProperty.Register(
			nameof(ValueLabelPath), typeof(string), typeof(DataSeriesWithValue), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(
			nameof(PathStyle), typeof(Style), typeof(DataSeriesWithValue), new PropertyMetadata(null)
		);
		/// <summary>
		/// Identifies <see cref="Title"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title), typeof(String), typeof(DataSeriesWithValue), new PropertyMetadata("Title")
		);
		#endregion
		#region properties
		/// <summary>
		/// The title for the values.
		/// </summary>
		public String Title { get { return (String)GetValue(TitleProperty); } set { SetValue(TitleProperty, value); } }
		/// <summary>
		/// The style to use for Path geometry.
		/// SHOULD be non-NULL.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// MUST be non-NULL.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// Binding path to the value axis label.
		/// MAY be NULL.
		/// If specified, this value will augment the one used for All Channels in <see cref="ISeriesItemValue"/>.
		/// </summary>
		public String ValueLabelPath { get { return (String)GetValue(ValueLabelPathProperty); } set { SetValue(ValueLabelPathProperty, value); } }
		/// <summary>
		/// Force an override of the <see cref="IProvideSeriesItemValues"/> property.
		/// </summary>
		public abstract IEnumerable<ISeriesItem> SeriesItemValues { get; }
		#endregion
		#region extensions
		/// <summary>
		/// Provide a readable name for DP update diagnostics.
		/// </summary>
		/// <param name="dp"></param>
		/// <returns></returns>
		protected override String DPName(DependencyProperty dp) {
			if (dp == ValuePathProperty) return nameof(ValuePath);
			else if (dp == ValueLabelPathProperty) return nameof(ValueLabelPath);
			else if (dp == PathStyleProperty) return nameof(PathStyle);
			else if (dp == TitleProperty) return nameof(Title);
			else return base.DPName(dp);
		}
		#endregion
		#region helpers
		/// <summary>
		/// Report an error if the <see cref="ValuePath"/> was not configured.
		/// </summary>
		/// <param name="iccc"></param>
		protected void EnsureValuePath(IChartComponentContext iccc) {
			var icei = iccc as IChartErrorInfo;
			if (String.IsNullOrEmpty(ValuePath)) {
				icei?.Report(new ChartValidationResult(NameOrType(), $"{nameof(ValuePath)} was not set, no values will generate", new[] { nameof(ValuePath) }));
			}
		}
		#endregion
	}
	#endregion
}
