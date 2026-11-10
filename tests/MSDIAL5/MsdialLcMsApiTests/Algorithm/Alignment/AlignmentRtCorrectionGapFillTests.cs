using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

/// <summary>
/// The two places alignment reads a file through the automatic RT correction without any file on
/// disk but a peak list: the gap filler, which extracts on the file's original RTs and reports on
/// the corrected axis, and the data accessor, which hands the aligner each file's peaks on the
/// corrected axis (CONF-19 of the final review).
/// </summary>
[TestClass]
public class AlignmentRtCorrectionGapFillTests {
    // File 1 runs 0.3 min late against the reference file 0: original 4.0 and 6.0 are 4.3 and
    // 6.3 on the corrected axis.
    private static AlignmentRetentionTimeCorrectionCollection LateByPointThree() {
        var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] {
            new AlignmentRetentionTimeCorrectionControlPoint(1, 100d, 4d, 4.3d, 1d),
            new AlignmentRetentionTimeCorrectionControlPoint(2, 200d, 6d, 6.3d, 1d),
        });
        return new AlignmentRetentionTimeCorrectionCollection(0, new Dictionary<int, IAlignmentRetentionTimeCorrectionModel> { [1] = model });
    }

    [TestMethod]
    public void GapFill_ExtractsAtTheOriginalRtAndReportsTheCorrectedRt() {
        // In file 1 the compound's apex lies at original 4.70, which is 5.00 on the corrected axis,
        // where the spot was found in the other files.
        var spectra = Enumerable.Range(0, 301)
            .Select(index => 3.5d + index * 0.01d)
            .Select((rt, index) => new RawSpectrum {
                Index = index,
                MsLevel = 1,
                ScanPolarity = ScanPolarity.Positive,
                ScanStartTime = rt,
                Spectrum = new[] { new RawPeakElement { Mz = 100d, Intensity = 10d + 10000d * Math.Exp(-Math.Pow(rt - 4.7d, 2) / (2d * 0.03d * 0.03d)) } },
            })
            .ToList();
        var ms1Spectra = new Ms1Spectra(spectra, IonMode.Positive, AcquisitionType.DDA);
        var parameter = new MsdialLcmsParameter {
            CentroidMs1Tolerance = 0.01f,
            RetentionTimeAlignmentTolerance = 0.1f,
            SmoothingLevel = 0,
            IsForceInsertForGapFilling = true,
        };
        var center = new ChromXs(new RetentionTime(5d, ChromXUnit.Min)) { Mz = new MzValue(100d) };

        var corrected = new AlignmentChromPeakFeature { FileID = 1, MasterPeakID = -1, PeakID = -1, IonMode = IonMode.Positive };
        new LcmsGapFiller(parameter, LateByPointThree()).GapFill(ms1Spectra, corrected, center, 0.1d, 1f);

        Assert.AreEqual(5d, corrected.ChromXsTop.RT.Value, 0.011d, "reported on the corrected axis, the spot's");
        Assert.IsTrue(corrected.ChromXsLeft.RT.Value < 5d && corrected.ChromXsRight.RT.Value > 5d, "the edges too");
        Assert.AreEqual(10010d, corrected.PeakHeightTop, 50d, "the apex at original 4.70, not the baseline at original 5.00");

        // Without the correction the same window is read at original 5.00, where there is no peak:
        // the difference is the mapping, not the data.
        var uncorrected = new AlignmentChromPeakFeature { FileID = 1, MasterPeakID = -1, PeakID = -1, IonMode = IonMode.Positive };
        new LcmsGapFiller(parameter).GapFill(ms1Spectra, uncorrected, center, 0.1d, 1f);
        Assert.IsTrue(uncorrected.PeakHeightTop < 100d, $"height {uncorrected.PeakHeightTop}");
    }

    [TestMethod]
    public void DataAccessor_HandsTheAlignerEachFilesPeaksOnTheCorrectedAxis_AndLeavesThePeakFileAlone() {
        var folder = Path.Combine(Path.GetTempPath(), "autort-accessor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var files = new[] {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "reference", PeakAreaBeanInformationFilePath = Path.Combine(folder, "reference.pai") },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "late", PeakAreaBeanInformationFilePath = Path.Combine(folder, "late.pai") },
            };
            foreach (var file in files) {
                MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath, new List<ChromatogramPeakFeature> {
                    Peak(0, 200d, 6d),
                    Peak(1, 100d, 4d),
                });
            }
            var accessor = new LcmsDataAccessor(new MsdialLcmsParameter(), LateByPointThree());

            var reference = accessor.GetMSScanProperties(files[0]);
            var late = accessor.GetMSScanProperties(files[1]);

            CollectionAssert.AreEqual(new[] { 4d, 6d }, reference.Select(peak => peak.ChromXs.RT.Value).ToArray(), "the reference file has no map");
            CollectionAssert.AreEqual(new[] { 100d, 200d }, late.Select(peak => peak.PrecursorMz).ToArray(), "sorted by m/z, then RT");
            Assert.AreEqual(4.3d, late[0].ChromXs.RT.Value, 1e-9);
            Assert.AreEqual(6.3d, late[1].ChromXs.RT.Value, 1e-9);
            var edges = (ChromatogramPeakFeature)late[0];
            Assert.AreEqual(4.25d, edges.ChromXsLeft.RT.Value, 1e-9);
            Assert.AreEqual(4.35d, edges.ChromXsRight.RT.Value, 1e-9);
            CollectionAssert.AreEquivalent(new[] { 4d, 6d },
                MsdialPeakSerializer.LoadChromatogramPeakFeatures(files[1].PeakAreaBeanInformationFilePath).Select(peak => peak.ChromXs.RT.Value).ToArray(),
                "the peak file keeps the original RTs");
        }
        finally {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static ChromatogramPeakFeature Peak(int id, double mz, double rt) {
        return new ChromatogramPeakFeature(new BaseChromatogramPeakFeature {
            ChromXsLeft = new ChromXs(rt - 0.05d, ChromXType.RT, ChromXUnit.Min),
            ChromXsTop = new ChromXs(rt, ChromXType.RT, ChromXUnit.Min),
            ChromXsRight = new ChromXs(rt + 0.05d, ChromXType.RT, ChromXUnit.Min),
            PeakHeightTop = 10000d,
            Mass = mz,
        }) {
            PeakID = id,
            MasterPeakID = id,
        };
    }
}
