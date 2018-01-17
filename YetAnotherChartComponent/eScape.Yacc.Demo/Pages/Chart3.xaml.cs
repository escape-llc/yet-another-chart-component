using eScape.Core.Host;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart3 : Page {
		public Chart3() {
			this.InitializeComponent();
		}
		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var list = new List<Observation2>();
			for (int ix = 0; ix < 30; ix++) list.Add(Observation2.PLACEHOLDER);
			var vm = new TimedObservationsVM(Dispatcher, list);
			DataContext = vm;
		}
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
			try {
				if(DataContext is IRequireRelease irr) {
					irr.Release();
				}
			}
			finally {
				base.OnNavigatingFrom(e);
			}
		}
	}
}
