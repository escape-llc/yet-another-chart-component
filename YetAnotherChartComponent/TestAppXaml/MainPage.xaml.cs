using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
			DataContext = vm;
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
		public ObservableCollection<Observation> Data { get; private set; } = new ObservableCollection<Observation>();
	}
}