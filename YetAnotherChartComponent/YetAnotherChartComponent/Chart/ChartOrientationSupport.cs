using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	/// <summary>
	/// Support for constructing rendering (device) coordinates according to the overall "orientation" of the chart.
	/// The <see cref="ChartOrientation"/> is considered the "direction" of chart elements, e.g. a Bar or Line, and is determined
	/// by <see cref="IChartAxis.Orientation"/> of the two axes.
	/// Coordinates are left in NDC and then manipulated by the P matrix.  This alleviates any "swapping" of coordinates.
	/// </summary>
	public class ChartOrientationSupport {
		#region ChartOrientation
		/// <summary>
		/// Overall chart "orientation" meaning which way the <see cref="YAxis"/> goes.
		/// </summary>
		public enum ChartOrientation {
			/// <summary>
			/// <see cref="AxisType.Category"/> renders as (X), <see cref="AxisType.Value"/> renders as (Y).
			/// This is the "default" orientation.
			/// (A1) Horizontal Category (dx)
			/// (A2) Vertical Value (dy)
			/// </summary>
			Vertical,
			/// <summary>
			/// <see cref="AxisType.Value"/> renders as (X), <see cref="AxisType.Category"/> renders as (Y).
			/// (A1) Vertical Category (dy)
			/// (A2) Horizontal Value (dx)
			/// </summary>
			Horizontal,
			/// <summary>
			/// Both axes are <see cref="AxisType.Category"/>, e.g. Heatmap.
			/// </summary>
			Discrete2D,
			/// <summary>
			/// Both axes are <see cref="AxisType.Value"/>, e.g. Scatter.
			/// </summary>
			Continuous2D
		}
		#endregion
		#region properties
		/// <summary>
		/// Axis to use for horizontal (X) coordinate.
		/// </summary>
		public IChartAxis XAxis { get; private set; }
		/// <summary>
		/// Axis to use for vertical (Y) coordinate.
		/// </summary>
		public IChartAxis YAxis { get; private set; }
		/// <summary>
		/// Overall chart orientation.
		/// </summary>
		public ChartOrientation Orientation { get; private set; }
		#endregion
		#region ctor
		/// <summary>
		/// Ctor.
		/// Sort out which axis is <see cref="XAxis"/> and which axis is <see cref="YAxis"/> and <see cref="Orientation"/>.
		/// <para/>
		/// One axis MUST be <see cref="AxisOrientation.Horizontal"/> and one MUST be <see cref="AxisOrientation.Vertical"/>.
		/// </summary>
		/// <param name="a1">First axis.</param>
		/// <param name="a2">Second axis.</param>
		public ChartOrientationSupport(IChartAxis a1, IChartAxis a2) {
			if (a1 == null) throw new ArgumentNullException(nameof(a1));
			if (a2 == null) throw new ArgumentNullException(nameof(a2));
			if (a1 == a2) throw new InvalidOperationException($"{nameof(a1)} equals {nameof(a2)}");
			if (a1.Orientation == a2.Orientation) throw new InvalidOperationException($"Orientations MUST NOT match: {a1.Orientation}");
			if (a1.Orientation == AxisOrientation.Horizontal) {
				XAxis = a1;
				YAxis = a2;
			}
			else {
				XAxis = a2;
				YAxis = a1;
			}
			if (a1.Type == AxisType.Category && a2.Type == AxisType.Category) {
				Orientation = ChartOrientation.Discrete2D;
			}
			else if (a1.Type == AxisType.Value && a2.Type == AxisType.Value) {
				Orientation = ChartOrientation.Continuous2D;
			}
			else {
				// one value one category
				if (a1.Orientation == AxisOrientation.Horizontal) {
					Orientation = ChartOrientation.Vertical;
				}
				else {
					Orientation = ChartOrientation.Horizontal;
				}
			}
		}
		#endregion
		#region public
		/// <summary>
		/// Create a coordinate based on the axes.
		/// </summary>
		/// <param name="a1">Associate with <paramref name="v1"/>.  MUST be either <see cref="XAxis"/> or <see cref="YAxis"/>.</param>
		/// <param name="v1"></param>
		/// <param name="a2">Associate with <paramref name="v2"/>.  MUST be either <see cref="XAxis"/> or <see cref="YAxis"/>.</param>
		/// <param name="v2"></param>
		/// <returns></returns>
		public Point For(IChartAxis a1, double v1, IChartAxis a2, double v2) {
			if (a1 == XAxis && a2 == YAxis) return new Point(v1, v2);
			if(a1 == YAxis && a2 == XAxis) return new Point(v2, v1);
			throw new ArgumentException($"Axes do not match {nameof(XAxis)} OR {nameof(YAxis)}");
		}
		/// <summary>
		/// Provide a combined MP transform for offset use.
		/// </summary>
		/// <param name="area"></param>
		/// <returns>Depends on orientation.</returns>
		public Matrix TransformForOffset(Rect area) {
			if(Orientation == ChartOrientation.Vertical)
				return MatrixSupport.TransformForOffsetX(area, XAxis, YAxis);
			else
				return MatrixSupport.TransformForOffsetY(area, XAxis, YAxis);
		}
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
			var a1range = XAxis.Range;
			var a2range = YAxis.Range;
			return Orientation == ChartOrientation.Vertical
				? new Matrix(1 / a1range, 0, 0, 1 / a2range, -XAxis.Minimum / a1range, -YAxis.Minimum / a2range)
				: new Matrix(0, 1 / a2range, -1 / a1range, 0, XAxis.Maximum / a1range, -YAxis.Minimum / a2range);
		}
		#endregion
	}
}
