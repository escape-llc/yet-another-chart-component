using eScapeLLC.UWP.Charts;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Yacc.Demo.VM {
	/// <summary>
	/// Example of a <see cref="ValueLabels.StyleSelector"/> converter.
	/// Compare <see cref="Observation.Value1"/> to <see cref="Observation.Value2"/> and return a style.
	/// </summary>
	public class CompareObservationValuesConverter : IValueConverter {
		public Style WhenGreater { get; set; }
		public Style WhenLess { get; set; }
		public Style WhenEqual { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language) {
			if(targetType == typeof(Style) && value is ILabelStyleSelectorContext ilssc) {
				if(ilssc.ItemValue is ISeriesItemValueCustom isivc && isivc.CustomValue is Observation obv) {
					// finally checked things enough to do something!
					if (obv.Value1 < obv.Value2) return WhenLess;
					else if (obv.Value1 > obv.Value2) return WhenGreater;
					return WhenEqual;
				}
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
}
