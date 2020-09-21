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
		public int CurrentChildCount { get; set; }
		public Chart8() {
			this.InitializeComponent();
		}

		/// <summary>
		/// Update the child count so the viewer can see how recycling levels out.
		/// </summary>
		/// <param name="ox"></param>
		protected override object InitializeDataContext(NavigationEventArgs e) {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var g1 = new List<GroupInfo> {
				new GroupInfo(0, "GroupC1"),
				new GroupInfo(1, "GroupC2"),
				new GroupInfo(2, "GroupC3"),
				new GroupInfo(3, "GroupC4"),
				new GroupInfo(4, "GroupC5")
			};
			var g2 = new List<GroupInfo> {
				new GroupInfo(0, "GroupR1"),
				new GroupInfo(1, "GroupR2"),
				new GroupInfo(2, "GroupR3"),
				new GroupInfo(3, "GroupR4"),
				new GroupInfo(4, "GroupR5")
			};
			var list = new List<ObservationGrouped> {
				new ObservationGrouped(g1[0].Label, g2[0].Label, 0, 0, 2),
				new ObservationGrouped(g1[0].Label, g2[1].Label, 0, 1, 6),
				new ObservationGrouped(g1[0].Label, g2[2].Label, 0, 2, 3),
				new ObservationGrouped(g1[0].Label, g2[3].Label, 0, 3, 1),
				new ObservationGrouped(g1[0].Label, g2[4].Label, 0, 4, 5),

				new ObservationGrouped(g1[1].Label, g2[0].Label, 1, 0, 2),
				new ObservationGrouped(g1[1].Label, g2[1].Label, 1, 1, 7),
				new ObservationGrouped(g1[1].Label, g2[2].Label, 1, 2, 3),
				new ObservationGrouped(g1[1].Label, g2[3].Label, 1, 3, 1),
				new ObservationGrouped(g1[1].Label, g2[4].Label, 1, 4, 5),

				new ObservationGrouped(g1[2].Label, g2[0].Label, 2, 0, 2),
				new ObservationGrouped(g1[2].Label, g2[1].Label, 2, 1, 7),
				new ObservationGrouped(g1[2].Label, g2[2].Label, 2, 2, 13),
				new ObservationGrouped(g1[2].Label, g2[3].Label, 2, 3, 1),
				new ObservationGrouped(g1[2].Label, g2[4].Label, 2, 4, 5),

				new ObservationGrouped(g1[3].Label, g2[0].Label, 3, 0, 2),
				new ObservationGrouped(g1[3].Label, g2[1].Label, 3, 1, 7),
				new ObservationGrouped(g1[3].Label, g2[2].Label, 3, 2, 13),
				new ObservationGrouped(g1[3].Label, g2[3].Label, 3, 3, 10),
				new ObservationGrouped(g1[3].Label, g2[4].Label, 3, 4, 9),

				new ObservationGrouped(g1[4].Label, g2[0].Label, 4, 0, 0),
				new ObservationGrouped(g1[4].Label, g2[1].Label, 4, 1, 6),
				new ObservationGrouped(g1[4].Label, g2[2].Label, 4, 2, 3),
				new ObservationGrouped(g1[4].Label, g2[3].Label, 4, 3, 1),
				new ObservationGrouped(g1[4].Label, g2[4].Label, 4, 4, 9),
			};
			var vm = new ObservationGroupedVM(Dispatcher, g1, g2, list);
			return vm;
		}
	}
}
