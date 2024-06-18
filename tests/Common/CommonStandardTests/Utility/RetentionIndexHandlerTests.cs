using CompMs.Common.Components;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.Common.Utility.Tests
{
    [TestClass()]
    public class RetentionIndexHandlerTests
    {

        [DataTestMethod()]
        [DataRow(1d, RiCompoundType.Alkanes)]
        [DataRow(2d, RiCompoundType.Alkanes)]
        [DataRow(3d, RiCompoundType.Alkanes)]
        [DataRow(1.5d, RiCompoundType.Alkanes)]
        [DataRow(0.5d, RiCompoundType.Alkanes)]
        [DataRow(3.5d, RiCompoundType.Alkanes)]
        [DataRow(0d, RiCompoundType.Alkanes)]
        [DataRow(4d, RiCompoundType.Alkanes)]
        [DataRow(1d, RiCompoundType.Fames)]
        [DataRow(2d, RiCompoundType.Fames)]
        [DataRow(3d, RiCompoundType.Fames)]
        [DataRow(1.5d, RiCompoundType.Fames)]
        [DataRow(0.5d, RiCompoundType.Fames)]
        [DataRow(3.5d, RiCompoundType.Fames)]
        [DataRow(0d, RiCompoundType.Fames)]
        [DataRow(4d, RiCompoundType.Fames)]
        public void ConvertAndConvertBack(double raw, RiCompoundType compoundType) {
            var handler = new RetentionIndexHandler(compoundType, new Dictionary<int, float> {
                [8] = 8f, [9] = 9f, [10] = 10f, [12] = 12f, [14] = 14f,
                [16] = 16f, [18] = 18f, [20] = 20f, [22] = 22f, [24] = 24f,
                [26] = 26f, [28] = 28f, [30] = 30f });

            var rt = new RetentionTime(raw);
            var ri = handler.Convert(rt);
            var actual = handler.ConvertBack(ri);

            Assert.AreEqual(rt.Value, actual.Value, 1e-5);
        }
    }
}