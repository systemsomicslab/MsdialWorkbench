#nullable enable
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests
{
    /// <summary>
    /// The engine namespace references nothing outside its allowlist (design 6.1 item 2, 6.2): no formula, data object,
    /// parser or MS-FINDER type, no process state or reflection, no clock, randomness or threads, and no
    /// culture-sensitive formatting, parsing, comparison or sort.
    /// </summary>
    [TestClass]
    public class FormulaFreeGuaranteeTests
    {
        public static IEnumerable<Type> EngineTypes() =>
            typeof(DiagnosticRuleSet).Assembly.GetTypes()
                .Where(t => t.Namespace != null
                            && (t.Namespace == IlReferenceScanner.EngineNamespace || t.Namespace.StartsWith(IlReferenceScanner.EngineNamespace + ".", StringComparison.Ordinal)))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

        [TestMethod]
        public void EngineReferencesOnlyAllowlist() {
            var scanner = new IlReferenceScanner(typeof(DiagnosticRuleSet).Assembly);
            var types = EngineTypes().ToList();
            foreach (var t in types) {
                scanner.Scan(t, inLoading: t.Namespace!.EndsWith(".Loading", StringComparison.Ordinal));
            }
            Assert.IsTrue(types.Count > 30, "only " + types.Count + " engine types were found");
            Assert.IsTrue(types.Any(IlReferenceScanner.IsCompilerGenerated), "compiler-generated types are scanned too");
            Assert.IsTrue(scanner.MemberReferences > 1000, "only " + scanner.MemberReferences + " member references were decoded");
            Assert.AreEqual(0, scanner.Findings.Count, string.Join("\n", scanner.Findings.Select(f => f.ToString())));
        }

        [TestMethod]
        public void OnlyLoadingTouchesFilesAndResources() {
            // The same scan without the .Loading allowances must flag the loader's file and resource access.
            var scanner = new IlReferenceScanner(typeof(DiagnosticRuleSet).Assembly);
            foreach (var t in EngineTypes()) {
                scanner.Scan(t, inLoading: false);
            }
            var flagged = scanner.Findings.Select(f => f.Reference).ToList();
            Assert.IsTrue(flagged.Any(r => r.StartsWith("System.IO.File", StringComparison.Ordinal)), string.Join("\n", flagged));
            Assert.IsTrue(flagged.Any(r => r.Contains("GetManifestResourceStream")), string.Join("\n", flagged));
            Assert.IsTrue(scanner.Findings.All(f => f.Scanned.Namespace!.EndsWith(".Loading", StringComparison.Ordinal)),
                          string.Join("\n", scanner.Findings.Where(f => !f.Scanned.Namespace!.EndsWith(".Loading", StringComparison.Ordinal))));
        }
    }

    /// <summary>The scanner flags what it must (planted defects) and nothing in clean code.</summary>
    [TestClass]
    public class IlReferenceScannerSelfTests
    {
        private static IReadOnlyList<IlReferenceScanner.Finding> ScanPlanted(Type t) {
            var scanner = new IlReferenceScanner(typeof(DiagnosticRuleSet).Assembly);
            scanner.Scan(t, inLoading: false);
            foreach (var nested in t.GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)) {
                scanner.Scan(nested, inLoading: false);
            }
            return scanner.Findings;
        }

        private static void AssertFlags(Type t, params string[] fragments) {
            var findings = ScanPlanted(t);
            var text = string.Join("\n", findings.Select(f => f.ToString()));
            foreach (var fragment in fragments) {
                Assert.IsTrue(findings.Any(f => f.ToString().Contains(fragment)), "not flagged: " + fragment + "\n" + text);
            }
        }

        [TestMethod]
        public void AFormulaReadIsFlagged() => AssertFlags(typeof(Planted.FormulaRead), "CompMs.Common.DataObj.Property.Formula");

        [TestMethod]
        public void ProcessStateAndStringReflectionAreFlagged() {
            AssertFlags(typeof(Planted.EnvironmentRead), "System.Environment");
            AssertFlags(typeof(Planted.TypeByName), "System.Type::GetType");
            AssertFlags(typeof(Planted.ActivatorUse), "System.Activator");
        }

        [TestMethod]
        public void NondeterminismIsFlagged() {
            AssertFlags(typeof(Planted.Nondeterminism), "get_Now", "System.Random", "Stopwatch", "NewGuid", "System.Threading.Tasks.Task");
        }

        [TestMethod]
        public void CultureSensitiveCallsAreFlagged() {
            AssertFlags(typeof(Planted.CultureSensitive),
                "Int32::ToString()", "Double::Parse(String)", "String::Compare(String, String)", "List<string>.Sort",
                "String::StartsWith(String)", "StringBuilder::Append(Double)", "String::Format(String, Object)",
                "Enumerable::OrderBy", "String::ToUpper()", "RegexOptions.IgnoreCase");
        }

        [TestMethod]
        public void CleanCodeIsNotFlagged() {
            var findings = ScanPlanted(typeof(Planted.Clean));
            Assert.AreEqual(0, findings.Count, string.Join("\n", findings.Select(f => f.ToString())));
        }
    }
}

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests.Planted
{
    // Planted defects for IlReferenceScannerSelfTests. They are never called.
#pragma warning disable CA1822, IDE0060
    internal static class FormulaRead
    {
        public static string Read(CompMs.Common.DataObj.Property.Formula formula) => formula.FormulaString;
    }

    internal static class EnvironmentRead
    {
        public static string? Read() => Environment.GetEnvironmentVariable("MSDIAL_X");
    }

    internal static class TypeByName
    {
        public static Type? Read() => Type.GetType("System.String");
    }

    internal static class ActivatorUse
    {
        public static object? Make() => Activator.CreateInstance(typeof(StringBuilder));
    }

    internal static class Nondeterminism
    {
        public static long Now() => DateTime.Now.Ticks;
        public static int Roll() => new Random().Next();
        public static long Time() => Stopwatch.StartNew().ElapsedTicks;
        public static Guid Id() => Guid.NewGuid();
        public static Task Run() => Task.Run(() => { });
    }

    internal static class CultureSensitive
    {
        public static string A(int x) => x.ToString();
        public static double B(string s) => double.Parse(s);
        public static int C(string a, string b) => string.Compare(a, b);
        public static void D(List<string> list) => list.Sort();
        public static bool E(string a) => a.StartsWith("x");
        public static string F(double x) => new StringBuilder().Append(x).ToString();
        public static string G(int x) => string.Format("{0}", x);
        public static List<string> H(IEnumerable<string> s) => s.OrderBy(x => x).ToList();
        public static string I(string s) => s.ToUpper();
        public static Regex J() => new Regex("x", RegexOptions.IgnoreCase);
    }

    internal static class Clean
    {
        private static readonly Regex Pattern = new Regex("^[a-z]+\\z", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static IEnumerable<string> Items(IEnumerable<string> s) {
            foreach (var x in s.OrderBy(x => x, StringComparer.Ordinal)) {
                yield return x + "!";
            }
        }

        public static string Text(long x, BigInteger b) =>
            new StringBuilder().Append(x.ToString(CultureInfo.InvariantCulture)).Append(' ', 2).Append(b.ToString(CultureInfo.InvariantCulture)).ToString();

        public static int Kind(string s) {
            switch (s) {
                case "a": return 1;
                case "b": return 2;
                case "c": return 3;
                case "d": return 4;
                case "e": return 5;
                case "f": return 6;
                case "g": return 7;
                case "h": return 8;
                default: return 0;
            }
        }

        public static bool Starts(string s) => s.StartsWith("x", StringComparison.Ordinal) && Pattern.IsMatch(s);

        public static string Canonical(JsonValue v) => CanonicalJson.ToText(v);

        public static int[] Table() => new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    }
#pragma warning restore CA1822, IDE0060
}
