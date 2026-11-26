using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass]
public class PeakSpottingCoreTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void GetRecalculatedChromPeakFeaturesByMs1MsTolerance_DropsPeakWhenRecalculatedChromatogramIsTooShort(int scanCount) {
        var spectra = new List<RawSpectrum>();
        for (int i = 0; i < scanCount; i++) {
            spectra.Add(new RawSpectrum
            {
                Index = i,
                ScanStartTime = 1d + i * 0.01,
                MsLevel = 1,
                ScanPolarity = ScanPolarity.Positive,
                Spectrum = [new RawPeakElement { Mz = 100d, Intensity = 1000d, }],
            });
        }
        var provider = new StubDataProvider(spectra);
        var parameter = new ParameterBase { IonMode = IonMode.Positive, };
        var feature = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
        {
            Mass = 100d,
            ChromXsLeft = new ChromXs(0.99),
            ChromXsTop = new ChromXs(1.0),
            ChromXsRight = new ChromXs(1.01),
        });

        var core = new PeakSpottingCore(parameter);
        var actual = core.GetRecalculatedChromPeakFeaturesByMs1MsTolerance([feature], provider, AcquisitionType.SWATH);

        Assert.AreEqual(0, actual.Count);
    }

    private sealed class StubDataProvider(IList<RawSpectrum> spectra) : IDataProvider
    {
        private readonly ReadOnlyCollection<RawSpectrum> _spectra = new(spectra);

        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _spectra;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => level == 1 ? _spectra : new([]);
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(LoadMsNSpectrums(level));
        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _spectra;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(_spectra);
    }
}
