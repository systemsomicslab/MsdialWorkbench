#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Engine;
using CompMs.Common.StructureFinder.DiagnosticRules.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// The evaluator's inputs (design 4.10, 6.1 item 1): the spectrum has exactly four members and the peak two, adduct
/// evidence refuses the assumed tier, and the options are copied and checked once.
/// </summary>
[TestClass]
public class EvaluatorInputTests
{
    private const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    private static string Surface(Type t) {
        var members = t.GetMembers(Declared)
            .Select(m => m.MemberType + " " + (m is MethodBase mb
                ? m.Name + "(" + string.Join(",", mb.GetParameters().Select(p => p.ParameterType.Name)) + ")"
                : m is PropertyInfo p ? p.PropertyType.Name + " " + m.Name : m.Name))
            .OrderBy(x => x, StringComparer.Ordinal);
        return string.Join("\n", members);
    }

    [TestMethod]
    public void InputSurfaceIsExactlyFourMembers() {
        // Any widening of either input type (a field, a property, a method, a second constructor) fails here, inherited
        // ones included: the spectrum derives from object directly and implements no interface (an explicit interface
        // implementation is not public, so the declared-member listing below would miss it).
        Assert.IsTrue(typeof(DiagnosticSpectrum).IsSealed);
        Assert.AreEqual(typeof(object), typeof(DiagnosticSpectrum).BaseType);
        Assert.AreEqual(0, typeof(DiagnosticSpectrum).GetInterfaces().Length);
        Assert.AreEqual(9, typeof(DiagnosticSpectrum).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                                     .Count(m => m.DeclaringType != typeof(object)));
        Assert.AreEqual(string.Join("\n", new[]
        {
            "Constructor .ctor(IReadOnlyList`1,Double,Nullable`1,String)",
            "Method get_InstrumentType()",
            "Method get_IonMode()",
            "Method get_Peaks()",
            "Method get_PrecursorMz()",
            "Property IReadOnlyList`1 Peaks",
            "Property Nullable`1 IonMode",
            "Property String InstrumentType",
            "Property Double PrecursorMz",
        }.OrderBy(x => x, StringComparer.Ordinal)), Surface(typeof(DiagnosticSpectrum)));
        Assert.AreEqual(4, typeof(DiagnosticSpectrum).GetProperties(Declared).Length);
        Assert.AreEqual(0, typeof(DiagnosticSpectrum).GetFields(Declared).Length);
        Assert.AreEqual(typeof(IReadOnlyList<DiagnosticPeak>), typeof(DiagnosticSpectrum).GetProperty("Peaks")!.PropertyType);
        Assert.AreEqual(typeof(DiagnosticIonMode?), typeof(DiagnosticSpectrum).GetProperty("IonMode")!.PropertyType);

        Assert.IsTrue(typeof(DiagnosticPeak).IsValueType);
        Assert.IsTrue(typeof(DiagnosticPeak).GetCustomAttributes(false).Any(a => a.GetType().Name == "IsReadOnlyAttribute"), "a readonly struct");
        Assert.AreEqual(string.Join("\n", new[]
        {
            "Constructor .ctor(Double,Double)",
            "Method get_Intensity()",
            "Method get_Mz()",
            "Property Double Intensity",
            "Property Double Mz",
        }.OrderBy(x => x, StringComparer.Ordinal)), Surface(typeof(DiagnosticPeak)));
        CollectionAssert.AreEqual(new[] { "Positive", "Negative" }, System.Enum.GetNames(typeof(DiagnosticIonMode)));
    }

    [TestMethod]
    public void TheSpectrumCopiesItsPeaks() {
        var peaks = new List<DiagnosticPeak> { new DiagnosticPeak(100.0, 5.0) };
        var s = new DiagnosticSpectrum(peaks, 300.0, DiagnosticIonMode.Negative, null!);
        peaks.Add(new DiagnosticPeak(200.0, 1.0));
        peaks[0] = new DiagnosticPeak(1.0, 1.0);
        Assert.AreEqual(1, s.Peaks.Count);
        Assert.AreEqual(100.0, s.Peaks[0].Mz);
        Assert.AreEqual(0, new DiagnosticSpectrum(null!, 300.0, null, "x").Peaks.Count);
    }

