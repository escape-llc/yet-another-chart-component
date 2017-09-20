using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TestAppXaml {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page {
		public MainPage() {
			this.InitializeComponent();
		}
		protected override void OnNavigatedTo(NavigationEventArgs e) {
			base.OnNavigatedTo(e);
			var vm = new ViewModel();
			vm.Data.Add(new Observation("Group 1", -0.5, 0.02));
			vm.Data.Add(new Observation("Group 2", 3, 10));
			vm.Data.Add(new Observation("Group 3", 2, 5));
			vm.Data.Add(new Observation("Group 4", 3, -10));
			vm.Data.Add(new Observation("Group 5", 4, -5));
			vm.Data.Add(new Observation("Group 6", -5.25, 0.04));
			vm.GroupCounter = vm.Data.Count;
			DataContext = vm;
		}

		private void add_item_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ViewModel).AddItem();
		}

		private void remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ViewModel).RemoveHead();
		}

		private void add_and_remove_head_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
			(DataContext as ViewModel).AddAndRemoveHead();
		}
	}
	public class Observation {
		public String Label { get; private set; }
		public double Value1 { get; private set; }
		public double Value2 { get; private set; }
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
		public Observation(String label, double v1, double v2) { Label = label; Value1 = v1; Value2 = v2; }
	}
	public class ViewModel {
		readonly Random rnd = new Random();
		public int GroupCounter { get; set; }
		public ObservableCollection<Observation> Data { get; private set; } = new ObservableCollection<Observation>();
		public void AddItem() {
			GroupCounter++;
			var obs = new Observation($"Group {GroupCounter}", 10*rnd.NextDouble() - 5, 10*rnd.NextDouble() - 4);
			Data.Add(obs);
		}
		public void RemoveHead() {
			Data.RemoveAt(0);
		}
		public void AddAndRemoveHead() {
			RemoveHead();
			AddItem();
		}
	}
}