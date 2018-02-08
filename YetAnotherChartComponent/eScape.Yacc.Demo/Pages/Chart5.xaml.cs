using System;
using System.Diagnostics;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	/// <summary>
	/// Chart5: stacked columns.
	/// </summary>
	public sealed partial class Chart5 : BasicPage {
		public override string PageTitle => "Stacked Column Chart";
		public Chart5() {
			this.InitializeComponent();
		}
		protected override object InitializeDataContext(NavigationEventArgs e) {
			var vm = new ObservationsVM(Dispatcher, new[] {
				new Observation2("Group 1", -0.5, 1, 2, 2.3),
				new Observation2("Group 2", 3, 10, 1, 2),
				new Observation2("Group 3", 2, 5, 3, 1),
				new Observation2("Group 4", 3, -10, 2, 6),
				new Observation2("Group 5", 4, -3.75, -3, 2),
				new Observation2("Group 6", -5.25, 0.5, 1, 5)
			});
			return vm;
		}
		private void chart_ChartError(eScapeLLC.UWP.Charts.Chart sender, eScapeLLC.UWP.Charts.ChartErrorEventArgs args) {
			foreach (var ev in args.Results) {
				Debug.WriteLine($"chart error {ev.Source}\t{String.Join(",", ev.MemberNames)}: {ev.ErrorMessage}");
			}
		}
	}
}
