#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The adapter (design 5.4, 6.1 item 5): annotation fixtures, the MassBank phantom peaks, the peak guards, the
/// non-positive drop, the instrument-type source and the r6 rounding.
/// </summary>
[TestClass]
public class DiagnosticAdapterTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    /// <summary>A fixture spectrum on which some rule fires under the default runtime.</summary>
    private static FixtureSpectrum FiringFixture() => Fixtures.First(f => {
        var spectrum = new DiagnosticSpectrum(f.Peaks.Select(p => new DiagnosticPeak(double.Parse(p.Mz, CultureInfo.InvariantCulture), double.Parse(p.Intensity, CultureInfo.InvariantCulture))).ToList(),
                                              double.Parse(f.PrecursorMz, CultureInfo.InvariantCulture), f.Positive ? DiagnosticIonMode.Positive : DiagnosticIonMode.Negative, f.InstrumentType ?? "");
        return AnyFired(Runtime.Evaluator.Evaluate(spectrum));
    });

    private DiagnosticQueryEvaluation Evaluate(string name, string text) => Runtime.Evaluate(WriteAndRead(_dir, name, text), false);

    private static List<string> PlainLines(FixtureSpectrum f) => f.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();

    [TestMethod]
    public void QuotedAnnotationGivesTheUnannotatedBytes() {
        var f = FiringFixture();
        var plain = Evaluate("plain.msp", Msp(f, null, PlainLines(f)));
        var lines = PlainLines(f);
        lines[0] = f.Peaks[0].Mz + " " + f.Peaks[0].Intensity + " \"C7H7O2\"";
        var quoted = Evaluate("quoted.msp", Msp(f, null, lines));
        Assert.AreEqual(DiagnosticRuleResult.StatusEvaluated, plain.Status);
        Assert.AreEqual(Signature(plain), Signature(quoted));
        Assert.IsTrue(AnyFired(plain.Result));
    }

    [TestMethod]
    public void UnquotedAnnotationIsRefusedAndTheReaderReallyProducedSevenSeven() {
        var f = FiringFixture();
        var lines = PlainLines(f);
        lines.Insert(0, "123.0441 1000 C7H7O2");
        var raw = WriteAndRead(_dir, "unquoted.msp", Msp(f, null, lines));
        Assert.AreEqual(lines.Count, raw.Ms2Spectrum.PeakList.Count, "the count stays right");
        Assert.IsTrue(raw.Ms2Spectrum.PeakList.Any(p => p.Mz == 7.0 && p.Intensity == 7.0), "the MS-FINDER reader turns the annotation into (7, 7)");
        Assert.IsFalse(raw.Ms2Spectrum.PeakList.Any(p => p.Mz == 123.0441), "the real peak is replaced");
        var e = Runtime.Evaluate(raw, false);
        Assert.AreEqual("skipped:peaks_suspect:peak_block_disagrees", e.Status);
        Assert.IsNull(e.Result);
    }

    [TestMethod]
    public void MassBankPhantomAnnotationPeaksAreDropped() {
        var f = FiringFixture();
        var plain = Evaluate("plain.txt", MassBank(f, null, null));
        var annotations = new List<(string, string)> { (f.Peaks[0].Mz, "C7H7O2+"), ("77.0386", "C6H5+"), ("1234.5678", "C70H10O2+") };
        var raw = WriteAndRead(_dir, "annotated.txt", MassBank(f, null, annotations));
        Assert.AreEqual(f.Peaks.Count + 2, raw.Ms2Spectrum.PeakList.Count, "PK$ANNOTATION adds zero-intensity peaks");
        var annotated = Runtime.Evaluate(raw, false);
        Assert.AreEqual(DiagnosticRuleResult.StatusEvaluated, plain.Status);
        Assert.AreEqual(Signature(plain), Signature(annotated));
        Assert.AreEqual(2, annotated.Info!.NonPositiveDropped);
        Assert.AreEqual(0, plain.Info!.NonPositiveDropped);
    }

    [TestMethod]
    public void PeakCountMismatchIsRefused() {
        var f = FiringFixture();
        var e = Evaluate("count.txt", MassBank(f, null, null, numPeak: f.Peaks.Count + 1));
        Assert.AreEqual("skipped:peaks_suspect:peak_count_mismatch", e.Status);

        var raw = WriteAndRead(_dir, "count.msp", Msp(f, null, PlainLines(f)));
        raw.Ms2PeakNumber = raw.Ms2PeakNumber - 1;
        Assert.AreEqual("skipped:peaks_suspect:peak_count_mismatch", Runtime.Evaluate(raw, false).Status);
    }

    [TestMethod]
    public void SubOneDaltonPeakIsRefused() {
        var f = FiringFixture();
        var lines = PlainLines(f);
        lines.Insert(0, "0.5\t100");
        Assert.AreEqual("skipped:peaks_suspect:sub_1da_peak", Evaluate("sub1.msp", Msp(f, null, lines)).Status);
    }

    [TestMethod]
    public void NonPositivePeaksAreDroppedBeforeIndexing() {
        var f = FiringFixture();
        var plain = Evaluate("plain.msp", Msp(f, null, PlainLines(f)));
        var lines = PlainLines(f);
        lines.Insert(0, "50.5\t0");
        lines.Add("2000.25\t0");
        var withZeros = Evaluate("zeros.msp", Msp(f, null, lines));
        Assert.AreEqual(Signature(plain), Signature(withZeros));
        Assert.AreEqual(2, withZeros.Info!.NonPositiveDropped);
    }

    [TestMethod]
    public void MarkerOutsideItsPlainFormIsADisagreement() {
        var f = FiringFixture();
        var text = Msp(f, null, PlainLines(f)).Replace("MSTYPE: MS1", "MSTYPE:MS1");
        Assert.AreEqual("skipped:peaks_suspect:peak_block_disagrees", Evaluate("marker.msp", text).Status);
    }

    [TestMethod]
    public void GcEiDerivativeQueriesAreSkipped() {
        var f = FiringFixture();
        var e = Runtime.Evaluate(WriteAndRead(_dir, "ei.msp", Msp(f, null)), true);
        Assert.AreEqual("skipped:ei_derivative", e.Status);
        Assert.IsNull(e.Result);
        Assert.IsNull(e.Info);
    }

    [TestMethod]
    public void InstrumentTypeSourceAndPeaksSource() {
        var raw = InMemory(DataType.Centroid, "QTOF");
        MsfinderDiagnosticAdapter.ToSpectrum(raw, "Orbitrap", out var info);
        Assert.AreEqual("query", info.InstrumentTypeSource);
        Assert.AreEqual("as_read", info.PeaksSource);

        var spectrum = MsfinderDiagnosticAdapter.ToSpectrum(InMemory(DataType.Centroid, " "), "Orbitrap", out info);
        Assert.AreEqual("option", info.InstrumentTypeSource);
        Assert.AreEqual("Orbitrap", spectrum.InstrumentType);

        spectrum = MsfinderDiagnosticAdapter.ToSpectrum(InMemory(DataType.Centroid, null), "", out info);
        Assert.AreEqual("none", info.InstrumentTypeSource);
        Assert.IsNull(spectrum.InstrumentType);

        MsfinderDiagnosticAdapter.ToSpectrum(InMemory(DataType.Profile, null), "", out info);
        Assert.AreEqual("msfinder_centroid", info.PeaksSource);
    }

    [TestMethod]
    public void IonModeBothHasNoIonMode() {
        Assert.AreEqual(DiagnosticIonMode.Positive, MsfinderDiagnosticAdapter.IonModeOf(IonMode.Positive));
        Assert.AreEqual(DiagnosticIonMode.Negative, MsfinderDiagnosticAdapter.IonModeOf(IonMode.Negative));
        Assert.IsNull(MsfinderDiagnosticAdapter.IonModeOf(IonMode.Both));
        var raw = InMemory(DataType.Centroid, "QTOF");
        raw.IonMode = IonMode.Both;
        Assert.AreEqual(DiagnosticRuleResult.StatusUnsupportedIonMode, Runtime.Evaluate(raw, false).Status);
    }

    private static RawData InMemory(DataType type, string? instrumentType) {
        var raw = new RawData { PrecursorMz = 341.087806, IonMode = IonMode.Negative, SpectrumType = type, InstrumentType = instrumentType };
        raw.Ms2Spectrum.PeakList.Add(new Peak { Mz = 161.024418, Intensity = 500 });
        raw.Ms2Spectrum.PeakList.Add(new Peak { Mz = 179.035, Intensity = 300 });
        raw.Ms2PeakNumber = 2;
        return raw;
    }

    /// <summary>
    /// r6 (design 5.4): Peak's Math.Round(x, 6) equals the Python port's form
    /// <c>x if abs(x) &gt;= 1e16 else float(round(x * 1e6)) / 1e6</c> (round half to even), and is idempotent, on
    /// 10^6 seeded values.
    /// </summary>
    [TestMethod]
    public void R6MatchesThePythonFormOnAMillionSeededValues() {
        var rng = new Random(20261118);
        static double Python(double x) => Math.Abs(x) >= 1e16 ? x : Math.Round(x * 1e6, MidpointRounding.ToEven) / 1e6;
        var failures = new List<string>();
        for (var i = 0; i < 1000000; i++) {
            double x;
            switch (i % 4) {
                case 0: x = rng.NextDouble() * 2000.0; break;
                case 1: x = Math.Round(rng.NextDouble() * 2000.0, 7) + (rng.Next(2) == 0 ? 5e-7 : -5e-7); break; // near half
                case 2: x = (rng.Next(0, 2000000000) + 0.5) / 1e6; break; // exact decimal halves
                default: x = Math.Pow(10, rng.NextDouble() * 20 - 4) * (rng.Next(2) == 0 ? 1 : -1); break; // wide range, both signs
            }
            var peak = new Peak { Mz = x };
            var py = Python(x);
            if (!(peak.Mz.Equals(py) && Math.Round(x, 6).Equals(py) && Math.Round(py, 6).Equals(py))) {
                failures.Add(x.ToString("R", CultureInfo.InvariantCulture));
                if (failures.Count > 10) {
                    break;
                }
            }
        }
        Assert.AreEqual(0, failures.Count, string.Join(", ", failures));
    }
}
