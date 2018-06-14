using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region item state interfaces
	#region ISeriesItem
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
		double XValue { get; }
		/// <summary>
		/// Category axis offset.
		/// </summary>
		double XOffset { get; }
		/// <summary>
		/// The category axis value after applying offset, e.g. <see cref="MarkerSeries.MarkerOffset"/>.
		/// </summary>
		double XValueAfterOffset { get; }
	}
	#endregion
	#region ISeriesItemValue
	/// <summary>
	/// Entry point to item values.
	/// </summary>
	public interface ISeriesItemValue {
		/// <summary>
		/// What "channel" this value is tracking.
		/// Value is host-dependent if tracking multiple values, else SHOULD be ZERO.
		/// </summary>
		int Channel { get; }
	}
	#endregion
	#region ISeriesItemValueDouble
	/// <summary>
	/// Item tracking double on a single channel.
	/// </summary>
	public interface ISeriesItemValueDouble : ISeriesItemValue {
		/// <summary>
		/// Value axis value.
		/// </summary>
		double Value { get; }
	}
	#endregion
	#region ISeriesItemValueCustom
	/// <summary>
	/// Item tracking custom object on a single channel
	/// </summary>
	public interface ISeriesItemValueCustom : ISeriesItemValueDouble {
		/// <summary>
		/// Value axis value.
		/// </summary>
		object CustomValue { get; }
	}
	#endregion
	#region ISeriesItemValues
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
	#endregion
	#region IProvideSeriesItemUpdates
	/// <summary>
	/// Event sent by a <see cref="DataSeries"/> when its tracked items has an incremental update.
	/// </summary>
	public sealed class SeriesItemUpdateEventArgs : EventArgs {
		/// <summary>
		/// Render context in effect.
		/// </summary>
		public IChartRenderContext Render { get; private set; }
		/// <summary>
		/// Triggering action.
		/// </summary>
		public NotifyCollectionChangedAction Action { get; private set; }
		/// <summary>
		/// Starting index.
		/// </summary>
		public int StartAt { get; private set; }
		/// <summary>
		/// Items involved.
		/// </summary>
		public IList<ISeriesItem> Items { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="icrc">Render context.</param>
		/// <param name="ncca">The action.</param>
		/// <param name="startat">Starting index.</param>
		/// <param name="isis">Items affected.</param>
		public SeriesItemUpdateEventArgs(IChartRenderContext icrc, NotifyCollectionChangedAction ncca, int startat, IEnumerable<ISeriesItem> isis) { Render = icrc; Action = ncca; StartAt = startat; Items = new List<ISeriesItem>(isis); }
	}
	/// <summary>
	/// Ability to provide incremental updates for <see cref="ISeriesItem"/> state.
	/// </summary>
	public interface IProvideSeriesItemUpdates {
		/// <summary>
		/// Register for incremental updates.
		/// </summary>
		event EventHandler<SeriesItemUpdateEventArgs> ItemUpdates;
	}
	#endregion
	#region IProvideSeriesItemValues
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
	#region ItemStateMatrix
	/// <summary>
	/// Item state with a world transform attached.
	/// </summary>
	public interface IItemStateMatrix {
		/// <summary>
		/// Get or Set the world transform.
		/// </summary>
		Matrix World { get; set; }
	}
	#endregion
	#region IItemStateGeometry<G>
	/// <summary>
	/// Item state with custom geometry attached.
	/// </summary>
	/// <typeparam name="G">Type of <see cref="Geometry"/>.</typeparam>
	public interface IItemStateGeometry<G> where G : Geometry {
		/// <summary>
		/// The geometry.
		/// </summary>
		G Geometry { get; set; }
	}
	#endregion
	#endregion
	#region ItemState implementations
	#region ItemStateCore
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
		public double XValue { get; private set; }
		/// <summary>
		/// The intra-unit offset component for e.g. <see cref="CategoryAxis"/>.
		/// </summary>
		public double XOffset { get; private set; }
		/// <summary>
		/// Calculated x value after intra-unit offset.
		/// </summary>
		public double XValueAfterOffset { get { return XValue + XOffset; } }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.  Used for intra-unit offset like in a <see cref="CategoryAxis"/>.</param>
		public ItemStateCore(int idx, double xv, double xo) { Index = idx; XValue = xv; XOffset = xo; }
		/// <summary>
		/// Shift this item by given count.
		/// Recalculates <see cref="Index"/> and <see cref="XValue"/>.
		/// </summary>
		/// <param name="count">Shift count.</param>
		/// <param name="ieval">Evaluator to use.</param>
		/// <param name="axis">Axis to map category-axis value.</param>
		/// <param name="callback">Post-update callback.  MAY be null.</param>
		public void Shift(int count, IEvaluator ieval, IChartAxis axis, Action<ItemStateCore> callback) {
			Index = Index + count;
			var xv = ieval.CategoryValue(XValue, Index);
			XValue = axis.For(xv);
			callback?.Invoke(this);
		}
	}
	#endregion
	#region ItemState<EL>
	/// <summary>
	/// Item state for single value.
	/// This is used when one element-per-item is generated, so it can be re-adjusted in Transforms et al.
	/// </summary>
	/// <typeparam name="EL">The element type.</typeparam>
	public class ItemState<EL> : ItemStateCore, ISeriesItem, ISeriesItemValueDouble where EL : DependencyObject {
		/// <summary>
		/// The generated element.
		/// </summary>
		public EL Element { get; private set; }
		/// <summary>
		/// The y value.
		/// </summary>
		public double Value { get; private set; }
		/// <summary>
		/// The channel.
		/// </summary>
		public int Channel { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Value channel; default to zero.</param>
		public ItemState(int idx, double xv, double xo, double yv, EL ele, int ch = 0) : base(idx, xv, xo) {
			Value = yv;
			Element = ele;
			Channel = ch;
		}
	}
	#endregion
	#region ItemStateCustom<EL>
	/// <summary>
	/// Wrapper for Custom value state.
	/// </summary>
	/// <typeparam name="EL">The element type.</typeparam>
	public class ItemStateCustom<EL> : ItemState<EL>, ISeriesItemValueCustom where EL : DependencyObject {
		/// <summary>
		/// The custom state for this value.
		/// </summary>
		public object CustomValue { get; private set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="cs">Custom state.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemStateCustom(int idx, double xv, double xo, double yv, object cs, EL ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) {
			CustomValue = cs;
		}
	}
	#endregion
	#region ItemStateWithPlacement<EL>
	/// <summary>
	/// Wrapper with placement.
	/// Caches the Placement instance.
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
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Value channel; default to zero.</param>
		public ItemStateWithPlacement(int idx, double xv, double xo, double yv, EL ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) { }
		/// <summary>
		/// Override to create placement.
		/// </summary>
		/// <returns></returns>
		protected abstract Placement CreatePlacement();
	}
	#endregion
	#region ItemStateCustomWithPlacement<EL>
	/// <summary>
	/// Wrapper for Custom value state with placement.
	/// </summary>
	/// <typeparam name="EL">The element type.</typeparam>
	public abstract class ItemStateCustomWithPlacement<EL> : ItemStateCustom<EL>, IProvidePlacement where EL : DependencyObject {
		Placement cache;
		/// <summary>
		/// (Cache and) return placement info.
		/// </summary>
		Placement IProvidePlacement.Placement { get { if (cache == null) cache = CreatePlacement(); return cache; } }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="cs">Custom state.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemStateCustomWithPlacement(int idx, double xv, double xo, double yv, object cs, EL ele, int ch = 0) : base(idx, xv, xo, yv, cs, ele, ch) { }
		/// <summary>
		/// Override to create placement.
		/// </summary>
		/// <returns></returns>
		protected abstract Placement CreatePlacement();
	}
	#endregion
	#region ItemStateMultiChannelCore
	/// <summary>
	/// Default implementation for <see cref="ISeriesItemValues"/>.
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
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="isis">Channel details.  THIS takes ownership.</param>
		public ItemStateMultiChannelCore(int idx, double xv, double xo, ISeriesItemValue[] isis) : base(idx, xv, xo) { YValues = isis; }
	}
	#endregion
	#region ItemState_Matrix<EL>
	/// <summary>
	/// Item state with transformation matrix.
	/// </summary>
	/// <typeparam name="EL">The Element type.</typeparam>
	public class ItemState_Matrix<EL> : ItemState<EL>, IItemStateMatrix where EL : FrameworkElement {
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Value channel; default to zero.</param>
		public ItemState_Matrix(int idx, double xv, double xo, double yv, EL ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) { }
		/// <summary>
		/// Alternate matrix for the M matrix.
		/// Used when establishing a local transform for <see cref="ItemState{E}.Element"/>.
		/// </summary>
		public Matrix World { get; set; }
	}
	#endregion
	#region ItemStateCustom_Matrix<EL>
	/// <summary>
	/// Item state with transformation matrix.
	/// </summary>
	/// <typeparam name="EL">The Element type.</typeparam>
	public class ItemStateCustom_Matrix<EL> : ItemStateCustom<EL>, IItemStateMatrix where EL : FrameworkElement {
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="cs">Custom state.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Value channel; default to zero.</param>
		public ItemStateCustom_Matrix(int idx, double xv, double xo, double yv, object cs, EL ele, int ch = 0) : base(idx, xv, xo, yv, cs, ele, ch) { }
		/// <summary>
		/// Alternate matrix for the M matrix.
		/// Used when establishing a local transform for <see cref="ItemState{E}.Element"/>.
		/// </summary>
		public Matrix World { get; set; }
	}
	#endregion
	#region ItemState_MatrixAndGeometry<G>
	/// <summary>
	/// Item with <see cref="Path"/> as element type, local matrix and geometry.
	/// </summary>
	/// <typeparam name="G">Type of geometry.</typeparam>
	public class ItemState_MatrixAndGeometry<G> : ItemState_Matrix<Path>, IItemStateGeometry<G> where G : Geometry {
		/// <summary>
		/// The geometry.
		/// If you are using Path.Data to reference geometry, choose <see cref="ItemState_Matrix{E}"/> or <see cref="ItemState{E}"/> instead.
		/// </summary>
		public G Geometry { get; set; }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="idx">Index.</param>
		/// <param name="xv">x-value.</param>
		/// <param name="xo">x-offset.</param>
		/// <param name="yv">y-value.</param>
		/// <param name="ele">Generated element.</param>
		/// <param name="ch">Value channel; default to zero.</param>
		public ItemState_MatrixAndGeometry(int idx, double xv, double xo, double yv, Path ele, int ch = 0) : base(idx, xv, xo, yv, ele, ch) { }
	}
	#endregion
	#endregion
	#region Evaluators
	/// <summary>
	/// Ability to evaluate data objects for chart values.
	/// </summary>
	public interface IEvaluator {
		/// <summary>
		/// Interpret the category-axis value or index, depending on whether it's defined.
		/// </summary>
		/// <param name="ox">Data object to evaluate if a binding is defined.</param>
		/// <param name="index">Index to use if no binding is defined.</param>
		/// <returns></returns>
		double CategoryFor(object ox, int index);
		/// <summary>
		/// Interpret the category-axis value or index, depending on whether it's defined.
		/// </summary>
		/// <param name="dx">Existing value.</param>
		/// <param name="index">Index.</param>
		/// <returns></returns>
		double CategoryValue(double dx, int index);
		/// <summary>
		/// Extract the value-axis value.
		/// </summary>
		/// <param name="ox"></param>
		/// <returns></returns>
		double ValueFor(object ox);
	}
	/// <summary>
	/// The package of <see cref="BindingEvaluator"/> in one place, evaluated once.
	/// </summary>
	internal class Evaluators : IEvaluator {
		#region data
		/// <summary>
		/// Category (x-axis) path; NULL to use the index.
		/// </summary>
		public readonly BindingEvaluator bx;
		/// <summary>
		/// Value (y-axis) path.  MUST NOT be NULL.
		/// </summary>
		public readonly BindingEvaluator by;
		/// <summary>
		/// Value label path.  MAY be NULL.
		/// </summary>
		public readonly BindingEvaluator byl;
		#endregion
		#region properties
		/// <summary>
		/// Return whether the <see cref="by"/> evaluator got initialized.
		/// </summary>
		public bool IsValid { get { return by != null; } }
		#endregion
		#region ctors
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="categoryPath">Path to the category value; MAY be NULL.</param>
		/// <param name="valuePath">Path to the value value; MUST NOT be NULL.</param>
		/// <param name="valueLabelPath">Path to the value label; MAY be NULL.</param>
		public Evaluators(String categoryPath, String valuePath, String valueLabelPath) {
			by = !String.IsNullOrEmpty(valuePath) ? new BindingEvaluator(valuePath) : null;
			bx = !String.IsNullOrEmpty(categoryPath) ? new BindingEvaluator(categoryPath) : null;
			byl = !String.IsNullOrEmpty(valueLabelPath) ? new BindingEvaluator(valueLabelPath) : null;
		}
		/// <summary>
		/// Copy ctor.
		/// </summary>
		/// <param name="bx">Category evaluator.</param>
		/// <param name="by">Value evaluator.</param>
		/// <param name="byl">Value label evaluator.</param>
		public Evaluators(BindingEvaluator bx, BindingEvaluator by, BindingEvaluator byl) { this.bx = bx; this.by = by; this.byl = byl; }
		#endregion
		#region public
		/// <summary>
		/// Use the <see cref="bx"/> evaluator to return the x-axis value, or index if it is NULL.
		/// </summary>
		/// <param name="ox">Object to evaluate.</param>
		/// <param name="index">Index value if <see cref="bx"/> is NULL.</param>
		/// <returns></returns>
		public double CategoryFor(object ox, int index) {
			var valuex = bx != null ? (double)bx.For(ox) : index;
			return valuex;
		}
		/// <summary>
		/// Use the <see cref="bx"/> evaluator to decide between two <see cref="double"/> values.
		/// </summary>
		/// <param name="dx">Return if <see cref="bx"/> is NOT NULL.</param>
		/// <param name="index">Otherwise.</param>
		/// <returns></returns>
		public double CategoryValue(double dx, int index) {
			var valuex = bx != null ? dx: index;
			return valuex;
		}
		/// <summary>
		/// Wrapper to call <see cref="DataSeries.CoerceValue(object, BindingEvaluator)"/>.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public double ValueFor(object item) {
			var valuey = DataSeries.CoerceValue(item, by);
			return valuey;
		}
		/// <summary>
		/// Evaluate the label or NULL.
		/// <para/>
		/// If NULL, either <see cref="byl"/> was NULL, OR the evaluation result was NULL.
		/// </summary>
		/// <param name="item">Source instance.</param>
		/// <returns>Evaluated value or NULL.</returns>
		public object LabelFor(object item) {
			return byl?.For(item);
		}
		/// <summary>
		/// Force (non-NULL) results to be a <see cref="String"/>.
		/// <para/>
		/// If <see cref="String.Empty"/>, <see cref="byl"/> was NULL. If NULL, the evaluation result was NULL.
		/// </summary>
		/// <param name="item">Source instance.</param>
		/// <returns>Evaluated string, NULL, or <see cref="String.Empty"/>.</returns>
		public String LabelStringFor(object item) {
			return byl != null ? byl.For(item)?.ToString() : String.Empty;
		}
		#endregion
	}
	#endregion
	#region RenderState implementations
	#region RenderStateCore<SIS,EL>
	/// <summary>
	/// Render state that uses a <see cref="Recycler{T, S}"/>.
	/// </summary>
	/// <typeparam name="SIS">Series item state class.</typeparam>
	/// <typeparam name="EL">UI element class.</typeparam>
	internal class RenderStateCore<SIS, EL> where SIS : class where EL : FrameworkElement {
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
		internal readonly Recycler<EL, SIS> recycler;
		/// <summary>
		/// The recycler's iterator to generate the elements.
		/// </summary>
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="state">Starting state; SHOULD be empty.</param>
		/// <param name="rc">The recycler.</param>
		internal RenderStateCore(List<SIS> state, Recycler<EL, SIS> rc) {
			itemstate = state;
			recycler = rc;
		}
		/// <summary>
		/// Get the next element from the recycler.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		internal Tuple<bool, EL> Next(SIS state) {
			return recycler.Next(state);
		}
	}
	#endregion
	#region RenderState_ValueAndLabel<SIS, EL>
	/// <summary>
	/// Extended state for common case of single value with category label.
	/// </summary>
	/// <typeparam name="SIS">Series item state type.</typeparam>
	/// <typeparam name="EL">Recycled element type.</typeparam>
	internal class RenderState_ValueAndLabel<SIS, EL> : RenderStateCore<SIS, EL> where SIS : class where EL : FrameworkElement {
		/// <summary>
		/// Evaluators for core values.
		/// </summary>
		internal readonly Evaluators evs;
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="state">Starting state; SHOULD be empty.</param>
		/// <param name="rc">The recycler.</param>
		/// <param name="evs">Evaluators.</param>
		internal RenderState_ValueAndLabel(List<SIS> state, Recycler<EL,SIS> rc, Evaluators evs) : base(state, rc) {
#pragma warning disable IDE0016 // Use 'throw' expression
			if (evs == null) throw new ArgumentNullException(nameof(evs));
			if (evs.by == null) throw new ArgumentNullException(nameof(evs.by));
#pragma warning restore IDE0016 // Use 'throw' expression
			this.evs = evs;
		}
	}
	#endregion
	#endregion
}
