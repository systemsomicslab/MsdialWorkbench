using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.AxisManager.Generic.Tests
{
    [TestClass()]
    public class BaseAxisManagerTests
    {
        [TestMethod()]
        public void UpdateInitialRangeTest() {
            var axis = new ContinuousAxisManager<double>(new AxisRange(0d, 100d), new RelativeMargin(.1d));
            Assert.AreEqual(0d, axis.Range.Minimum.Value, 1e-10);
            Assert.AreEqual(100d, axis.Range.Maximum.Value, 1e-10);
            Assert.AreEqual(0d, axis.InitialRange.Minimum.Value, 1e-10);
            Assert.AreEqual(100d, axis.InitialRange.Maximum.Value, 1e-10);

            axis.Recalculate(1000);
            axis.Reset();
            Assert.AreEqual(-10d, axis.Range.Minimum.Value, 1e-10);
            Assert.AreEqual(110d, axis.Range.Maximum.Value, 1e-10);
            Assert.AreEqual(-10d, axis.InitialRange.Minimum.Value, 1e-10);
            Assert.AreEqual(110d, axis.InitialRange.Maximum.Value, 1e-10);

            axis.UpdateInitialRange(new AxisRange(0d, 1000d));
            Assert.AreEqual(0d, axis.Range.Minimum.Value, 1e-10);
            Assert.AreEqual(1000d, axis.Range.Maximum.Value, 1e-10);
            Assert.AreEqual(0d, axis.InitialRange.Minimum.Value, 1e-10);
            Assert.AreEqual(1000d, axis.InitialRange.Maximum.Value, 1e-10);

            axis.Recalculate(0);
            axis.Reset();
            Assert.AreEqual(0d, axis.Range.Minimum.Value, 1e-10);
            Assert.AreEqual(1000d, axis.Range.Maximum.Value, 1e-10);
            Assert.AreEqual(0d, axis.InitialRange.Minimum.Value, 1e-10);
            Assert.AreEqual(1000d, axis.InitialRange.Maximum.Value, 1e-10);

            axis.Recalculate(1000);
            axis.Reset();
            Assert.AreEqual(-100d, axis.Range.Minimum.Value, 1e-10);
            Assert.AreEqual(1100d, axis.Range.Maximum.Value, 1e-10);
            Assert.AreEqual(-100d, axis.InitialRange.Minimum.Value, 1e-10);
            Assert.AreEqual(1100d, axis.InitialRange.Maximum.Value, 1e-10);
        }
    }
}