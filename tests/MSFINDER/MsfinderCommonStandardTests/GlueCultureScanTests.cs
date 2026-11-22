#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules;
using CompMs.Common.StructureFinder.DiagnosticRules.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.Tests;

/// <summary>
/// The glue-writer IL scan (design 6.2): the MS-FINDER writers of the new outputs and INI keys never format a double
/// with Double.ToString (any overload) or pass a boxed double to string.Concat/Format. Because the IL does not say
/// what an object parameter holds, every string.Concat/Format/Join with an object parameter is banned, and so is
/// writing a floating-point or object value through TextWriter, StringBuilder or Convert.ToString. Their doubles go
/// through ExactDecimal, so the bytes do not depend on the target framework.
/// </summary>
[TestClass]
public class GlueCultureScanTests
{
    /// <summary>The glue writers and the types declared beside them (the legacy-format structure columns excepted).</summary>
    private static readonly Type[] Glue =
    {
        typeof(DiagnosticRuleExport), typeof(DiagnosticRuleIni), typeof(DiagnosticTimingLog), typeof(DiagnosticTimingRow),
        typeof(DiagnosticRuleRun), typeof(DiagnosticRunCounts), typeof(DiagnosticQueryContext), typeof(DiagnosticOutputNames),
        typeof(DiagnosticRuleQuery), typeof(DiagnosticRunEnvironment), typeof(DiagnosticEvidenceView), typeof(DiagnosticEvidenceOutcomeRow),
        typeof(DiagnosticEvidenceTermRow), typeof(DiagnosticRuleRuntimeCache),
        typeof(DiagrulesHarness), typeof(DiagrulesRun), typeof(DiagrulesOutputNames), typeof(DiagrulesReaderCounts),
        typeof(DiagrulesMspReader), typeof(DiagrulesRecord), typeof(CollisionEnergyBin),
    };

    private static bool IsFloating(Type t) => t == typeof(double) || t == typeof(float) || t == typeof(decimal)
                                              || Nullable.GetUnderlyingType(t) is Type u && IsFloating(u);

    private static bool IsObject(Type t) => t == typeof(object) || t == typeof(object[]) || t.IsGenericParameter;

    private static string? MemberReason(MethodBase m, IlReferenceScanner.Site site) {
        var t = m.DeclaringType;
        if (t is null) {
            return null;
        }
        var name = m.Name;
        var ps = m.GetParameters().Select(p => p.ParameterType).ToArray();
        if (IsFloating(t) && name == "ToString") {
            return t.Name + ".ToString";
        }
        if (t == typeof(object) && name == "ToString" && site.Constrained != null && IsFloating(site.Constrained)) {
            return site.Constrained.Name + ".ToString through a constrained call";
        }
        if (t == typeof(string) && (name == "Concat" || name == "Format" || name == "Join") && (m.IsGenericMethod || ps.Any(IsObject))) {
            return "string." + name + " with an object parameter (a boxed double formats with the culture and the framework)";
        }
        if (typeof(TextWriter).IsAssignableFrom(t) && (name == "Write" || name == "WriteLine") && ps.Any(p => IsFloating(p) || IsObject(p))) {
            return "TextWriter." + name + "(" + string.Join(", ", ps.Select(p => p.Name)) + ")";
        }
        if (t == typeof(StringBuilder) && (name == "Append" || name == "Insert" || name == "AppendFormat") && ps.Any(p => IsFloating(p) || IsObject(p))) {
            return "StringBuilder." + name + "(" + string.Join(", ", ps.Select(p => p.Name)) + ")";
        }
        if (t == typeof(Convert) && name == "ToString" && ps.Length > 0 && (IsFloating(ps[0]) || IsObject(ps[0]))) {
            return "Convert.ToString(" + ps[0].Name + ")";
        }
        return null;
    }

    private static IlReferenceScanner Scan(IEnumerable<Type> roots) {
        var scanner = new IlReferenceScanner(typeof(DiagnosticSpectrum).Assembly, (t, s) => null, MemberReason);
        foreach (var root in roots) {
            scanner.Scan(root, inLoading: false);
            foreach (var nested in root.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
                scanner.Scan(nested, inLoading: false);
            }
        }
        return scanner;
    }

    [TestMethod]
    public void GlueWritersFormatNoDoubleThemselves() {
        var scanner = Scan(Glue);
        Assert.AreEqual(0, scanner.Findings.Count, string.Join("\n", scanner.Findings));
        Assert.IsTrue(scanner.MemberReferences > 300, "the scan decoded " + scanner.MemberReferences + " member references");
    }

    [TestMethod]
    public void EveryPublicTypeOfTheExportFileIsScanned() {
        var declared = typeof(DiagnosticRuleExport).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(DiagnosticRuleExport).Namespace && !t.IsNested && t.IsPublic)
            .Where(t => Glue.Contains(t) || t.Name.StartsWith("DiagnosticRun", StringComparison.Ordinal) || t.Name.StartsWith("DiagnosticQueryC", StringComparison.Ordinal)
                        || t.Name.StartsWith("DiagnosticOutput", StringComparison.Ordinal) || t.Name.StartsWith("DiagnosticTiming", StringComparison.Ordinal))
            .ToList();
        CollectionAssert.IsSubsetOf(declared, Glue);
    }

    [TestMethod]
    public void PlantedFormattingIsFlagged() {
        var refs = Scan(new[] { typeof(Planted) }).Findings.Select(f => f.Reference + " " + f.Reason).ToList();
        var all = string.Join("\n", refs);
        foreach (var expected in new[] { "Double.ToString", "Double::ToString(IFormatProvider)", "string.Concat", "string.Format", "TextWriter.Write", "StringBuilder.Append", "Convert.ToString" }) {
            Assert.IsTrue(refs.Any(r => r.Contains(expected)), expected + " not flagged:\n" + all);
        }
    }

    /// <summary>A planted writer with every banned form.</summary>
    private static class Planted
    {
        public static string Write(double x, TextWriter w, StringBuilder sb) {
            w.Write(x);
            sb.Append(x);
            return x.ToString(CultureInfo.InvariantCulture) + "a" + x + string.Format(CultureInfo.InvariantCulture, "{0}", x) + string.Format("{0}", x)
                   + Convert.ToString(x, CultureInfo.InvariantCulture) + x.ToString() + string.Concat((object)x, "b");
        }
    }
}
