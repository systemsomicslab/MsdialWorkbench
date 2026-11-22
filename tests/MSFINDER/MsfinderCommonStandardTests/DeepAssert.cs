using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Riken.Metabolomics.MsfinderCommon.Tests
{
    /// <summary>
    /// Deep equality of two object graphs by reflection over public readable properties, for the key-isolation
    /// tests (design 6.4 item 4).
    /// </summary>
    internal static class DeepAssert
    {
        private const int MaxDepth = 12;

        public static void AreEqual(object expected, object actual, string root, ISet<string> skippedPaths = null) {
            var differences = new List<string>();
            Compare(expected, actual, root, skippedPaths ?? new HashSet<string>(), differences, 0);
            if (differences.Count > 0) {
                Assert.Fail("Objects differ at: " + string.Join("; ", differences.Take(10)));
            }
        }

        private static void Compare(object expected, object actual, string path, ISet<string> skipped, List<string> differences, int depth) {
            if (skipped.Contains(path)) return;
            if (expected is null || actual is null) {
                if (!(expected is null && actual is null)) differences.Add(path + " (null against non-null)");
                return;
            }
            var type = expected.GetType();
            if (type != actual.GetType()) {
                differences.Add(path + " (type " + type.Name + " against " + actual.GetType().Name + ")");
                return;
            }
            if (type.IsPrimitive || type.IsEnum || expected is string || expected is decimal || expected is DateTime || expected is Guid) {
                if (!expected.Equals(actual)) differences.Add(path);
                return;
            }
            if (depth >= MaxDepth) {
                differences.Add(path + " (deeper than " + MaxDepth + ")");
                return;
            }
            if (expected is IEnumerable expectedItems) {
                var e = expectedItems.Cast<object>().ToList();
                var a = ((IEnumerable)actual).Cast<object>().ToList();
                if (e.Count != a.Count) {
                    differences.Add(path + " (count " + e.Count + " against " + a.Count + ")");
                    return;
                }
                for (var i = 0; i < e.Count; i++) {
                    Compare(e[i], a[i], path + "[" + i + "]", skipped, differences, depth + 1);
                }
                return;
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                Compare(property.GetValue(expected), property.GetValue(actual), path + "." + property.Name, skipped, differences, depth + 1);
            }
        }
    }
}
