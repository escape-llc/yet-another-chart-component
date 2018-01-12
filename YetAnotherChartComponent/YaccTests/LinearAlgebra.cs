using eScapeLLC.UWP.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Yacc.Tests {
	/// <summary>
	/// Important: avoid using ZERO and values that are equal to each other (e.g. X/Y, MIN/MAX)!
	/// These can disguise errors in the underlying calculations.
	/// </summary>
	[TestClass]
	public class UnitTest_LinearAlgebra {
		#region test data
		#region bounding box (P)
		const double TOP = 10;
		const double LEFT = 20;
		const double WIDTH = 60;
		const double HEIGHT = 40;
		static readonly Rect Bounds = new Rect(LEFT, TOP, WIDTH, HEIGHT);
		#endregion
		#region test coordinates
		const double XX = 3;
		const double YY = 6;
		// this is the test point in WC
		static readonly Point TestPoint = new Point(XX, YY);
		// this is the TestPoint in NDC (after M transform)
		static readonly Point TestPoint_ndc = new Point(.5, 0);
		// this is the final DC coordinates of the WC TestPoint (after MP transform)
		static readonly Point TestPoint_dc =
			new Point(Bounds.Left + Bounds.Width*TestPoint_ndc.X, Bounds.Top + Bounds.Height*TestPoint_ndc.Y);
		// this is in marker's local coordinate system
		// it SHOULD equate with the (M) center coordinate
		static readonly Point TestPointMarkerCenter = new Point(.5, .5);
		// generic origin
		static readonly Point Origin = new Point(0, 0);
		#endregion
		#region axis information (M)
		const double Y_MIN = -4;
		const double Y_MAX = 6;
		const double X_MIN1 = 0;
		const double X_MAX1 = 6;
		const double X_RANGE1 = X_MAX1 - X_MIN1;
		const double X_MIN2 = X_MIN1 + 2;
		const double X_MAX2 = X_MAX1 + 2;
		const double X_RANGE2 = X_MAX2 - X_MIN2;
		const double Y_RANGE = Y_MAX - Y_MIN;
		static readonly Point W1UL = new Point(X_MIN1, Y_MAX);
		static readonly Point W1LR = new Point(X_MAX1, Y_MIN);
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
		public Matrix Projection { get; } = MatrixSupport.ProjectionFor(Bounds);
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
		#region helpers
		public bool AreInThreshold(double d1, double d2) {
			return Math.Abs(d1 - d2) < 0.0001;
		}
		void AssertDouble(double d1, double d2, string message) {
			if (!AreInThreshold(d1, d2))
				Assert.AreEqual(d1, d2, message);
		}
		void MatrixEqual(Matrix mx1, Matrix mx2) {
			AssertDouble(mx1.M11, mx2.M11, "M11 failed");
			AssertDouble(mx1.M12, mx2.M12, "M12 failed");
			AssertDouble(mx1.M21, mx2.M21, "M21 failed");
			AssertDouble(mx1.M22, mx2.M22, "M22 failed");
			AssertDouble(mx1.OffsetX, mx2.OffsetX, "OffsetX failed");
			AssertDouble(mx1.OffsetY, mx2.OffsetY, "OffsetY failed");
		}
		#endregion
		#region transform flame tests
		[TestMethod]
		public void Matrix_Projection() {
			var point = Projection.Transform(Origin);
			var point2 = Projection.Transform(TestPoint_ndc);
			TestContext.WriteLine($"point {point}  point2 {point2}");
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
			Assert.AreEqual(Bounds.Left + Bounds.Width * TestPoint_ndc.X, point2.X, "X failed.2");
			Assert.AreEqual(Bounds.Top + Bounds.Height * TestPoint_ndc.Y, point2.Y, "Y failed.2");
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
			var point = World1.Transform(W1UL);
			AssertDouble(0, point.X, "UL.X1 failed");
			AssertDouble(0, point.Y, "UL.Y failed");
			point = World1.Transform(W1LR);
			AssertDouble(1, point.X, "LR.X1 failed");
			AssertDouble(1, point.Y, "LR.Y failed");
			point = World1.Transform(new Point(X_MIN1 + (X_MAX1 - X_MIN1)/2, Y_MIN + (Y_MAX - Y_MIN)/2));
			AssertDouble(.5, point.X, "mid.X failed");
			AssertDouble(.5, point.Y, "mid.Y failed");
		}
		[UITestMethod]
		public void World1_CombineModelProjection() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = World1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var point = gt.TransformPoint(W1UL);
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
			point = gt.TransformPoint(W1LR);
			Assert.AreEqual(Bounds.Left + Bounds.Width, point.X, "X failed");
			Assert.AreEqual(Bounds.Top + Bounds.Height, point.Y, "Y failed");
		}
		#endregion
		#region inverse
		[TestMethod]
		public void Matrix_Inverse_Projection() {
			var point = Projection.Transform(Origin);
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
			var invproj = MatrixSupport.Invert(Projection);
			TestContext.WriteLine($"proj {Projection}  inv {invproj}");
			var ppoint = invproj.Transform(point);
			Assert.AreEqual(Origin.X, ppoint.X, "X failed");
			Assert.AreEqual(Origin.Y, ppoint.Y, "Y failed");
		}
		[UITestMethod]
		public void Matrix_Inverse_MatchesMatrixTransform() {
			var invproj = MatrixSupport.Invert(Projection);
			var mat = new MatrixTransform() { Matrix = Projection };
			var matinv = mat.Inverse;
			Assert.IsInstanceOfType(matinv, typeof(MatrixTransform), "matinv failed");
			var mat2 = (matinv as MatrixTransform).Matrix;
			TestContext.WriteLine($"matinv {matinv}  mat2 {mat2}  invproj {invproj}");
			MatrixEqual(mat2, invproj);
		}
		#endregion
		#region multiply
		[TestMethod]
		public void Matrix_Multiply_World1() {
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			TestContext.WriteLine($"modelproj {modelproj}");
			var point = modelproj.Transform(W1UL);
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
		}
		[UITestMethod]
		public void Matrix_Multiply_MatchesTransformGroup() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = World1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			TestContext.WriteLine($"modelproj {modelproj}");
			MatrixEqual(gt.Value, modelproj);
		}
		#endregion
		#region translate transform
		[UITestMethod]
		public void Matrix_Translate_MatchesTransformGroup() {
			var gt = new TransformGroup();
			gt.Children.Add(new TranslateTransform() { X = TestPoint.X, Y = TestPoint.Y });
			gt.Children.Add(new MatrixTransform() { Matrix = World1 });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var modeltrans = MatrixSupport.Translate(World1, TestPoint.X, TestPoint.Y);
			TestContext.WriteLine($"modeltrans {modeltrans}");
			MatrixEqual(gt.Value, modeltrans);
		}
		[TestMethod]
		public void Matrix_Translate_Transform() {
			var wtrans = MatrixSupport.Translate(World1, TestPoint.X, TestPoint.Y);
			TestContext.WriteLine($"modeltrans {wtrans}");
			// with the translation, (0,0) is our test point (WC->NDC)
			var point = wtrans.Transform(Origin);
			AssertDouble(TestPoint_ndc.X, point.X, "X failed");
			AssertDouble(TestPoint_ndc.Y, point.Y, "Y failed");
			// cross-check through W1 inverse
			var winv = MatrixSupport.Invert(World1);
			// this goes back to world coordinates (NDC->WC)
			var point2 = winv.Transform(point);
			Assert.AreEqual(TestPoint.X, point2.X, "X failed.2");
			Assert.AreEqual(TestPoint.Y, point2.Y, "Y failed.2");
		}
		#endregion
		#region marker transforms
		/// <summary>
		/// The M matrix must have the coordinates "baked in".
		/// </summary>
		[TestMethod]
		public void World1_MarkerTransform_Flame() {
			const double DIMENSION = 5;
			// must "walk out" the marker dimensions to DC
			var mkwid_indc = MK_WIDTH * WIDTH * World1.M11;
			// now "walk in" the DC to Y-axis
			var mkhgt_inyaxis = mkwid_indc/HEIGHT/World1.M22;
			TestContext.WriteLine($"marker width in DC {mkwid_indc} height in y-axis {mkhgt_inyaxis}");
			Assert.AreEqual(DIMENSION, mkwid_indc, "mkwid in DC failed");
			Assert.AreEqual(-1.25, mkhgt_inyaxis, "mkhgt in Y-axis failed");
			// multiply Mk * M * P
			// this matrix establishes the local coordinate system for the marker (5x5 pixels based on dimensions)
			// TODO get the marker center (.5,.5) to "line up" on the "target" point in M-coordinates
			// TODO get the translation for target M-coordinate
			var marker = MatrixSupport.LocalTransform(World1, MK_WIDTH, Bounds, -.5, -.5);
			var model2 = MatrixSupport.Multiply(World1, marker);
			var modelproj = MatrixSupport.Multiply(Projection, model2);
			var ul = modelproj.Transform(Origin);
			var center = modelproj.Transform(TestPointMarkerCenter);
			var lr = modelproj.Transform(new Point(1,1));
			TestContext.WriteLine($"marker matx {marker}  model2 {model2}  final {modelproj}  UL {ul}  center {center}  LR {lr}");
			// because of Y-axis "shift" the ZERO is 60 percent of the axis range
			var zerodp = HEIGHT * .6;
			var mkhalf = DIMENSION / 2;
			Assert.AreEqual(Bounds.Left, center.X, "center.X failed");
			Assert.AreEqual(Bounds.Top + zerodp, center.Y, "center.Y failed");
			Assert.AreEqual(Bounds.Left - mkhalf, ul.X, "ul.X failed");
			Assert.AreEqual(Bounds.Top + zerodp - 2.5, ul.Y, "ul.Y failed");
			Assert.AreEqual(Bounds.Left + mkhalf, lr.X, "lr.X failed");
			Assert.AreEqual(Bounds.Top + zerodp + 2.5, lr.Y, "lr.Y failed");
		}
		[TestMethod]
		public void World1_MarkerTransform_ForPoint() {
			var world1t = MatrixSupport.Translate(World1, TestPoint.X, TestPoint.Y);
			TestContext.WriteLine($"world1t {world1t}");
			var marker = MatrixSupport.LocalTransform(world1t, MK_WIDTH, Bounds, -.5, -.5);
			var model2 = MatrixSupport.Multiply(world1t, marker);
			var mkproj = MatrixSupport.Multiply(Projection, model2);
			var modelproj = MatrixSupport.Multiply(Projection, World1);
			var reference = modelproj.Transform(TestPoint);
			var target = mkproj.Transform(TestPointMarkerCenter);
			TestContext.WriteLine($"marker {marker}  model2 {model2}  Mk.M.P {mkproj}");
			TestContext.WriteLine($"M.P {modelproj}  pt {TestPointMarkerCenter}  ref {reference}  target {target}");
			Assert.AreEqual(reference.X, target.X, "target.X failed");
			Assert.AreEqual(reference.Y, target.Y, "target.Y failed");
		}
		#endregion
	}
}
