using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart.Tests
{
    [TestClass()]
    public class RawDataUnavailableChartTests
    {
        [TestMethod()]
        public void TheRawMs2ChromatogramSaysWhyBeforeAnyPeakIsSelected() {
            var peak = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>(null);
            var msdec = new ReactivePropertySlim<MSDecResult?>(null);

            using var model = new Ms2ChromatogramsModel(peak, msdec, new FailingLoader(), new FailingProvider(), new ParameterBase(), AcquisitionType.DDA, new MessageBroker(), isRawDataAvailable: false);

            Assert.IsTrue(model.IsRawSelected.Value);
            Assert.AreEqual(RawDataGuard.RawMs2ChromatogramTitle, model.ChromatogramsModel.Value?.GraphTitle);
            Assert.AreEqual(0, model.ChromatogramsModel.Value!.DisplayChromatograms.Count);

            peak.Value = new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature());
            Assert.AreEqual(RawDataGuard.RawMs2ChromatogramTitle, model.ChromatogramsModel.Value?.GraphTitle);
            Assert.AreEqual(0, model.ChromatogramsModel.Value!.DisplayChromatograms.Count);
        }

        [TestMethod()]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public void BothCanBeSelectedOnlyWithTheRawData(bool isRawDataAvailable, bool expected) {
            var peak = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>(null);
            var msdec = new ReactivePropertySlim<MSDecResult?>(null);

            using var model = new Ms2ChromatogramsModel(peak, msdec, new FailingLoader(), new FailingProvider(), new ParameterBase(), AcquisitionType.SWATH, new MessageBroker(), isRawDataAvailable);

            Assert.AreEqual(expected, model.IsBothEnabled.Value);
            Assert.IsTrue(model.IsDeconvolutedEnabled.Value);
        }

        [TestMethod()]
        public void TheSurveyScanKeepsItsUnavailableTitle() {
            var peak = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>(null);
            using var spectrum = new SurveyScanSpectrum(peak, _ => Observable.Return(new List<SpectrumPeakWrapper>(0)));

            using var model = new SurveyScanModel(spectrum, s => s.Mass, s => s.Intensity, unavailableTitle: RawDataGuard.Ms1SpectrumTitle);
            peak.Value = new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature());

            Assert.AreEqual(RawDataGuard.Ms1SpectrumTitle, model.Elements.GraphTitle);
        }

        [TestMethod()]
        public void TheSurveyScanIsTitledByItsSpectrumOtherwise() {
            var peak = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>(null);
            using var spectrum = new SurveyScanSpectrum(peak, _ => Observable.Return(new List<SpectrumPeakWrapper>(0)));

            using var model = new SurveyScanModel(spectrum, s => s.Mass, s => s.Intensity);
            peak.Value = new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature());

            StringAssert.StartsWith(model.Elements.GraphTitle, "MS1 spectra max intensity: 0");
        }

        sealed class FailingLoader : IMsSpectrumLoader<ChromatogramPeakFeatureModel>
        {
            public IObservable<IMSScanProperty?> LoadScanAsObservable(ChromatogramPeakFeatureModel target) {
                Assert.Fail("The raw MS/MS spectrum was asked for without raw data.");
                return Observable.Return<IMSScanProperty?>(null);
            }
        }

        sealed class FailingProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => throw new AssertFailedException("The raw data were read.");
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => throw new AssertFailedException("The raw data were read.");
            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => throw new AssertFailedException("The raw data were read.");
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => throw new AssertFailedException("The raw data were read.");
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => throw new AssertFailedException("The raw data were read.");
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => throw new AssertFailedException("The raw data were read.");
        }
    }
}
