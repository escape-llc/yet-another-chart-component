using eScape.Core.Page;
using eScapeLLC.UWP.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart8 : BasicPage {
		public override string PageTitle => "Heatmap";
		public Chart8() {
			this.InitializeComponent();
		}

		/// <summary>
		/// Update the child count so the viewer can see how recycling levels out.
		/// </summary>
		/// <param name="ox"></param>
		protected override object InitializeDataContext(NavigationEventArgs e) {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var colgroups = new List<GroupInfo> {
				new GroupInfo(0, "GroupC1"),
				new GroupInfo(1, "GroupC2"),
				new GroupInfo(2, "GroupC3"),
				new GroupInfo(3, "GroupC4"),
				new GroupInfo(4, "GroupC5"),
				new GroupInfo(5, "GroupC6")
			};
			var rowgroups = new List<GroupInfo> {
				new GroupInfo(0, "GroupR1"),
				new GroupInfo(1, "GroupR2"),
				new GroupInfo(2, "GroupR3"),
				new GroupInfo(3, "GroupR4"),
				new GroupInfo(4, "GroupR5"),
				new GroupInfo(5, "GroupR6"),
				new GroupInfo(6, "GroupR7")
			};
			var list = new List<ObservationGrouped> {
				new ObservationGrouped(colgroups[0].Label, rowgroups[0].Label, 0, 0, 2),
				new ObservationGrouped(colgroups[0].Label, rowgroups[1].Label, 0, 1, 6),
				new ObservationGrouped(colgroups[0].Label, rowgroups[2].Label, 0, 2, 3),
				new ObservationGrouped(colgroups[0].Label, rowgroups[3].Label, 0, 3, 1),
				new ObservationGrouped(colgroups[0].Label, rowgroups[4].Label, 0, 4, 5),
				new ObservationGrouped(colgroups[0].Label, rowgroups[5].Label, 0, 5, 5),
				new ObservationGrouped(colgroups[0].Label, rowgroups[6].Label, 0, 6, 6),

				new ObservationGrouped(colgroups[1].Label, rowgroups[0].Label, 1, 0, 2),
				new ObservationGrouped(colgroups[1].Label, rowgroups[1].Label, 1, 1, 7),
				new ObservationGrouped(colgroups[1].Label, rowgroups[2].Label, 1, 2, 3),
				new ObservationGrouped(colgroups[1].Label, rowgroups[3].Label, 1, 3, 1),
				new ObservationGrouped(colgroups[1].Label, rowgroups[4].Label, 1, 4, 5),
				new ObservationGrouped(colgroups[1].Label, rowgroups[5].Label, 1, 5, 5),
				new ObservationGrouped(colgroups[1].Label, rowgroups[6].Label, 1, 6, 0),

				new ObservationGrouped(colgroups[2].Label, rowgroups[0].Label, 2, 0, 4),
				new ObservationGrouped(colgroups[2].Label, rowgroups[1].Label, 2, 1, 7),
				new ObservationGrouped(colgroups[2].Label, rowgroups[2].Label, 2, 2, 13),
				new ObservationGrouped(colgroups[2].Label, rowgroups[3].Label, 2, 3, 12),
				new ObservationGrouped(colgroups[2].Label, rowgroups[4].Label, 2, 4, 5),
				new ObservationGrouped(colgroups[2].Label, rowgroups[5].Label, 2, 5, 5),
				new ObservationGrouped(colgroups[2].Label, rowgroups[6].Label, 2, 6, 9),

				new ObservationGrouped(colgroups[3].Label, rowgroups[0].Label, 3, 0, 11),
				new ObservationGrouped(colgroups[3].Label, rowgroups[1].Label, 3, 1, 7),
				new ObservationGrouped(colgroups[3].Label, rowgroups[2].Label, 3, 2, 13),
				new ObservationGrouped(colgroups[3].Label, rowgroups[3].Label, 3, 3, 10),
				new ObservationGrouped(colgroups[3].Label, rowgroups[4].Label, 3, 4, 9),
				new ObservationGrouped(colgroups[3].Label, rowgroups[5].Label, 3, 5, 5),
				new ObservationGrouped(colgroups[3].Label, rowgroups[6].Label, 3, 6, 6),

				new ObservationGrouped(colgroups[4].Label, rowgroups[0].Label, 4, 0, 0),
				new ObservationGrouped(colgroups[4].Label, rowgroups[1].Label, 4, 1, 6),
				new ObservationGrouped(colgroups[4].Label, rowgroups[2].Label, 4, 2, 3),
				new ObservationGrouped(colgroups[4].Label, rowgroups[3].Label, 4, 3, 8),
				new ObservationGrouped(colgroups[4].Label, rowgroups[4].Label, 4, 4, 9),
				new ObservationGrouped(colgroups[4].Label, rowgroups[5].Label, 4, 5, 5),
				new ObservationGrouped(colgroups[4].Label, rowgroups[6].Label, 4, 6, 6),

				new ObservationGrouped(colgroups[5].Label, rowgroups[0].Label, 5, 0, 3),
				new ObservationGrouped(colgroups[5].Label, rowgroups[1].Label, 5, 1, 1),
				new ObservationGrouped(colgroups[5].Label, rowgroups[2].Label, 5, 2, 8),
				new ObservationGrouped(colgroups[5].Label, rowgroups[3].Label, 5, 3, 2),
				new ObservationGrouped(colgroups[5].Label, rowgroups[4].Label, 5, 4, 11),
				new ObservationGrouped(colgroups[5].Label, rowgroups[5].Label, 5, 5, 2),
				new ObservationGrouped(colgroups[5].Label, rowgroups[6].Label, 5, 6, 6),
			};
			var vm = new ObservationGroupedVM(Dispatcher, colgroups, rowgroups, list);
			return vm;
		}
	}
}
