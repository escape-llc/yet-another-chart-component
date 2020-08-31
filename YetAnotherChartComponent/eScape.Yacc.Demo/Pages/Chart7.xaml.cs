using eScape.Core.Page;
using eScapeLLC.UWP.Charts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart7 : BasicPage {
		public override string PageTitle => "Sharing";
		public int CurrentChildCount { get; set; }
		public ObservableCollection<LegendBase> AllLegendItems { get; set; }
		public Chart7() {
			AllLegendItems = new ObservableCollection<LegendBase>();
			this.InitializeComponent();
			chart1.Loaded += Chart1_Loaded;
		}

		private void Chart1_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			foreach(var li in chart1.LegendItems.Concat(chart3.LegendItems).Concat(chart2.LegendItems)) {
				AllLegendItems.Add(li);
			}
			chart1.Loaded -= Chart1_Loaded;
		}

		/// <summary>
		/// Update the child count so the viewer can see how recycling levels out.
		/// </summary>
		/// <param name="ox"></param>
		protected override object InitializeDataContext(NavigationEventArgs e) {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var list = new List<Observation2>();
			for (int ix = 0; ix < 30; ix++) list.Add(Observation2.PLACEHOLDER);
			var vm = new TimedObservations2VM(Dispatcher, list);
			vm.ResetCounter();
			return vm;
		}
	}
}
