using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Information.Tests
{
    [TestClass]
    public class PeakInformationAnalysisModelTests
    {
        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(InitialValueTestData))]
        public void InitialValueTest(IObservable<ChromatogramPeakFeatureModel> target, ChromatogramPeakFeatureModel initialValue) {
            var model = new PeakInformationAnalysisModel(target);
            model.Add(t => new MzPoint(t.Mass));
            var mz = new MzPoint(initialValue.Mass);
            model.Add(t => new HeightAmount(t.Intensity));
            var height = new HeightAmount(initialValue.Intensity);
            Assert.AreEqual(initialValue.Name, model.Annotation);
            Assert.AreEqual(mz.Point, model.PeakPoints[0].Point);
            Assert.AreEqual(height.Amount, model.PeakAmounts[0].Amount);
        }


        public static IEnumerable<object[]> InitialValueTestData {
            get {
                var x = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 500d,
                    PeakHeightTop = 1000d,
                })
                {
                    Name = "Test 1",
                };
                var y = new ChromatogramPeakFeatureModel(x);
                yield return new object[] { new ReactivePropertySlim<ChromatogramPeakFeatureModel>(y), y, };
                var z = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 1000d,
                    PeakHeightTop = 2000d,
                })
                {
                    Name = "Test 2",
                };
                var w = new ChromatogramPeakFeatureModel(z);
                yield return new object[] { Observable.Return(w).ToReactiveProperty(), w, };
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(NextValueTestData))]
        public void NextValueTest(IReactiveProperty<ChromatogramPeakFeatureModel> target, ChromatogramPeakFeatureModel nextValue) {
            var model = new PeakInformationAnalysisModel(target);
            target.Value = nextValue;
            model.Add(t => new MzPoint(t.Mass));
            model.Add(t => new HeightAmount(t.Intensity));
            var mz = new MzPoint(nextValue.Mass);
            var height = new HeightAmount(nextValue.Intensity);
            Assert.AreEqual(nextValue.Name, model.Annotation);
            Assert.AreEqual(mz.Point, model.PeakPoints[0].Point);
            Assert.AreEqual(height.Amount, model.PeakAmounts[0].Amount);
        }


        public static IEnumerable<object[]> NextValueTestData {
            get {
                var x = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 500d,
                    PeakHeightTop = 1000d,
                })
                {
                    Name = "Test 1",
                    AdductType = AdductIon.GetAdductIon("[M-H]-"),
                };
                var y = new ChromatogramPeakFeatureModel(x);
                var x2 = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 501d,
                    PeakHeightTop = 1001d,
                })
                {
                    Name = "Test 1-2",
                    AdductType = AdductIon.GetAdductIon("[M-H]-"),
                };
                var y2 = new ChromatogramPeakFeatureModel(x2);
                yield return new object[] { Observable.Return(y).ToReactiveProperty(), y2, };
                var z = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 1000d,
                    PeakHeightTop = 2000d,
                })
                {
                    Name = "Test 2",
                    AdductType = AdductIon.GetAdductIon("[M-H]-"),
                };
                var w = new ChromatogramPeakFeatureModel(z);
                var z2 = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 1001d,
                    PeakHeightTop = 2001d,
                })
                {
                    Name = "Test 2-2",
                    AdductType = AdductIon.GetAdductIon("[M-H]-"),
                };
                var w2 = new ChromatogramPeakFeatureModel(z2);
                yield return new object[] { Observable.Return(w).ToReactiveProperty(), w2, };
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DynamicData(nameof(UpdateValueTestData))]
        public void UpdateValueTest(IReactiveProperty<ChromatogramPeakFeatureModel> target, string name, double mz, double intensity) {
            var model = new PeakInformationAnalysisModel(target);
            target.Value.Name = name;
            target.Value.Mass = mz;
            target.Value.Intensity = intensity;
            model.Add(t => new MzPoint(t.Mass));
            model.Add(t => new HeightAmount(t.Intensity));
            var mz_ = new MzPoint(target.Value.Mass);
            var height = new HeightAmount(target.Value.Intensity);
            Assert.AreEqual(target.Value.Name, model.Annotation);
            Assert.AreEqual(mz_.Point, model.PeakPoints[0].Point);
            Assert.AreEqual(height.Amount, model.PeakAmounts[0].Amount);
        }


        public static IEnumerable<object[]> UpdateValueTestData {
            get {
                var x = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 500d,
                    PeakHeightTop = 1000d,
                })
                {
                    Name = "Test 1",
                };
                var y = new ChromatogramPeakFeatureModel(x);
                yield return new object[] { Observable.Return(y).ToReactiveProperty(), "Test 1-2", 501d, 1001d, };
                var z = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
                {
                    Mass = 1000d,
                    PeakHeightTop = 2000d,
                })
                {
                    Name = "Test 2",
                };
                var w = new ChromatogramPeakFeatureModel(z);
                yield return new object[] { Observable.Return(w).ToReactiveProperty(), "Test 2-2", 1001d, 2001d, };
            }
        }
    }
}
