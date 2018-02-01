using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace eScapeLLC.UWP.Charts {
	#region TransactionalCollection
	/// <summary>
	/// Like ObservableCollection, but defers when the notify collection events fire.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TransactionalCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged {
		private const string CountString = "Count";
		private const string IndexerName = "Item[]";
		#region internal classes
		/// <summary>
		/// Controls the available operations while in a transaction.
		/// </summary>
		public interface ITransactionController {
			/// <summary>
			/// Add to end of collection.
			/// </summary>
			/// <param name="obj"></param>
			void Add(T obj);
			/// <summary>
			/// Remove at index.
			/// </summary>
			/// <param name="idx"></param>
			void Remove(int idx);
			/// <summary>
			/// Update at index.
			/// </summary>
			/// <param name="idx"></param>
			/// <param name="obj"></param>
			void Update(int idx, T obj);
			/// <summary>
			/// Insert at index.
			/// </summary>
			/// <param name="idx"></param>
			/// <param name="obj"></param>
			void Insert(int idx, T obj);
			/// <summary>
			/// Move to new index, overwriting the item there.
			/// </summary>
			/// <param name="oldidx"></param>
			/// <param name="newidx"></param>
			void Move(int oldidx, int newidx);
			/// <summary>
			/// Clear contents.
			/// </summary>
			void Clear();
			/// <summary>
			/// Access by index.
			/// </summary>
			/// <param name="idx"></param>
			/// <returns></returns>
			T Get(int idx);
		}
		#region log actions
		abstract class LogAction { }
		class ClearAction : LogAction { }
		class AddAction : LogAction { internal int idx; internal T obj; }
		class RemoveAction : LogAction { internal int idx; internal T obj; }
		class UpdateAction : LogAction { internal int idx; internal T original; internal T obj; }
		class InsertAction : LogAction { internal int idx; internal T obj; }
		class MoveAction : LogAction { internal int oldidx; internal int newidx; internal T obj; }
		#endregion
		/// <summary>
		/// Perform the operations and log them for afterward.
		/// </summary>
		class ListController : ITransactionController {
			internal readonly TransactionalCollection<T> coll;
			internal readonly List<LogAction> log = new List<LogAction>();
			public ListController(TransactionalCollection<T> coll) { this.coll = coll; }
			public T Get(int idx) { return coll[idx]; }
			public void Add(T obj) {
				coll.Add(obj);
				log.Add(new AddAction() { idx = coll.Count - 1, obj = obj });
			}
			public void Remove(int idx) {
				var ox = coll[idx];
				coll.RemoveItem(idx);
				log.Add(new RemoveAction() { idx = idx, obj = ox });
			}
			public void Update(int idx, T obj) {
				var ox = coll[idx];
				coll.SetItem(idx, obj);
				log.Add(new UpdateAction() { idx = idx, original = ox, obj = obj });
			}
			public void Insert(int idx, T obj) {
				coll.InsertItem(idx, obj);
				log.Add(new InsertAction() { idx = idx, obj = obj });
			}
			public void Move(int oldidx, int newidx) {
				var ox = coll[oldidx];
				coll.MoveItem(oldidx, newidx);
				log.Add(new MoveAction() { oldidx = oldidx, newidx = newidx, obj = ox });
			}
			public void Clear() {
				coll.ClearItems();
				log.Add(new ClearAction());
			}
		}
		#endregion
		#region events
		/// <summary>
		/// Connect for collection-changed events.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		/// <summary>
		/// Connect for property changed events.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
		#region properties
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Makes a fresh list.
		/// </summary>
		public TransactionalCollection() : base(new List<T>()) { }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">List to use.  Takes ownership.</param>
		public TransactionalCollection(List<T> source) : base(source) { }
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="source">Source to use.  Copies into a fresh list.</param>
		public TransactionalCollection(IEnumerable<T> source) : base(new List<T>()) { CopyFrom(source); }
		#endregion
		#region helpers
		private void CopyFrom(IEnumerable<T> collection) {
			IList<T> items = Items;
			if (collection != null && items != null) {
				using (IEnumerator<T> enumerator = collection.GetEnumerator()) {
					while (enumerator.MoveNext()) {
						items.Add(enumerator.Current);
					}
				}
			}
		}
		/// <summary>
		/// Trigger the property changed event.
		/// </summary>
		/// <param name="name"></param>
		protected void SendPropertyChanged(String name) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		/// <summary>
		/// Overload for fully-formed event args.
		/// </summary>
		/// <param name="nccea">Event args.</param>
		protected void SendCollectionChanged(NotifyCollectionChangedEventArgs nccea) {
			CollectionChanged?.Invoke(this, nccea);
		}
		#endregion
		#region overrides
		/// <summary>
		/// Override.  May not need any more.
		/// </summary>
		protected override void ClearItems() {
			//CheckReentrancy();
			base.ClearItems();
#if false
			SendPropertyChanged(CountString);
			SendPropertyChanged(IndexerName);
			SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
#endif
		}
		/// <summary>
		/// Override.  May not need any more.
		/// </summary>
		protected override void RemoveItem(int index) {
			//CheckReentrancy();
			T removedItem = this[index];
			base.RemoveItem(index);
#if false
			SendPropertyChanged(CountString);
			SendPropertyChanged(IndexerName);
			SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
#endif
		}
		/// <summary>
		/// Override.  May not need any more.
		/// </summary>
		protected override void InsertItem(int index, T item) {
			//CheckReentrancy();
			base.InsertItem(index, item);
#if false
			SendPropertyChanged(CountString);
			SendPropertyChanged(IndexerName);
			SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
#endif
		}
		/// <summary>
		/// Override.  May not need any more.
		/// </summary>
		protected override void SetItem(int index, T item) {
			//CheckReentrancy();
			T originalItem = this[index];
			base.SetItem(index, item);
#if false
			SendPropertyChanged(IndexerName);
			SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, originalItem, item, index));
#endif
		}
		/// <summary>
		/// Override.  May not need any more.
		/// </summary>
		protected virtual void MoveItem(int oldIndex, int newIndex) {
			//CheckReentrancy();
			T removedItem = this[oldIndex];
			base.RemoveItem(oldIndex);
			base.InsertItem(newIndex, removedItem);
#if false
			SendPropertyChanged(IndexerName);
			SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex));
#endif
		}
		#endregion
		#region public
		/// <summary>
		/// Execute the list transaction.
		/// </summary>
		/// <param name="tx">Transaction action.  MUST modify the collection ONLY via the controller parameter.</param>
		public void Transaction(Action<ITransactionController> tx) {
			var ctx = new ListController(this);
			tx(ctx);
			bool[] propchange = new bool[2];
			// play back log and generate events.
			foreach (var la in ctx.log) {
				switch (la) {
				case ClearAction ca:
					propchange[0] = propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
					break;
				case RemoveAction ra:
					propchange[0] = propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, ra.obj, ra.idx));
					break;
				case InsertAction ia:
					propchange[0] = propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, ia.obj, ia.idx));
					break;
				case UpdateAction ua:
					propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, ua.original, ua.obj, ua.idx));
					break;
				case MoveAction ma:
					propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, ma.obj, ma.newidx, ma.oldidx));
					break;
				case AddAction aa:
					propchange[0] = propchange[1] = true;
					SendCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, aa.obj, aa.idx));
					break;
				}
			}
			// batch the property change events
			if (propchange[0]) {
				SendPropertyChanged(CountString);
			}
			if (propchange[1]) {
				SendPropertyChanged(IndexerName);
			}
		}
		#endregion
	}
	#endregion
}
