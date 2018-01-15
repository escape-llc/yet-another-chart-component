using eScape.Core.VM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

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
			// build the list
			var pl = new ObservableCollection<PageItem>();
			// build offline so we don't trigger events
			pl.Add(new PageItem() { Symbol = Symbol.Map, Title = "Demo", Description = "The demo chart", PageType = typeof(Chart1) });
			pl.Add(new PageItem() { Symbol = Symbol.Map, Title = "Default", Description = "Default styles", PageType = typeof(Chart2) });
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
		public int GroupCounter { get; set; }
		public double Value1Average { get; private set; }
		public double Value2Average { get; private set; }
		public ObservableCollection<Observation> Data { get; private set; }
		public ObservationsVM(CoreDispatcher dx, IEnumerable<Observation> initial): base(dx) {
			Data = new ObservableCollection<Observation>(initial);
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
}
