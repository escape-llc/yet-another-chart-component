using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region DataSeries
	/// <summary>
	/// Base class of components that represent a data series.
	/// This class commits to a Data source, Category axis, Value axis, but no values.
	/// </summary>
	public abstract class DataSeries : ChartComponent, IProvideValueExtents, IProvideCategoryExtents {
		#region DPs
		/// <summary>
		/// Identifies <see cref="DataSourceName"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DataSourceNameProperty = DependencyProperty.Register(
			nameof(DataSourceName), typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="CategoryPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CategoryPathProperty = DependencyProperty.Register(
			nameof(CategoryPath), typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
		/// <summary>
		/// Identifies <see cref="CategoryLabelPath"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CategoryLabelPathProperty = DependencyProperty.Register(
			nameof(CategoryLabelPath), typeof(string), typeof(DataSeries), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
		);
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
			if (dp == CategoryPathProperty) return "CategoryPath";
			else if (dp == CategoryLabelPathProperty) return "CategoryLabelPath";
			else if (dp == DataSourceNameProperty) return "DataSourceName";
			return dp.ToString();
		}
		/// <summary>
		/// Resolve axis references.
		/// </summary>
		/// <param name="icrc">The context.</param>
		protected void EnsureAxes(IChartComponentContext icrc) {
			if (ValueAxis == null && !String.IsNullOrEmpty(ValueAxisName)) {
				ValueAxis = icrc.Find(ValueAxisName) as IChartAxis;
			} else {
				if (icrc is IChartErrorInfo icei) {
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
			foreach (var vy in vys) {
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
		/// <summary>
		/// Take the actual value from the source and coerce it to the double type, until we get full polymorphism on the y-value.
		/// Currently handles <see cref="double"/>, <see cref="int"/>, <see cref="short"/>, and <see cref="DateTime"/> types.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="be"></param>
		/// <returns></returns>
		public static double CoerceValue(object item, BindingEvaluator be) {
			var ox = be.For(item);
			if (ox is short sx) return (double)sx;
			if (ox is int ix) return (double)ix;
			if (ox is long lx) return (double)lx;
			if (ox is DateTime dt) return (double)dt.Ticks;
			return (double)ox;
		}
		#endregion
	}
	#endregion
	#region DataSeriesWithValue
	/// <summary>
	/// Derive from this series type when the series has a single value binding, e.g. Line, Column, Marker.
	/// This class commits to the ValuePath and PathStyle of those elements.
	/// Series type with multiple value bindings SHOULD use <see cref="DataSeries"/> instead.
	/// </summary>
	public abstract class DataSeriesWithValue : DataSeries, IProvideSeriesItemValues {
		#region DPs
		/// <summary>
		/// ValuePath DP.
		/// </summary>
		public static readonly DependencyProperty ValuePathProperty = DependencyProperty.Register(
			nameof(ValuePath), typeof(string), typeof(DataSeriesWithValue), new PropertyMetadata(null, new PropertyChangedCallback(PropertyChanged_ValueDirty))
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
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// Binding path to the value axis value.
		/// </summary>
		public String ValuePath { get { return (String)GetValue(ValuePathProperty); } set { SetValue(ValuePathProperty, value); } }
		/// <summary>
		/// Force an override of IProvideSeriesItemValues property.
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
			if (dp == ValuePathProperty) return "ValuePath";
			else if (dp == PathStyleProperty) return "PathStyle";
			else if (dp == TitleProperty) return "Title";
			else return base.DPName(dp);
		}
		#endregion
	}
	#endregion
	#region item state interfaces
	/// <summary>
	/// Entry point to series item data.
	/// </summary>
	public interface ISeriesItem {
		/// <summary>
		/// The index.
		/// </summary>
		int Index { get; }
		/// <summary>
		/// The category axis value for the <see cref="Index"/>.
		/// </summary>
		double XValueIndex { get; }
		/// <summary>
		/// The category axis value after applying offset, e.g. <see cref="MarkerSeries.MarkerOffset"/>.
		/// </summary>
		double XValueOffset { get; }
	}
	/// <summary>
	/// Item tracking a single channel.
	/// </summary>
	public interface ISeriesItemValue {
		/// <summary>
		/// Value axis value.
		/// </summary>
		double YValue { get; }
		/// <summary>
		/// What "channel" this value is tracking.
		/// Value is host-dependent if tracking multiple values, else SHOULD be ZERO.
		/// </summary>
		int Channel { get; }
	}
	/// <summary>
	/// Item tracking multiple channels.
	/// </summary>
	public interface ISeriesItemValues {
		/// <summary>
		/// Enumerator to traverse the values.
		/// SHOULD order-by channel.
		/// </summary>
		IEnumerable<ISeriesItemValue> YValues { get; }
	}
	/// <summary>
	/// Ability to provide access to the current series item state.
	/// </summary>
	public interface IProvideSeriesItemValues {
		/// <summary>
		/// Enumerator to traverse the item values.
		/// SHOULD operate on a COPY of the actual underlying sequence.
		/// </summary>
		IEnumerable<ISeriesItem> SeriesItemValues { get; }
	}
	#endregion
	#region ItemState implementations
	/// <summary>
	/// Simplest item state to start from.
	/// </summary>
	public class ItemStateCore : ISeriesItem {
		/// <summary>
		/// The index of this value from data source.
		/// </summary>
		public int Index { get; private set; }
		/// <summary>
		/// The x value for <see cref="Index"/>.
		/// </summary>
		public double XValueIndex { get; private set; }
		/// <summary>
		/// The x value after intra-unit offset.
		/// </summary>
		public double XValueOffset { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		public ItemStateCore(int idx, double xv, double xvo) { Index = idx; XValueIndex = xv; XValueOffset = xvo; }
	}
	/// <summary>
	/// Item state for single value.
	/// This is used when one element-per-item is generated, so it can be re-adjusted in Transforms et al.
	/// </summary>
	/// <typeparam name="EL">The element type.</typeparam>
	public class ItemState<EL> : ItemStateCore, ISeriesItem, ISeriesItemValue where EL : DependencyObject {
		/// <summary>
		/// The generated element.
		/// </summary>
		public EL Element { get; private set; }
		/// <summary>
		/// The y value.
		/// </summary>
		public double YValue { get; private set; }
		/// <summary>
		/// The channel.
		/// </summary>
		public int Channel { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="ele"></param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemState(int idx, double xv, double xvo, double yv, EL ele, int ch = 0) :base(idx, xv, xvo) {
			YValue = yv;
			Element = ele;
			Channel = ch;
		}
	}
	/// <summary>
	/// Wrapper with placement.
	/// </summary>
	/// <typeparam name="EL">The element type.</typeparam>
	public abstract class ItemStateWithPlacement<EL> : ItemState<EL>, IProvidePlacement where EL : DependencyObject {
		Placement cache;
		/// <summary>
		/// (Cache and) return placement info.
		/// </summary>
		Placement IProvidePlacement.Placement { get { if (cache == null) cache = CreatePlacement(); return cache; } }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="ele"></param>
		/// <param name="ch"></param>
		public ItemStateWithPlacement(int idx, double xv, double xvo, double yv, EL ele, int ch = 0) : base(idx, xv, xvo, yv, ele, ch) { }
		/// <summary>
		/// Override to create placement.
		/// </summary>
		/// <returns></returns>
		protected abstract Placement CreatePlacement();
	}
	/// <summary>
	/// Default implementation for <see cref="IProvideSeriesItemValues"/>.
	/// </summary>
	public class ItemStateMultiChannelCore : ItemStateCore, ISeriesItemValues {
		/// <summary>
		/// Return all the channels.
		/// </summary>
		public IEnumerable<ISeriesItemValue> YValues { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">X-value.</param>
		/// <param name="xvo"></param>
		/// <param name="isis">Channel details.  THIS takes ownership.</param>
		public ItemStateMultiChannelCore(int idx, double xv, double xvo, ISeriesItemValue[] isis) : base(idx, xv, xvo) { YValues = isis; }
	}

	/// <summary>
	/// Item state with transformation matrix.
	/// </summary>
	/// <typeparam name="EL">The Element type.</typeparam>
	public class ItemState_Matrix<EL> : ItemState<EL> where EL : FrameworkElement {
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="ele"></param>
		/// <param name="ch"></param>
		public ItemState_Matrix(int idx, double xv, double xvo, double yv, EL ele, int ch = 0) : base(idx, xv, xvo, yv, ele, ch) { }
		/// <summary>
		/// Alternate matrix for the M matrix.
		/// Used when establishing a local transform for <see cref="ItemState{E}.Element"/>.
		/// </summary>
		public Matrix World { get; set; }
	}
	/// <summary>
	/// Item with <see cref="Path"/> as element type, local matrix and geometry.
	/// </summary>
	/// <typeparam name="G">Type of geometry.</typeparam>
	public class ItemState_MatrixAndGeometry<G> : ItemState_Matrix<Path> where G : Geometry {
		/// <summary>
		/// The geometry.
		/// If you are using Path.Data to reference geometry, choose <see cref="ItemState_Matrix{E}"/> or <see cref="ItemState{E}"/> instead.
		/// </summary>
		public G Geometry { get; set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="ele"></param>
		/// <param name="ch"></param>
		public ItemState_MatrixAndGeometry(int idx, double xv, double xvo, double yv, Path ele, int ch = 0) : base(idx, xv, xvo, yv, ele, ch) { }
	}
	#endregion
	#region RenderState implementations
	/// <summary>
	/// Common state for implementations of <see cref="IDataSourceRenderer"/>.
	/// Contains no references to any values on either axis, just core bookkeeping.
	/// The "basic" case has a list of state elements, and a recycler for its UI elements.
	/// </summary>
	/// <typeparam name="SIS">Series item state type.</typeparam>
	/// <typeparam name="EL">Recycled element type.</typeparam>
	internal class RenderStateCore<SIS, EL> where SIS: class where EL: FrameworkElement {
		/// <summary>
		/// Tracks the index from Render().
		/// </summary>
		internal int ix;
		/// <summary>
		/// Collects the item states created in Render().
		/// Transfer to host in Postamble().
		/// </summary>
		internal readonly List<SIS> itemstate;
		/// <summary>
		/// Recycles the elements.
		/// </summary>
		internal readonly Recycler<EL> recycler;
		/// <summary>
		/// The recycler's iterator to generate the elements.
		/// </summary>
		internal readonly IEnumerator<EL> elements;
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="state">Starting state; SHOULD be empty.</param>
		/// <param name="rc">The recycler.</param>
		internal RenderStateCore(List<SIS> state, Recycler<EL> rc) {
			itemstate = state;
			recycler = rc;
			elements = recycler.Items().GetEnumerator();
		}
		/// <summary>
		/// Convenience method to call for the next element from the recycler's iterator.
		/// </summary>
		/// <returns>Next element or NULL.</returns>
		internal EL NextElement() {
			if (elements.MoveNext()) return elements.Current;
			else return null;
		}
	}
	/// <summary>
	/// Extended state for common case of single value with category label.
	/// </summary>
	/// <typeparam name="SIS">Series item state type.</typeparam>
	/// <typeparam name="EL">Recycled element type.</typeparam>
	internal class RenderState_ValueAndLabel<SIS,EL> : RenderStateCore<SIS,EL> where SIS : class where EL : FrameworkElement {
		/// <summary>
		/// Binds x-value.
		/// </summary>
		internal readonly BindingEvaluator bx;
		/// <summary>
		/// Binds y-value.
		/// </summary>
		internal readonly BindingEvaluator by;
		/// <summary>
		/// Binds label; MAY be NULL.
		/// </summary>
		internal readonly BindingEvaluator bl;
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="state">Starting state; SHOULD be empty.</param>
		/// <param name="rc">The recycler.</param>
		/// <param name="bx">Evaluate x-value.</param>
		/// <param name="bl">Evaluate label MAY be NULL.</param>
		/// <param name="by">Evaluate y-value.</param>
		internal RenderState_ValueAndLabel(List<SIS> state, Recycler<EL> rc, BindingEvaluator bx, BindingEvaluator bl, BindingEvaluator by) :base(state, rc) {
			this.bx = bx;
			this.by = by;
			this.bl = bl;
		}
	}
	#endregion
}
