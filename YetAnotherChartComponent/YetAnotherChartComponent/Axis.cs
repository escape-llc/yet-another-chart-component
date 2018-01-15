using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

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
		public static readonly DependencyProperty PathStyleProperty = DependencyProperty.Register("PathStyle", typeof(Style), typeof(AxisCommon), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="LabelStyle"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register("LabelStyle", typeof(Style), typeof(AxisCommon), new PropertyMetadata(null));
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
		/// </summary>
		protected void ApplyLabelStyle() {
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
	#region ValueAxis
	/// <summary>
	/// Value axis is a "vertical" axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Path for the axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Geometry for the axis bar.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
		/// <summary>
		/// List of active TextBlocks for labels.
		/// </summary>
		protected List<TextBlock> TickLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// Creates Value/Left/Vertical axis.
		/// </summary>
		public ValueAxis() :base(AxisType.Value, AxisOrientation.Vertical, Side.Left) {
			CommonInit();
		}
		#endregion
		#region helpers
		private void CommonInit() {
			TickLabels = new List<TextBlock>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinWidth = 32;
		}
		void DoBindings(IChartEnterLeaveContext icelc) {
			BindTo(this, "PathStyle", Axis, Path.StyleProperty);
		}
		#endregion
		#region extensions
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			DoBindings(icelc);
			ApplyLabelStyle();
			if (PathStyle == null && Theme != null) {
				if (Theme.PathAxisCategory != null) PathStyle = Theme.PathAxisCategory;
			}
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc">The context.</param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Claim the space indicated by properties.
		/// </summary>
		/// <param name="iclc"></param>
		void IRequireLayout.Layout(IChartLayoutContext iclc) {
			var space = AxisMargin + AxisLineThickness + MinWidth;
			iclc.ClaimSpace(this, Side, space);
		}
		/// <summary>
		/// Layout axis components (bar, grid, labels).
		/// Each component has a corresponding transform (applied in Transforms()).  Right and Left are DUALs of each other wrt to horizontal axis.
		/// Axis "bar" and Tick marks:
		///		x: PX (scale 1)
		///		y: "axis" scale
		/// Tick labels:
		///		x, y: PX
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			// axis and tick marks
			AxisGeometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Side == Side.Right ? 0 : icrc.Area.Width, Minimum, Side == Side.Right ? AxisLineThickness : icrc.Area.Width - AxisLineThickness, Maximum);
			AxisGeometry.Figures.Add(pf);
			// grid lines
			var tc = new TickCalculator(Minimum, Maximum);
			_trace.Verbose($"grid range:{tc.Range} tintv:{tc.TickInterval}");
			//icrc.Remove(TickLabels);
			// grid lines and tick labels
			// layout and recycle labels
			var padding = AxisLineThickness + 2 * AxisMargin;
			var tbr = new Recycler<TextBlock>(TickLabels, () => {
				if (LabelStyle != null) {
					// let style override everything but what MUST be calculated
					var tb = new TextBlock() {
						Width = icrc.Area.Width - padding,
						Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0)
					};
					tb.Style = LabelStyle;
					return tb;
				} else {
					// SHOULD NOT execute this code, unless default style failed!
					var tb = new TextBlock() {
						FontSize = 10,
						Foreground = Axis.Fill,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = Side == Side.Right ? HorizontalAlignment.Left : HorizontalAlignment.Right,
						Width = icrc.Area.Width - padding,
						TextAlignment = Side == Side.Right ? TextAlignment.Left : TextAlignment.Right,
						Padding = Side == Side.Right ? new Thickness(padding, 0, 0, 0) : new Thickness(0, 0, padding, 0)
					};
					return tb;
				}
			});
			var tbget = tbr.Items().GetEnumerator();
			foreach (var tick in tc.GetTicks()) {
				//_trace.Verbose($"grid vx:{tick}");
				if (tbget.MoveNext()) {
					var tb = tbget.Current;
					tb.Text = tick.ToString(String.IsNullOrEmpty(LabelFormatString) ? "G" : LabelFormatString);
					tb.SetValue(Canvas.LeftProperty, icrc.Area.Left);
					tb.SetValue(Canvas.TopProperty, tick);
					// cheat: save the grid value so we can rescale the Canvas.Top in Transforms()
					tb.Tag = tick;
				}
			}
			// VT and internal bookkeeping
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			foreach (var tb in tbr.Unused) {
				TickLabels.Remove(tb);
			}
			TickLabels.AddRange(tbr.Created);
			Dirty = false;
		}
		/// <summary>
		/// X-coordinates	"px"
		/// Y-coordinates	[0..1]
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scaley = icrc.Area.Height / Range;
			var matx = new Matrix(1, 0, 0, -scaley, icrc.Area.Left + AxisMargin*(Side == Side.Right ? 1 : -1), icrc.Area.Top + Maximum*scaley);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sy:{scaley:F3} matx:{matx} a:{icrc.Area} sa:{icrc.SeriesArea}");
			foreach(var tb in TickLabels) {
				var vx = (double)tb.Tag;
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left);
				var adj = tb.FontSize / 2;
				var top = icrc.Area.Bottom - (vx - Minimum) * scaley - adj;
				tb.SetValue(Canvas.TopProperty, top);
			}
		}
		#endregion
	}
	#endregion
	#region CategoryAxis
	/// <summary>
	/// Horizontal Category axis.
	/// Category axis cells start on the left and extend rightward (in device X-units).
	/// The discrete category axis is a simple "positional-index" axis [0..N-1].  Each index defines a "cell" that allows "normalized" positioning [0..1) within the cell.
	/// Certain series types MAY extend the discrete axis by ONE cell, to draw the "last" elements there.
	/// </summary>
	public class CategoryAxis : AxisCommon, IRequireLayout, IRequireRender, IRequireTransforms, IRequireEnterLeave {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Error);
		#region properties
		/// <summary>
		/// Path for axis "bar".
		/// </summary>
		protected Path Axis { get; set; }
		/// <summary>
		/// Axis bar geometry.
		/// </summary>
		protected PathGeometry AxisGeometry { get; set; }
		/// <summary>
		/// Manage labels.
		/// </summary>
		protected Dictionary<int, Tuple<double, string>> LabelMap { get; set; } = new Dictionary<int, Tuple<double, string>>();
		/// <summary>
		/// List of active TextBlocks for labels.
		/// </summary>
		protected List<TextBlock> TickLabels { get; set; }
		/// <summary>
		/// The layer to manage components.
		/// </summary>
		protected IChartLayer Layer { get; set; }
		#endregion
		#region ctor
		/// <summary>
		/// Default ctor.
		/// </summary>
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal, Side.Bottom) {
			CommonInit();
		}
		private void CommonInit() {
			TickLabels = new List<TextBlock>();
			Axis = new Path();
			AxisGeometry = new PathGeometry();
			Axis.Data = AxisGeometry;
			MinHeight = 24;
		}
		#endregion
		#region extensions
		/// <summary>
		/// Clear the label map in addition to default impl.
		/// </summary>
		public override void ResetLimits() {
			LabelMap.Clear();
			base.ResetLimits();
		}
		/// <summary>
		/// Labels are cached for presentation.
		/// </summary>
		/// <param name="valueWithLabel"></param>
		/// <returns>base.For(double)</returns>
		public override double For(Tuple<double, string> valueWithLabel) {
			var mv = base.For(valueWithLabel.Item1);
			int key = (int)mv;
			if(LabelMap.ContainsKey(key)) {
				// should be an error but just overwrite it
				LabelMap[key] = valueWithLabel;
			}
			else {
				LabelMap.Add(key, valueWithLabel);
			}
			return mv;
		}
		/// <summary>
		/// Add elements and attach bindings.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Enter(IChartEnterLeaveContext icelc) {
			Layer = icelc.CreateLayer(Axis);
			BindTo(this, "PathStyle", Axis, Path.StyleProperty);
			ApplyLabelStyle();
			if (PathStyle == null && Theme != null) {
				if (Theme.PathAxisCategory != null) PathStyle = Theme.PathAxisCategory;
			}
		}
		/// <summary>
		/// Reverse effect of Enter.
		/// </summary>
		/// <param name="icelc"></param>
		void IRequireEnterLeave.Leave(IChartEnterLeaveContext icelc) {
			icelc.DeleteLayer(Layer);
			Layer = null;
		}
		/// <summary>
		/// Claim the space indicated by properties.
		/// </summary>
		/// <param name="iclc"></param>
		void IRequireLayout.Layout(IChartLayoutContext iclc) {
			var space = AxisMargin + AxisLineThickness + MinHeight; 
			iclc.ClaimSpace(this, Side, space);
		}
		/// <summary>
		/// Compute axis visual elements.
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireRender.Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			AxisGeometry.Figures.Clear();
			//icrc.Remove(TickLabels);
			var pf = PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness);
			AxisGeometry.Figures.Add(pf);
			var i1 = (int)Minimum;
			var i2 = (int)Maximum;
			var scalex = icrc.Area.Width / Range;
			// recycle and lay out tick labels
			var tbr = new Recycler<TextBlock>(TickLabels, () => {
				if (LabelStyle != null) {
					// let style override everything but what MUST be calculated
					var tb = new TextBlock() {
						Width = scalex,
					};
					tb.Style = LabelStyle;
					return tb;
				} else {
					// SHOULD NOT execute this code, unless default style failed!
					var tb = new TextBlock() {
						FontSize = 10,
						Foreground = Axis.Fill,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Center,
						Width = scalex,
						TextAlignment = TextAlignment.Center
					};
					return tb;
				}
			});
			var tbget = tbr.Items().GetEnumerator();
			for (var ix = i1; ix <= i2; ix++) {
				if(LabelMap.ContainsKey(ix)) {
					// create a label
					var tpx = LabelMap[ix];
					_trace.Verbose($"key {ix} label {tpx.Item2}");
					if(tbget.MoveNext()) {
						var tb = tbget.Current;
						tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + ix * scalex);
						tb.SetValue(Canvas.TopProperty, icrc.Area.Top + AxisLineThickness + 2 * AxisMargin);
						// cheat: save the grid value so we can rescale the Left in Transforms()
						tb.Tag = tpx;
						tb.Text = tpx.Item2;
					}
				}
			}
			// VT and internal bookkeeping
			Layer.Remove(tbr.Unused);
			Layer.Add(tbr.Created);
			foreach (var tb in tbr.Unused) {
				TickLabels.Remove(tb);
			}
			TickLabels.AddRange(tbr.Created);
			Dirty = false;
		}
		/// <summary>
		/// X-coordinates	axis
		/// Y-coordinates	"px"
		/// Grid-coordinates (x:axis, y:[0..1])
		/// </summary>
		/// <param name="icrc"></param>
		void IRequireTransforms.Transforms(IChartRenderContext icrc) {
			var scalex = icrc.Area.Width / Range;
			var matx = new Matrix(scalex, 0, 0, 1, icrc.Area.Left, icrc.Area.Top + AxisMargin);
			AxisGeometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sx:{scalex:F3} matx:{matx} a:{icrc.Area}");
			foreach (var tb in TickLabels) {
				var vx = (Tuple<double,String>)tb.Tag;
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + vx.Item1 * scalex);
				tb.SetValue(Canvas.TopProperty, icrc.Area.Top + AxisLineThickness + 2 * AxisMargin);
				tb.Width = scalex;
			}
		}
		#endregion
	}
	#endregion
}
