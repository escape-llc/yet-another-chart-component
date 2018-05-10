using eScape.Core.Page;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart3 : BasicPage {
		public override string PageTitle => "Recycling";
		public Chart3() {
			this.InitializeComponent();
		}
		protected override object InitializeDataContext(NavigationEventArgs e) {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var list = new List<Observation2>();
			for (int ix = 0; ix < 30; ix++) list.Add(Observation2.PLACEHOLDER);
			var vm = new TimedObservationsVM(Dispatcher, list);
			vm.ResetCounter();
			return vm;
		}
	}
}
