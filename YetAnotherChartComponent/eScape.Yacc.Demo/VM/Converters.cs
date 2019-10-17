using eScapeLLC.UWP.Charts;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Yacc.Demo.VM {
	public class ValueAxisGridZeroLine : IValueConverter {
		public Style WhenZero { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language) {
			if (WhenZero == null) return null;
			if (value is IValueAxisLabelSelectorContext ivalsc) {
				if (targetType == typeof(Tuple<Style, String>)) {
					var ox = ivalsc.AllTicks[ivalsc.Index];
					if(ox.Value == 0.0) {
						return new Tuple<Style, String>(WhenZero, null);
					}
				}
			}
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	public class CategoryAxisDateRangeConverter : IValueConverter {
		DayOfWeek DowFor(object ox) {
			if (ox is DateTime dt) {
				return dt.DayOfWeek;
			} else if (ox is DateTimeOffset dto) {
				return dto.DayOfWeek;
			}
			return DayOfWeek.Sunday;
		}
		public object Convert(object value, Type targetType, object parameter, string language) {
			if (value is ICategoryAxisLabelSelectorContext icalsc) {
				if (targetType == typeof(bool)) {
					var range = icalsc.Axis.Range;
					var show = true;
					if (range > 30) {
						// if showing more than 30, just show Monday dates
						// yes this will skip over Monday US holidays like Labor Day
						var ox = icalsc.AllLabels[icalsc.Index].Label;
						var dow = DowFor(ox);
						if (icalsc.Index > 1 && dow == DayOfWeek.Tuesday) {
							// see if previous day was monday and show this one if not
							var dow2 = DowFor(icalsc.AllLabels[icalsc.Index - 1].Label);
							show = dow2 != DayOfWeek.Monday;
						} else {
							show = dow == DayOfWeek.Monday;
						}
					}
					return show;
				}
			}
			return true;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	public class CategoryAxisDateFormatConverter : IValueConverter {
		public String FormatString { get; set; } = "dd-MMM-yy";
		public object Convert(object value, Type targetType, object parameter, string language) {
			if (value is ICategoryAxisLabelSelectorContext icalsc) {
				if (targetType == typeof(Tuple<Style, String>)) {
					var ox = icalsc.AllLabels[icalsc.Index].Label;
					if (ox is DateTime dt) {
						return new Tuple<Style,String>(null, dt.ToString(FormatString));
					}
					else if(ox is DateTimeOffset dto) {
						return new Tuple<Style, String>(null, dto.ToString(FormatString));
					} else {
						return new Tuple<Style, String>(null, ox?.ToString());
					}
				}
			}
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	/// <summary>
	/// Example of a <see cref="ValueLabels.LabelSelector"/> converter.
	/// Only accept labels for values that match the <see cref="IProvideValueExtents"/> limits, i.e. min/max values.
	/// </summary>
	public class MinMaxObservationValueConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, string language) {
			if (value is ILabelSelectorContext ilssc) {
				if (targetType == typeof(bool)) {
					if (ilssc.Source is IProvideValueExtents ipve && ilssc.ItemValue is ISeriesItemValueDouble isivd) {
						// finally checked things enough to do something!
						return isivd.Value >= ipve.Maximum || isivd.Value <= ipve.Minimum;
					}
				}
			}
			return true;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language) {
			throw new NotImplementedException();
		}
	}
	/// <summary>
	/// Example of a <see cref="ValueLabels.LabelFormatter"/> converter.
	/// Compare <see cref="Observation.Value1"/> to <see cref="Observation.Value2"/> and return a style/label.
	/// </summary>
	public class CompareObservationValuesConverter : IValueConverter {
		public Style WhenGreater { get; set; }
		public Style WhenLess { get; set; }
		public Style WhenEqual { get; set; }
		string Format(Observation obv, String indi) {
			return String.Format("{0:F2}{1}", Math.Abs(obv.Value1 - obv.Value2), indi);
		}
		public object Convert(object value, Type targetType, object parameter, string language) {
			if(value is ILabelSelectorContext ilssc) {
				if(ilssc.ItemValue is ISeriesItemValueCustom isivc && isivc.CustomValue is Observation obv) {
					// finally checked things enough to do something!
					if (targetType == typeof(Tuple<Style,String>)) {
						// select the style based on the difference
						// format the label based on the difference
						// select the "trend indicator" in Unicode arrows
						// Left Right Arrow (U+2194)
						// Down Arrow (U+2193)
						// Up Arrow (U+2191)
						if (obv.Value1 < obv.Value2) return new Tuple<Style,String>(WhenLess, Format(obv, "\u2193"));
						else if (obv.Value1 > obv.Value2) return new Tuple<Style, String>(WhenGreater, Format(obv, "\u2191"));
						return new Tuple<Style, String>(WhenEqual, Format(obv, "\u2194"));
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
