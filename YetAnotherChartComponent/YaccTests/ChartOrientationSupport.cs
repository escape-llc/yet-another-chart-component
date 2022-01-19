using eScapeLLC.UWP.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Yacc.Tests {
	[TestClass]
	public class UnitTest_ChartOrientationSupport {
		#region bounding box (P)
		const double TOP = 10;
		const double LEFT = 20;
		const double WIDTH = 60;
		const double HEIGHT = 40;
		static readonly Rect Bounds = new Rect(LEFT, TOP, WIDTH, HEIGHT);
		#endregion
		#region axis information (M)
		const double Y_MIN = -4;
		const double Y_MAX = 6;
		const double X_MIN1 = 0;
		const double X_MAX1 = 5;
		const double X_RANGE1 = X_MAX1 - X_MIN1;
		const double X_MIN2 = X_MIN1 + 2;
		const double X_MAX2 = X_MAX1 + 2;
		const double X_RANGE2 = X_MAX2 - X_MIN2;
		const double Y_RANGE = Y_MAX - Y_MIN;
		static readonly Point W1UL = new Point(X_MIN1, Y_MAX);
		static readonly Point W1LR = new Point(X_MAX1, Y_MIN);
		#endregion
		#region properties
		public TestContext TestContext { get; set; }
		/// <summary>
		/// Maps the rectangle the WC displays in to DC.
		/// </summary>
		public Matrix Projection { get; } = MatrixSupport.ProjectionFor(Bounds);
		public IChartAxis Axis1Horizontal { get; } = new StubIChartAxis()
		.Orientation_Get(() => AxisOrientation.Horizontal)
		.Range_Get(()=>X_RANGE1)
		.Minimum_Get(()=> X_MIN1)
		.Maximum_Get(()=> X_MAX1);
		public IChartAxis Axis1Vertical { get; } = new StubIChartAxis()
		.Orientation_Get(() => AxisOrientation.Vertical)
		.Range_Get(() => X_RANGE1)
		.Minimum_Get(() => X_MIN1)
		.Maximum_Get(() => X_MAX1);
		public IChartAxis Axis2Vertical { get; } = new StubIChartAxis()
			.Orientation_Get(() => AxisOrientation.Vertical)
			.Range_Get(() => Y_RANGE)
			.Minimum_Get(() => Y_MIN)
			.Maximum_Get(() => Y_MAX);
		public IChartAxis Axis2Horizontal { get; } = new StubIChartAxis()
			.Orientation_Get(() => AxisOrientation.Horizontal)
			.Range_Get(() => Y_RANGE)
			.Minimum_Get(() => Y_MIN)
			.Maximum_Get(() => Y_MAX);
		#endregion
		#region negative cases
		[TestMethod, ExpectedException(typeof(ArgumentNullException)), TestCategory("chartorientation")]
		public void AxesCannotBeNull_a1() {
			var a1 = new StubIChartAxis();
			var a2 = new StubIChartAxis();
			var cos = new ChartOrientationSupport(null, a2);
		}
		[TestMethod, ExpectedException(typeof(ArgumentNullException)), TestCategory("chartorientation")]
		public void AxesCannotBeNull_a2() {
			var a1 = new StubIChartAxis();
			var a2 = new StubIChartAxis();
			var cos = new ChartOrientationSupport(a1, null);
		}
		[TestMethod, ExpectedException(typeof(ArgumentNullException)), TestCategory("chartorientation")]
		public void AxesCannotBeNull_a1a2() {
			var cos = new ChartOrientationSupport(null, null);
		}
		[TestMethod, ExpectedException(typeof(InvalidOperationException)), TestCategory("chartorientation")]
		public void AxesCannotBeTheSameInstance() {
			var a1 = new StubIChartAxis();
			var cos = new ChartOrientationSupport(a1, a1);
		}
		[TestMethod,ExpectedException(typeof(InvalidOperationException)), TestCategory("chartorientation")]
		public void AxesCannotMatchOrientation() {
			var a1 = new StubIChartAxis();
			var a2 = new StubIChartAxis();
			var cos = new ChartOrientationSupport(a1, a2);
		}
		#endregion
		#region positive cases
		[TestMethod, TestCategory("chartorientation")]
		public void ConstructHorizontal() {
			var a1 = new StubIChartAxis().Orientation_Get(()=>AxisOrientation.Vertical);
			var a2 = new StubIChartAxis().Orientation_Get(() => AxisOrientation.Horizontal);
			var cos = new ChartOrientationSupport(a1, a2);
			Assert.AreEqual(ChartOrientationSupport.ChartOrientation.Horizontal, cos.Orientation, "Orientation failed");
		}
		[TestMethod, TestCategory("chartorientation")]
		public void ConstructVertical() {
			var a1 = new StubIChartAxis().Orientation_Get(() => AxisOrientation.Horizontal);
			var a2 = new StubIChartAxis().Orientation_Get(() => AxisOrientation.Vertical);
			var cos = new ChartOrientationSupport(a1, a2);
			Assert.AreEqual(ChartOrientationSupport.ChartOrientation.Vertical, cos.Orientation, "Orientation failed");
		}
		[TestMethod, TestCategory("chartorientation")]
		public void ProjectAxesVertical() {
			var a1 = Axis1Horizontal;
			var a2 = Axis2Vertical;
			var cos = new ChartOrientationSupport(a1, a2);
			Assert.AreEqual(ChartOrientationSupport.ChartOrientation.Vertical, cos.Orientation, "Orientation failed");
			var model = MatrixSupport.ModelFor(a1, a2);
			var proj = cos.ProjectionFor(Bounds);
			var modelproj = MatrixSupport.Multiply(proj, model);
			TestContext.WriteLine($"model {model} proj {proj} modelproj {modelproj}");
			var ndc = model.Transform(W1UL);
			var point = modelproj.Transform(W1UL);
			TestContext.WriteLine($"w1ul {W1UL} ndc {ndc} point {point}");
			// UL in NDC is (0,0)
			UnitTest_LinearAlgebra.AssertDouble(0, ndc.X, "ndcX failed(Horizontal)");
			UnitTest_LinearAlgebra.AssertDouble(0, ndc.Y, "ndcY failed(Horizontal)");
			// maps to DC(Left,Top)
			Assert.AreEqual(Bounds.Left, point.X, "X failed(Vertical)");
			Assert.AreEqual(Bounds.Top, point.Y, "Y failed(Vertical)");
		}
		[TestMethod, TestCategory("chartorientation")]
		public void ProjectAxesHorizontal() {
			var a1 = Axis1Vertical;
			var a2 = Axis2Horizontal;
			var cos = new ChartOrientationSupport(a1, a2);
			Assert.AreEqual(ChartOrientationSupport.ChartOrientation.Horizontal, cos.Orientation, "Orientation failed");
			var model = MatrixSupport.ModelFor(a1, a2);
			var proj = cos.ProjectionFor(Bounds);
			var modelproj = MatrixSupport.Multiply(proj, model);
			TestContext.WriteLine($"model {model} proj {proj} modelproj {modelproj}");
			var source = W1UL;
			var ndc = model.Transform(source);
			var dc = proj.Transform(ndc);
			var point = modelproj.Transform(source);
			TestContext.WriteLine($"world {source} ndc {ndc} dc {dc} point {point}");
			// UL in NDC is (0,0)
			UnitTest_LinearAlgebra.AssertDouble(0, ndc.X, "ndcX failed(Horizontal)");
			UnitTest_LinearAlgebra.AssertDouble(0, ndc.Y, "ndcY failed(Horizontal)");
			// maps to DC(Right,Top)
			Assert.AreEqual(Bounds.Right, dc.X, "X failed(Horizontal)");
			Assert.AreEqual(Bounds.Top, dc.Y, "Y failed(Horizontal)");
		}
		[TestMethod, TestCategory("chartorientation")]
		public void ProjectAxesHorizontal_CenterPoint()
		{
			var a1 = Axis1Vertical;
			var a2 = Axis2Horizontal;
			var cos = new ChartOrientationSupport(a1, a2);
			Assert.AreEqual(ChartOrientationSupport.ChartOrientation.Horizontal, cos.Orientation, "Orientation failed");
			//var model = MatrixSupport.ModelFor(a1, a2);

			var model = new Matrix(1 / a1.Range, 0, 0, 1 / a2.Range, -a1.Minimum / a1.Range, -a2.Minimum / a2.Range);
			var proj = cos.ProjectionFor(Bounds);
			// putting these together DOES NOT work, because of the coordinate "flip"
			var modelproj = MatrixSupport.Multiply(proj, model);
			TestContext.WriteLine($"model {model} proj {proj} modelproj {modelproj}");
			var source = new Point(W1UL.X + X_RANGE1/2.0, W1UL.Y - Y_RANGE/2.0);
			// running each one separately WORKS
			var ndc = model.Transform(source);
			var dc = proj.Transform(ndc);
			// this DOES NOT!
			var point = modelproj.Transform(source);
			TestContext.WriteLine($"world {source} ndc {ndc} dc {dc} point {point}");
			// UL in NDC is (0,0)
			UnitTest_LinearAlgebra.AssertDouble(.5, ndc.X, "ndcX failed(Horizontal)");
			UnitTest_LinearAlgebra.AssertDouble(.5, ndc.Y, "ndcY failed(Horizontal)");
			// maps to DC(Right,Top)
			Assert.AreEqual(Bounds.Right - Bounds.Width/2, dc.X, "X failed(Horizontal)");
			Assert.AreEqual(Bounds.Top + Bounds.Height/2, dc.Y, "Y failed(Horizontal)");
		}
		#endregion
	}
}
