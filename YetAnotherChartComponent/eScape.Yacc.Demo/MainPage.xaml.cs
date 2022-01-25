using eScape.Core;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo {
	/// <summary>
	/// Chart demo page.
	/// </summary>
	public sealed partial class MainPage : Page {
		static readonly LogTools.Flag _trace = LogTools.Add("MainPage", LogTools.Level.Verbose);
		PageItem current;
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
		private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args) {
			// for some reason this is invoked TWO TIMES
			if (args.InvokedItemContainer.DataContext is PageItem pi) {
				if(pi == current) {
					_trace.Warn($"ItemInvoked double-hit: {pi.Description}");
					return;
				}
				try {
					Notification.Content = pi;
					Notification.Show(5000);
					// for now just send in PI
					MainFrame.Navigate(pi.PageType, pi);
				}
				catch(Exception ex) {
					_trace.Error($"ItemInvoked.unhandled: {ex}");
				}
				finally {
					current = pi;
				}
			}
		}
	}
}