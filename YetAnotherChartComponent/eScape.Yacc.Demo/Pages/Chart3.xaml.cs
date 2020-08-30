using eScape.Core.Page;
using System.Collections.Generic;
using System.Threading;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;
using System;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart3 : BasicPage {
		public override string PageTitle => "Recycling";
		public int CurrentChildCount { get; set; }
		Timer cctimer;
		public Chart3() {
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
				if (child == null) {
					CurrentChildCount = 0;
				}
				else {
					// everything lives in the canvas on one level
					CurrentChildCount = VisualTreeHelper.GetChildrenCount(child);
				}
				// tell x:Bind
				Bindings.Update();
			});
		}
		protected override object InitializeDataContext(NavigationEventArgs e) {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var list = new List<Observation2>();
			for (int ix = 0; ix < 30; ix++) list.Add(Observation2.PLACEHOLDER);
			var vm = new TimedObservations2VM(Dispatcher, list);
			vm.ResetCounter();
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
	}
}
