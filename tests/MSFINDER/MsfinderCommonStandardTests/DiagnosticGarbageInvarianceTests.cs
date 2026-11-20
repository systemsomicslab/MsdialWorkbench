#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using static Riken.Metabolomics.MsfinderCommon.Tests.DiagnosticQueryFiles;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The dynamic garbage test (design 6.1 item 5): every fixture spectrum, wrapped as a realistic query, gives the
/// same status, canonical result bytes and input_sha256 when everything that is not a declared adapter input is
/// garbage, in the file (MSP/MAT and MassBank), in memory, and in the query folder's .fgt/.sfd files. Queries are
/// written at test time from the linked per-rule fixtures (OQ03).
/// </summary>
[TestClass]
public class DiagnosticGarbageInvarianceTests
{
    private string _dir = "";

    [TestInitialize]
    public void Init() => _dir = NewTempDirectory();

    [TestCleanup]
    public void Cleanup() => DeleteDirectory(_dir);

    private DiagnosticQueryEvaluation Evaluate(string name, string text) => Runtime.Evaluate(WriteAndRead(_dir, name, text), false);

    private static void AssertSame(FixtureSpectrum f, DiagnosticQueryEvaluation clean, DiagnosticQueryEvaluation garbage, string what) {
        var a = Signature(clean);
        var b = Signature(garbage);
        Assert.AreEqual(a.Status, b.Status, f.Label + " " + what + ": status");
        Assert.AreEqual(a.InputSha256, b.InputSha256, f.Label + " " + what + ": input_sha256");
        Assert.AreEqual(a.Canonical, b.Canonical, f.Label + " " + what + ": canonical result");
    }

    [TestMethod]
    public void A_MspFileGarbageChangesNothing() {
        var evaluated = 0;
        for (var i = 0; i < Fixtures.Count; i++) {
            var f = Fixtures[i];
            var clean = Evaluate("q" + i + ".msp", Msp(f, null));
            var garbage = Evaluate("g" + i + ".mat", Msp(f, new Random(20261118 + i)));
            AssertSame(f, clean, garbage, "(a)");
            if (clean.Status == DiagnosticRuleResult.StatusEvaluated) {
                evaluated++;
            }
        }
        Assert.IsTrue(evaluated > Fixtures.Count / 2, evaluated + " of " + Fixtures.Count + " evaluated");
    }

    [TestMethod]
    public void A2_MassBankFieldGarbageChangesNothing() {
        for (var i = 0; i < Fixtures.Count; i++) {
            var f = Fixtures[i];
            var rng = new Random(20261118 + i);
            var cleanAnnotations = f.Peaks.Take(2).Select(p => (p.Mz, "C7H7O2+")).ToList();
            var mzs = new HashSet<double>(f.Peaks.Select(p => double.Parse(p.Mz, CultureInfo.InvariantCulture)));
            var garbageAnnotations = new List<(string, string)>();
            foreach (var p in f.Peaks.Take(2)) {
                garbageAnnotations.Add((p.Mz, GarbageWord(rng)));
            }
            for (var k = 0; k < 3; k++) {
                var mz = Math.Round(1.0 + rng.NextDouble() * 1500.0, 4);
                if (!mzs.Contains(mz)) {
                    garbageAnnotations.Add((mz.ToString("R", CultureInfo.InvariantCulture), GarbageWord(rng)));
                }
            }
            var clean = Evaluate("q" + i + ".txt", MassBank(f, null, cleanAnnotations));
            var garbage = Evaluate("g" + i + ".txt", MassBank(f, rng, garbageAnnotations));
            AssertSame(f, clean, garbage, "(a')");
        }
    }

    /// <summary>The declared adapter inputs, which are never garbaged (design 6.1 item 5).</summary>
    private static readonly string[] Declared =
    {
        nameof(RawData.PrecursorMz), nameof(RawData.IonMode), nameof(RawData.InstrumentType), nameof(RawData.SpectrumType),
        nameof(RawData.Ms2Spectrum), nameof(RawData.Ms2PeakNumber), nameof(RawData.RawdataFilePath),
    };

