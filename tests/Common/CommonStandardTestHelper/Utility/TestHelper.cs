using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CommonStandardTestHelper.Utility
{
    public static class TestHelper
    {
        public static void AreEqual<T>(this CollectionAssert assert, IList<T> expected, IList<T> actual, Action<Assert, T, T> assertAction) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                assertAction(Assert.That, expected[i], actual[i]);
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
    }
}
