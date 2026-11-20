#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The adapter member allowlist (design 6.1 item 3): the engine's IL scanner, run over the adapter, its strict
/// peak-block reader and its info type with another allowlist. The only MS-FINDER members they may use are the
/// declared adapter inputs; the BCL is judged as in the engine (culture, process state, reflection, clock), and only
/// <c>File.ReadLines</c> of System.IO is allowed.
/// </summary>
[TestClass]
public class AdapterAllowlistTests
{
    /// <summary>The declared adapter inputs (design 5.4) and the MS-FINDER method that centroids.</summary>
    private static readonly string[] AllowedMsfinderMembers =
    {
        "RawData::get_PrecursorMz", "RawData::get_IonMode", "RawData::get_InstrumentType", "RawData::get_SpectrumType",
        "RawData::get_Ms2Spectrum", "RawData::get_Ms2PeakNumber", "RawData::get_RawdataFilePath",
        "Spectrum::get_PeakList", "FragmentAssigner::GetCentroidMsMsSpectrum", "Peak::get_Mz", "Peak::get_Intensity",
    };

    private static readonly Type[] AllowedMsfinderTypes =
    {
        typeof(RawData), typeof(Spectrum), typeof(Peak), typeof(IonMode), typeof(DataType), typeof(FragmentAssigner),
    };

    private static readonly Type[] AllowedEngineTypes = { typeof(DiagnosticSpectrum), typeof(DiagnosticPeak), typeof(DiagnosticIonMode) };

    private static readonly Type[] Scanned = { typeof(MsfinderDiagnosticAdapter), typeof(PeakBlockCheck), typeof(DiagnosticInputInfo) };

    private static string? TypeReason(Type t, IlReferenceScanner.Site site) {
        var ns = t.Namespace ?? "";
        if (t.Assembly == site.Scanned.Assembly && ns == site.Scanned.Namespace) {
            var outer = t;
            while (outer.DeclaringType != null) {
                outer = outer.DeclaringType;
            }
            return Scanned.Contains(outer) || IlReferenceScanner.IsCompilerGenerated(t) ? null : "another MS-FINDER diagnostic type: " + t.Name;
        }
        if ((t.FullName ?? "").StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)) {
            return null;
        }
        if (ns.StartsWith("Rfx.", StringComparison.Ordinal) || ns.StartsWith("Riken.", StringComparison.Ordinal)) {
            return AllowedMsfinderTypes.Contains(t) ? null : "MS-FINDER type outside the declared inputs: " + t.FullName;
        }
        if (ns.StartsWith(IlReferenceScanner.EngineNamespace, StringComparison.Ordinal)) {
            return AllowedEngineTypes.Contains(t) ? null : "engine type other than the evaluator input: " + t.FullName;
        }
        if (ns == "System.IO") {
            return t == typeof(File) ? null : "System.IO." + t.Name + ": only File.ReadLines";
        }
        if (ns == "System.Text.RegularExpressions" || ns == "System.Globalization" || ns == "System.Collections.Generic" || ns == "System.Linq") {
            return null;
        }
        if (ns == "System" && new[] { "Environment", "AppDomain", "GC", "Activator", "Console", "Random", "Convert", "TimeZoneInfo" }.Contains(t.Name)) {
            return site.CompilerGenerated && t.Name == "Environment" ? null : "System." + t.Name;
        }
        if (ns == "System" || ns == "System.Runtime.CompilerServices" && (t.Name.EndsWith("Attribute", StringComparison.Ordinal) || t.Name == "RuntimeHelpers")) {
            return null;
        }
        return "not on the adapter allowlist: " + (t.FullName ?? t.Name);
    }

    private readonly HashSet<string> _msfinderMembers = new HashSet<string>(StringComparer.Ordinal);

    private string? MemberReason(MethodBase m, IlReferenceScanner.Site site) {
        var t = m.DeclaringType;
        if (t is null) {
            return null;
        }
        var ns = t.Namespace ?? "";
        if (ns.StartsWith("Rfx.", StringComparison.Ordinal) || ns.StartsWith("Riken.", StringComparison.Ordinal)) {
            if (t.Assembly == site.Scanned.Assembly && ns == site.Scanned.Namespace) {
                return null; // judged by type
            }
            var key = t.Name + "::" + m.Name;
            _msfinderMembers.Add(key);
            return AllowedMsfinderMembers.Contains(key) ? null : "MS-FINDER member outside the declared inputs: " + key;
        }
        if (ns.StartsWith(IlReferenceScanner.EngineNamespace, StringComparison.Ordinal)) {
            return m.IsConstructor || m.Name.StartsWith("get_", StringComparison.Ordinal) ? null : "engine member " + t.Name + "::" + m.Name;
        }
        if (t == typeof(File)) {
            return m.Name == "ReadLines" ? null : "File." + m.Name + ": only File.ReadLines";
        }
        if (t == typeof(CultureInfo)) {
            return m.Name == "get_InvariantCulture" ? null : "CultureInfo." + m.Name + ": only the invariant culture";
        }
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) && t.GetGenericArguments()[0] == typeof(Peak)) {
            return m.Name == "GetEnumerator" || m.Name == "get_Count" ? null : "List<Peak>." + m.Name + ": only enumeration";
        }
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>.Enumerator)) {
            return null;
        }
        return EngineRules.MemberReason(m, site);
    }

    private IlReferenceScanner Scan(params Type[] roots) {
        var scanner = new IlReferenceScanner(typeof(DiagnosticSpectrum).Assembly, TypeReason, MemberReason);
        foreach (var root in roots) {
            scanner.Scan(root, inLoading: false);
            foreach (var nested in root.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
                scanner.Scan(nested, inLoading: false);
            }
        }
        return scanner;
    }

    [TestMethod]
    public void AdapterReferencesOnlyTheDeclaredInputs() {
        var scanner = Scan(Scanned);
        Assert.AreEqual(0, scanner.Findings.Count, string.Join("\n", scanner.Findings));
        Assert.IsTrue(scanner.MemberReferences > 50, "the scan decoded " + scanner.MemberReferences + " member references");
        CollectionAssert.AreEquivalent(AllowedMsfinderMembers, _msfinderMembers.ToArray(),
            "every declared input is used, and nothing else: " + string.Join(", ", _msfinderMembers.OrderBy(x => x, StringComparer.Ordinal)));
    }

    [TestMethod]
    public void PlantedFormulaReadIsFlagged() {
        var scanner = Scan(typeof(PlantedAdapter));
        var refs = scanner.Findings.Select(f => f.Reference).ToList();
        Assert.IsTrue(refs.Any(r => r.Contains("get_Formula")), string.Join("\n", refs));
        Assert.IsTrue(refs.Any(r => r.Contains("get_Comment")), string.Join("\n", refs));
        Assert.IsTrue(refs.Any(r => r.Contains("ReadAllText")), string.Join("\n", refs));
        Assert.IsTrue(refs.Any(r => r.Contains("Double::Parse")), string.Join("\n", refs));
    }

    /// <summary>A planted adapter that reads what the real one must not.</summary>
    private static class PlantedAdapter
    {
        public static string Read(RawData raw) => raw.Formula + raw.Ms2Spectrum.PeakList[0].Comment + File.ReadAllText(raw.RawdataFilePath)
                                                  + double.Parse("1.5").ToString(CultureInfo.InvariantCulture);
    }
}
