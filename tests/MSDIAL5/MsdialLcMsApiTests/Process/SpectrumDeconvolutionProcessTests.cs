using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process.Tests;

/// <summary>
/// An AIF file whose product-ion scans carry no collision energy above 0 gets no deconvolution at all. Before,
/// every energy was skipped with "No correct CE information in AIF-MSDEC", no .dcl was written, and the run
/// failed later at the first reader of the missing file with an error that named neither the file nor the cause.
/// </summary>
[TestClass]
public sealed class SpectrumDeconvolutionProcessTests
{
    [TestMethod]
    public void AnAifFileWithoutAnyCollisionEnergyIsReportedByName() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 1, ScanStartTime = 1d, },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 0d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, }, },
        });
        var file = new AnalysisFileBean { AnalysisFileName = "QC-AIF-01", AcquisitionType = AcquisitionType.AIF, };

        var ex = Assert.ThrowsException<InvalidOperationException>(() => Deconvolute(provider, file));

        StringAssert.Contains(ex.Message, "QC-AIF-01");
        StringAssert.Contains(ex.Message, "no MS2 scan carries a collision energy above 0");
    }

    [TestMethod]
    public void AnAifFileWithACollisionEnergyIsDeconvolutedAtIt() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 1, ScanStartTime = 1d, },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 35d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, }, },
        });
        var file = new AnalysisFileBean { AnalysisFileName = "QC-AIF-02", AcquisitionType = AcquisitionType.AIF, };

        var collections = Deconvolute(provider, file);

        Assert.AreEqual(1, collections.Count);
        Assert.AreEqual(35d, collections[0].CollisionEnergy);
    }

    [TestMethod]
    public void ANonAifFileWithoutACollisionEnergyIsStillDeconvoluted() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 1, ScanStartTime = 1d, },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 0d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, }, },
        });
        var file = new AnalysisFileBean { AnalysisFileName = "QC-SWATH-01", AcquisitionType = AcquisitionType.SWATH, };

        var collections = Deconvolute(provider, file);

        Assert.AreEqual(1, collections.Count);
    }

    private static List<CompMs.MsdialCore.MSDec.MSDecResultCollection> Deconvolute(IDataProvider provider, AnalysisFileBean file) {
        var storage = new MsdialLcmsDataStorage { MsdialLcmsParameter = new MsdialLcmsParameter(), };
        var process = new SpectrumDeconvolutionProcess(storage);
        return process.Deconvolute(provider, [], file, new ChromatogramPeaksDataSummaryDto(), null, CancellationToken.None);
    }
}
