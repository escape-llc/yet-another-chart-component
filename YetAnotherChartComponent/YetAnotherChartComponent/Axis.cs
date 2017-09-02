using eScape.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eScapeLLC.UWP.Charts {
	#region ValueAxis
	public class ValueAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("ValueAxis", LogTools.Level.Verbose);
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; }
			return value;
		}
		public override void Render(IChartRenderContext icrc) {
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum}");
		}
	}
	#endregion
	#region CategoryAxis
	public class CategoryAxis : ChartComponent, IChartAxis {
		static LogTools.Flag _trace = LogTools.Add("CategoryAxis", LogTools.Level.Verbose);
		public double Minimum { get; protected set; } = double.NaN;
		public double Maximum { get; protected set; } = double.NaN;
		public void ResetLimits() { Minimum = double.NaN; Maximum = double.NaN; }
		public double For(double value) {
			if (double.IsNaN(Minimum) || value < Minimum) { Minimum = value; }
			if (double.IsNaN(Maximum) || value > Maximum) { Maximum = value; }
			return value;
		}
		public override void Render(IChartRenderContext icrc) {
			_trace.Verbose($"{Name} min:{Minimum} max:{Maximum}");
		}
	}
	#endregion
}
