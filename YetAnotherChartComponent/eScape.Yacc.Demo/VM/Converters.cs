using eScapeLLC.UWP.Charts;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Yacc.Demo.VM {
	/// <summary>
	/// Example of a <see cref="ValueLabels.LabelFormatter"/> converter.
	/// Compare <see cref="Observation.Value1"/> to <see cref="Observation.Value2"/> and return a style/label.
	/// </summary>
	public class CompareObservationValuesConverter : IValueConverter {
		public Style WhenGreater { get; set; }
		public Style WhenLess { get; set; }
		public Style WhenEqual { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language) {
			if(value is ILabelSelectorContext ilssc) {
				if(ilssc.ItemValue is ISeriesItemValueCustom isivc && isivc.CustomValue is Observation obv) {
					// finally checked things enough to do something!
					if (targetType == typeof(Style)) {
						// select the style based on the difference
						if (obv.Value1 < obv.Value2) return WhenLess;
						else if (obv.Value1 > obv.Value2) return WhenGreater;
						return WhenEqual;
					}
					else if(targetType == typeof(String)) {
						// reformat the label based on the difference
						// select the "trend indicator" in Unicode arrows
						// Left RIght Arrow (U+2914)
						var indi = "\u2194";
						// Down Arrow (U+2193)
						if (obv.Value1 < obv.Value2) indi = "\u2193";
						// Up Arrow (U+2191)
						else if (obv.Value1 > obv.Value2) indi = "\u2191";
						return String.Format("{0:F2}{1}", Math.Abs(obv.Value1 - obv.Value2), indi);
					}
				}
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
}
