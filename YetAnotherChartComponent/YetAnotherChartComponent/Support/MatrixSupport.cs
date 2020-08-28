using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace eScapeLLC.UWP.Charts {
	#region MatrixSupport
// this pragma is wanted because The System gets confused about Matrix, even though it's FQTN'd!
#pragma warning disable CS0419
	/// <summary>
	/// Static helpers for XAML <see cref="Windows.UI.Xaml.Media.Matrix"/>.
	/// In graphics programming, coordinate transforms are represented using affine matrices and homogeneous coordinates.
	/// In XAML, the <see cref="Windows.UI.Xaml.Media.Matrix"/> struct is the workhorse.  This structure "leaves out" the last column, because its values are fixed at (0 0 1).
	/// What matrix algebra would call M31 and M32, <see cref="Windows.UI.Xaml.Media.Matrix"/> calls OffsetX and OffsetY.
	/// One can use <see cref="TransformGroup"/> to do matrix algebra, but it requires the UI thread (just to do matrix arithmetic)!
	///
	///	WC	world coordinates
	///	NDC	normalized device coordinates [0..1]
	///	DC	device coordinates
	///
	/// Matrix pipeline: WC --> M --> NDC --> P --> DC
	/// Transforms MAY be multiplied together into a single matrix representing their composite action.
	///
	/// Composite model: (Mn * ... * M1) = M where M1 is the "initial" WC matrix that feeds P matrix.
	/// In composite model, the WC will be in whatever coordinate system the Mn matrix represents (which may be NDC, like a marker).
	///
	/// Reverse pipeline: DC --> Inv(P) --> NDC --> Inv(M) --> WC
	/// MUST use the inverse of (each) matrix to go in the opposite direction.
	/// </summary>
	public static class MatrixSupport {
		// these are the affine matrix's third column
		const double M33 = 1;
		/// <summary>
		/// Multiply 3x3 affine matrices.
		/// Adapted for <see cref="Windows.UI.Xaml.Media.Matrix"/>.
		/// Multiply(m1,m2) is M1 * M2.
		/// Matrix multiplication is NOT commutative.
		/// ZERO terms are now optimized out.
		/// </summary>
		/// <param name="lh">Lefthand matrix.</param>
		/// <param name="rh">Righthand matrix.</param>
		/// <returns>New matrix.</returns>
		public static Matrix Multiply(Matrix lh, Matrix rh) {
			double c11 = lh.M11 * rh.M11 + lh.M12 * rh.M21;
			double c12 = lh.M11 * rh.M12 + lh.M12 * rh.M22;
			double c21 = lh.M21 * rh.M11 + lh.M22 * rh.M21;
			double c22 = lh.M21 * rh.M12 + lh.M22 * rh.M22;
			double c31 = lh.OffsetX * rh.M11 + lh.OffsetY * rh.M21 + M33 * rh.OffsetX;
			double c32 = lh.OffsetX * rh.M12 + lh.OffsetY * rh.M22 + M33 * rh.OffsetY;
			return new Matrix(c11, c12, c21, c22, c31, c32);
		}
		/// <summary>
		/// Calculate inverse of affine matrix using determinant/adjugate method.
		/// Adapted for <see cref="Windows.UI.Xaml.Media.Matrix"/>.
		/// ZERO terms are now optimized out.
		/// </summary>
		/// <param name="mx">Source matrix.</param>
		/// <returns>New matrix if invertable.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Determinant is Zero.</exception>
		public static Matrix Invert(Matrix mx) {
			double det = mx.M11 * (mx.M22 * M33) - mx.M12 * (mx.M21 * M33);
			if (det == 0)
				throw new ArgumentOutOfRangeException("Determinant is Zero");
			double invdet = 1 / det;
			double c11 = (mx.M22 * M33) * invdet;
			double c12 = (-mx.M12 * M33) * invdet;
			double c21 = (-mx.M21 * M33) * invdet;
			double c22 = (mx.M11 * M33) * invdet;
			double c31 = (mx.M12 * mx.OffsetY - mx.OffsetX * mx.M22) * invdet;
			double c32 = (mx.M21 * mx.OffsetX - mx.M11 * mx.OffsetY) * invdet;
			return new Matrix(c11, c12, c21, c22, c31, c32);
		}
		/// <summary>
		/// Create the projection (P) matrix for the target rectangle.
		/// Projection maps NDC to the display rectangle.
		/// </summary>
		/// <param name="area">Target rectangle.</param>
		/// <returns>New matrix.</returns>
		public static Matrix ProjectionFor(Rect area) {
			return new Matrix(area.Width, 0, 0, area.Height, area.Left, area.Top);
		}
		/// <summary>
		/// Create the model (M) matrix for the given axis' extents.
		/// Uses axis Range and extent values.
		/// Model maps world coordinates to NDC.
		/// The basis vectors normalize the axis range.
		/// The Y scale is reversed because cartesian goes reverse (+up) of device y-axis (+down).
		/// The translation components compensate for the axis "ends".  Note these are also normalized.
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
		/// Create a final (MVP) matrix for NDC in X axis, and axis.Range in Y-axis.
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
		/// Create a final (MP) matrix for this rectangle and axes.
		/// It would "normally" be MVP matrix, but for how V == I so we leave it out.
		/// </summary>
		/// <param name="area">Target area.</param>
		/// <param name="xaxis">The x-axis.</param>
		/// <param name="yaxis">The y-axis.</param>
		/// <returns></returns>
		public static Matrix TransformFor(Rect area, IChartAxis xaxis, IChartAxis yaxis) {
			if (xaxis == null) throw new ArgumentNullException(nameof(xaxis));
			if (yaxis == null) throw new ArgumentNullException(nameof(yaxis));
			return Multiply(ModelFor(xaxis, yaxis), ProjectionFor(area));
		}
		/// <summary>
		/// <see cref="GeometryShim{G}"/> Offset version.  Caller must account for the Matrix.OffsetX component there.
		/// <para/>
		/// Create a final (MP) matrix for this rectangle and axes.
		/// It would "normally" be MVP matrix, but for how V == I so we leave it out.
		/// </summary>
		/// <param name="area">Target area. The Left component is eliminated from output matrix.</param>
		/// <param name="xaxis">The x-axis.</param>
		/// <param name="yaxis">The y-axis.</param>
		/// <returns></returns>
		public static Matrix TransformForOffsetX(Rect area, IChartAxis xaxis, IChartAxis yaxis) {
			if (xaxis == null) throw new ArgumentNullException(nameof(xaxis));
			if (yaxis == null) throw new ArgumentNullException(nameof(yaxis));
			var proj = ProjectionFor(area);
			// remove the x-offset
			proj.OffsetX = 0;
			return Multiply(ModelFor(xaxis, yaxis), proj);
		}
		/// <summary>
		/// Translate matrix by given offset.
		/// </summary>
		/// <param name="mx">Source matrix.</param>
		/// <param name="xo">X offset in MX units.</param>
		/// <param name="yo">Y offset in MX units.</param>
		/// <returns></returns>
		public static Matrix Translate(Matrix mx, double xo, double yo) {
			return new Matrix(mx.M11, mx.M12, mx.M21, mx.M22, mx.OffsetX + xo * mx.M11, mx.OffsetY + yo * mx.M22);
		}
		/// <summary>
		/// Take the given x-axis normalized unit width, and convert to dimensions in each axis.
		/// Takes the x-axis as a reference, and cross-calculates the scale for the y-axis to make it "square" in DC.
		/// For example, if WIDTH evaluates to 16px (on x-axis), a y-axis magnitude is calculated that also represents 16px.
		/// E.g. Range = 10, Basis = .1, width = .5; answer = .05 x-axis units.
		/// Cannot return a Windows.Foundation.Size type, because Width and Height cannot be negative!
		/// </summary>
		/// <param name="mx">Model (M) transform to operate in.</param>
		/// <param name="width">Width [0..1].  Unit is (M) x-axis basis.  Also return value Point.X.</param>
		/// <param name="area">The layout area.  Unit is DC.  Provides DC to size the dimensions.</param>
		/// <returns>Rescaled dimensions.</returns>
		public static Point Rescale(Matrix mx, double width, Rect area) {
			// "walk out" the x-axis dimension through P (to get DC)
			var wid_dc = width * area.Width * mx.M11;
			// now "walk in" the DC to Y-axis (M) (to get NDC)
			var hgt_yaxis = wid_dc / area.Height / mx.M22;
			return new Point(width, hgt_yaxis);
		}
		/// <summary>
		/// Create a transform for a local coordinate system, e.g. a Marker.
		/// This transform creates a coordinate system of NDC centered at (XX,YY) on whatever point the "outer" transform represents.
		/// See <see cref="Rescale"/> for more information.
		/// </summary>
		/// <param name="mx">Model (M) transform.  SHOULD be translated to the "center" point that aligns with NDC origin.</param>
		/// <param name="mkwidth">Marker width [0..1].  Unit is (M) x-axis basis. E.g. Range = 10, Basis = .1, mkwidth = .5; answer = .05 x-axis units.</param>
		/// <param name="area">The layout area.  Unit is DC.  Provide DC to size the local coordinate system.</param>
		/// <param name="xx">Translate local x.  Unit is NDC.  Additional offset to align an "origin".</param>
		/// <param name="yy">Translate local y.  Unit is NDC.  Additional offset to align an "origin".</param>
		/// <returns>New matrix.</returns>
		public static Matrix LocalFor(Matrix mx, double mkwidth, Rect area, double xx, double yy) {
			var axes = Rescale(mx, mkwidth, area);
			var marker = new Matrix(axes.X, 0, 0, axes.Y, xx * axes.X, yy * axes.Y);
			return marker;
		}
	}
#pragma warning restore CS0419
	#endregion
}
