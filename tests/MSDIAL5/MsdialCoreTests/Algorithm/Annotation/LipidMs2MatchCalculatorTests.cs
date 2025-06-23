using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class LipidMs2MatchCalculatorTests
    {
        [TestMethod()]
        public void CalculateTest() {
            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, CollisionCrossSection = 102,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms2Tolerance = 0.05f,
                MassRangeBegin = 0,
                MassRangeEnd = 2000,
                SquaredWeightedDotProductCutOff = 0.5f,
                SquaredSimpleDotProductCutOff = 0.5f,
                SquaredReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f,
                MinimumSpectrumMatch = 3,
            };
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                }
            };

            var calculator = new LipidMs2MatchCalculator();
            var result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);

            Assert.AreEqual(0.8547842944, result.SquaredWeightedDotProduct, 0.00001);
            Assert.AreEqual(0.8550592192, result.SquaredSimpleDotProduct, 0.00001);
            Assert.AreEqual(0.7361639372, result.SquaredReverseDotProduct, 0.00001);
            Assert.AreEqual(3f/6 + 0.5, result.MatchedPeaksPercentage);
            Assert.AreEqual(3, result.MatchedPeaksCount);
            var expected = ((System.Math.Sqrt(result.SquaredWeightedDotProduct) + System.Math.Sqrt(result.SquaredSimpleDotProduct) + System.Math.Sqrt(result.SquaredReverseDotProduct)) / 3 + result.MatchedPeaksPercentage) / 2; 
            Assert.AreEqual(expected, result.TotalScore);
            Assert.IsTrue(result.IsSpectrumMatch);
        }

        [TestMethod()]
        public void CalculateFailedTest() {
            var target = new ChromatogramPeakFeature {
                PrecursorMz = 810.604, CollisionCrossSection = 102,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                }
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms2Tolerance = 0.05f,
                MassRangeBegin = 0,
                MassRangeEnd = 2000,
                SquaredWeightedDotProductCutOff = 0.5f,
                SquaredSimpleDotProductCutOff = 0.5f,
                SquaredReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f,
                MinimumSpectrumMatch = 3,
            };
            var reference = new MoleculeMsReference {
                Name = "PC 18:0_20:4", CompoundClass = "PC",
                PrecursorMz = 810.601, CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                }
            };

            var calculator = new LipidMs2MatchCalculator();

            var result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsTrue(result.IsSpectrumMatch);

            parameter.SquaredWeightedDotProductCutOff = 0.9f;
            result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsFalse(result.IsSpectrumMatch);

            parameter.SquaredWeightedDotProductCutOff = 0.5f;
            parameter.SquaredSimpleDotProductCutOff = 0.9f;
            result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsFalse(result.IsSpectrumMatch);

            parameter.SquaredSimpleDotProductCutOff = 0.5f;
            parameter.SquaredReverseDotProductCutOff = 0.8f;
            result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsFalse(result.IsSpectrumMatch);

            parameter.SquaredReverseDotProductCutOff = 0.5f;
            parameter.MatchedPeaksPercentageCutOff = 0.6f;
            result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsTrue(result.IsSpectrumMatch);

            parameter.MatchedPeaksPercentageCutOff = 0.5f;
            parameter.MinimumSpectrumMatch = 7;
            result = calculator.Calculate(new MSScanMatchQuery(target, parameter), reference);
            Assert.IsFalse(result.IsSpectrumMatch);
        }
    }
}