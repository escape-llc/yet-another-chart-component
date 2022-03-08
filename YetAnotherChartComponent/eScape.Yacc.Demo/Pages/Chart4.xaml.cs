using eScape.Core.Page;
using System.Diagnostics;
using Windows.UI.Xaml.Navigation;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart4 : BasicPage {
		public override string PageTitle => "Candlestick";
		public Chart4() {
			this.InitializeComponent();
		}
		protected override object InitializeDataContext(NavigationEventArgs e) {
			return new DateRangeVM(Dispatcher);
		}

		private void CalendarDatePicker_DateChanged(Windows.UI.Xaml.Controls.CalendarDatePicker sender, Windows.UI.Xaml.Controls.CalendarDatePickerDateChangedEventArgs args) {
			if(DataContext is DateRangeVM drvm) drvm.RefreshData();
		}
		/// <summary>
		/// NOTE: this is required to get the <see cref="FrameworkElement.ActualWidth"/> binding to work properly!
		/// This is required only for the right-edge rotated label transform (see the XAML).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void chart_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e) {
			//Debug.WriteLine($"size changed {chart.ActualWidth}");
			Bindings.Update();
		}
	}
}
