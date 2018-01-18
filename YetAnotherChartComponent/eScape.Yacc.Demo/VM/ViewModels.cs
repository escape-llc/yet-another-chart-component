using eScape.Core.Host;
using eScape.Core.VM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Yacc.Demo.Pages;

namespace Yacc.Demo.VM {
	#region MainPageVM
	/// <summary>
	/// Represents an item on the nav pane.
	/// </summary>
	public class PageItem {
		public Symbol Symbol { get; set; }
		public String Title { get; set; }
		public String Description { get; set; }
		public Type PageType { get; set; }
	}
	/// <summary>
	/// VM for the main page.
	/// </summary>
	public class MainPageVM : CoreViewModel {
		public ObservableCollection<PageItem> PageList { get; private set; }
		public MainPageVM(CoreDispatcher dx) : base(dx) {
			// build offline so we don't trigger events
			var pl = new ObservableCollection<PageItem> {
				new PageItem() { Symbol = Symbol.Map, Title = "Demo", Description = "The demo chart (as seen on The Internet).", PageType = typeof(Chart1) },
				new PageItem() { Symbol = Symbol.Font, Title = "Default", Description = "Default styles in case you forget!", PageType = typeof(Chart2) },
				new PageItem() { Symbol = Symbol.Clock, Title = "Recycling", Description = "Paths get recycled efficiently as values enter and leave chart.", PageType = typeof(Chart3) },
				new PageItem() { Symbol = Symbol.Account, Title = "Candlestick", Description = "Cached real data (to avoid permissions) for your enjoyment.", PageType = typeof(Chart4) }
			};
			PageList = pl;
		}
	}
	#endregion
	#region ObservationsVM
	/// <summary>
	/// Represents one "cell" of the chart.
	/// This would be typical of a SQL DAO or other domain object.
	/// </summary>
	public class Observation {
		public String Label { get; private set; }
		public double Value1 { get; private set; }
		public double Value2 { get; private set; }
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
		public Observation(String label, double v1, double v2) { Label = label; Value1 = v1; Value2 = v2; }
	}
	/// <summary>
	/// Simple VM that maintains list of observations and the average of its two values.
	/// </summary>
	public class ObservationsVM : CoreViewModel {
		readonly Random rnd = new Random();
		public int GroupCounter { get; private set; }
		public double Value1Average { get; private set; }
		public double Value2Average { get; private set; }
		public ObservableCollection<Observation> Data { get; private set; }
		public ObservationsVM(CoreDispatcher dx, IEnumerable<Observation> initial): base(dx) {
			Data = new ObservableCollection<Observation>(initial);
			GroupCounter = Data.Count;
			Recalculate();
		}
		/// <summary>
		/// Randomly generate an item and add it to end and recalculate.
		/// </summary>
		public void AddTail() {
			GroupCounter++;
			var obs = new Observation($"Group {GroupCounter}", 10 * rnd.NextDouble() - 5, 10 * rnd.NextDouble() - 4);
			Data.Add(obs);
			Recalculate();
		}
		/// <summary>
		/// Randomly generate an item and add it to front and recalculate.
		/// </summary>
		public void AddHead() {
			GroupCounter++;
			var obs = new Observation($"Group {GroupCounter}", 10 * rnd.NextDouble() - 4, 10 * rnd.NextDouble() - 3);
			Data.Insert(0, obs);
			Recalculate();
		}
		/// <summary>
		/// Remove the first item and recalculate.
		/// </summary>
		public void RemoveHead() {
			if (Data.Count > 0) {
				Data.RemoveAt(0);
				Recalculate();
			}
		}
		/// <summary>
		/// Remove the last item and recalculate
		/// </summary>
		public void RemoveTail() {
			if (Data.Count > 0) {
				Data.RemoveAt(Data.Count - 1);
				Recalculate();
			}
		}
		/// <summary>
		/// Modify both ends of list and recalculate.
		/// </summary>
		public void AddAndRemoveHead() {
			RemoveHead();
			AddTail();
			Recalculate();
		}
		/// <summary>
		/// Recalculate and trigger INPC.
		/// </summary>
		void Recalculate() {
			if (Data.Count > 0) {
				Value1Average = Data.Average((ob) => ob.Value1);
				Value2Average = Data.Average((ob) => ob.Value2);
			} else {
				Value1Average = 0;
				Value2Average = 0;
			}
			Changed(nameof(Value1Average));
			Changed(nameof(Value2Average));
		}
	}
	#endregion
	#region TimedObservationsVM
	public class Observation2 : Observation {
		public double Value3 { get; private set; }
		public double Value4 { get; private set; }
		public Observation2(String label, double v1, double v2, double v3, double v4) : base(label, v1, v2) { Value3 = v3; Value4 = v4; }
		public static Observation2 PLACEHOLDER = new Observation2("-", double.NaN, double.NaN, double.NaN, double.NaN);
	}
	/// <summary>
	/// This VM demonstrates how to use a NOT observable collection, to avoid extra "churn" caused by individual add/remove operations.
	/// This also allows the path recycling to stabilize once chart reaches Window Size elements.
	/// </summary>
	public class TimedObservationsVM : CoreViewModel, IRequireRefresh, IRequireRelease {
		readonly Random rnd = new Random();
		/// <summary>
		/// In this VM, the group counter is bound to the DataSource.ExternalRefresh DP.
		/// Whenever the list is done changing, an <see cref="INotifyPropertyChanged"/> is triggered.
		/// NOTE that we are in manual control of when that happens (see <see cref="TimerCallback"/>)
		/// </summary>
		public int GroupCounter { get; private set; }
		/// <summary>
		/// The list of data.
		/// NOTE this does NOT implement <see cref="INotifyCollectionChanged"/>.
		/// </summary>
		public List<Observation2> Data { get; private set; }
		/// <summary>
		/// Command to toggle the timer.
		/// </summary>
		public ICommand Toggle { get; private set; }
		/// <summary>
		/// How fast to add values in MS.
		/// </summary>
		public int Speed { get; set; } = 500;
		/// <summary>
		/// Size to maintain the collection at.
		/// </summary>
		public int WindowSize { get; set; } = 30;
		public bool IsRunning { get; private set; }
		public bool AutoStart { get; set; }
		protected Timer Timer { get; set; }
		public TimedObservationsVM(CoreDispatcher dx) :this(dx, new List<Observation2>()) {}
		public TimedObservationsVM(CoreDispatcher dx, List<Observation2> data) :base(dx) {
			Data = data;
			Toggle = new DelegateCommand((ox) => { if (IsRunning) StopTimer(); else StartTimer(); }, (ox) => true);
			GroupCounter = Data.Count;
		}
		public void ResetCounter() { GroupCounter = 0; }
		void StartTimer() {
			try {
				if (Timer == null) {
					Timer = new Timer(TimerCallback, this, Speed, Speed);
				} else {
					Timer.Change(Speed, Speed);
				}
			}
			finally {
				IsRunning = true;
				Changed(nameof(IsRunning));
			}
		}
		void StopTimer() {
			try {
				if (Timer != null) Timer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			finally {
				IsRunning = false;
				Changed(nameof(IsRunning));
			}
		}
		protected async void TimerCallback(object ox) {
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, ()=> {
				RemoveHead();
				AddTail();
				// Since we're not using ObservableCollection, we MUST trigger a refresh when we're done changing the list
				Changed(nameof(GroupCounter));
			});
		}
		void AddTail() {
			GroupCounter++;
			var v1 =  4 + .5 - rnd.NextDouble();
			var v2 = 3 + .5 - rnd.NextDouble();
			var v3 = -2 + .5 - rnd.NextDouble();
			var v4 = -4 + .5 - rnd.NextDouble();
			var obs = new Observation2($"[{GroupCounter}]", v1, v2, v3, v4);
			Data.Add(obs);
		}
		void RemoveHead() {
			if (Data.Count > WindowSize) {
				Data.RemoveAt(0);
			}
		}
		async Task IRequireRefresh.RefreshAsync() {
			if (AutoStart) {
				await Task.Delay(1000);
				StartTimer();
			}
		}
		void IRequireRelease.Release() {
			try {
				if(Timer != null) Timer.Dispose();
			} finally {
				Timer = null;
			}
		}
	}
	#endregion
}
