using eScape.Core.Page;
using eScapeLLC.UWP.Charts;
using System;
using System.Diagnostics;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart1 : BasicPage {
		public override string PageTitle => "Demo Chart";
		public int CurrentChildCount { get; private set; }
		Timer cctimer;

		public Chart1() {
			this.InitializeComponent();
			cctimer = new Timer(Timer_Callback, this, 1000, 1000);
		}
		/// <summary>
		/// Update the child count so the viewer can see how recycling levels out.
		/// </summary>
		/// <param name="ox"></param>
		async void Timer_Callback(object ox) {
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
				// the chart has one child, which is a Canvas
				var child = VisualTreeHelper.GetChild(chart, 0);
				// everything lives in the canvas on one level
				CurrentChildCount = VisualTreeHelper.GetChildrenCount(child);
				// tell x:Bind
				Bindings.Update();
			});
		}
		protected override object InitializeDataContext(NavigationEventArgs e) {
			var vm = new ObservationsVM(Dispatcher, new[] {
				new Observation("Group 1", -0.5, 1),
				new Observation("Group 2", 3, 10),
				new Observation("Group 3", 2, 5),
				new Observation("Group 4", 3, -10),
				new Observation("Group 5", 4, -3.75),
				new Observation("Group 6", -5.25, 0.5)
			});
			return vm;
		}
		protected override void DataContextReleased(NavigatingCancelEventArgs ncea) {
			base.DataContextReleased(ncea);
			if (cctimer != null) {
				try {
					cctimer.Change(Timeout.Infinite, Timeout.Infinite);
					cctimer.Dispose();
				} finally {
					cctimer = null;
				}
			}
		}
		private void Add_item_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddTail();
		}

		private void Remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveHead();
		}

		private void Add_and_remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddAndRemoveHead();
		}

		private void Remove_tail_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveTail();
		}

		private void Add_head_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddHead();
		}

		private void Chart_ChartError(Chart sender, ChartErrorEventArgs args) {
			foreach(var ev in args.Results) {
				Debug.WriteLine($"chart error {ev.Source}\t{String.Join(",", ev.MemberNames)}: {ev.ErrorMessage}");
			}
		}
	}
}
