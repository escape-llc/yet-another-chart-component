using eScape.Core;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.UI.Xaml;

namespace eScapeLLC.UWP.Charts {
	#region IDataSourceRenderer
	/// <summary>
	/// Ability to render the items of a data source.
	/// preamble, foreach render, postamble.
	/// </summary>
	public interface IDataSourceRenderer {
		/// <summary>
		/// Return a state object that gets passed back on subsequent calls.
		/// Includes limit initialization.
		/// </summary>
		/// <param name="icrc">Render context.</param>
		/// <returns>NULL: do not participate; !NULL: The state.</returns>
		object Preamble(IChartRenderContext icrc);
		/// <summary>
		/// Render the current item.
		/// Includes limit updates.
		/// Not called if Preamble() returned NULL.
		/// </summary>
		/// <param name="state">Return from preamble().</param>
		/// <param name="index">Data index [0..N).</param>
		/// <param name="item">Current item.</param>
		void Render(object state, int index, object item);
		/// <summary>
		/// Apply axis and other linked component updates.
		/// Called after all items are processed, and before Postamble().
		/// Not called if Preamble() returned NULL.
		/// </summary>
		/// <param name="state">Return from preamble().</param>
		void RenderComplete(object state);
		/// <summary>
		/// Perform terminal actions.
		/// Axis limits were finalized (in RenderComplete) and MAY be use in layout calculations.
		/// Not called if Preamble() returned NULL.
		/// </summary>
		/// <param name="state">Return from preamble().</param>
		void Postamble(object state);
	}
	#endregion
	#region DataSource
	/// <summary>
	/// Refresh delegate.
	/// </summary>
	/// <param name="ds">Originating component.</param>
	public delegate void DataSourceRefreshRequestEventHandler(DataSource ds);
	/// <summary>
	/// Represents a source of data for one-or-more series.
	/// Primary purpose is to consolidate the data traversal for all series using this data.
	/// This is important when the data changes; only one notification is handled instead one per series.
	/// Automatically tracks anything that implements <see cref="INotifyCollectionChanged"/>.
	/// Otherwise, owner must call Refresh() at appropriate time.
	/// </summary>
	public class DataSource : FrameworkElement {
		static LogTools.Flag _trace = LogTools.Add("DataSource", LogTools.Level.Error);
		#region data
		List<IDataSourceRenderer> _renderers = new List<IDataSourceRenderer>();
		#endregion
		#region items DP
		/// <summary>
		/// Identifies <see cref="Items"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
			"Items", typeof(System.Collections.IEnumerable), typeof(DataSource), new PropertyMetadata(null, new PropertyChangedCallback(ItemsPropertyChanged))
		);
		private static void ItemsPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSource ds = dobj as DataSource;
			if (dpcea.OldValue != dpcea.NewValue) {
				DetachCollectionChanged(ds, dpcea.OldValue);
				AttachCollectionChanged(ds, dpcea.NewValue);
				ds.Refresh();
			}
		}
		private static void DetachCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= ds.ItemsCollectionChanged;
			}
		}
		private static void AttachCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ds.ItemsCollectionChanged);
			}
		}
		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			Refresh();
		}
		#endregion
		#region properties
		/// <summary>
		/// Data source for the series.
		/// If the object implements <see cref="INotifyCollectionChanged"/> (e.g. <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>), updates are tracked automatically.
		/// Otherwise (e.g. <see cref="System.Collections.IList"/>), owner must call Refresh() after the underlying source is modified.
		/// </summary>
		public System.Collections.IEnumerable Items { get { return (System.Collections.IEnumerable)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
		/// <summary>
		/// True: render required.
		/// </summary>
		public bool IsDirty { get; set; }
		#endregion
		#region events
		/// <summary>
		/// "External" interest in this source's updates.
		/// </summary>
		public event DataSourceRefreshRequestEventHandler RefreshRequest;
		#endregion
		#region extension points
		/// <summary>
		/// Hook for dirty.
		/// Default impl.
		/// </summary>
		protected virtual void Dirty() { IsDirty = true; }
		/// <summary>
		/// Hook for clean.
		/// Default impl.
		/// </summary>
		protected virtual void Clean() { IsDirty = false; }
		/// <summary>
		/// Process the items through the list of <see cref="IDataSourceRenderer"/>.
		/// Default impl.
		/// </summary>
		/// <param name="icrc">Render context. icrc.Area is set to Rect.Empty.</param>
		protected virtual void ProcessItems(IChartRenderContext icrc) {
			_trace.Verbose($"ProcessData {Name} i:{Items} c:{_renderers.Count}");
			if (Items == null) return;
			if (_renderers.Count == 0) return;
			var pmap = new Dictionary<IDataSourceRenderer, object>();
			// Phase I: init each renderer; it may opt-out by returning NULL
			foreach (var idsr in _renderers) {
				var preamble = idsr.Preamble(icrc);
				// TODO may want an exception instead
				if (preamble != null) {
					pmap.Add(idsr, preamble);
				}
			}
			if (pmap.Count > 0) {
				// Phase II: traverse the data and distribute to renderers
				int ix = 0;
				foreach (var item in Items) {
					foreach (var idsr in _renderers) {
						if (pmap.TryGetValue(idsr, out object preamble)) {
							idsr.Render(preamble, ix, item);
						}
					}
					ix++;
				}
				// Phase III: finalize all axes etc. before we finalize renderers
				// this MUST occur so all renders see the same axes limits in postamble!
				foreach (var idsr in _renderers) {
					if (pmap.TryGetValue(idsr, out object preamble)) {
						idsr.RenderComplete(preamble);
					}
				}
				// Phase IV: finalize renderers
				foreach (var idsr in _renderers) {
					if (pmap.TryGetValue(idsr, out object preamble)) {
						idsr.Postamble(preamble);
					}
				}
			}
			Clean();
		}
		#endregion
		#region public
		/// <summary>
		/// Register for rendering notification.
		/// </summary>
		/// <param name="idsr"></param>
		public void Register(IDataSourceRenderer idsr) { if(!_renderers.Contains(idsr)) _renderers.Add(idsr); }
		/// <summary>
		/// Unregister for rendering notification.
		/// </summary>
		/// <param name="isdr"></param>
		public void Unregister(IDataSourceRenderer isdr) { _renderers.Remove(isdr); }
		/// <summary>
		/// Process items if IsDirty == true.
		/// </summary>
		public void Render(IChartRenderContext icrc) { if (IsDirty) ProcessItems(icrc); }
		/// <summary>
		/// Mark as dirty and fire refresh request event.
		/// Use this with sources that <b>don't</b> implement <see cref="INotifyCollectionChanged"/>.
		/// </summary>
		public void Refresh() { Dirty(); RefreshRequest?.Invoke(this); }
		#endregion
	}
	#endregion
}
