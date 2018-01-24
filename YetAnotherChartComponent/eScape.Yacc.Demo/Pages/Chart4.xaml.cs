using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Yacc.Demo.VM;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Yacc.Demo.Pages {
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Chart4 : BasicPage {
		public Chart4() {
			this.InitializeComponent();
		}
		/// <summary>
		/// Load the cached prices file and parse it.
		/// </summary>
		/// <returns></returns>
		protected override async Task<object> InitializeDataContextAsync() {
			var sf = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\MSFT.json");
			var jsonText = await Windows.Storage.FileIO.ReadTextAsync(sf);
			var json = JsonObject.Parse(jsonText);
			var prices = json.GetNamedObject("Time Series (Daily)");
			var list = new List<Observation2>();
			foreach(var price in prices) {
				// this is an object not an array so we lose the ordering
				var date = price.Key;
				var ohlcv = price.Value.GetObject();
				var open = ohlcv.GetNamedString("1. open");
				var high = ohlcv.GetNamedString("2. high");
				var low = ohlcv.GetNamedString("3. low");
				var close = ohlcv.GetNamedString("4. close");
				var volume = ohlcv.GetNamedString("5. volume");
				var obs = new Observation2(date, double.Parse(open), double.Parse(high), double.Parse(low), double.Parse(close), int.Parse(volume));
				list.Add(obs);
			}
			// since it's a Map we need to sort things based on the "date" keys
			list.Sort((o1, o2) => o1.Label.CompareTo(o2.Label));
			var vm = new TimedObservationsVM(Dispatcher, list.Take(21).ToList());
			return vm;
		}
	}
}
