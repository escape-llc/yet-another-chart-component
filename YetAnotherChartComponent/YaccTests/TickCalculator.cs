using eScapeLLC.UWP.Charts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Yacc.Tests {
	[TestClass]
	public class UnitTest_TickCalculator {
		public TestContext TestContext { get; set; }
		[TestMethod]
		public void FlameTest_CrossZero_1s() {
			double max = 5.25;
			double min = -3.5;
			var tc = new TickCalculator(min, max);
			TestContext.WriteLine("range {0} tickintv {1}", tc.Range, tc.TickInterval);
			Assert.AreEqual(8.75, tc.Range, "Range failed");
			Assert.AreEqual(0, tc.DecimalPlaces, "DecimalPlaces failed");
			Assert.AreEqual(1, tc.TickInterval, "TickInterval failed");
			var ticks = tc.GetTicks().ToList();
			Assert.AreEqual(9, ticks.Count, "ticks.Count failed");
			Assert.AreEqual(0, ticks[0], "ticks[0] failed");
			Assert.AreEqual(1, ticks[1], "ticks[1] failed");
			Assert.AreEqual(-1, ticks[2], "ticks[2] failed");
			Assert.AreEqual(2, ticks[3], "ticks[3] failed");
			Assert.AreEqual(-2, ticks[4], "ticks[4] failed");
			Assert.AreEqual(3, ticks[5], "ticks[5] failed");
			Assert.AreEqual(-3, ticks[6], "ticks[6] failed");
			Assert.AreEqual(4, ticks[7], "ticks[7] failed");
			Assert.AreEqual(5, ticks[8], "ticks[8] failed");
		}
		[TestMethod]
		public void FlameTest2_CrossZero_10s() {
			double max = 50.25;
			double min = -30.5;
			var tc = new TickCalculator(min, max);
			TestContext.WriteLine("range {0} tickintv {1}", tc.Range, tc.TickInterval);
			Assert.AreEqual(80.75, tc.Range, "Range failed");
			Assert.AreEqual(1, tc.DecimalPlaces, "DecimalPlaces failed");
			Assert.AreEqual(10, tc.TickInterval, "TickInterval failed");
			var ticks = tc.GetTicks().ToList();
			Assert.AreEqual(9, ticks.Count, "ticks.Count failed");
			Assert.AreEqual(0, ticks[0], "ticks[0] failed");
			Assert.AreEqual(10, ticks[1], "ticks[1] failed");
			Assert.AreEqual(-10, ticks[2], "ticks[2] failed");
			Assert.AreEqual(20, ticks[3], "ticks[3] failed");
			Assert.AreEqual(-20, ticks[4], "ticks[4] failed");
			Assert.AreEqual(30, ticks[5], "ticks[5] failed");
			Assert.AreEqual(-30, ticks[6], "ticks[6] failed");
			Assert.AreEqual(40, ticks[7], "ticks[7] failed");
			Assert.AreEqual(50, ticks[8], "ticks[8] failed");
		}
		[TestMethod]
		public void FlameTest_CrossZero_Tenths() {
			double max = .525;
			double min = -.35;
			var tc = new TickCalculator(min, max);
			TestContext.WriteLine("range {0} tickintv {1}", tc.Range, tc.TickInterval);
			Assert.AreEqual(.875, tc.Range, "Range failed");
			Assert.AreEqual(-1, tc.DecimalPlaces, "DecimalPlaces failed");
			Assert.AreEqual(0.1, tc.TickInterval, "TickInterval failed");
			var ticks = tc.GetTicks().ToList();
			Assert.AreEqual(9, ticks.Count, "ticks.Count failed");
			var epi = Math.Pow(10, tc.DecimalPlaces - 1);
			Assert.AreEqual(0, ticks[0], "ticks[0] failed");
			Assert.AreEqual(.1, ticks[1], "ticks[1] failed");
			Assert.AreEqual(-.1, ticks[2], "ticks[2] failed");
			Assert.AreEqual(.2, ticks[3], "ticks[3] failed");
			Assert.AreEqual(-.2, ticks[4], "ticks[4] failed");
			Assert.IsTrue(TickCalculator.Equals(.3, ticks[5], epi), "ticks[5] failed");
			Assert.IsTrue(TickCalculator.Equals(-.3, ticks[6], epi), "ticks[6] failed");
			Assert.AreEqual(.4, ticks[7], "ticks[7] failed");
			Assert.AreEqual(.5, ticks[8], "ticks[8] failed");
		}
		[TestMethod]
		public void FlameTest_PositiveOnly_Tenths() {
			double max = 5.25;
			double min = 3.5;
			var tc = new TickCalculator(min, max);
			TestContext.WriteLine("range {0} tickintv {1}", tc.Range, tc.TickInterval);
			Assert.AreEqual(1.75, tc.Range, "Range failed");
			Assert.AreEqual(-1, tc.DecimalPlaces, "DecimalPlaces failed");
			Assert.AreEqual(0.1, tc.TickInterval, "TickInterval failed");
			var ticks = tc.GetTicks().ToList();
			Assert.AreEqual(18, ticks.Count, "ticks.Count failed");
			var epi = Math.Pow(10, tc.DecimalPlaces - 1);
			Assert.IsTrue(TickCalculator.Equals(4.4, ticks[0], epi), "ticks[0] failed");
			Assert.IsTrue(TickCalculator.Equals(4.5, ticks[1], epi), "ticks[1] failed");
			Assert.IsTrue(TickCalculator.Equals(4.3, ticks[2], epi), "ticks[2] failed");
			Assert.IsTrue(TickCalculator.Equals(4.6, ticks[3], epi), "ticks[3] failed");
			Assert.IsTrue(TickCalculator.Equals(4.2, ticks[4], epi), "ticks[4] failed");
			Assert.IsTrue(TickCalculator.Equals(4.7, ticks[5], epi), "ticks[5] failed");
			Assert.IsTrue(TickCalculator.Equals(4.1, ticks[6], epi), "ticks[6] failed");
			Assert.IsTrue(TickCalculator.Equals(4.8, ticks[7], epi), "ticks[7] failed");
			Assert.IsTrue(TickCalculator.Equals(4.0, ticks[8], epi), "ticks[8] failed");
			Assert.IsTrue(TickCalculator.Equals(4.9, ticks[9], epi), "ticks[9] failed");
			Assert.IsTrue(TickCalculator.Equals(3.9, ticks[10], epi), "ticks[10] failed");
			Assert.IsTrue(TickCalculator.Equals(5.0, ticks[11], epi), "ticks[11] failed");
			Assert.IsTrue(TickCalculator.Equals(3.8, ticks[12], epi), "ticks[12] failed");
			Assert.IsTrue(TickCalculator.Equals(5.1, ticks[13], epi), "ticks[13] failed");
			Assert.IsTrue(TickCalculator.Equals(3.7, ticks[14], epi), "ticks[14] failed");
			Assert.IsTrue(TickCalculator.Equals(5.2, ticks[15], epi), "ticks[15] failed");
			Assert.IsTrue(TickCalculator.Equals(3.6, ticks[16], epi), "ticks[16] failed");
			Assert.IsTrue(TickCalculator.Equals(3.5, ticks[17], epi), "ticks[17] failed");
		}
		[TestMethod]
		public void FlameTest_NegativeOnly_Tenths() {
			double max = -3.5;
			double min = -5.25;
			var tc = new TickCalculator(min, max);
			TestContext.WriteLine("range {0} tickintv {1}", tc.Range, tc.TickInterval);
			Assert.AreEqual(1.75, tc.Range, "Range failed");
			Assert.AreEqual(-1, tc.DecimalPlaces, "DecimalPlaces failed");
			Assert.AreEqual(0.1, tc.TickInterval, "TickInterval failed");
			var ticks = tc.GetTicks().ToList();
			Assert.AreEqual(18, ticks.Count, "ticks.Count failed");
			var epi = Math.Pow(10, tc.DecimalPlaces - 1);
			Assert.IsTrue(TickCalculator.Equals(-4.4, ticks[0], epi), "ticks[0] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.5, ticks[2], epi), "ticks[2] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.3, ticks[1], epi), "ticks[1] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.6, ticks[4], epi), "ticks[4] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.2, ticks[3], epi), "ticks[3] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.7, ticks[6], epi), "ticks[6] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.1, ticks[5], epi), "ticks[5] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.8, ticks[8], epi), "ticks[8] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.0, ticks[7], epi), "ticks[7] failed");
			Assert.IsTrue(TickCalculator.Equals(-4.9, ticks[10], epi), "ticks[10] failed");
			Assert.IsTrue(TickCalculator.Equals(-3.9, ticks[9], epi), "ticks[9] failed");
			Assert.IsTrue(TickCalculator.Equals(-5.0, ticks[12], epi), "ticks[12] failed");
			Assert.IsTrue(TickCalculator.Equals(-3.8, ticks[11], epi), "ticks[11] failed");
			Assert.IsTrue(TickCalculator.Equals(-5.1, ticks[14], epi), "ticks[14] failed");
			Assert.IsTrue(TickCalculator.Equals(-3.7, ticks[13], epi), "ticks[13] failed");
			Assert.IsTrue(TickCalculator.Equals(-5.2, ticks[16], epi), "ticks[16] failed");
			Assert.IsTrue(TickCalculator.Equals(-3.6, ticks[15], epi), "ticks[15] failed");
			Assert.IsTrue(TickCalculator.Equals(-3.5, ticks[17], epi), "ticks[17] failed");
		}
	}
}
