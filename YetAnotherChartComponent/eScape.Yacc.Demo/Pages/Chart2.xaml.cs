using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Chart2 : Page {
		public Chart2() {
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
			vm.GroupCounter = vm.Data.Count;
			DataContext = vm;
		}

		private void add_item_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddTail();
		}

		private void remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveHead();
		}

		private void add_and_remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddAndRemoveHead();
		}

		private void remove_tail_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).RemoveTail();
		}

		private void add_head_Click(object sender, RoutedEventArgs e) {
			(DataContext as ObservationsVM).AddHead();
		}
	}
}
