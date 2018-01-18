using System.Collections.Generic;
using System.Threading.Tasks;
using Yacc.Demo.VM;

namespace Yacc.Demo.Pages {
	public sealed partial class Chart3 : BasicPage {
		public Chart3() {
			this.InitializeComponent();
		}
#pragma warning disable 1998
		protected override async Task<object> InitializeDataContextAsync() {
			// start out with placeholders so chart "appears" empty and "scrolls" to the left
			var list = new List<Observation2>();
			for (int ix = 0; ix < 30; ix++) list.Add(Observation2.PLACEHOLDER);
			var vm = new TimedObservationsVM(Dispatcher, list) { AutoStart = true };
			return vm;
		}
#pragma warning restore 1998
	}
}
