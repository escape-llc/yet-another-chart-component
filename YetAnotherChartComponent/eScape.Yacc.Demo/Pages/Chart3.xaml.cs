using eScape.Core.Host;
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
			var vm = new TimedObservationsVM(Dispatcher);
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
