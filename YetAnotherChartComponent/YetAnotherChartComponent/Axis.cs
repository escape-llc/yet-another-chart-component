using eScape.Core;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region AxisCommon
	/// <summary>
	/// Consolidate common properties for axes and implement IChartAxis.
	/// </summary>
	public abstract class AxisCommon : ChartComponent, IChartAxis {
		#region properties
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
		public Side Side { get; private set; }
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
		/// The PX width of the axis "line".
		/// </summary>
		public double AxisLineThickness { get; set; } = 2;
		/// <summary>
		/// The PX margin of the axis "line" from the edge of its bounds facing the data area.
		/// </summary>
		public double AxisMargin { get; set; } = 2;
		/// <summary>
		/// The brush for the axis "line".
		/// </summary>
		public Brush Brush { get { return (Brush)GetValue(BrushProperty); } set { SetValue(BrushProperty, value); } }
		/// <summary>
		/// The brush for the axis grid lines.
		/// </summary>
		public Brush GridBrush { get { return (Brush)GetValue(GridBrushProperty); } set { SetValue(GridBrushProperty, value); } }
		public double GridStrokeThickness { get; set; } = 0.5;
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Brush"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(Brush), typeof(AxisCommon), new PropertyMetadata(null));
		/// <summary>
		/// Identifies <see cref="GridBrush"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register("GridBrush", typeof(Brush), typeof(AxisCommon), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="at"></param>
		/// <param name="ao"></param>
		/// <param name="sd"></param>
		public AxisCommon(AxisType at, AxisOrientation ao, Side sd) {
			Type = at;
			Orientation = ao;
			Side = sd;
		}
		#endregion
		#region public
		/// <summary>
		/// Reset the limits to NaN.
		/// </summary>
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; Dirty = true; }
		/// <summary>
		/// Update limits based on given value.
		/// </summary>
		/// <param name="value">Value to "see".</param>
		/// <returns>The "mapped" value. By default it's the identity.</returns>
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; Dirty = true; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; Dirty = true; }
			return value;
		}
		#endregion
	}
	#endregion
	#region ValueAxis
	/// <summary>
	/// Value axis is a vertical axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : AxisCommon {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Verbose);
		#region properties
		protected Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		protected Path GridLines { get; set; }
		protected GeometryGroup GridGeometry { get; set; }
		protected List<TextBlock> TickLabels { get; set; }
		#endregion
		#region ctor
		public ValueAxis() :base(AxisType.Value, AxisOrientation.Vertical, Side.Right) {
			CommonInit();
		}
		public ValueAxis(Side sd) :base(AxisType.Value, AxisOrientation.Vertical, sd) {
			CommonInit();
		}
		private void CommonInit() {
			TickLabels = new List<TextBlock>();
			Segments = new Path();
			Geometry = new PathGeometry();
			Segments.Data = Geometry;
			BindBrush(this, "Brush", Segments, Path.FillProperty);
			GridLines = new Path();
			GridGeometry = new GeometryGroup();
			GridLines.Data = GridGeometry;
			BindBrush(this, "GridBrush", GridLines, Path.StrokeProperty);
			GridLines.SetValue(Path.StrokeThicknessProperty, GridStrokeThickness);
		}
		#endregion
		#region extensions
		public override void Enter(IChartEnterLeaveContext icelc) {
			icelc.Add(Segments);
			icelc.Add(GridLines);
		}
		public override void Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(GridLines);
			icelc.Remove(Segments);
		}
		public override void Layout(IChartLayoutContext iclc) {
			// TODO add space for "nominal" label width
			var space = AxisMargin + AxisLineThickness + 48;
			iclc.ClaimSpace(this, Side.Right, space);
		}
		public override void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			// axis and tick marks
			Geometry.Figures.Clear();
			var pf = PathHelper.Rectangle(0, Minimum, AxisLineThickness, Maximum);
			Geometry.Figures.Add(pf);
			// grid lines
			var start = Math.Round(Minimum);
			var end = Math.Round(Maximum);
			var incr = 1.0;
			_trace.Verbose($"grid start:{start} end:{end} inc:{incr}");
			GridGeometry.Children.Clear();
			icrc.Remove(TickLabels);
			for (var vx = start; vx <= end; vx += incr) {
				//_trace.Verbose($"grid vx:{vx}");
				var grid = new LineGeometry() { StartPoint = new Point(0, vx), EndPoint = new Point(1, vx) };
				GridGeometry.Children.Add(grid);
				var tb = new TextBlock() { FontSize = 10, Text = vx.ToString("F3"), VerticalAlignment =VerticalAlignment.Center, TextAlignment = TextAlignment.Left };
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + 2*AxisMargin);
				tb.SetValue(Canvas.TopProperty, vx);
				// cheat: save the grid value so we can rescale the Top
				tb.Tag = vx;
				TickLabels.Add(tb);
			}
			icrc.Add(TickLabels);
			Dirty = false;
		}
		/// <summary>
		/// X-coordinates	"px"
		/// Y-coordinates	axis
		/// </summary>
		/// <param name="icrc"></param>
		public override void Transforms(IChartRenderContext icrc) {
			var scaley = icrc.Area.Height / Range;
			var matx = new Matrix(1, 0, 0, scaley, icrc.Area.Left + AxisMargin, icrc.Area.Top + icrc.Area.Height/2);
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			var gscaley = icrc.SeriesArea.Height / Range;
			var gscalex = icrc.SeriesArea.Width;
			var gmatx = new Matrix(gscalex, 0, 0, gscaley, icrc.SeriesArea.Left, icrc.SeriesArea.Top + icrc.SeriesArea.Height / 2);
			_trace.Verbose($"transforms sy:{scaley} matx:{matx} gmatx:{gmatx} sa:{icrc.SeriesArea}");
			GridGeometry.Transform = new MatrixTransform() { Matrix = gmatx };
			foreach(var tb in TickLabels) {
				var vx = (double)tb.Tag;
				tb.SetValue(Canvas.LeftProperty, icrc.Area.Left + 2 * AxisMargin);
				tb.SetValue(Canvas.TopProperty, icrc.Area.Bottom - (vx - Minimum)*scaley - tb.FontSize/2);
			}
		}
		#endregion
	}
	#endregion
	#region CategoryAxis
	/// <summary>
	/// Horizontal Category axis.
	/// </summary>
	public class CategoryAxis : AxisCommon {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Verbose);
		#region properties
		protected Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		#endregion
		#region ctor
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal, Side.Bottom) {
			CommonInit();
		}
		public CategoryAxis(Side sd) :base(AxisType.Category, AxisOrientation.Horizontal, sd) {
			CommonInit();
		}
		private void CommonInit() {
			Segments = new Path();
			this.Geometry = new PathGeometry();
			Segments.Data = this.Geometry;
			BindBrush(this, "Brush", Segments, Path.FillProperty);
		}
		#endregion
		#region extensions
		public override void Enter(IChartEnterLeaveContext icelc) {
			icelc.Add(Segments);
		}
		public override void Leave(IChartEnterLeaveContext icelc) {
			icelc.Remove(Segments);
		}
		public override void Layout(IChartLayoutContext iclc) {
			// TODO add space for "nominal" text height
			var space = AxisMargin + AxisLineThickness + 24;
			iclc.ClaimSpace(this, Side.Bottom, space);
		}
		public override void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			Geometry.Figures.Clear();
			var pf = PathHelper.Rectangle(Minimum, 0, Maximum, AxisLineThickness);
			Geometry.Figures.Add(pf);
			Dirty = false;
		}
		public override void Transforms(IChartRenderContext icrc) {
			var scalex = icrc.Area.Width / Range;
			var matx = new Matrix(scalex, 0, 0, 1, icrc.Area.Left, icrc.Area.Top + AxisMargin);
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
			_trace.Verbose($"transforms sx:{scalex} matx:{matx} a:{icrc.Area}");
		}
		#endregion
	}
	#endregion
}
