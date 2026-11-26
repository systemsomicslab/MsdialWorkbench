using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm.Tests
{
    /// <summary>
    /// The m/z a file's annotated peak is quantified at. The library QUANTMASS replaces the
    /// deconvoluted model peak m/z only when "Replace quant mass by user defined value" is on and
    /// the reference defines a QUANTMASS; otherwise the model peak m/z stays. Both annotation paths
    /// ("Only report top hit" on and off) follow the same rule.
    /// </summary>
    /// <remarks>
    /// The case that motivated this: an integer library QUANTMASS (147) with accurate-mass data. The
    /// per-file EIC is drawn at the quant mass with the centroid tolerance, so quantifying at 147
    /// instead of 147.0686 misses the ion even though the analyst never asked for the library value.
    /// </remarks>
    [TestClass()]
    public class AnnotationQuantMassTests
    {
        private const double ModelPeakMz = 147.0686d;
        private const double LibraryQuantMass = 147d;

        [DataTestMethod()]
        [DataRow(false)]
        [DataRow(true)]
        public void WithTheOptionOffTheModelPeakMzIsKept(bool onlyReportTopHit) {
            var annotated = Annotate(onlyReportTopHit, replaceQuantMass: false, referenceQuantMass: LibraryQuantMass);

            Assert.IsFalse(annotated.IsUnknown, "the peak must be annotated for the quant mass rule to be in play");
            Assert.AreEqual(ModelPeakMz, annotated.QuantMass);
        }

        [DataTestMethod()]
        [DataRow(false)]
        [DataRow(true)]
        public void WithTheOptionOnTheLibraryQuantMassIsUsed(bool onlyReportTopHit) {
            var annotated = Annotate(onlyReportTopHit, replaceQuantMass: true, referenceQuantMass: LibraryQuantMass);

            Assert.IsFalse(annotated.IsUnknown);
            Assert.AreEqual(LibraryQuantMass, annotated.QuantMass);
        }

        [DataTestMethod()]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void AReferenceWithoutQuantMassKeepsTheModelPeakMz(bool onlyReportTopHit, bool replaceQuantMass) {
            var annotated = Annotate(onlyReportTopHit, replaceQuantMass, referenceQuantMass: 0d);

            Assert.IsFalse(annotated.IsUnknown);
            Assert.AreEqual(ModelPeakMz, annotated.QuantMass);
        }

        [DataTestMethod()]
        [DataRow(false)]
        [DataRow(true)]
        public void AnUnannotatedPeakKeepsTheModelPeakMz(bool onlyReportTopHit) {
            // A reference far outside the search window, so nothing is retrieved.
            var annotated = Annotate(onlyReportTopHit, replaceQuantMass: true, referenceQuantMass: LibraryQuantMass, referenceRt: 50d);

            Assert.IsTrue(annotated.IsUnknown);
            Assert.AreEqual(ModelPeakMz, annotated.QuantMass);
        }

        private static AnnotatedMSDecResult Annotate(bool onlyReportTopHit, bool replaceQuantMass, double referenceQuantMass, double referenceRt = 5d) {
            var parameter = new MsdialGcmsParameter
            {
                RetentionType = RetentionType.RT,
                IsReplaceQuantmassByUserDefinedValue = replaceQuantMass,
            };
            parameter.OnlyReportTopHitInMspSearch = onlyReportTopHit;
            parameter.MspSearchParam.Ms1Tolerance = .5f;
            parameter.MspSearchParam.Ms2Tolerance = .5f;
            parameter.MspSearchParam.RtTolerance = 1f;

            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                QuantMass = referenceQuantMass,
                ChromXs = new ChromXs(referenceRt, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 73, Intensity = 100, },
                    new SpectrumPeak { Mass = 147, Intensity = 60, },
                    new SpectrumPeak { Mass = 205, Intensity = 40, },
                    new SpectrumPeak { Mass = 291, Intensity = 20, },
                },
            };
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference> { reference, }, "MspDB", DataBaseSource.Msp, SourceType.MspDB, "MspPath");
            var item = new DataBaseItem<MoleculeDataBase>(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>());
            var annotation = new Annotation(item, parameter, numThreads: 1);

            // Accurate-mass deconvoluted spectrum of the same compound.
            var msdec = new MSDecResult
            {
                ScanID = 0,
                ChromXs = new ChromXs(5d, ChromXType.RT, ChromXUnit.Min),
                ModelPeakMz = ModelPeakMz,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 73.0468, Intensity = 100, },
                    new SpectrumPeak { Mass = ModelPeakMz, Intensity = 60, },
                    new SpectrumPeak { Mass = 205.0874, Intensity = 40, },
                    new SpectrumPeak { Mass = 291.1062, Intensity = 20, },
                },
            };

            var results = annotation.MainProcess(new List<MSDecResult> { msdec, }, new ReportProgress(null, 0d, 100d));
            Assert.AreEqual(1, results.Length);
            return results[0];
        }
    }
}
