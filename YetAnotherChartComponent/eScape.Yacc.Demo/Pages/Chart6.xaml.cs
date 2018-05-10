using eScape.Core.Page;
using eScapeLLC.UWP.Charts;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	/// <summary>
	/// Pie chart page.
	/// </summary>
	public sealed partial class Chart6 : BasicPage {
		public override string PageTitle => "Pie Chart";
		public Chart6() {
			this.InitializeComponent();
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
		private void Chart_ChartError(Chart sender, ChartErrorEventArgs args) {
			foreach (var ev in args.Results) {
				Debug.WriteLine($"chart error {ev.Source}\t{String.Join(",", ev.MemberNames)}: {ev.ErrorMessage}");
			}
		}
	}
}
