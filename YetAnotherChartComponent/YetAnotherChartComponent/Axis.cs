using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxis
	/// <summary>
	/// Value axis is a vertical axis that represents the "Y" coordinate.
	/// </summary>
	public class ValueAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Verbose);
		#region properties
		public AxisType Type { get; } = AxisType.Value;
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		public Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// The brush for the series.
		/// </summary>
		public Brush Brush { get { return (Brush)GetValue(BrushProperty); } set { SetValue(BrushProperty, value); } }
		#endregion
		#region ctor
		public ValueAxis() {
			Segments = new Path();
			this.Geometry = new PathGeometry();
			Segments.Data = this.Geometry;
			BindBrush(this, "Brush", Segments, Path.FillProperty);
		}
		#endregion
		#region brush
		/// <summary>
		/// Identifies <see cref="Brush"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(Brush), typeof(ValueAxis), new PropertyMetadata(null));
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
		public override void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			Geometry.Figures.Clear();
			var pf = PathHelper.Rectangle(0, 0, 4, icrc.Dimensions.Height - 1);
			Geometry.Figures.Add(pf);
			Dirty = false;
		}
		public override void Transforms(IChartRenderContext icrc) {
			// TODO replace "4" with max text width for ticks
			var matx = new Matrix(1, 0, 0, 1, icrc.Dimensions.Width, 0);
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
	#region CategoryAxis
	public class CategoryAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Verbose);
		#region properties
		public AxisType Type { get; } = AxisType.Category;
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		public Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		/// <summary>
		/// The brush for the series.
		/// </summary>
		public Brush Brush { get { return (Brush)GetValue(BrushProperty); } set { SetValue(BrushProperty, value); } }
		#endregion
		#region brush
		/// <summary>
		/// Identifies <see cref="Brush"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(Brush), typeof(CategoryAxis), new PropertyMetadata(null));
		#endregion
		#region ctor
		public CategoryAxis() {
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
		public override void Render(IChartRenderContext icrc) {
			if (!Dirty) return;
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			Geometry.Figures.Clear();
			var pf = PathHelper.Rectangle(0, 0, icrc.Dimensions.Width - 1, 4);
			Geometry.Figures.Add(pf);
			Dirty = false;
		}
		public override void Transforms(IChartRenderContext icrc) {
			// TODO replace "4" with max text width for ticks
			var matx = new Matrix(1, 0, 0, 1, 0, icrc.Dimensions.Height);
			Geometry.Transform = new MatrixTransform() { Matrix = matx };
		}
		#endregion
	}
	#endregion
}
