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
		const double TOP = 10;
		const double LEFT = 10;
		const double WIDTH = 60;
		const double HEIGHT = 40;
		static readonly Rect Bounds = new Rect(TOP, LEFT, WIDTH, HEIGHT);
		const double XX = 3;
		const double YY = 5;
		static readonly Point TestPoint = new Point(XX, YY);
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
		public TestContext TestContext { get; set; }
		/// <summary>
		/// Projection maps the rectangle the model coordinate system displays in.
		/// </summary>
		public Matrix Projection { get; } = new Matrix(WIDTH, 0, 0, HEIGHT, LEFT, TOP);
		/// <summary>
		/// Model maps the "cartesian" coordinate system to a normalized basis.
		/// The basis vectors normalize the axis range.
		/// The Y components are reversed because cartesian goes reverse (+up) of device y-axis (+down).
		/// The translation component compensates for the axis (left/top) "end".  Note these are also normalized.
		/// Version for X1.
		/// </summary>
		public Matrix ModelX1 { get; } = new Matrix(1 / X_RANGE1, 0, 0, -1 / Y_RANGE, -X_MIN1 / X_RANGE1, Y_MAX / Y_RANGE);
		/// <summary>
		/// Version for X2.
		/// </summary>
		public Matrix ModelX2 { get; } = new Matrix(1 / X_RANGE2, 0, 0, -1 / Y_RANGE, -X_MIN2 / X_RANGE2, Y_MAX / Y_RANGE);
		public bool AreInThreshold(double d1, double d2) {
			return Math.Abs(d1 - d2) < 0.0001;
		}
		void AssertDouble(double d1, double d2, string message) {
			if (!AreInThreshold(d1, d2))
				Assert.AreEqual(d1, d2, message);
		}
		[TestMethod]
		public void Matrix_Projection() {
			var point = Projection.Transform(new Windows.Foundation.Point(0, 0));
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
		}
		[TestMethod]
		public void Matrix_ModelX1() {
			var point = ModelX1.Transform(TestPoint);
			AssertDouble(0.5, point.X, "X failed");
			AssertDouble(0, point.Y, "Y failed");
		}
		[TestMethod]
		public void Matrix_ModelX2() {
			var point = ModelX2.Transform(TestPoint);
			AssertDouble((XX - X_MIN2)/X_RANGE2, point.X, "X failed");
			AssertDouble(0, point.Y, "Y failed");
		}
		[TestMethod]
		public void Model_NomalizesAxes() {
			var point = ModelX1.Transform(new Point(X_MIN1, Y_MAX));
			AssertDouble(0, point.X, "UL.X1 failed");
			AssertDouble(0, point.Y, "UL.Y failed");
			point = ModelX1.Transform(new Point(X_MAX1, Y_MIN));
			AssertDouble(1, point.X, "LR.X1 failed");
			AssertDouble(1, point.Y, "LR.Y failed");
			point = ModelX1.Transform(new Point(X_MIN1 + (X_MAX1 - X_MIN1)/2, Y_MIN + (Y_MAX - Y_MIN)/2));
			AssertDouble(.5, point.X, "mid.X failed");
			AssertDouble(.5, point.Y, "mid.Y failed");
		}
		[TestMethod]
		public void Matrix_Multiply() {
			var modelproj = eScapeLLC.UWP.Charts.MatrixHelper.Multiply(Projection, ModelX1);
			TestContext.WriteLine($"modelproj {modelproj}");
			var point = modelproj.Transform(new Point(X_MIN1, Y_MAX));
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
		}
		[UITestMethod]
		public void Matrix_MultiplySameAsTransformGroup() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = ModelX1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var modelproj = eScapeLLC.UWP.Charts.MatrixHelper.Multiply(Projection, ModelX1);
			TestContext.WriteLine($"modelproj {modelproj}");
			Assert.AreEqual(gt.Value.M11, modelproj.M11, "M11 failed");
			Assert.AreEqual(gt.Value.M12, modelproj.M12, "M12 failed");
			Assert.AreEqual(gt.Value.M21, modelproj.M21, "M21 failed");
			Assert.AreEqual(gt.Value.M22, modelproj.M22, "M22 failed");
			Assert.AreEqual(gt.Value.OffsetX, modelproj.OffsetX, "OffsetX failed");
			Assert.AreEqual(gt.Value.OffsetY, modelproj.OffsetY, "OffsetY failed");
		}
		[UITestMethod]
		public void Matrix_CombineModelProjection() {
			var gt = new TransformGroup();
			gt.Children.Add(new MatrixTransform() { Matrix = ModelX1 });
			gt.Children.Add(new MatrixTransform() { Matrix = Projection });
			TestContext.WriteLine($"final matrix {gt.Value}");
			var point = gt.TransformPoint(new Point(X_MIN1, Y_MAX));
			Assert.AreEqual(Bounds.Left, point.X, "X failed");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed");
			point = gt.TransformPoint(new Point(X_MAX1, Y_MIN));
			Assert.AreEqual(Bounds.Left + Bounds.Width, point.X, "X failed");
			Assert.AreEqual(Bounds.Top + Bounds.Height, point.Y, "Y failed");
		}
	}
}
