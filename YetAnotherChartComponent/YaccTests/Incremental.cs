using eScapeLLC.UWP.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Yacc.Tests {
	public class TestItem : ISeriesItem, ISeriesItemValue {
		public int Index { get; set; }
		public double XValue { get; set; }
		public double XOffset { get; set; }
		public double XValueAfterOffset { get { return XValue + XOffset; } }
		public double Value { get; set; }
		public int Channel => 0;
	}
	public class DataSourceObject {
		public double Xvalue { get; set; }
		public double Yvalue { get; set; }
	}
	#region UnitTest_IncrementalUpdate
	[TestClass]
	public class UnitTest_IncrementalUpdate {
		public TestContext TestContext { get; set; }
		#region helpers
		List<DataSourceObject> CreateSource(int start = 0, int count = 3) {
			var items = new List<DataSourceObject> ();
			for(int ix = 0; ix < count; ix++) {
				items.Add(new DataSourceObject() { Xvalue = start + ix, Yvalue = (start + ix)*10 + ix });
			}
			return items;
		}
		TestItem CreateItem(int ix, object dso) {
			return new TestItem() {
				Index = ix,
				XValue = (dso as DataSourceObject).Xvalue,
				Value = (dso as DataSourceObject).Yvalue
			};
		}
		TestItem CreateItem(int ix) {
			return new TestItem() {
				Index = ix,
				XValue = ix,
				Value = ix
			};
		}
		void ResequenceAdd(int rpc, TestItem istate) {
			TestContext.WriteLine($"reseq before:{istate.Index} after:{istate.Index + rpc}");
			istate.Index = istate.Index + rpc;
		}
		void ResequenceRemove(int rpc, TestItem istate) {
			TestContext.WriteLine($"reseq before:{istate.Index} after:{istate.Index - rpc}");
			istate.Index = istate.Index - rpc;
		}
		void Compare(int index, TestItem ti, DataSourceObject dso) {
			Assert.AreEqual(index, ti.Index, "ti.Index failed");
			Assert.AreEqual(dso.Xvalue, ti.XValue, "ti.XValue failed");
			Assert.AreEqual(dso.Xvalue, ti.XValueAfterOffset, "ti.XValueAfterOffset failed");
			Assert.AreEqual(dso.Yvalue, ti.Value, "ti.Value failed");
		}
		void Compare(List<TestItem> itemstate) {
			for (int ix = 0; ix < itemstate.Count; ix++) {
				Assert.AreEqual(ix, itemstate[ix].Index, $"itemstate[{ix}].Index failed");
			}
		}
		void Dump(String title, List<TestItem> tis) {
			TestContext.WriteLine($"{title}[{tis.Count}]");
			for(int ix = 0; ix < tis.Count; ix++) {
				TestContext.WriteLine($"[{ix}]: index:{tis[ix].Index} xv:{tis[ix].XValue} yv:{tis[ix].Value}");
			}
		}
		#endregion
		#region Add
		[TestMethod, TestCategory("Incremental")]
		public void Add_NullAddsNothing() {
			var itemstate = new List<TestItem>();
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalAdd(0, items, itemstate, (ix, dso) => null, (rpc,istate)=> {
				reprocct++;
				ResequenceAdd(rpc, istate);
			});
			Dump("After", itemstate);
			Assert.AreEqual(0, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(0, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
		}
		[TestMethod, TestCategory("Incremental")]
		public void Add_ToEmptyList() {
			var itemstate = new List<TestItem>();
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalAdd(0, items, itemstate, CreateItem, (rpc, istate) => {
				reprocct++;
				ResequenceAdd(rpc, istate);
			});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(3, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
			Compare(itemstate);
			Compare(0, itemstate[0], items[0]);
			Compare(1, itemstate[1], items[1]);
			Compare(2, itemstate[2], items[2]);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Add_ToFrontOfList() {
			var itemstate = new List<TestItem>() {
				// "insert" here
				CreateItem(0),
				CreateItem(1),
				CreateItem(2)
			};
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalAdd(0, items, itemstate, CreateItem,
				(rpc, istate) => {
					reprocct++;
					ResequenceAdd(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(6, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(3, reprocct, "reprocct failed");
			Compare(itemstate);
			Compare(0, itemstate[0], items[0]);
			Compare(1, itemstate[1], items[1]);
			Compare(2, itemstate[2], items[2]);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Add_ToEndOfList() {
			var itemstate = new List<TestItem>() {
				CreateItem(0),
				CreateItem(1),
				CreateItem(2)
				// "insert" here
			};
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalAdd(itemstate.Count, items, itemstate, CreateItem,
				(rpc, istate) => {
					reprocct++;
					ResequenceAdd(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(6, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
			Compare(itemstate);
			Compare(3, itemstate[3], items[0]);
			Compare(4, itemstate[4], items[1]);
			Compare(5, itemstate[5], items[2]);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Add_ToInsideOfList() {
			var itemstate = new List<TestItem>() {
				CreateItem(0),
				CreateItem(1),
				// "insert" here
				CreateItem(2),
				CreateItem(3)
			};
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalAdd(itemstate.Count/2, items, itemstate, CreateItem,
				(rpc, istate) => {
					reprocct++;
					ResequenceAdd(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(7, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(2, reprocct, "reprocct failed");
			Compare(itemstate);
			Compare(2, itemstate[2], items[0]);
			Compare(3, itemstate[3], items[1]);
			Compare(4, itemstate[4], items[2]);
		}
		#endregion
		#region Remove
		[TestMethod, TestCategory("Incremental")]
		public void Remove_FromEmptyList() {
			var itemstate = new List<TestItem>();
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalRemove(0, items, itemstate, null, (rpc, istate) => {
				reprocct++;
				ResequenceAdd(rpc, istate);
			});
			Dump("After", itemstate);
			Assert.AreEqual(0, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(0, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
			Compare(itemstate);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Remove_FromFrontOfList() {
			var itemstate = new List<TestItem>() {
				// "remove" here
				CreateItem(0),
				CreateItem(1),
				CreateItem(2)
			};
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalRemove(0, items, itemstate, null,
				(rpc, istate) => {
					reprocct++;
					ResequenceRemove(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(0, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
			Compare(itemstate);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Remove_FromFrontOfList2() {
			var itemstate = new List<TestItem>() {
				// "remove" here
				CreateItem(0),
				CreateItem(1),
				CreateItem(2),
				CreateItem(3),
				CreateItem(4),
				CreateItem(5)
			};
			var items = CreateSource();
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalRemove(0, items, itemstate, null,
				(rpc, istate) => {
					reprocct++;
					ResequenceRemove(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(3, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(3, reprocct, "reprocct failed");
			Compare(itemstate);
		}
		[TestMethod, TestCategory("Incremental")]
		public void Remove_FromEndOfList() {
			var itemstate = new List<TestItem>() {
				CreateItem(0),
				CreateItem(1),
				CreateItem(2),
				// "remove" here
				CreateItem(3),
				CreateItem(4),
				CreateItem(5),
			};
			var items = CreateSource(3);
			var reprocct = 0;
			Dump("Before", itemstate);
			var reproc = ChartComponent.IncrementalRemove(itemstate.Count/2, items, itemstate, null,
				(rpc, istate) => {
					reprocct++;
					ResequenceRemove(rpc, istate);
				});
			Dump("After", itemstate);
			Assert.AreEqual(3, reproc.Count, "reproc.Count failed");
			Assert.AreEqual(3, itemstate.Count, "itemstate.Count failed");
			Assert.AreEqual(0, reprocct, "reprocct failed");
			Compare(itemstate);
		}
		#endregion
	}
	#endregion
	#region UnitTest_ObservableCollection
	/// <summary>
	/// Verify the contents of the callbacks for various <see cref="ObservableCollection{T}"/> operations.
	/// </summary>
	[TestClass]
	public class UnitTest_ObservableCollection {
		public TestContext TestContext { get; set; }
		#region helpers
		ObservableCollection<TestItem> CreateSource(int start = 0, int count = 3) {
			var items = new ObservableCollection<TestItem>();
			for (int ix = 0; ix < count; ix++) {
				items.Add(new TestItem() { Index = ix, XValue = start + ix, Value = (start + ix) * 10 + ix });
			}
			return items;
		}
		TestItem CreateItem(int ix) {
			return new TestItem() {
				Index = ix,
				XValue = ix,
				Value = ix
			};
		}
		#endregion
		[TestMethod, TestCategory("Observable")]
		public void AddToEnd() {
			var oc = CreateSource();
			NotifyCollectionChangedAction? ncca = null;
			int itemct = -1;
			int nsi = -1;
			bool oinull = false;
			oc.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
				ncca = e.Action;
				itemct = e.NewItems.Count;
				nsi = e.NewStartingIndex;
				oinull = e.OldItems == null;
			};
			var item = CreateItem(99);
			oc.Add(item);
			Assert.IsTrue(ncca.HasValue, "HasValue failed");
			Assert.IsTrue(oinull, "OldItems null failed");
			Assert.AreEqual(NotifyCollectionChangedAction.Add, ncca.Value, "Action failed");
			Assert.AreEqual(1, itemct, "NewItems.Count failed");
			Assert.AreEqual(3, nsi, "NewStartingIndex failed");
		}
		[TestMethod, TestCategory("Observable")]
		public void ReplaceItem() {
			var oc = CreateSource();
			NotifyCollectionChangedAction? ncca = null;
			int itemct = -1;
			int oitemct = -1;
			int nsi = -1;
			int osi = -1;
			oc.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
				ncca = e.Action;
				itemct = e.NewItems.Count;
				oitemct = e.OldItems.Count;
				nsi = e.NewStartingIndex;
				osi = e.OldStartingIndex;
			};
			var item = CreateItem(99);
			var index = oc.Count - 1;
			oc[index] = item;
			Assert.IsTrue(ncca.HasValue, "HasValue failed");
			Assert.AreEqual(NotifyCollectionChangedAction.Replace, ncca.Value, "Action failed");
			Assert.AreEqual(1, itemct, "NewItems.Count failed");
			Assert.AreEqual(1, oitemct, "OldItems.Count failed");
			Assert.AreEqual(index, nsi, "NewStartingIndex failed");
			Assert.AreEqual(index, osi, "OldStartingIndex failed");
		}
		[TestMethod, TestCategory("Observable")]
		public void RemoveFromEnd() {
			var oc = CreateSource();
			NotifyCollectionChangedAction? ncca = null;
			int itemct = -1;
			int osi = -1;
			bool ninull = false;
			oc.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => {
				ncca = e.Action;
				itemct = e.OldItems.Count;
				osi = e.OldStartingIndex;
				ninull = e.NewItems == null;
			};
			var index = oc.Count - 1;
			oc.RemoveAt(index);
			Assert.IsTrue(ncca.HasValue, "HasValue failed");
			Assert.AreEqual(NotifyCollectionChangedAction.Remove, ncca.Value, "Action failed");
			Assert.IsTrue(ninull, "NewItems null failed");
			Assert.AreEqual(1, itemct, "OldItems.Count failed");
			Assert.AreEqual(index, osi, "OldStartingIndex failed");
		}
	}
	#endregion
}