    [TestMethod]
    public void B_InMemoryGarbageOverTwoHundredSeedsChangesNothing() {
        var n = Fixtures.Count;
        var assigned = 0;
        for (var i = 0; i < 200; i++) {
            foreach (var index in new[] { i % n, (i * 7 + 3) % n }) {
                var f = Fixtures[index];
                var name = "b" + index + ".msp";
                var clean = Evaluate(name, Msp(f, null));
                var raw = RawDataParcer.RawDataFileReader(Path.Combine(_dir, name), new AnalysisParamOfMsfinder());
                var rng = new Random(20261118 + i);
                assigned += Garble(raw, rng, Declared);
                assigned += Garble(raw.Ms2Spectrum, rng, new[] { nameof(Spectrum.PeakList) });
                raw.Ms2Spectrum.PrecursorMz = raw.PrecursorMz + 1 + rng.Next(100); // conflicts with RawData's
                raw.Ms2Spectrum.IonType = raw.IonMode == IonMode.Positive ? IonMode.Negative : IonMode.Positive;
                foreach (var peak in raw.Ms2Spectrum.PeakList) {
                    assigned += Garble(peak, rng, new[] { nameof(Peak.Mz), nameof(Peak.Intensity) });
                }
                AssertSame(f, clean, Runtime.Evaluate(raw, false), "(b) seed " + i);
            }
        }
        Assert.IsTrue(assigned > 200 * 2 * 20, assigned + " garbage assignments");
    }

