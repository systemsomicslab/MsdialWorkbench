using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Threading;

namespace CompMs.Common.Utility.Tests
{
    [TestClass]
    public class DecimalTextTests
    {
        private static T Under<T>(string culture, Func<T> read) {
            var thread = Thread.CurrentThread;
            var saved = thread.CurrentCulture;
            try {
                thread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                return read();
            }
            finally {
                thread.CurrentCulture = saved;
            }
        }

        // What MS-FINDER wrote in its settings file before it fixed its culture, on a PC with a
        // decimal point and on one with a decimal comma, and the value both mean.
        [DataTestMethod]
        [DataRow("0.001", 0.001)]
        [DataRow("0,001", 0.001)]
        [DataRow("2,5", 2.5)]
        [DataRow("-0,25", -0.25)]
        [DataRow("1,5E-05", 1.5e-5)]
        [DataRow("20", 20.0)]
        [DataRow(" 0,5 ", 0.5)]
        public void TryParse_ReadsAPointOrADecimalComma(string text, double expected) {
            foreach (var culture in new[] { "en-US", "fr-FR", "de-DE", "ja-JP" }) {
                var (ok, value) = Under(culture, () => (DecimalText.TryParse(text, out double v), v));
                Assert.IsTrue(ok, $"{text} under {culture}");
                Assert.AreEqual(expected, value, 1e-12, $"{text} under {culture}");
            }
        }

        // A point is present, or more than one comma: only digit grouping fits, as before.
        [DataTestMethod]
        [DataRow("1,234.5", 1234.5)]
        [DataRow("1,234,567", 1234567.0)]
        public void TryParse_KeepsDigitGroupingWhereACommaCannotBeDecimal(string text, double expected) {
            foreach (var culture in new[] { "en-US", "fr-FR", "de-DE" }) {
                var (ok, value) = Under(culture, () => (DecimalText.TryParse(text, out double v), v));
                Assert.IsTrue(ok, $"{text} under {culture}");
                Assert.AreEqual(expected, value, 1e-9, $"{text} under {culture}");
            }
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("TRUE")]
        [DataRow("1.2.3")]
        public void TryParse_RefusesWhatIsNotANumber(string text) {
            Assert.IsFalse(DecimalText.TryParse(text, out double _));
            Assert.IsFalse(DecimalText.TryParse(text, out float _));
        }

        [TestMethod]
        public void TryParse_ReadsAFloatTheSameWay() {
            var (ok, value) = Under("de-DE", () => (DecimalText.TryParse("5,23", out float v), v));
            Assert.IsTrue(ok);
            Assert.AreEqual(5.23f, value);
            Assert.IsTrue(DecimalText.TryParse("213.7653", out float ccs));
            Assert.AreEqual(213.7653f, ccs);
        }
    }
}
