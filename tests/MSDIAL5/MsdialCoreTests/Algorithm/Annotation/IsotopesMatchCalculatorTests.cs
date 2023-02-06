using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class IsotopesMatchCalculatorTests
    {
        [TestMethod()]
        public void CalculateTest() {
            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604,
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
            };
            var isotopicPeaks = new List<IsotopicPeak>
            {
                new IsotopicPeak { RelativeAbundance = 1},
                new IsotopicPeak { RelativeAbundance = 3.5},
                new IsotopicPeak { RelativeAbundance = 4},
                new IsotopicPeak { RelativeAbundance = 4.1},
                new IsotopicPeak { RelativeAbundance = 1.2},
            };
            var reference = new MoleculeMsReference {
                PrecursorMz = 810.601,
                IsotopicPeaks = new List<IsotopicPeak>
                {
                    new IsotopicPeak { RelativeAbundance = 1},
                    new IsotopicPeak { RelativeAbundance = 4},
                    new IsotopicPeak { RelativeAbundance = 6},
                    new IsotopicPeak { RelativeAbundance = 4},
                    new IsotopicPeak { RelativeAbundance = 1},
                },
            };

            var calculator = new IsotopesMatchCalculator();
            var result = calculator.Calculate(new IsotopesMatchQuery(isotopicPeaks, target.PrecursorMz, parameter.Ms1Tolerance), reference);
            Assert.AreEqual(0.35060975609756084, result.IsotopeSimilarity, 0.00001);
        }
    }
}