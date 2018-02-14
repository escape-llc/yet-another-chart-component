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
	}
}
