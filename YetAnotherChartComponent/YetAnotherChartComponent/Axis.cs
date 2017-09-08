using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region AxisCommon
	/// <summary>
	/// Consolidate common properties for axes.
	/// </summary>
	public abstract class AxisCommon : ChartComponent {
		#region properties
		public AxisType Type { get; private set; }
		public AxisOrientation Orientation { get; private set; }
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		public double AxisLineThickness { get; set; } = 2;
		public double AxisMargin { get; set; } = 2;
		/// <summary>
		/// The brush for the axis "line".
		/// </summary>
		public Brush Brush { get { return (Brush)GetValue(BrushProperty); } set { SetValue(BrushProperty, value); } }
		#endregion
		#region DPs
		/// <summary>
		/// Identifies <see cref="Brush"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(Brush), typeof(AxisCommon), new PropertyMetadata(null));
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="at"></param>
		/// <param name="ao"></param>
		public AxisCommon(AxisType at, AxisOrientation ao) {
			Type = at;
			Orientation = ao;
		}
		#endregion
	}
	#endregion
	#region ValueAxis
	/// <summary>
	/// Value axis is a vertical axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : AxisCommon, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Verbose);
		#region properties
		protected Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		#endregion
		#region ctor
		public ValueAxis() :base(AxisType.Value, AxisOrientation.Vertical) {
			Segments = new Path();
			this.Geometry = new PathGeometry();
			Segments.Data = this.Geometry;
			BindBrush(this, "Brush", Segments, Path.FillProperty);
		}
		#endregion
		#region IChartAxis
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; Dirty = true; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; Dirty = true; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; Dirty = true; }
			return value;
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
			iclc.ClaimSpace(this, Side.Right, 100);
		}
		public override void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			Geometry.Figures.Clear();
			var pf = PathHelper.Rectangle(0, Minimum, AxisLineThickness, Maximum);
			Geometry.Figures.Add(pf);
			Dirty = false;
		}
		public override void Transforms(IChartRenderContext icrc) {
			var scaley = icrc.Area.Height / Range;
			var matx = new Matrix(1, 0, 0, scaley, icrc.Area.Left + AxisMargin, icrc.Area.Top + icrc.Area.Height/2);
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region CategoryAxis
	/// <summary>
	/// Horizontal Category axis.
	/// </summary>
	public class CategoryAxis : AxisCommon, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Verbose);
		#region properties
		protected Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		#endregion
		#region ctor
		public CategoryAxis() : base(AxisType.Category, AxisOrientation.Horizontal) {
			Segments = new Path();
			this.Geometry = new PathGeometry();
			Segments.Data = this.Geometry;
			BindBrush(this, "Brush", Segments, Path.FillProperty);
		}
		#endregion
		#region IChartAxis
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; Dirty = true; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; Dirty = true; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; Dirty = true; }
			return value;
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
			iclc.ClaimSpace(this, Side.Bottom, 32);
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
		}
		#endregion
	}
	#endregion
}