    /// <summary>Sets every writable property not in <paramref name="keep"/> to garbage of its type; returns how many
    /// were set.</summary>
    private static int Garble(object target, Random rng, string[] keep) {
        var count = 0;
        foreach (var p in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!p.CanWrite || p.GetIndexParameters().Length > 0 || keep.Contains(p.Name)) {
                continue;
            }
            var value = GarbageValue(p.PropertyType, rng);
            try {
                p.SetValue(target, value);
                count++;
            }
            catch (TargetInvocationException) {
            }
            catch (ArgumentException) {
            }
        }
        return count;
    }

    private static object? GarbageValue(Type t, Random rng) {
        if (t == typeof(string)) {
            switch (rng.Next(4)) {
                case 0: return null;
                case 1: return "";
                case 2: return RandomFormula(rng);
                default: return Garbage(rng);
            }
        }
        if (t == typeof(double) || t == typeof(float)) {
            var d = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1.0, 0.0, rng.NextDouble() * 1e6 }[rng.Next(6)];
            return t == typeof(float) ? (object)(float)d : d;
        }
        if (t == typeof(int)) {
            return rng.Next(int.MinValue, int.MaxValue);
        }
        if (t == typeof(bool)) {
            return rng.Next(2) == 0;
        }
        if (t.IsEnum) {
            var values = Enum.GetValues(t);
            return values.GetValue(rng.Next(values.Length));
        }
        if (t == typeof(List<Peak>)) {
            return Enumerable.Range(0, rng.Next(5)).Select(_ => new Peak { Mz = rng.NextDouble() * 1000, Intensity = rng.NextDouble() * 1000, Comment = RandomFormula(rng) }).ToList();
        }
        if (t == typeof(Spectrum)) {
            var s = new Spectrum((List<Peak>)GarbageValue(typeof(List<Peak>), rng)!, rng.NextDouble() * 1000, IonMode.Both);
            return s;
        }
        if (typeof(IList).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null) {
            return Activator.CreateInstance(t);
        }
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }

    [TestMethod]
    public void C_FgtAndSfdGarbageOrDeletionChangesNothing() {
        foreach (var index in new[] { 0, Fixtures.Count / 3, 2 * Fixtures.Count / 3 }) {
            var f = Fixtures[index];
            var text = Msp(f, null);
            var clean = Evaluate("c.msp", text);
            var folder = Path.Combine(_dir, "c");
            Directory.CreateDirectory(folder);
            var rng = new Random(index);
            File.WriteAllText(Path.Combine(folder, "C10H10O5.fgt"), "NAME: " + RandomFormula(rng) + "\r\nFORMULA: " + RandomFormula(rng) + "\r\nChemOntDescriptions: " + Garbage(rng) + "\r\n");
            File.WriteAllText(Path.Combine(folder, "C10H10O5.sfd"), "NAME: " + Garbage(rng) + "\r\nFormula: " + RandomFormula(rng) + "\r\nTotalScore: 9.9\r\n");
            AssertSame(f, clean, Evaluate("c.msp", text), "(c) garbage .fgt/.sfd");
            Directory.Delete(folder, recursive: true);
            AssertSame(f, clean, Evaluate("c.msp", text), "(c) deleted .fgt/.sfd");
        }
    }

    /// <summary>
    /// MS-FINDER's reader parses with the current culture, so both processes fix it at start-up: under de-DE the
    /// peaks differ (and the adapter's strict re-read refuses them).
    /// </summary>
    [TestMethod]
    public void ReaderDependsOnProcessCulture() {
        var f = Fixtures.First(x => x.Peaks.Any(p => p.Mz.Contains(".")));
        var path = Path.Combine(_dir, "culture.msp");
        File.WriteAllText(path, Msp(f, null));
        var invariant = RawDataParcer.RawDataFileReader(path, new AnalysisParamOfMsfinder());
        var saved = Thread.CurrentThread.CurrentCulture;
        RawData german;
        try {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            german = RawDataParcer.RawDataFileReader(path, new AnalysisParamOfMsfinder());
        }
        finally {
            Thread.CurrentThread.CurrentCulture = saved;
        }
        var a = invariant.Ms2Spectrum.PeakList.Select(p => (p.Mz, p.Intensity)).ToList();
        var b = german.Ms2Spectrum.PeakList.Select(p => (p.Mz, p.Intensity)).ToList();
        Assert.IsFalse(a.SequenceEqual(b), "the peaks read under de-DE equal the invariant ones");
        Assert.AreEqual("skipped:peaks_suspect:peak_block_disagrees", Runtime.Evaluate(german, false).Status);
    }

    [TestMethod]
    public void NonVacuity() {
        var firedRules = new HashSet<string>(StringComparer.Ordinal);
        var firing = 0;
        var firingOfMeasured = 0;
        FixtureSpectrum? shifted = null;
        DiagnosticQueryEvaluation? shiftedClean = null;
        for (var i = 0; i < Fixtures.Count; i++) {
            var f = Fixtures[i];
            var e = Evaluate("n" + i + ".msp", Msp(f, null));
            if (!AnyFired(e.Result)) {
                continue;
            }
            firing++;
            if (Runtime.RuleSet.Rules.Any(r => r.RuleId == f.RuleId && r.Measure && r.SourceChecked)) {
                firingOfMeasured++;
            }
            foreach (var o in e.Result!.Outcomes.Where(o => o.Fired)) {
                firedRules.Add(o.RuleId);
            }
            if (shifted is null) {
                shifted = f;
                shiftedClean = e;
            }
        }
        // MS-FINDER evaluates the default filter: measure and source_checked. A measure:true rule that is not
        // source_checked is listed as not measured, so it cannot fire here.
        var measured = Runtime.RuleSet.Rules.Where(r => r.Measure && r.SourceChecked).Select(r => r.RuleId).ToList();
        var silent = measured.Where(id => !firedRules.Contains(id)).ToList();
        Assert.AreEqual(0, silent.Count, "measure:true rules that fire on no fixture: " + string.Join(", ", silent));
        Assert.IsTrue(measured.Count > 0);
        // The fixtures of rules MS-FINDER does not evaluate cannot fire their own rule, so the 30 % is taken over the
        // fixtures of the evaluated rules.
        var measuredFixtures = Fixtures.Count(x => measured.Contains(x.RuleId));
        Assert.IsTrue(firingOfMeasured * 10 >= measuredFixtures * 3,
                      firingOfMeasured + " of " + measuredFixtures + " fixtures of evaluated rules fire (" + firing + " of " + Fixtures.Count + " in all)");

        // Moving one matched peak by 0.1 Da changes the bytes.
        var outcome = shiftedClean!.Result!.Outcomes.First(o => o.Fired && o.Chosen != null && o.Chosen.Matched.Count > 0);
        var peakIndex = outcome.Chosen!.Matched[0].PeakIndex;
        var lines = shifted!.Peaks.Select(p => p.Mz + "\t" + p.Intensity).ToList();
        var sorted = shifted.Peaks.Select((p, k) => (Mz: double.Parse(p.Mz, CultureInfo.InvariantCulture), k)).OrderBy(x => x.Mz).ToList();
        var line = sorted[peakIndex].k;
        lines[line] = (sorted[peakIndex].Mz + 0.1).ToString("R", CultureInfo.InvariantCulture) + "\t" + shifted.Peaks[line].Intensity;
        var moved = Evaluate("moved.msp", Msp(shifted, null, lines));
        Assert.AreNotEqual(Signature(shiftedClean).Canonical, Signature(moved).Canonical);
    }
}
