using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart1 : Page {
		public Chart1() {
			this.InitializeComponent();
		}
		/// <summary>
		/// Initialize VM and set the DataContext.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			var vm = new ObservationsVM(Dispatcher, new[] {
				new Observation("Group 1", -0.5, 1),
				new Observation("Group 2", 3, 10),
				new Observation("Group 3", 2, 5),
				new Observation("Group 4", 3, -10),
				new Observation("Group 5", 4, -3.75),
				new Observation("Group 6", -5.25, 0.5)
			});
			DataContext = vm;
		}

		private void Add_item_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddTail();
		}

		private void Remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveHead();
		}

		private void Add_and_remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddAndRemoveHead();
		}

		private void Remove_tail_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveTail();
		}

		private void Add_head_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddHead();
		}
	}
}
