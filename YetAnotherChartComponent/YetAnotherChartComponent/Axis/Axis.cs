using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace eScapeLLC.UWP.Charts {
	#region TickCalculator
	/// <summary>
	/// Helper to calculate grid lines/tick marks.
	/// The "sweet spot" for grids is 10, so calculations use base-10 logarithms.
	/// The grid "origin" is selected as follows:
	///		If the extents have opposing signs, ZERO is the "center" point (regardless of where it falls),
	///		Otherwise it's the arithmetic midpoint (Minimum + Range/2) rounded to the nearest TickInterval.
	/// </summary>
	public class TickCalculator {
		#region properties
		/// <summary>
		/// The low extent.
		/// </summary>
		public double Minimum { get; private set; }
		/// <summary>
		/// The high extent.
		/// </summary>
		public double Maximum { get; private set; }
		/// <summary>
		/// Range of the interval.
		/// Initialized in ctor.
		/// </summary>
		public double Range { get; private set; }
		/// <summary>
		/// Computed tick interval, based on range.
		/// Initialized in ctor.
		/// SHOULD be a Power of Ten.
		/// </summary>
		public double TickInterval { get; private set; }
		/// <summary>
		/// Power of 10 to use for ticks.
		/// Based on Log10(Range).
		/// </summary>
		public int DecimalPlaces { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Initialize Range, TickInterval.
		/// </summary>
		/// <param name="minimum">The minimum value.</param>
		/// <param name="maximum">The maximum value.</param>
		public TickCalculator(double minimum, double maximum) {
			Minimum = minimum;
			Maximum = maximum;
			Range = Math.Abs(maximum - minimum);
			DecimalPlaces = (int)Math.Round(Math.Log10(Range) - 1.0);
			TickInterval = Math.Pow(10, DecimalPlaces);
		}
		#endregion
		#region public
		/// <summary>
		/// Round the number to nearest multiple.
		/// </summary>
		/// <param name="val">Source value.</param>
		/// <param name="multiple">Multiple for rounding.</param>
		/// <returns>Rounded value.</returns>
		public static double RoundTo(double val, double multiple) {
			return Math.Round(val / multiple) * multiple;
		}
		/// <summary>
		/// Compare the values, using the given threshold.
		/// As is common in floating-point-land, the bit pattern produced by a numeric "string" may not match exactly.
		/// Underlying cause is the power-of-two/power-of-ten mismatch.
		/// There is also possibility of accumulated error, depending on source calculations.
		/// </summary>
		/// <param name="v1">Value 1.</param>
		/// <param name="v2">Value 2.</param>
		/// <param name="epsilon">Threshold for comparison.</param>
		/// <returns>True: under threshold; False: over or equal threshold.</returns>
		public static bool Equals(double v1, double v2, double epsilon) {
			return Math.Abs(v1 - v2) < epsilon;
		}
		/// <summary>
		/// Enumerate the ticks.
		/// Starts at the "center" and works "outward" toward each extent, alternating positive/negative direction.
		/// Once an extent is "filled", only values of the opposite extent SHALL appear.
		/// If extents "cross" zero, start at zero, otherwise the "center" of the range, to nearest tick.
		/// </summary>
		/// <returns>Series of values, as described above.</returns>
		public IEnumerable<double> GetTicks() {
			var center = Math.Sign(Minimum) != Math.Sign(Maximum) ? 0 : RoundTo(Minimum + Range / 2, TickInterval);
			yield return center;
			var inside = true;
			for (int ix = 1; inside; ix++) {
				bool didu = false, didl = false;
				// do it this way to avoid accumulated error
				var upper = center + ix * TickInterval;
				var lower = center - ix * TickInterval;
				if (upper <= Maximum) {
					yield return upper;
					didu = true;
				}
				if (lower >= Minimum) {
					yield return lower;
					didl = true;
				}
				inside = didu || didl;
			}
		}
		#endregion
	}
	#endregion
	#region AxisCommon
	/// <summary>
	/// Consolidate common properties for axes and implement IChartAxis.
	/// By default, axes auto-scale their limits based on "observed" values.
	/// Setting the LimitXXX properties to a non-NaN "fixes" that limit.
	/// It's possible to have one or both limits "fixed" in this way.
	/// When limits are fixed, some chart elements may not appear due to clipping.
	/// </summary>
	public abstract class AxisCommon : ChartComponent, IChartAxis, IRequireChartTheme {
		static LogTools.Flag _trace = LogTools.Add("AxisCommon", LogTools.Level.Error);
		#region properties
		#region axis
		/// <summary>
		/// The axis type.
		/// </summary>
		public AxisType Type { get; private set; }
		/// <summary>
		/// The axis orientation.
		/// </summary>
		public AxisOrientation Orientation { get; private set; }
		/// <summary>
		/// The axis side.
		/// </summary>
		public Side Side { get; set; }
		/// <summary>
		/// The lower limit or NaN if not initialized.
		/// </summary>
		public double Minimum { get; protected set; } = double.NaN;
		/// <summary>
		/// The upper limit or NaN if not initialized.
		/// </summary>
		public double Maximum { get; protected set; } = double.NaN;
		/// <summary>
		/// The axis range or NaN if limits were not initialized.
		/// </summary>
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		/// <summary>
		/// Set this to override auto-scaling behavior on Minimum.
		/// Default value is NaN.
		/// </summary>
		public double LimitMinimum { get; set; } = double.NaN;
		/// <summary>
		/// Set this to override auto-scaling behavior on Maximum.
		/// Default value is NaN.
		/// </summary>
		public double LimitMaximum { get; set; } = double.NaN;
		#endregion
		#region presentation
		/// <summary>
		/// The PX width of the axis "line".
		/// Default value is 2.
		/// </summary>
		public double AxisLineThickness { get; set; } = 2;
		/// <summary>
		/// The PX margin of the axis "line" from the edge of its bounds facing the data area.
		/// Default value is 2.
		/// </summary>
		public double AxisMargin { get; set; } = 2;
		/// <summary>
		/// Alternate format string for labels.
		/// </summary>
		public String LabelFormatString { get; set; }
		/// <summary>
		/// Receive the theme for axis labels, etc.
		/// </summary>
		public IChartTheme Theme { get; set; }
		/// <summary>
		/// The style to use for Path geometry.
		/// </summary>
		public Style PathStyle { get { return (Style)GetValue(PathStyleProperty); } set { SetValue(PathStyleProperty, value); } }
		/// <summary>
		/// The style to apply to labels.
		/// </summary>
		public Style LabelStyle { get { return (Style)GetValue(LabelStyleProperty); } set { SetValue(LabelStyleProperty, value); } }
		#endregion
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="PathStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register(nameof(PathStyle), typeof(Style), typeof(AxisCommon), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(nameof(LabelStyle), typeof(Style), typeof(AxisCommon), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="at"></param>
		/// <param name="ao"></param>
		/// <param name="sd"></param>
		protected AxisCommon(AxisType at, AxisOrientation ao, Side sd) {
			Type = at;
			Orientation = ao;
			Side = sd;
		}
		#endregion
		#region helpers
		/// <summary>
		/// Initialize the LabelStyle from the Theme, if it is NULL.
		/// Provide error reporting.
		/// </summary>
		/// <param name="icei">For error reporting.</param>
		protected void ApplyLabelStyle(IChartErrorInfo icei) {
			if (LabelStyle == null && Theme != null) {
				switch (Side) {
				case Side.Left:
					if (Theme.LabelAxisLeft != null) LabelStyle = Theme.LabelAxisLeft;
					break;
				case Side.Right:
					if (Theme.LabelAxisRight != null) LabelStyle = Theme.LabelAxisRight;
					break;
				case Side.Top:
					if (Theme.LabelAxisTop != null) LabelStyle = Theme.LabelAxisTop;
					break;
				case Side.Bottom:
					if (Theme.LabelAxisBottom != null) LabelStyle = Theme.LabelAxisBottom;
					break;
				}
				if(LabelStyle == null) {
					if (icei != null) {
						var sidex = $"LabelAxis{Side}";
						icei.Report(new ChartValidationResult(NameOrType(), $"{nameof(LabelStyle)} not found and {sidex} not found", new[] { nameof(LabelStyle), sidex }));
					}
				}
			} else {
				if (icei != null) {
					var sidex = $"LabelAxis{Side}";
					icei.Report(new ChartValidationResult(NameOrType(), $"{nameof(LabelStyle)} not found and no Theme was found for {sidex}", new[] { nameof(LabelStyle), sidex }));
				}
			}
		}
		#endregion
		#region public
		/// <summary>
		/// Reset the limits to LimitXXX.
		/// Set Dirty = true.
		/// </summary>
		public virtual void ResetLimits() { Minimum = LimitMinimum; Maximum = LimitMaximum; Dirty = true; }
		/// <summary>
		/// Map given data value to that of the axis.
		/// </summary>
		/// <param name="value">Value to "see".</param>
		/// <returns>The "mapped" value. By default it's the identity.</returns>
		public virtual double For(double value) { return value; }
		/// <summary>
		/// Map given value, and register label.
		/// </summary>
		/// <param name="valueWithLabel"></param>
		/// <returns>Identity Tuple.Value1.</returns>
		public virtual double For(Tuple<double, String> valueWithLabel) { return valueWithLabel.Item1; }
		/// <summary>
		/// Calculate the scale factor for this axis OR NaN.
		/// </summary>
		/// <param name="dimension"></param>
		/// <returns>The scale OR NaN.</returns>
		public virtual double ScaleFor(double dimension) { return double.IsNaN(Range) || double.IsNaN(dimension) ? double.NaN : dimension / Range; }
		/// <summary>
		/// Update the min/max.
		/// Sets Dirty = true if it updates either limit.
		/// </summary>
		/// <param name="value"></param>
		public void UpdateLimits(double value) {
			if (double.IsNaN(LimitMinimum) && (double.IsNaN(Minimum) || value < Minimum)) { Minimum = value; Dirty = true; }
			if (double.IsNaN(LimitMaximum) && (double.IsNaN(Maximum) || value > Maximum)) { Maximum = value; Dirty = true; }
		}
		#endregion
	}
	#endregion
}
