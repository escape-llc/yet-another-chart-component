using eScapeLLC.UWP.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Yacc.Tests {
	[TestClass]
	public class UnitTest_LinearAlgebra {
		#region test data
		#region bounding box (P)
		const double TOP = 10;
		const double LEFT = 10;
		const double WIDTH = 60;
		const double HEIGHT = 40;
		static readonly Rect Bounds = new Rect(TOP, LEFT, WIDTH, HEIGHT);
		#endregion
		#region test coordinates
		const double XX = 3;
		const double YY = 5;
		static readonly Point TestPoint = new Point(XX, YY);
		static readonly Point TestPointMarker = new Point(1, 0);
		static readonly Point Origin = new Point(0, 0);
		#endregion
		#region axis information (M)
		const double Y_MIN = -5;
		const double Y_MAX = 5;
		const double X_MIN1 = 0;
		const double X_MAX1 = 6;
		const double X_RANGE1 = X_MAX1 - X_MIN1;
		const double X_MIN2 = X_MIN1 + 2;
		const double X_MAX2 = X_MAX1 + 2;
		const double X_RANGE2 = X_MAX2 - X_MIN2;
		const double Y_RANGE = Y_MAX - Y_MIN;
		#endregion
		#region marker information (Mk)
		const double MK_OFFSET = .25;
		const double MK_WIDTH = .5;
		#endregion
		#endregion
		#region properties
		public TestContext TestContext { get; set; }
		/// <summary>
		/// Maps the rectangle the WC displays in to DC.
		/// </summary>
		public Matrix Projection { get; } = new Matrix(WIDTH, 0, 0, HEIGHT, LEFT, TOP);
		/// <summary>
		/// Maps the "world" coordinate system (WC) to a normalized basis.
		/// The basis vectors normalize the axis ranges.
		/// The Y components are reversed because WC goes reverse (+up) of DC (+down).
		/// The translation component compensates for the axis (left/top) "end".  Note these are also normalized.
		/// Version for X1.
		/// </summary>
		public Matrix World1 { get; } = new Matrix(1 / X_RANGE1, 0, 0, -1 / Y_RANGE, -X_MIN1 / X_RANGE1, Y_MAX / Y_RANGE);
		/// <summary>
		/// Version for X2.
		/// </summary>
		public Matrix World2 { get; } = new Matrix(1 / X_RANGE2, 0, 0, -1 / Y_RANGE, -X_MIN2 / X_RANGE2, Y_MAX / Y_RANGE);
		#endregion
		public bool AreInThreshold(double d1, double d2) {
			return Math.Abs(d1 - d2) < 0.0001;
		}
		void AssertDouble(double d1, double d2, string message) {
			if (!AreInThreshold(d1, d2))
				Assert.AreEqual(d1, d2, message);
		}
		[TestMethod]
		public void Matrix_Projection() {
			var point = Projection.Transform(Origin);
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
		}
		[TestMethod]
		public void Matrix_World1() {
			var point = World1.Transform(TestPoint);
			AssertDouble(0.5, point.X, "X failed");
			AssertDouble(0, point.Y, "Y failed");
		}
		[TestMethod]
		public void Matrix_World2() {
			var point = World2.Transform(TestPoint);
			AssertDouble((XX - X_MIN2)/X_RANGE2, point.X, "X failed");
			AssertDouble(0, point.Y, "Y failed");
		}
		[TestMethod]
		public void World1_NomalizesAxes() {
			var point = World1.Transform(new Point(X_MIN1, Y_MAX));
			AssertDouble(0, point.X, "UL.X1 failed");
			AssertDouble(0, point.Y, "UL.Y failed");
			point = World1.Transform(new Point(X_MAX1, Y_MIN));
			AssertDouble(1, point.X, "LR.X1 failed");
			AssertDouble(1, point.Y, "LR.Y failed");
			point = World1.Transform(new Point(X_MIN1 + (X_MAX1 - X_MIN1)/2, Y_MIN + (Y_MAX - Y_MIN)/2));
			AssertDouble(.5, point.X, "mid.X failed");
			AssertDouble(.5, point.Y, "mid.Y failed");
		}
		[TestMethod]
		public void World1_Multiply() {
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			TestContext.WriteLine($"modelproj {modelproj}");
			var point = modelproj.Transform(new Point(X_MIN1, Y_MAX));
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
		}
		[UITestMethod]
		public void Matrix_MultiplySameAsTransformGroup() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = World1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			TestContext.WriteLine($"modelproj {modelproj}");
			Assert.AreEqual(gt.Value.M11, modelproj.M11, "M11 failed");
			Assert.AreEqual(gt.Value.M12, modelproj.M12, "M12 failed");
			Assert.AreEqual(gt.Value.M21, modelproj.M21, "M21 failed");
			Assert.AreEqual(gt.Value.M22, modelproj.M22, "M22 failed");
			Assert.AreEqual(gt.Value.OffsetX, modelproj.OffsetX, "OffsetX failed");
			Assert.AreEqual(gt.Value.OffsetY, modelproj.OffsetY, "OffsetY failed");
		}
		[UITestMethod]
		public void World1_CombineModelProjection() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = World1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var point = gt.TransformPoint(new Point(X_MIN1, Y_MAX));
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
			point = gt.TransformPoint(new Point(X_MAX1, Y_MIN));
			Assert.AreEqual(Bounds.Left + Bounds.Width, point.X, "X failed");
			Assert.AreEqual(Bounds.Top + Bounds.Height, point.Y, "Y failed");
		}
		Matrix MarkerTransform(Matrix mx, double mkwidth, Rect area, double ymax, double xx, double yy) {
			// must "walk out" the marker dimensions to DC
			var mkwid_indc = mkwidth * area.Width * mx.M11;
			// now "walk in" the DC to Y-axis
			var mkhgt_inyaxis = mkwid_indc / area.Height / mx.M22;
			// IST have to UNDO the y-axis inversion before we go through model (M) matrix!
			var marker = new Matrix(mkwidth, 0, 0, mkhgt_inyaxis, xx, ymax - yy);
			return marker;
		}
		[TestMethod]
		public void World1_MarkerTransform_Flame() {
			// must "walk out" the marker dimensions to DC
			var mkwid_indc = MK_WIDTH * WIDTH * World1.M11;
			// now "walk in" the DC to Y-axis
			var mkhgt_inyaxis = mkwid_indc/HEIGHT/World1.M22;
			TestContext.WriteLine($"marker width in DC {mkwid_indc} height in y-axis {mkhgt_inyaxis}");
			Assert.AreEqual(5, mkwid_indc, "mkwid in DC failed");
			Assert.AreEqual(-1.25, mkhgt_inyaxis, "mkhgt in Y-axis failed");
			// multiply Mk * M * P
			// this matrix establishes the local coordinate system for the marker (5x5 pixels based on dimensions)
			// TODO get the marker center (.5,.5) to "line up" on the "target" point in M-coordinates
			// TODO get the translation for target M-coordinate
			var marker = MarkerTransform(World1, MK_WIDTH, Bounds, Y_MAX, Origin.X, Origin.Y);
			var model2 = MatrixSupport.Multiply(World1, marker);
			var modelproj = MatrixSupport.Multiply(Projection, model2);
			var origin = modelproj.Transform(Origin);
			var center = modelproj.Transform(new Point(.5, .5));
			var corner = modelproj.Transform(new Point(1,1));
			TestContext.WriteLine($"marker matx {marker}  model2 {model2}  final {modelproj}  origin {origin}  center {center}  corner {corner}");
			Assert.AreEqual(LEFT, origin.X, "origin.X failed");
			Assert.AreEqual(TOP, origin.Y, "origin.Y failed");
			Assert.AreEqual(LEFT + 2.5, center.X, "center.X failed");
			Assert.AreEqual(TOP + 2.5, center.Y, "center.Y failed");
			Assert.AreEqual(LEFT + 5, corner.X, "corner.X failed");
			Assert.AreEqual(TOP + 5, corner.Y, "corner.Y failed");
		}
		[TestMethod]
		public void World1_MarkerTransform_ForPoint() {
			var marker = MarkerTransform(World1, MK_WIDTH, Bounds, Y_MAX, TestPointMarker.X, TestPointMarker.Y);
			var model2 = MatrixSupport.Multiply(World1, marker);
			var mkproj = MatrixSupport.Multiply(Projection, model2);
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			var reference = modelproj.Transform(TestPointMarker);
			var target = mkproj.Transform(new Point(.5, .5));
			TestContext.WriteLine($"marker {marker}  model2 {model2}  Mk.M.P {mkproj}  M.P {modelproj}  pt {TestPointMarker}  ref {reference}  target {target}");
			Assert.AreEqual(reference.X, target.X, "target.X failed");
			Assert.AreEqual(reference.Y, target.Y, "target.Y failed");
		}
	}
}
