using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Tests.Utility
{
    public static class TestHelper
    {
        public static void AreEqual<T>(this CollectionAssert assert, IList<T> expected, IList<T> actual, Action<Assert, T, T> assertAction) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                assertAction(Assert.That, expected[i], actual[i]);
            }
        }

        public static void AreEqual<T>(this CollectionAssert assert, IList<T> expected, IList<T> actual, Action<Assert, T, T, string> assertAction) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                assertAction(Assert.That, expected[i], actual[i], string.Empty);
            }
        }

        public static void AreEqual<T>(this CollectionAssert assert, IList<T> expected, IList<T> actual, Action<T, T> assertAction) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                assertAction(expected[i], actual[i]);
            }
        }

        public static void AreEqual<T>(this CollectionAssert assert, IReadOnlyList<T> expected, IReadOnlyList<T> actual, Action<T, T> assertAction) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                assertAction(expected[i], actual[i]);
            }
        }

        public static void AreEquivalent<TKey, TValue>(this CollectionAssert assert, IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            CollectionAssert.AreEquivalent(expected.Keys.ToArray(), actual.Keys.ToArray());
            foreach (var key in expected.Keys) {
                Assert.AreEqual(expected[key], actual[key]);
            }
        }
    }
}