    [TestMethod]
    public void AdductEvidenceRefusesTheAssumedTier() {
        CollectionAssert.AreEqual(new[] { "CrossPolarity", "WithinPolarityPair", "Curated", "Assumed" }, System.Enum.GetNames(typeof(AdductTier)));
        Assert.ThrowsException<ArgumentException>(() => new AdductEvidence("[M-H]-", AdductTier.Assumed));
        Assert.ThrowsException<ArgumentException>(() => new AdductEvidence("[M-H]-", (AdductTier)17));
        Assert.ThrowsException<ArgumentNullException>(() => new AdductEvidence(null!, AdductTier.Curated));
        var e = new AdductEvidence("[M-H]-", AdductTier.WithinPolarityPair);
        Assert.AreEqual("[M-H]-", e.CanonicalAdduct);
        Assert.AreEqual(AdductTier.WithinPolarityPair, e.Tier);
        // It carries no formula: only the name and the tier.
        CollectionAssert.AreEquivalent(new[] { "CanonicalAdduct", "Tier" }, typeof(AdductEvidence).GetProperties(Declared).Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void OptionDefaultsAreTheDesigns() {
        var o = new DiagnosticEvaluationOptions();
        Assert.AreEqual(RuleFilter.MeasuredAndSourceChecked, o.Filter);
        Assert.IsNull(o.PrecursorToleranceDa);
        Assert.IsNull(o.ProductToleranceOverride);
        Assert.AreEqual(0.01, o.MsfinderProductTolerance.AbsDa);
        Assert.AreEqual(0.0, o.MsfinderProductTolerance.Ppm);
        Assert.IsNull(o.LossReferenceOverride);
        Assert.AreEqual(0, o.DecoyShiftDa);
        Assert.AreEqual(1.0, o.FloorScale);
        Assert.AreEqual(0, o.KOffset);
        Assert.AreEqual(OverlapMode.Refuse, o.Overlap);
        Assert.AreEqual(4096, o.AssignmentCap);
        Assert.IsFalse(o.Strict);
        Assert.IsFalse(o.Verbose);
        CollectionAssert.AreEquivalent(
            new[] { "Filter", "PrecursorToleranceDa", "ProductToleranceOverride", "MsfinderProductTolerance", "LossReferenceOverride",
                    "DecoyShiftDa", "FloorScale", "KOffset", "Overlap", "AssignmentCap", "Strict", "Verbose" },
            typeof(DiagnosticEvaluationOptions).GetProperties(Declared).Select(p => p.Name).ToArray());
        Assert.IsFalse(EvaluationSettings.From(o).VariantActive);
    }

    [TestMethod]
    public void TheSettingsAreASnapshot() {
        var o = new DiagnosticEvaluationOptions { DecoyShiftDa = 5, MsfinderProductTolerance = ProductTolerance.FromPpm(20.0) };
        var s = EvaluationSettings.From(o);
        o.DecoyShiftDa = 9;
        o.MsfinderProductTolerance = ProductTolerance.Da(0.05);
        o.Overlap = OverlapMode.Runtime;
        Assert.AreEqual(5, s.DecoyShiftDa);
        Assert.AreEqual(5000000000000L, s.DecoyShiftPda);
        Assert.AreEqual(20.0, s.MsfinderProductTolerance.Ppm);
        Assert.AreEqual(0.0, s.MsfinderProductTolerance.AbsDa);
        Assert.AreEqual(OverlapMode.Refuse, s.Overlap);
    }

    [TestMethod]
    public void EachVariantOptionIsAVariantAndNoOtherIs() {
        var variants = new List<Action<DiagnosticEvaluationOptions>>
        {
            o => o.PrecursorToleranceDa = 0.01,
            o => o.ProductToleranceOverride = ProductTolerance.Da(0.005),
            o => o.LossReferenceOverride = LossReference.Hypothesis,
            o => o.DecoyShiftDa = -7,
            o => o.FloorScale = 0.5,
            o => o.KOffset = -1,
            o => o.Overlap = OverlapMode.Runtime,
            o => o.AssignmentCap = 4095,
        };
        foreach (var set in variants) {
            var o = new DiagnosticEvaluationOptions();
            set(o);
            Assert.IsTrue(EvaluationSettings.From(o).VariantActive);
        }
        var plain = new DiagnosticEvaluationOptions
        {
            Filter = RuleFilter.All, MsfinderProductTolerance = ProductTolerance.Da(0.05), Strict = true, Verbose = true,
        };
        Assert.IsFalse(EvaluationSettings.From(plain).VariantActive);
    }

    [TestMethod]
    public void InvalidOptionsAreRefused() {
        var invalid = new List<Action<DiagnosticEvaluationOptions>>
        {
            o => o.PrecursorToleranceDa = double.NaN,
            o => o.PrecursorToleranceDa = -0.01,
            o => o.PrecursorToleranceDa = double.PositiveInfinity,
            o => o.ProductToleranceOverride = ProductTolerance.Da(-0.005),
            o => o.ProductToleranceOverride = ProductTolerance.FromPpm(double.NaN),
            o => o.MsfinderProductTolerance = ProductTolerance.Da(double.NegativeInfinity),
            o => o.FloorScale = 0.0,
            o => o.FloorScale = -1.0,
            o => o.FloorScale = double.NaN,
            o => o.FloorScale = double.PositiveInfinity,
            o => o.AssignmentCap = 0,
            o => o.DecoyShiftDa = 9008,
            o => o.DecoyShiftDa = int.MinValue,
            o => o.DecoyShiftDa = int.MaxValue,
            o => o.DecoyShiftDa = -9008,
            // These wrap in unchecked long arithmetic to a few milli-Da, below 2^53 pDa.
            o => o.DecoyShiftDa = 1752440687,
            o => o.DecoyShiftDa = 498062090,
            o => o.DecoyShiftDa = -1752440687,
            o => o.Filter = (RuleFilter)9,
            o => o.Overlap = (OverlapMode)9,
            o => o.LossReferenceOverride = (LossReference)9,
        };
        foreach (var set in invalid) {
            var o = new DiagnosticEvaluationOptions();
            set(o);
            Assert.ThrowsException<ArgumentException>(() => EvaluationSettings.From(o));
        }
        Assert.ThrowsException<ArgumentNullException>(() => EvaluationSettings.From(null!));
        // The largest decoys, whose shift delta * 10^12 stays below 2^53 pDa, and a zero tolerance, are accepted.
        Assert.AreEqual(9007L, EvaluationSettings.MaxDecoyShiftDa);
        Assert.AreEqual(-9007, EvaluationSettings.From(new DiagnosticEvaluationOptions { DecoyShiftDa = -9007 }).DecoyShiftDa);
        Assert.AreEqual(-9007000000000000L, EvaluationSettings.From(new DiagnosticEvaluationOptions { DecoyShiftDa = -9007 }).DecoyShiftPda);
        Assert.AreEqual(9007000000000000L, EvaluationSettings.From(new DiagnosticEvaluationOptions { DecoyShiftDa = 9007 }).DecoyShiftPda);
        Assert.AreEqual(0.0, EvaluationSettings.From(new DiagnosticEvaluationOptions { PrecursorToleranceDa = 0.0 }).PrecursorToleranceDa);
    }
}
