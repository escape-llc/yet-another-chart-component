using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxis
	public class ValueAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Verbose);
		public AxisType Type { get; } = AxisType.Value;
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum; } }
		public Path Segments { get; set; }
		protected PathGeometry Geometry { get; set; }
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; }
			return value;
		}
		public override void Enter() {
			Segments = new Path();
			this.Geometry = new PathGeometry();
			Segments.Data = this.Geometry;
		}
		public override void Render(IChartRenderContext icrc) {
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
			Geometry.Figures.Clear();
			var pf = new PathFigure {
				StartPoint = new Windows.Foundation.Point(0, 0)
			};
			var ls = new LineSegment() {
				Point = new Windows.Foundation.Point(0, icrc.Dimensions.Height - 1)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(4, icrc.Dimensions.Height - 1)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(4, 0)
			};
			pf.Segments.Add(ls);
			ls = new LineSegment() {
				Point = new Windows.Foundation.Point(0, 0)
			};
			pf.Segments.Add(ls);
			Geometry.Figures.Add(pf);
		}
	}
	#endregion
	#region CategoryAxis
	public class CategoryAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Verbose);
		public AxisType Type { get; } = AxisType.Category;
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public double Range { get { return double.IsNaN(Minimum) || double.IsNaN(Maximum) ? double.NaN : Maximum - Minimum + 1; } }
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; }
			return value;
		}
		public override void Render(IChartRenderContext icrc) {
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum} r:{Range}");
		}
	}
	#endregion
}
