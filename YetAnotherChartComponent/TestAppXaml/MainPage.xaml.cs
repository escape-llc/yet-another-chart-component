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
			vm.Data.Add(new Observation(0, 0));
			vm.Data.Add(new Observation(1, 10));
			vm.Data.Add(new Observation(2, 5));
			vm.Data.Add(new Observation(3, -10));
			vm.Data.Add(new Observation(4, -5));
			vm.Data.Add(new Observation(5, 0));
			DataContext = vm;
		}
	}
	public class Observation {
		public double X { get; set; }
		public double Y { get; set; }
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
		public Observation(double xx, double yy) { X = xx; Y = yy; }
	}
	public class ViewModel {
		public ObservableCollection<Observation> Data { get; private set; } = new ObservableCollection<Observation>();
	}
}