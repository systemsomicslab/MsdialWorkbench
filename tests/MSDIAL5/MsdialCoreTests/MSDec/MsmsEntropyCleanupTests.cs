using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.MSDec.Tests
{
    /// <summary>
    /// The MS/MS entropy cleanup. The first tests mirror tests/test_spectrum_cleanup.py of msdial_spectrum_catalog, whose
    /// rule this is; the 3-ion floor mirrors its export (kept_raw_too_few_peaks).
    /// </summary>
    [TestClass]
    public class MsmsEntropyCleanupTests
    {
        private static List<SpectrumPeak> Peaks(params (double Mass, double Intensity)[] peaks) {
            return peaks.Select(p => new SpectrumPeak(p.Mass, p.Intensity)).ToList();
        }

        private static (double, double)[] Pairs(IEnumerable<SpectrumPeak> peaks) {
            return peaks.Select(p => (p.Mass, p.Intensity)).ToArray();
        }

        private static List<SpectrumPeak> Signal() => Peaks((100d, 1000d), (150d, 600d), (200d, 300d));

        private static List<SpectrumPeak> Noise() => Enumerable.Range(0, 300).Select(k => new SpectrumPeak(100.5 + k, 20 + k % 41)).ToList();

        /// <summary>Drop the weakest positive level, one at a time, until the entropy is at most the ceiling; one level is always kept.</summary>
        private static List<SpectrumPeak> BruteForce(List<SpectrumPeak> peaks, double ceiling) {
            while (MsmsEntropyCleanup.SpectralEntropy(peaks) > ceiling && peaks.Where(p => p.Intensity > 0).Select(p => p.Intensity).Distinct().Count() > 1) {
                var weakest = peaks.Where(p => p.Intensity > 0).Min(p => p.Intensity);
                peaks = peaks.Where(p => p.Intensity != weakest).ToList();
            }
            return peaks;
        }

        [TestMethod]
        public void EntropyValues() {
            Assert.AreEqual(0d, MsmsEntropyCleanup.SpectralEntropy(Peaks()));
            Assert.AreEqual(0d, MsmsEntropyCleanup.SpectralEntropy(Peaks((100d, 5d))));
            Assert.AreEqual(Math.Log(4), MsmsEntropyCleanup.SpectralEntropy(Enumerable.Range(0, 4).Select(m => new SpectrumPeak(m, 7d))), 1e-12);
            Assert.AreEqual(-(0.25 * Math.Log(0.25) + 0.75 * Math.Log(0.75)), MsmsEntropyCleanup.SpectralEntropy(Peaks((1d, 1d), (2d, 3d))), 1e-12);
            Assert.AreEqual(0d, MsmsEntropyCleanup.SpectralEntropy(Peaks((1d, 1d), (2d, 0d))), 1e-12, "zero intensity carries nothing");
            Assert.AreEqual(0d, MsmsEntropyCleanup.SpectralEntropy(Peaks((1d, 1d), (2d, -5d))), 1e-12, "neither does a negative one");
        }

        [TestMethod]
        public void ALowEntropySpectrumIsUnchanged() {
            var peaks = Peaks((50d, 1d), (60d, 1000d), (70d, 3d));

            var result = MsmsEntropyCleanup.Clean(peaks, 4.5);

            Assert.AreSame(peaks, result.Peaks);
            Assert.AreEqual(0, result.Removed);
            Assert.IsNull(result.IntensityThreshold);
            Assert.AreEqual(MsmsEntropyCleanupStatus.Unchanged, result.Status);
            Assert.AreEqual(1000d, result.BasePeak);
            Assert.IsNull(result.LargestRemoved);
        }

        [TestMethod]
        public void NoiseIsDroppedAndOrderIsKept() {
            var signal = Signal();
            var peaks = signal.Concat(Noise()).OrderBy(p => p.Mass).ToList(); // m/z order, as stored

            var result = MsmsEntropyCleanup.Clean(peaks, 4.5);

            Assert.IsTrue(result.EntropyBefore > 4.5);
            Assert.IsTrue(result.EntropyAfter <= 4.5);
            Assert.IsTrue(signal.All(result.Peaks.Contains));
            CollectionAssert.AreEqual(result.Peaks.OrderBy(p => p.Mass).ToList(), result.Peaks);
            Assert.AreEqual(peaks.Count - result.Peaks.Count, result.Removed);
            Assert.IsTrue(result.Peaks.All(p => p.Intensity >= result.IntensityThreshold));
            Assert.AreEqual(MsmsEntropyCleanupStatus.Cleaned, result.Status);
            Assert.AreEqual(1000d, result.BasePeak);
            Assert.AreEqual(peaks.Except(result.Peaks).Max(p => p.Intensity), result.LargestRemoved);
            Assert.IsTrue(result.LargestRemoved < result.IntensityThreshold);
            Assert.AreEqual(303, peaks.Count, "the input is not modified");
        }

        [TestMethod]
        public void EqualIntensitiesGoTogether() {
            var flat = Enumerable.Range(0, 200).Select(m => new SpectrumPeak(m, 10d)).ToList();
            var peaks = flat.Concat(Peaks((500d, 100d))).ToList();

            var result = MsmsEntropyCleanup.CleanByEntropy(peaks, 4.5);

            Assert.IsTrue(result.EntropyBefore > 4.5);
            CollectionAssert.AreEqual(new[] { (500d, 100d) }, Pairs(result.Peaks), "the 200 equal ions go together");

            // One level above the ceiling cannot be split; it is kept.
            var alone = MsmsEntropyCleanup.CleanByEntropy(flat, 4.5);
            Assert.AreEqual(200, alone.Peaks.Count);
            Assert.AreEqual(MsmsEntropyCleanupStatus.AboveCeiling, alone.Status);
            Assert.AreEqual(0, alone.Removed);
            Assert.IsTrue(alone.EntropyAfter > 4.5);

            var above = MsmsEntropyCleanup.CleanByEntropy(flat.Concat(Peaks((900d, 1d))).ToList(), 4.5);
            Assert.AreEqual(MsmsEntropyCleanupStatus.AboveCeiling, above.Status);
            Assert.AreEqual(1, above.Removed);
            Assert.AreEqual(200, above.Peaks.Count);
        }

        [TestMethod]
        public void ZeroAndNegativeEntriesAreLeftAsTheyAre() {
            var signal = Signal();
            var noise = Noise();
            var zeros = Enumerable.Range(0, 50).Select(k => new SpectrumPeak(400.5 + k, 0d)).Concat(Peaks((460d, -3d))).ToList();

            var result = MsmsEntropyCleanup.Clean(signal.Concat(noise).Concat(zeros).ToList(), 4.5);

            Assert.IsTrue(zeros.All(result.Peaks.Contains));
            Assert.AreEqual(signal.Count + noise.Count - (result.Peaks.Count - zeros.Count), result.Removed);

            var low = signal.Concat(zeros).ToList();
            var lowResult = MsmsEntropyCleanup.Clean(low, 4.5);
            Assert.AreSame(low, lowResult.Peaks);
            Assert.AreEqual(0, lowResult.Removed);
        }

        [TestMethod]
        public void MatchesTheOneLevelAtATimeRule() {
            var random = new Random(7);
            var ceilings = new[] { 1.0, 2.5, 4.5 };
            for (int trial = 0; trial < 200; trial++) {
                var n = random.Next(1, 401);
                var peaks = Enumerable.Range(0, n).Select(k => {
                    var choice = random.Next(3);
                    var intensity = choice == 0 ? 0 : choice == 1 ? random.Next(1, 21) : random.Next(1, 100001);
                    return new SpectrumPeak(100d + k, intensity);
                }).ToList();
                var ceiling = ceilings[random.Next(ceilings.Length)];

                var result = MsmsEntropyCleanup.CleanByEntropy(peaks, ceiling);

                CollectionAssert.AreEqual(result.Removed > 0 ? BruteForce(peaks, ceiling) : peaks, result.Peaks, $"trial {trial}");
                if (result.Status != MsmsEntropyCleanupStatus.AboveCeiling) {
                    Assert.IsTrue(result.EntropyAfter <= ceiling + 1e-9, $"trial {trial}");
                }
            }
        }

        [TestMethod]
        public void DroppingTheWeakestIonNeverRaisesTheEntropy() {
            var random = new Random(11);
            for (int trial = 0; trial < 300; trial++) {
                var values = Enumerable.Range(0, random.Next(2, 31)).Select(_ => 0.01 + random.NextDouble() * 999.99).OrderByDescending(v => v).ToArray();
                var entropies = Enumerable.Range(1, values.Length).Select(k => MsmsEntropyCleanup.SpectralEntropy(values.Take(k).Select(v => new SpectrumPeak(0d, v)))).ToArray();
                for (int k = 1; k < entropies.Length; k++) {
                    Assert.IsTrue(entropies[k - 1] <= entropies[k] + 1e-12, $"trial {trial}");
                }
            }
        }

        [TestMethod]
        [DataRow(0d)]
        [DataRow(-1d)]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        public void TheCeilingMustBeFiniteAndPositive(double ceiling) {
            Assert.IsFalse(MsmsEntropyCleanup.IsValidMaxEntropy(ceiling));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => MsmsEntropyCleanup.Clean(Peaks((1d, 1d)), ceiling));
        }

        [TestMethod]
        public void ASpectrumThatWouldKeepFewerThanThreeIonsIsKeptRaw() {
            // Two strong ions above a flat floor: the entropy rule alone keeps only the two.
            var peaks = Peaks((100d, 1000d), (200d, 900d)).Concat(Enumerable.Range(0, 400).Select(k => new SpectrumPeak(300d + k, 5d))).ToList();
            Assert.AreEqual(2, MsmsEntropyCleanup.CleanByEntropy(peaks, 1.0).Peaks.Count);

            var result = MsmsEntropyCleanup.Clean(peaks, 1.0);

            Assert.AreEqual(MsmsEntropyCleanupStatus.KeptRawTooFewPeaks, result.Status);
            Assert.AreSame(peaks, result.Peaks);
            Assert.AreEqual(0, result.Removed);
            Assert.AreEqual(result.EntropyBefore, result.EntropyAfter);

            // Zero entries do not count towards the three.
            var withZeros = peaks.Concat(Enumerable.Range(0, 5).Select(k => new SpectrumPeak(800d + k, 0d))).ToList();
            Assert.AreEqual(MsmsEntropyCleanupStatus.KeptRawTooFewPeaks, MsmsEntropyCleanup.Clean(withZeros, 1.0).Status);

            // A spectrum that keeps exactly three is cleaned.
            var three = Peaks((100d, 1000d), (200d, 900d), (250d, 800d)).Concat(Enumerable.Range(0, 400).Select(k => new SpectrumPeak(300d + k, 5d))).ToList();
            var cleaned = MsmsEntropyCleanup.Clean(three, 1.2);
            Assert.AreEqual(MsmsEntropyCleanupStatus.Cleaned, cleaned.Status);
            Assert.AreEqual(3, cleaned.Peaks.Count);
        }

        [TestMethod]
        public void TheCleanupIsMonotoneInTheCeiling() {
            var random = new Random(13);
            for (int trial = 0; trial < 100; trial++) {
                var peaks = Enumerable.Range(0, random.Next(3, 300)).Select(k => new SpectrumPeak(100d + k, random.Next(1, 5000))).ToList();
                var previous = int.MaxValue;
                foreach (var ceiling in new[] { 6.0, 5.0, 4.5, 3.5, 2.5, 1.5, 0.5 }) {
                    var kept = MsmsEntropyCleanup.CleanByEntropy(peaks, ceiling).Peaks;
                    Assert.IsTrue(kept.Count <= previous, $"trial {trial}: a lower ceiling never keeps more ions");
                    previous = kept.Count;
                }
            }
        }

        [TestMethod]
        public void TheProtectedFloorKeepsIonsAboveAShareOfTheBasePeak() {
            // A flat spectrum: the cut reaches ions at half the base peak.
            var peaks = Peaks((100d, 1000d))
                .Concat(Enumerable.Range(0, 150).Select(k => new SpectrumPeak(200d + k, 500d - k)))
                .Concat(Enumerable.Range(0, 100).Select(k => new SpectrumPeak(400d + k, 50d + k)))
                .ToList();
            var unprotected = MsmsEntropyCleanup.Clean(peaks, 3.0);
            Assert.AreEqual(MsmsEntropyCleanupStatus.Cleaned, unprotected.Status);
            Assert.IsTrue(unprotected.LargestRemoved > 300d, "the relative cut drops ions above 30% of the base peak");

            var protectedResult = MsmsEntropyCleanup.Clean(peaks, 3.0, protectedPercentOfBasePeak: 30d);

            Assert.IsTrue(protectedResult.Peaks.Where(p => p.Intensity >= 300d).Count() == peaks.Count(p => p.Intensity >= 300d));
            Assert.IsTrue(protectedResult.LargestRemoved < 300d);
            Assert.AreEqual(MsmsEntropyCleanupStatus.AboveCeiling, protectedResult.Status, "the protected ions keep the spectrum above the ceiling");
            Assert.IsTrue(protectedResult.EntropyAfter > 3.0);

            // A floor above the entropy cut changes nothing.
            var above = MsmsEntropyCleanup.Clean(peaks, 3.0, protectedPercentOfBasePeak: 60d);
            CollectionAssert.AreEqual(unprotected.Peaks, above.Peaks);
            Assert.AreEqual(MsmsEntropyCleanupStatus.Cleaned, above.Status);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => MsmsEntropyCleanup.Clean(peaks, 3.0, protectedPercentOfBasePeak: 101d));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => MsmsEntropyCleanup.Clean(peaks, 3.0, protectedPercentOfBasePeak: -1d));
        }

        [TestMethod]
        public void ApplyCleansResultsInPlaceAndCountsThem() {
            var noisy = new MSDecResult { Spectrum = Signal().Concat(Noise()).ToList(), Splash = "old" };
            var quiet = new MSDecResult { Spectrum = Signal() };
            var tooFew = new MSDecResult { Spectrum = Peaks((100d, 1000d), (200d, 900d)).Concat(Enumerable.Range(0, 2000).Select(k => new SpectrumPeak(300d + k, 5d))).ToList() };
            var empty = new MSDecResult();
            var quietSpectrum = quiet.Spectrum;

            var summary = MsmsEntropyCleanup.Apply(new[] { noisy, quiet, tooFew, empty }, 4.5);

            Assert.AreEqual(3, summary.Spectra, "a result without a spectrum is not counted");
            Assert.AreEqual(1, summary.Cleaned);
            Assert.AreEqual(1, summary.Unchanged);
            Assert.AreEqual(1, summary.KeptRawTooFewPeaks);
            Assert.AreEqual(303 - noisy.Spectrum.Count, summary.PeaksRemoved);
            Assert.IsTrue(MsmsEntropyCleanup.SpectralEntropy(noisy.Spectrum) <= 4.5);
            Assert.AreEqual(MSDecHandler.CalculateSplash(noisy.Spectrum), noisy.Splash, "the SPLASH follows the cleaned spectrum");
            Assert.AreSame(quietSpectrum, quiet.Spectrum);
            Assert.AreEqual(2002, tooFew.Spectrum.Count);
            Assert.IsNull(tooFew.Splash, "a result without a SPLASH does not get one");
        }

        [TestMethod]
        public void ProjectsRecordTheSettingOnlyWhenTheSpectraWereCleaned() {
            var parameter = new ParameterBase { MachineCategory = MachineCategory.LCMS };
            Assert.IsNull(MsmsEntropyCleanup.Describe(parameter), "off");

            parameter.ChromDecBaseParam.IsDoMsmsEntropyCleanup = true;
            Assert.AreEqual("max_entropy=4.5; logarithm=natural", MsmsEntropyCleanup.Describe(parameter));
            parameter.ChromDecBaseParam.MsmsMaxSpectralEntropy = 3.25;
            parameter.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak = 20d;
            Assert.AreEqual("max_entropy=3.25; logarithm=natural; protected_pct_of_base=20", MsmsEntropyCleanup.Describe(parameter));

            parameter.MachineCategory = MachineCategory.GCMS;
            Assert.IsNull(MsmsEntropyCleanup.Describe(parameter), "only LC-MS runs the cleanup");
        }

        [TestMethod]
        public void AnMspRecordSaysWhetherItsSpectrumWasCleaned() {
            var parameter = new ParameterBase { MachineCategory = MachineCategory.LCMS };
            Assert.IsFalse(MspRecord(parameter).Contains("MSMSENTROPYCLEANUP"), "off, the record is as before");

            parameter.ChromDecBaseParam.IsDoMsmsEntropyCleanup = true;
            StringAssert.Contains(MspRecord(parameter), "MSMSENTROPYCLEANUP: max_entropy=4.5; logarithm=natural\r\n");
        }

        private static string MspRecord(ParameterBase parameter) {
            var builder = new NistRecordBuilder();
            builder.SetNameProperty("x");
            builder.SetProjectParameterProperties(parameter.ProjectParam);
            builder.SetMsmsEntropyCleanup(parameter);
            builder.SetScan(new MSScanProperty { Spectrum = Signal() });
            using var stream = new MemoryStream();
            builder.Export(stream);
            return Encoding.ASCII.GetString(stream.ToArray());
        }

        [TestMethod]
        public void TheSettingIsOffByDefaultAndSurvivesASaveAndLoad() {
            var parameter = new ChromDecBaseParameter();
            Assert.IsFalse(parameter.IsDoMsmsEntropyCleanup);
            Assert.AreEqual(4.5, parameter.MsmsMaxSpectralEntropy);
            Assert.AreEqual(0d, parameter.MsmsEntropyCleanupProtectedPercentOfBasePeak);

            parameter.IsDoMsmsEntropyCleanup = true;
            parameter.MsmsMaxSpectralEntropy = 3.75;
            parameter.MsmsEntropyCleanupProtectedPercentOfBasePeak = 25d;
            var loaded = SaveAndLoadAsProjectsDo<ChromDecBaseParameter, ChromDecBaseParameter>(parameter);

            Assert.IsTrue(loaded.IsDoMsmsEntropyCleanup);
            Assert.AreEqual(3.75, loaded.MsmsMaxSpectralEntropy);
            Assert.AreEqual(25d, loaded.MsmsEntropyCleanupProtectedPercentOfBasePeak);
        }

        [TestMethod]
        public void AProjectSavedBeforeTheCleanupExistedLoadsWithItOff() {
            var loaded = SaveAndLoadAsProjectsDo<ChromDecBaseParameterWithoutCleanup, ChromDecBaseParameter>(new ChromDecBaseParameterWithoutCleanup { RelativeAmplitudeCutoff = 0.01F });

            Assert.IsFalse(loaded.IsDoMsmsEntropyCleanup);
            Assert.AreEqual(MsmsEntropyCleanup.DefaultMaxEntropy, loaded.MsmsMaxSpectralEntropy, "an absent ceiling reads as the default, not 0");
            Assert.AreEqual(0d, loaded.MsmsEntropyCleanupProtectedPercentOfBasePeak);
            Assert.AreEqual(0.01F, loaded.RelativeAmplitudeCutoff);
        }

        private static TLoad SaveAndLoadAsProjectsDo<TSave, TLoad>(TSave saved) {
            using var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(saved, stream);
            stream.Position = 0;
            return MessagePackDefaultHandler.LoadFromStream<TLoad>(stream);
        }

        /// <summary>The parameter as it was serialised before keys 11 to 13 (the cleanup) existed.</summary>
        [MessagePackObject]
        public sealed class ChromDecBaseParameterWithoutCleanup
        {
            [Key(0)] public bool IsDoMs2ChromDeconvolution { get; set; } = false;
            [Key(1)] public float SigmaWindowValue { get; set; } = 0.5F;
            [Key(2)] public float AmplitudeCutoff { get; set; } = 0;
            [Key(3)] public float AveragePeakWidth { get; set; } = 30;
            [Key(4)] public float KeptIsotopeRange { get; set; } = 5;
            [Key(5)] public bool RemoveAfterPrecursor { get; set; } = true;
            [Key(6)] public bool KeepOriginalPrecursorIsotopes { get; set; } = false;
            [Key(7)] public AccuracyType AccuracyType { get; set; } = AccuracyType.IsAccurate;
            [Key(8)] public double TargetCE { get; set; } = 0;
            [Key(9)] public float RelativeAmplitudeCutoff { get; set; } = 0;
            [Key(10)] public bool ExecuteChromDeconvolution { get; set; } = true;
        }
    }
}
