using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region MatrixSupport
	/// <summary>
	/// Static helpers for the <see cref="Matrix"/> class.
	/// In XAML and many other places, coordinate transforms are represented using affine matrices in homogeneous coordinates.
	/// In XAML, the Matrix struct is the workhorse.  This structure "leaves out" the last column, because its values are fixed at (0 0 1).
	/// One can use <see cref="TransformGroup"/> to accomplish this, but it requires the UI thread just to do matrix arithmetic!
	/// What matrix algebra would call M13 and M23, <see cref="Matrix"/> calls OffsetX and OffsetY.
	/// </summary>
	public static class MatrixSupport {
		// these are the affine matrix's third column
		const double M31 = 0;
		const double M32 = 0;
		const double M33 = 1;
		/// <summary>
		/// Multiply 3x3 affine matrices.
		/// Adapted for <see cref="Matrix"/>.
		/// Note parameter order is BACKWARDS from the math: Multiply(m1,m2) is M2 * M1.
		/// Matrix multiplication is NOT commutative.
		/// </summary>
		/// <param name="rh">Righthand matrix.</param>
		/// <param name="lh">Lefthand matrix.</param>
		/// <returns>New matrix.</returns>
		public static Matrix Multiply(Matrix rh, Matrix lh) {
			double c11 = rh.M11 * lh.M11 + rh.M12 * lh.M21 + rh.OffsetX * M31;
			double c12 = rh.M11 * lh.M12 + rh.M12 * lh.M22 + rh.OffsetX * M32;
			double c13 = rh.M11 * lh.OffsetX + rh.M12 * lh.OffsetY + rh.OffsetX * M33;
			double c21 = rh.M21 * lh.M11 + rh.M22 * lh.M21 + rh.OffsetY * M31;
			double c22 = rh.M21 * lh.M12 + rh.M22 * lh.M22 + rh.OffsetY * M32;
			double c23 = rh.M21 * lh.OffsetX + rh.M22 * lh.OffsetY + rh.OffsetY * M33;
#if false
			// C31/C32/C33 "cancel out" and equal M31/M32/M33 respectively, hence the reason they are not "in the Matrix"!
			double c31 = M31 * b.M11 + M32 * b.M21 + M33 * M31;
			double c32 = M31 * b.M12 + M32 * b.M22 + M33 * M32;
			double c33 = M31 * b.OffsetX + M32 * b.OffsetY + M33 * M33;
#endif
			return new Matrix(c11, c12, c21, c22, c13, c23);
		}
		/// <summary>
		/// Calculate inverse of affine matrix using determinant/adjugate method.
		/// Adapted for <see cref="Matrix"/>.
		/// </summary>
		/// <param name="mx">Source matrix.</param>
		/// <returns>New matrix if invertable.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Determinant is Zero.</exception>
		public static Matrix Invert(Matrix mx) {
			// NOTE some terms "drop out" because M31 and M32 are ZERO
			// Mxx are defined as CONST, so let the compiler optimize it
			double det = mx.M11 * (mx.M22 * M33 - M32 * mx.OffsetY) -
									 mx.M12 * (mx.M21 * M33 - mx.OffsetY * M31) +
									 mx.OffsetX * (mx.M21 * M32 - mx.M22 * M31);
			if (det == 0)
				throw new ArgumentOutOfRangeException("Determinant is Zero");
			double invdet = 1 / det;
			double c11 = (mx.M22 * M33 - M32 * mx.OffsetY) * invdet;
			double c12 = (mx.OffsetX * M32 - mx.M12 * M33) * invdet;
			double c13 = (mx.M12 * mx.OffsetY - mx.OffsetX * mx.M22) * invdet;
			double c21 = (mx.OffsetY * M31 - mx.M21 * M33) * invdet;
			double c22 = (mx.M11 * M33 - mx.OffsetX * M31) * invdet;
			double c23 = (mx.M21 * mx.OffsetX - mx.M11 * mx.OffsetY) * invdet;
#if false
			// as expected, C31/C32/C33 cancel out to M31/M32/M33
			double c31 = (mx.M21 * M32 - M31 * mx.M22) * invdet;
			double c32 = (M31 * mx.M12 - mx.M11 * M32) * invdet;
			double c33 = (mx.M11 * mx.M22 - mx.M21 * mx.M12) * invdet;
#endif
			return new Matrix(c11, c12, c21, c22, c13, c23);
		}
		/// <summary>
		/// Create the projection (P) matrix for the target rectangle.
		/// Projection maps the rectangle the model coordinate system displays in.
		/// </summary>
		/// <param name="area">Target rectangle.</param>
		/// <returns>New matrix.</returns>
		public static Matrix ProjectionFor(Rect area) {
			return new Matrix(area.Width, 0, 0, area.Height, area.Left, area.Top);
		}
		/// <summary>
		/// Create the model (M) matrix for the given axis' extents.
		/// Uses axis Range and Minimum values.
		/// Model maps the "cartesian" coordinate system to a normalized basis.
		/// The basis vectors normalize the axis range.
		/// The Y scale is reversed because cartesian goes reverse (+up) of device y-axis (+down).
		/// The translation component compensates for the axis "end".  Note these are also normalized.
		/// </summary>
		/// <param name="xaxis">The x-axis.</param>
		/// <param name="yaxis">The y-axis.</param>
		/// <returns>New matrix</returns>
		public static Matrix ModelFor(IChartAxis xaxis, IChartAxis yaxis) {
			if (xaxis == null) throw new ArgumentNullException(nameof(xaxis));
			if (yaxis == null) throw new ArgumentNullException(nameof(yaxis));
			var xrange = xaxis.Range;
			var yrange = yaxis.Range;
			var matx = new Matrix(1 / xrange, 0, 0, -1 / yrange, -xaxis.Minimum / xrange, yaxis.Maximum / yrange);
			return matx;
		}
		/// <summary>
		/// Create a final (MVP) matrix for [0..1] in X axis, and axis.Range in Y-axis.
		/// Y-axis is inverted.
		/// All components are pre-multiplied instead of using <see cref="MatrixSupport.Multiply"/>.
		/// </summary>
		/// <param name="area">Target area.</param>
		/// <param name="yaxis">The y-axis.</param>
		/// <returns>New matrix.</returns>
		public static Matrix TransformFor(Rect area, IChartAxis yaxis) {
			if (yaxis == null) throw new ArgumentNullException(nameof(yaxis));
			var scaley = area.Height / yaxis.Range;
			var matx = new Matrix(area.Width, 0, 0, -scaley, area.Left, area.Top + yaxis.Maximum * scaley);
			return matx;
		}
		/// <summary>
		/// Create a final (MVP) matrix for this rectangle and axes.
		/// It would "normally" be MVP matrix, but for how V == I so we leave it out.
		/// </summary>
		/// <param name="area">Target area.</param>
		/// <param name="xaxis">The x-axis.</param>
		/// <param name="yaxis">The y-axis.</param>
		/// <returns></returns>
		public static Matrix TransformFor(Rect area, IChartAxis xaxis, IChartAxis yaxis) {
			if (xaxis == null) throw new ArgumentNullException(nameof(xaxis));
			if (yaxis == null) throw new ArgumentNullException(nameof(yaxis));
			return MatrixSupport.Multiply(ProjectionFor(area), ModelFor(xaxis, yaxis));
		}
		/// <summary>
		/// Translate matrix by given offset.
		/// </summary>
		/// <param name="mx">Source matrix.</param>
		/// <param name="xo">X offset.</param>
		/// <param name="yo">Y offset.</param>
		/// <returns></returns>
		public static Matrix Translate(Matrix mx, double xo, double yo) {
			return new Matrix(mx.M11, mx.M12, mx.M21, mx.M22, mx.OffsetX + xo * mx.M11, mx.OffsetY + yo * mx.M22);
		}
		/// <summary>
		/// Create a transform for a local coordinate system, e.g. a Marker.
		/// This transform creates a coordinate system of NDC centered at (.5,.5) on whatever point the "outer" transform represents.
		/// Takes the x-axis as a reference, and cross-calculates the scale for the y-axis to make it "square"
		/// in PX coordinates.
		/// For example, if MKWIDTH evaluates to 16px on x-axis, a y-axis magnitude is calculated that also represents 16px.
		/// </summary>
		/// <param name="mx">Model (M) transform.  Assumed to be translated to the "center" point.</param>
		/// <param name="mkwidth">Marker width [0..1].</param>
		/// <param name="area">The layout area.</param>
		/// <param name="xx">Translate local x.</param>
		/// <param name="yy">Translate local y.</param>
		/// <returns>New matrix.</returns>
		public static Matrix MarkerTransform(Matrix mx, double mkwidth, Rect area, double xx, double yy) {
			// must "walk out" the marker dimensions through P
			var mkwid_proj = mkwidth * area.Width * mx.M11;
			// now "walk in" the DC to Y-axis (M)
			var mkhgt_yaxis = mkwid_proj / area.Height / mx.M22;
			var marker = new Matrix(mkwidth, 0, 0, mkhgt_yaxis, xx * mkwidth, yy * mkhgt_yaxis);
			return marker;
		}
	}
	#endregion
}
