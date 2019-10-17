using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace eScapeLLC.UWP.Charts {
	#region IAxisLabelSelectorContext
	/// <summary>
	/// Base context for axis label selector/formatter.
	/// </summary>
	public interface IAxisLabelSelectorContext {
		/// <summary>
		/// Current axis label index.
		/// </summary>
		int Index { get; }
		/// <summary>
		/// The axis presenting labels.
		/// </summary>
		IChartAxis Axis { get; }
		/// <summary>
		/// Axis rendering area in DC.
		/// </summary>
		Rect Area { get; }
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
		static readonly LogTools.Flag _trace = LogTools.Add("AxisCommon", LogTools.Level.Error);
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
			if (LabelStyle != null) return;
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
