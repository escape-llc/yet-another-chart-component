using System;
using System.Collections.Generic;
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
		double XValueIndex { get; }
		/// <summary>
		/// The category axis value after applying offset, e.g. <see cref="MarkerSeries.MarkerOffset"/>.
		/// </summary>
		double XValueOffset { get; }
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
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="ele"></param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemState(int idx, double xv, double xvo, double yv, EL ele, int ch = 0) : base(idx, xv, xvo) {
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
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="cs">Custom state.</param>
		/// <param name="ele"></param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemStateCustom(int idx, double xv, double xvo, double yv, object cs, EL ele, int ch = 0) : base(idx, xv, xvo, yv, ele, ch) {
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
		/// <param name="idx"></param>
		/// <param name="xv"></param>
		/// <param name="xvo"></param>
		/// <param name="yv"></param>
		/// <param name="cs">Custom state.</param>
		/// <param name="ele"></param>
		/// <param name="ch">Channel; default to zero.</param>
		public ItemStateCustomWithPlacement(int idx, double xv, double xvo, double yv, object cs, EL ele, int ch = 0) : base(idx, xv, xvo, yv, cs, ele, ch) { }
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
		/// <param name="xv">X-value.</param>
		/// <param name="xvo"></param>
		/// <param name="isis">Channel details.  THIS takes ownership.</param>
		public ItemStateMultiChannelCore(int idx, double xv, double xvo, ISeriesItemValue[] isis) : base(idx, xv, xvo) { YValues = isis; }
	}
	#endregion
	#region ItemState_Matrix<EL>
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
	#endregion
	#region ItemState_MatrixAndGeometry<G>
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
	#endregion
	#region RenderState implementations
	#region RenderStateCore<SIS,EL>
	/// <summary>
	/// Common state for implementations of <see cref="IDataSourceRenderer"/>.
	/// Contains no references to any values on either axis, just core bookkeeping.
	/// The "basic" case has a list of state elements, and a recycler for its UI elements.
	/// </summary>
	/// <typeparam name="SIS">Series item state type.</typeparam>
	/// <typeparam name="EL">Recycled element type.</typeparam>
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
		internal readonly Recycler<EL> recycler;
		/// <summary>
		/// The recycler's iterator to generate the elements.
		/// </summary>
		internal readonly IEnumerator<Tuple<bool,EL>> elements;
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
		internal Tuple<bool, EL> NextElement() {
			if (elements.MoveNext()) return elements.Current;
			else return null;
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
		/// Binds x-value; MAY be NULL.
		/// </summary>
		internal readonly BindingEvaluator bx;
		/// <summary>
		/// Binds y-value.  MUST be non-NULL.
		/// </summary>
		internal readonly BindingEvaluator by;
		/// <summary>
		/// Binds label; MAY be NULL.
		/// </summary>
		internal readonly BindingEvaluator bl;
		/// <summary>
		/// Binds custom y-label; MAY be NULL.
		/// </summary>
		internal readonly BindingEvaluator byl;
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="state">Starting state; SHOULD be empty.</param>
		/// <param name="rc">The recycler.</param>
		/// <param name="bx">Evaluate x-value.</param>
		/// <param name="bl">Evaluate label MAY be NULL.</param>
		/// <param name="by">Evaluate y-value.</param>
		/// <param name="byl">Evaluate custom y-label MAY be NULL.</param>
		internal RenderState_ValueAndLabel(List<SIS> state, Recycler<EL> rc, BindingEvaluator bx, BindingEvaluator bl, BindingEvaluator by, BindingEvaluator byl) : base(state, rc) {
#pragma warning disable IDE0016 // Use 'throw' expression
			if (by == null) throw new ArgumentNullException(nameof(by));
#pragma warning restore IDE0016 // Use 'throw' expression
			this.bx = bx;
			this.by = by;
			this.bl = bl;
			this.byl = byl;
		}
	}
	#endregion
	#endregion
}
