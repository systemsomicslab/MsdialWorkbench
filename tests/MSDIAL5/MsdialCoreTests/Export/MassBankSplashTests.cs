using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Threading;

namespace CompMs.MsdialCore.Export.Tests
{
    /// <summary>
    /// PK$SPLASH of a MassBank record: the SPLASH of the peaks as PK$PEAK prints them, whatever the culture.
    /// </summary>
    /// <remarks>
    /// The MassBank Validator recomputes the SPLASH from the printed peaks and refuses a record whose PK$SPLASH differs.
    /// NSSplash's own string constructor parses with the culture of the thread, under which "56.04679" can be read as
    /// 5604679 (de-DE) or not at all (fr-FR), so <see cref="MassBankSplash.Compute"/> parses the pairs itself.
    /// </remarks>
    [TestClass()]
    public class MassBankSplashTests
    {
        // L-pyroglutamic acid as MassBankRecordHandler prints it; the official Validator (MassBank-cli-tools 1.3.0)
        // accepted a record with this PK$PEAK and this PK$SPLASH.
        private const string PyroglutamicPeaks = "56.04679:124.37 56.05220:308.91 84.04584:1638.24 130.04871:378.55";
        private const string PyroglutamicSplash = "splash10-001i-9100000000-f421ad321e181e6f6111";

        [TestMethod()]
        public void Compute_GivesTheSplashTheValidatorAccepted() {
            Assert.AreEqual(PyroglutamicSplash, MassBankSplash.Compute(PyroglutamicPeaks));
        }

        [DataTestMethod()]
        [DataRow("de-DE")] // '.' groups digits
        [DataRow("fr-FR")] // ',' is the decimal separator, a no-break space groups digits
        [DataRow("ar-SA")] // Arabic separators and the Umm al-Qura calendar
        public void Compute_IsTheSameInEveryCulture(string culture) {
            var invariant = Under(CultureInfo.InvariantCulture, () => MassBankSplash.Compute(PyroglutamicPeaks));
            var other = Under(new CultureInfo(culture), () => MassBankSplash.Compute(PyroglutamicPeaks));

            Assert.AreEqual(invariant, other);
            Assert.AreEqual(PyroglutamicSplash, other);
        }

        [TestMethod()]
        public void Compute_RefusesWhatIsNotAPrintedPeakList() {
            Assert.ThrowsException<ArgumentException>(() => MassBankSplash.Compute(" "));
            Assert.ThrowsException<FormatException>(() => MassBankSplash.Compute("56.04679"));
            Assert.ThrowsException<FormatException>(() => MassBankSplash.Compute("56,04679:124,37"));
        }

        private static T Under<T>(CultureInfo culture, Func<T> action) {
            var previous = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = culture;
                return action();
            }
            finally {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
