using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace Yacc.Demo {
	#region MainPage
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
			var vm = new ViewModel();
			vm.Data.Add(new Observation("Group 1", -0.5, 0.02));
			vm.Data.Add(new Observation("Group 2", 3, 10));
			vm.Data.Add(new Observation("Group 3", 2, 5));
			vm.Data.Add(new Observation("Group 4", 3, -10));
			vm.Data.Add(new Observation("Group 5", 4, -5));
			vm.Data.Add(new Observation("Group 6", -5.25, 0.04));
			vm.GroupCounter = vm.Data.Count;
			vm.Recalculate();
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
	#endregion
	#region converters
	/// <summary>
	/// Converter for bool to Visibility.
	/// </summary>
	public class BoolToVisibilityConverter : IValueConverter {
		/// <summary>
		/// convert.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="language"></param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, string language) {
			var isChecked = (bool)value;
			return isChecked ? Visibility.Visible : Visibility.Collapsed;
		}
		/// <summary>
		/// Unconvert.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="language"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	#endregion
	#region View Model stuff
	/// <summary>
	/// Represents one "cell" of the chart.
	/// This would be typical of a SQL DAO or other domain object.
	/// </summary>
	public class Observation {
		public String Label { get; private set; }
		public double Value1 { get; private set; }
		public double Value2 { get; private set; }
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
		public Observation(String label, double v1, double v2) { Label = label; Value1 = v1; Value2 = v2; }
	}
	/// <summary>
	/// Simple VM that maintains list of observations and the average of its two values.
	/// </summary>
	public class ViewModel : INotifyPropertyChanged {
		readonly Random rnd = new Random();
		public event PropertyChangedEventHandler PropertyChanged;
		public int GroupCounter { get; set; }
		public double Value1Average { get; private set; }
		public double Value2Average { get; private set; }
		public ObservableCollection<Observation> Data { get; private set; } = new ObservableCollection<Observation>();
		/// <summary>
		/// Trigger INotifyPropertyChanged event.
		/// </summary>
		/// <param name="name"></param>
		void Changed(String name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
		/// <summary>
		/// Randomly generate an item and add it to end and recalculate.
		/// </summary>
		public void AddItem() {
			GroupCounter++;
			var obs = new Observation($"Group {GroupCounter}", 10*rnd.NextDouble() - 5, 10*rnd.NextDouble() - 4);
			Data.Add(obs);
			Recalculate();
		}
		/// <summary>
		/// Remove the first item and recalculate.
		/// </summary>
		public void RemoveHead() {
			Data.RemoveAt(0);
			Recalculate();
		}
		/// <summary>
		/// Modify both ends of list and recalculate.
		/// </summary>
		public void AddAndRemoveHead() {
			RemoveHead();
			AddItem();
			Recalculate();
		}
		/// <summary>
		/// Recalculate and trigger INPC.
		/// </summary>
		public void Recalculate() {
			Value1Average = Data.Average((ob) => ob.Value1);
			Value2Average = Data.Average((ob) => ob.Value2);
			Changed(nameof(Value1Average));
			Changed(nameof(Value2Average));
		}
	}
	#endregion
}