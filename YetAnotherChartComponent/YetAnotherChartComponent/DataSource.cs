using eScape.Core;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.UI.Xaml;

namespace eScapeLLC.UWP.Charts {
	#region IDataSourceRenderer
	/// <summary>
	/// Ability to render the items of a data source.
	/// </summary>
	public interface IDataSourceRenderer {
		/// <summary>
		/// Return a state object that gets passed back on subsequent calls.
		/// </summary>
		/// <returns>NULL: do not participate; !NULL: The state.</returns>
		object Preamble();
		/// <summary>
		/// Render the current item.
		/// Not called if Preamble() returned NULL.
		/// </summary>
		/// <param name="state">Return from preamble().</param>
		/// <param name="index">Data index [0..N).</param>
		/// <param name="item">Current item.</param>
		void Render(object state, int index, object item);
		/// <summary>
		/// Perform terminal actions.
		/// Not called if Preamble() returned NULL.
		/// </summary>
		/// <param name="state">Return from preamble().</param>
		void Postamble(object state);
	}
	#endregion
	#region DataSource
	/// <summary>
	/// Represents a source of data for one-or-more series.
	/// Primary purpose is to consolidate the data traversal for all series using this data.
	/// This is important when the data changes; only one notification is handled instead one per series.
	/// </summary>
	public class DataSource : FrameworkElement {
		static LogTools.Flag _trace = LogTools.Add("DataSource", LogTools.Level.Verbose);
		#region data
		List<IDataSourceRenderer> _renderers = new List<IDataSourceRenderer>();
		#endregion
		#region items DP
		/// <summary>
		/// Identifies <see cref="DataSource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
			"Items", typeof(System.Collections.IEnumerable), typeof(DataSource), new PropertyMetadata(null, new PropertyChangedCallback(ItemsPropertyChanged))
		);
		private static void ItemsPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSource ds = dobj as DataSource;
			if (dpcea.OldValue != dpcea.NewValue) {
				DetachItemsCollectionChanged(ds, dpcea.OldValue);
				AttachItemsCollectionChanged(ds, dpcea.NewValue);
				ds.Dirty();
				ds.ProcessData(dpcea.Property);
			}
		}
		private static void DetachItemsCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged -= ds.ItemsCollectionChanged;
			}
		}
		private static void AttachItemsCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged) {
				(dataSource as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ds.ItemsCollectionChanged);
			}
		}
		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			Dirty();
			ProcessData(ItemsProperty);
		}
		#endregion
		#region properties
		/// <summary>
		/// Data source for the series.
		/// </summary>
		public System.Collections.IEnumerable Items { get { return (System.Collections.IEnumerable)GetValue(ItemsProperty); } set { SetValue(ItemsProperty, value); } }
		/// <summary>
		/// True: render required.
		/// </summary>
		public bool IsDirty { get; set; }
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
		/// Process the items.
		/// Default impl.
		/// </summary>
		/// <param name="dp">MUST be ItemsProperty.</param>
		protected virtual void ProcessData(DependencyProperty dp) {
			_trace.Verbose($"ProcessData {Name} i:{Items} c:{_renderers.Count}");
			if (dp != ItemsProperty) return;
			if (Items == null) return;
			if (_renderers.Count == 0) return;
			var pmap = new Dictionary<IDataSourceRenderer, object>();
			// init each renderer; it may opt-out by returning NULL
			foreach (var idsr in _renderers) {
				var preamble = idsr.Preamble();
				// TODO may want an exception instead
				if (preamble != null) {
					pmap.Add(idsr, preamble);
				}
			}
			if (pmap.Count > 0) {
				// traverse the data and distribute to renderers
				int ix = 0;
				foreach (var item in Items) {
					foreach (var idsr in _renderers) {
						var preamble = default(object);
						if (pmap.TryGetValue(idsr, out preamble)) {
							idsr.Render(preamble, ix, item);
						}
					}
					ix++;
				}
				// finalize renderers
				foreach (var idsr in _renderers) {
					var preamble = default(object);
					if (pmap.TryGetValue(idsr, out preamble)) {
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
		public void Render() { if (IsDirty) ProcessData(ItemsProperty); }
		#endregion
	}
	#endregion
}
