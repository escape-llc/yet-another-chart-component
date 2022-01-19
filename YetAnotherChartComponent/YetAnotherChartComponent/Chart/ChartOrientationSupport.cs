using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Support for constructing rendering (device) coordinates according to the overall "orientation" of the chart.
	/// The <see cref="ChartOrientation"/> is considered the "direction" of chart elements, e.g. a Bar or Line, and is determined
	/// by <see cref="IChartAxis.Orientation"/> of <see cref="Axis2"/>.
	/// Coordinates are left in NDC and then manipulated by the P matrix.  This alleviates any "swapping" of coordinates.
	/// </summary>
	public class ChartOrientationSupport {
		#region ChartOrientation
		/// <summary>
		/// Overall chart "orientation" meaning which way the <see cref="Axis2"/> goes.
		/// </summary>
		public enum ChartOrientation {
			/// <summary>
			/// <see cref="Axis1"/> renders as horizontal (X), <see cref="Axis2"/> renders as vertical (Y).
			/// This is the "default" orientation.
			/// (A1) Horizontal Category (dx)
			/// (A2) and Vertical Value (dy)
			/// </summary>
			Vertical,
			/// <summary>
			/// <see cref="Axis1"/> renders as vertical (Y), <see cref="Axis2"/> renders as horizontal (X).
			/// (A1) Vertical Category (dy)
			/// (A2) Horizontal Value (dx)
			/// </summary>
			Horizontal
		}
		#endregion
		#region properties
		/// <summary>
		/// "First" axis always treated as series data "X" coordinate.
		/// </summary>
		public IChartAxis Axis1 { get; private set; }
		/// <summary>
		/// "Second" axis always treated as series data "Y" coordinate.
		/// </summary>
		public IChartAxis Axis2 { get; private set; }
		/// <summary>
		/// Overall chart orientation.  Controls transposition of coordinates for rendering.
		/// </summary>
		public ChartOrientation Orientation { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// </summary>
		/// <param name="a1">Use for <see cref="Axis1"/>.</param>
		/// <param name="a2">Use for <see cref="Axis2"/>.</param>
		public ChartOrientationSupport(IChartAxis a1, IChartAxis a2) {
			if (a1 == null) throw new ArgumentNullException(nameof(a1));
			if (a2 == null) throw new ArgumentNullException(nameof(a2));
			if (a1 == a2) throw new InvalidOperationException($"{nameof(a1)} equals {nameof(a2)}");
			if(a1.Orientation == AxisOrientation.Horizontal && a2.Orientation == AxisOrientation.Vertical) {
				Orientation = ChartOrientation.Vertical;
			}
			else if(a1.Orientation == AxisOrientation.Vertical && a2.Orientation == AxisOrientation.Horizontal) {
				Orientation = ChartOrientation.Horizontal;
			}
			else {
				throw new InvalidOperationException($"Invalid axis combination: a1:{a1.Orientation} and a2:{a2.Orientation}");
			}
			Axis1 = a1;
			Axis2 = a2;
		}
		#endregion
		#region public
		/// <summary>
		/// Return the projection matrix for orientation.
		/// When evaluated on NDC, produces correct DC coordinates.
		/// </summary>
		/// <param name="area">Projection area.</param>
		/// <returns>Vertical: P-matrix; Horizontal: P-prime matrix.</returns>
		public Matrix ProjectionFor(Rect area) {
			return Orientation == ChartOrientation.Vertical
				? new Matrix(area.Width, 0, 0, area.Height, area.Left, area.Top)
				: new Matrix(0, area.Height, -area.Width, 0, area.Right, area.Top);
		}
		/// <summary>
		/// Return M Transform that corresponds to the orientation.
		/// When Horizontal the Component (Basis) Vectors are swapped.
		/// </summary>
		/// <returns>Vertical: M-matrix; Horizontal: M-prime matrix.</returns>
		public Matrix ModelFor() {
			var a1range = Axis1.Range;
			var a2range = Axis2.Range;
			return Orientation == ChartOrientation.Vertical
				? new Matrix(1 / a1range, 0, 0, 1 / a2range, -Axis1.Minimum / a1range, -Axis2.Minimum / a2range)
				: new Matrix(0, 1 / a2range, -1 / a1range, 0, Axis1.Maximum / a1range, -Axis2.Minimum / a2range);
		}
		#endregion
	}
}
