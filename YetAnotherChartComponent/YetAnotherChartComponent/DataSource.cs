using eScape.Core;
using System;
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
		/// The name of the data source this renderer is bound to.
		/// </summary>
		String DataSourceName { get; }
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
	#region IProvideDataSourceRenderer
	/// <summary>
	/// Ability to provide an IDataSourceRenderer interface.
	/// </summary>
	public interface IProvideDataSourceRenderer {
		/// <summary>
		/// The renderer to use.
		/// MUST return a stable value.
		/// </summary>
		IDataSourceRenderer Renderer { get; }
	}
	#endregion
	#region DataSourceRefreshRequestEventHandler
	/// <summary>
	/// Refresh delegate.
	/// </summary>
	/// <param name="ds">Originating component.</param>
	public delegate void DataSourceRefreshRequestEventHandler(DataSource ds);
	#endregion
	#region IDataSourceRenderContext
	/// <summary>
	/// Context for the DataSource.Render method.
	/// </summary>
	public interface IDataSourceRenderContext : IChartRenderContext {
	}
	#endregion
	#region DataSource
	/// <summary>
	/// Represents a source of data for one-or-more series.
	/// Primary purpose is to consolidate the data traversal for rendering this data.
	/// This is important when the data changes; only one notification is handled instead one per series.
	/// The <see cref="Items"/> property automatically tracks anything that implements <see cref="INotifyCollectionChanged"/>.
	/// Otherwise, owner must call <see cref="Refresh"/> at appropriate time, or alternatively, increment the <see cref="RefreshRequest"/> property.
	/// The latter is handier when you are doing collection updtaes from a View Model.
	/// </summary>
	public class DataSource : FrameworkElement {
		static LogTools.Flag _trace = LogTools.Add("DataSource", LogTools.Level.Error);
		#region data
		List<IDataSourceRenderer> _renderers = new List<IDataSourceRenderer>();
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Items"/> DP.
		/// </summary>
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
			nameof(Items), typeof(System.Collections.IEnumerable), typeof(DataSource), new PropertyMetadata(null, new PropertyChangedCallback(ItemsPropertyChanged))
		);
		/// <summary>
		/// Identifies <see cref="ExternalRefresh"/> DP.
		/// </summary>
		public static readonly DependencyProperty ExternalRefreshProperty = DependencyProperty.Register(
			nameof(ExternalRefresh), typeof(int), typeof(DataSource), new PropertyMetadata(null, new PropertyChangedCallback(ExternalRefreshPropertyChanged))
		);
		/// <summary>
		/// Trigger a refresh when the value changes.
		/// </summary>
		/// <param name="dobj"></param>
		/// <param name="dpcea"></param>
		private static void ExternalRefreshPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSource ds = dobj as DataSource;
			if(dpcea.NewValue is int bx) {
				if (dpcea.NewValue != dpcea.OldValue && ds.Items != null) {
					ds.Refresh();
				}
			}
		}
		/// <summary>
		/// Do the <see cref="INotifyCollectionChanged"/> bookkeeping.
		/// </summary>
		/// <param name="dobj"></param>
		/// <param name="dpcea"></param>
		private static void ItemsPropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs dpcea) {
			DataSource ds = dobj as DataSource;
			if (dpcea.OldValue != dpcea.NewValue) {
				DetachCollectionChanged(ds, dpcea.OldValue);
				AttachCollectionChanged(ds, dpcea.NewValue);
				ds.Refresh();
			}
		}
		private static void DetachCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged incc) {
				incc.CollectionChanged -= ds.ItemsCollectionChanged;
			}
		}
		private static void AttachCollectionChanged(DataSource ds, object dataSource) {
			if (dataSource is INotifyCollectionChanged incc) {
				incc.CollectionChanged += ds.ItemsCollectionChanged;
			}
		}
		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs nccea) {
			_trace.Verbose($"cc {nccea.Action} nsi:[{nccea.NewStartingIndex}] {nccea.NewItems?.Count} osi:[{nccea.OldStartingIndex}] {nccea.OldItems?.Count}");
			switch(nccea.Action) {
			case NotifyCollectionChangedAction.Reset:
			default:
				Refresh();
				break;
			}
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
		/// SHOULD only be used within the framework, as it's not a DP or awt.
		/// </summary>
		public bool IsDirty { get; set; }
		/// <summary>
		/// Means for an "external source" (like a View Model) to attach a data binding to this property and trigger data source refreshes.
		/// ONLY use this if your <see cref="Items"/> DOES NOT implement <see cref="INotifyCollectionChanged"/>.
		/// </summary>
		public int ExternalRefresh { get { return (int)GetValue(ExternalRefreshProperty); } set { SetValue(ExternalRefreshProperty, value); } }
		/// <summary>
		/// The current list of renderers.
		/// </summary>
		public IEnumerable<IDataSourceRenderer> Renderers { get { return _renderers.AsReadOnly(); } }
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
		/// Sets IsDirty = True.
		/// Default impl.
		/// </summary>
		protected virtual void Dirty() { IsDirty = true; }
		/// <summary>
		/// Hook for clean.
		/// Sets IsDirty = False.
		/// Default impl.
		/// </summary>
		protected virtual void Clean() { IsDirty = false; }
		/// <summary>
		/// Process the items through the list of <see cref="IDataSourceRenderer"/>.
		/// Default impl.
		/// </summary>
		/// <param name="idsrc">Render context. icrc.Area is set to Rect.Empty.</param>
		protected virtual void RenderPipeline(IDataSourceRenderContext idsrc) {
			_trace.Verbose($"RenderPipeline {Name} i:{Items} c:{_renderers.Count}");
			if (Items == null) return;
			if (_renderers.Count == 0) return;
			var pmap = new Dictionary<IDataSourceRenderer, object>();
			// Phase I: init each renderer; it may opt-out by returning NULL
			foreach (var idsr in _renderers) {
				var state = idsr.Preamble(idsrc);
				// TODO may want an exception instead
				if (state != null) {
					pmap.Add(idsr, state);
				}
			}
			if (pmap.Count > 0) {
				// Phase II: traverse the data and distribute to renderers
				int ix = 0;
				foreach (var item in Items) {
					foreach (var idsr in _renderers) {
						if (pmap.TryGetValue(idsr, out object state)) {
							idsr.Render(state, ix, item);
						}
					}
					ix++;
				}
				// Phase IIIa: finalize all axes etc. before we finalize renderers
				// this MUST occur so all renders see the same axes limits in postamble!
				foreach (var idsr in _renderers) {
					if (pmap.TryGetValue(idsr, out object state)) {
						idsr.RenderComplete(state);
					}
				}
				// Phase IV: finalize renderers
				foreach (var idsr in _renderers) {
					if (pmap.TryGetValue(idsr, out object state)) {
						idsr.Postamble(state);
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
		/// <param name="idsr">Instance to register.</param>
		public void Register(IDataSourceRenderer idsr) { if(!_renderers.Contains(idsr)) _renderers.Add(idsr); }
		/// <summary>
		/// Unregister for rendering notification.
		/// </summary>
		/// <param name="idsr">Instance to unregister.</param>
		public void Unregister(IDataSourceRenderer idsr) { _renderers.Remove(idsr); }
		/// <summary>
		/// Process items if IsDirty == true.
		/// </summary>
		/// <param name="idsrc">The context.</param>
		public void Render(IDataSourceRenderContext idsrc) { if (IsDirty) RenderPipeline(idsrc); }
		/// <summary>
		/// Mark as dirty and fire refresh request event.
		/// Use this with sources that <b>don't</b> implement <see cref="INotifyCollectionChanged"/>.
		/// ALSO use this if you are not using <see cref="ExternalRefresh"/> property.
		/// </summary>
		public void Refresh() { Dirty(); RefreshRequest?.Invoke(this); }
		#endregion
	}
	#endregion
}
