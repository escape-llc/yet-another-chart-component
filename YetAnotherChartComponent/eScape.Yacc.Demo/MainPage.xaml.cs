using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo {
	/// <summary>
	/// Chart demo page.
	/// </summary>
	public sealed partial class MainPage : Page {
		public MainPage() {
			this.InitializeComponent();
		}
		/// <summary>
		/// Initialize VM and set the DataContext.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			var vm = new MainPageVM(Dispatcher);
			DataContext = vm;
		}

		private void HamburgerMenu_ItemInvoked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.HamburgetMenuItemInvokedEventArgs e) {
			if(e.InvokedItem is PageItem pi) {
				Notification.Content = pi;
				Notification.Show();
				// for now just send in PI
				MainFrame.Navigate(pi.PageType, pi);
			}
		}
	}
}