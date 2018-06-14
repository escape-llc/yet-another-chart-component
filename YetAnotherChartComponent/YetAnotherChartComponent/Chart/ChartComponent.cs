using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace eScapeLLC.UWP.Charts {
	#region ChartComponent
	/// <summary>
	/// Base class of chart components.
	/// It is FrameworkElement primarily to participate in DataContext and Binding.
	/// </summary>
	public abstract class ChartComponent : FrameworkElement {
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		protected ChartComponent() { }
		#endregion
		#region events
		/// <summary>
		/// "External" interest in this component's updates.
		/// </summary>
		public event RefreshRequestEventHandler RefreshRequest;
		#endregion
		#region properties
		/// <summary>
		/// True: visuals require re-computing.
		/// </summary>
		public bool Dirty { get; protected set; }
		#endregion
		#region helpers
		/// <summary>
		/// Return the name if set, otherwise the type.
		/// </summary>
		/// <returns>Name or type.</returns>
		public String NameOrType() { return String.IsNullOrEmpty(Name) ? GetType().Name : Name; }
		/// <summary>
		/// Mark self as dirty and invoke the RefreshRequest event.
		/// </summary>
		/// <param name="rrt">Request type.</param>
		/// <param name="aus">Axis update status.</param>
		protected void Refresh(RefreshRequestType rrt, AxisUpdateState aus) { Dirty = true; RefreshRequest?.Invoke(this, new RefreshRequestEventArgs(rrt, aus, this)); }
		/// <summary>
		/// Generic DP property change handler.
		/// Calls ChartComponent.Refresh(ValueDirty, Unknown).
		/// </summary>
		/// <param name="ddo"></param>
		/// <param name="dpcea"></param>
		protected static void PropertyChanged_ValueDirty(DependencyObject ddo, DependencyPropertyChangedEventArgs dpcea) {
			var cc = ddo as ChartComponent;
			cc.Refresh(RefreshRequestType.ValueDirty, AxisUpdateState.Unknown);
		}
		/// <summary>
		/// Bind source.Path to the target.DP.
		/// </summary>
		/// <param name="source">Source instance.</param>
		/// <param name="path">Component's (source) property path.</param>
		/// <param name="target">Target DO.</param>
		/// <param name="dp">FE's (target) DP.</param>
		protected static void BindTo(object source, String path, DependencyObject target, DependencyProperty dp) {
			Binding bx = new Binding() {
				Path = new PropertyPath(path),
				Source = source,
				Mode = BindingMode.OneWay
			};
			target.ClearValue(dp);
			BindingOperations.SetBinding(target, dp, bx);
		}
		/// <summary>
		/// Transfer any binding from the source DP to the given target's DP.
		/// </summary>
		/// <param name="source">Source element.</param>
		/// <param name="path"></param>
		/// <param name="target">Target framework element.</param>
		/// <param name="dp"></param>
		protected static void ApplyBinding(FrameworkElement source, String path, DependencyObject target, DependencyProperty dp) {
			var bx = source.GetBindingExpression(dp);
			if (bx != null) {
				target.ClearValue(dp);
				BindingOperations.SetBinding(target, dp, bx.ParentBinding);
			} else {
				BindTo(source, path, target, dp);
			}
		}
		/// <summary>
		/// Boilerplate for assigning a "local property" from a "reference value" while applying <see cref="IChartErrorInfo"/>.
		/// </summary>
		/// <param name="icei">For error reporting; MAY be NULL.</param>
		/// <param name="vsource">Validation source in error reports.</param>
		/// <param name="localprop">Local property in error reports.</param>
		/// <param name="refprop">Reference property in error reports.</param>
		/// <param name="localcheck">The local check; True to proceed to sourcecheck.</param>
		/// <param name="sourcecheck">The source check; True to proceed to refcheck.</param>
		/// <param name="refcheck">The reference check; True to proceed to action.</param>
		/// <param name="applyvalue">Execute if everything returned True.</param>
		protected static void AssignFromRef(IChartErrorInfo icei, String vsource, String localprop, String refprop, bool localcheck, bool sourcecheck, bool refcheck, Action applyvalue) {
			if (!localcheck) return;
			if (sourcecheck) {
				if (refcheck)
					applyvalue();
				else {
					if (icei != null) {
						icei.Report(new ChartValidationResult(vsource, $"{localprop} not found and {refprop} not found", new[] { localprop, refprop }));
					}
				}
			} else {
				if (icei != null) {
					icei.Report(new ChartValidationResult(vsource, $"{localprop} not found and no Theme was found", new[] { localprop, refprop }));
				}
			}
		}
		/// <summary>
		/// Template version of incremental add.
		/// </summary>
		/// <typeparam name="IS">Item state type.</typeparam>
		/// <param name="startAt">From incremental add.</param>
		/// <param name="items">From incremental add.</param>
		/// <param name="itemstate">From the series component.</param>
		/// <param name="producestate">Produce the new item(s). MAY return NULL.  Signature(index, item).</param>
		/// <param name="resequence">Resequence remaining item(s).  Signature(index, rcount, istate).</param>
		/// <returns>The list of newly-produced item(s).  If this has any items, the itemstate list was sorted.</returns>
		public static List<IS> IncrementalAdd<IS>(int startAt, IList items, List<IS> itemstate, Func<int, object, IS> producestate, Action<int, IS> resequence) where IS: ISeriesItem {
			var reproc = new List<IS>();
			for (int ix = 0; ix < items.Count; ix++) {
				var istate = producestate(startAt + ix, items[ix]);
				if (istate != null) {
					reproc.Add(istate);
				}
			}
			// adjust indices based on added items
			foreach (var itx in itemstate.Where(ix => ix.Index >= startAt)) {
				resequence(items.Count, itx);
			}
			if (reproc.Count > 0) {
				// add collected items and re-sort by index
				itemstate.AddRange(reproc);
				if (itemstate.Count > 1) {
					itemstate.Sort((i1, i2) => i1.Index.CompareTo(i2.Index));
				}
			}
			return reproc;
		}
		/// <summary>
		/// Template version of incremental remove.
		/// </summary>
		/// <typeparam name="IS">Item state type.</typeparam>
		/// <param name="startAt">From incremental add.</param>
		/// <param name="items">From incremental add.</param>
		/// <param name="itemstate">From the series component.</param>
		/// <param name="collect">Predicate for adding to the removed item list.  Return true to collect.  MAY be NULL to collect all.</param>
		/// <param name="resequence">Resequence remaining item(s).</param>
		/// <returns>The list of removed item(s).</returns>
		public static List<IS> IncrementalRemove<IS>(int startAt, IList items, List<IS> itemstate, Func<IS, bool> collect, Action<int, IS> resequence) where IS: ISeriesItem {
			var reproc = new List<IS>();
			for (int ix = 0; ix < items.Count; ix++) {
				var remx = itemstate.SingleOrDefault(iix => iix.Index == startAt + ix);
				if (remx != null) {
					// remove requested item(s)
					if (collect == null || collect(remx)) {
						reproc.Add(remx);
					}
					itemstate.Remove(remx);
				}
			}
			foreach (var itx in itemstate.Where(ix => ix.Index >= startAt)) {
				resequence(items.Count, itx);
			}
			return reproc;
		}
		#endregion
	}
	#endregion
}
