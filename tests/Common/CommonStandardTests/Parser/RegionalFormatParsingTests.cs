using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.Common.Parser.Tests
{
    /// <summary>
    /// The embedded IUPAC table and MSP files use a point as the decimal separator on every PC. These
    /// tests read them on the calling thread under regional formats that do not: fr-FR groups digits
    /// with a space and used to throw, de-DE groups with a point and used to scale every number by a
    /// power of ten, and tr-TR lower-cases "I" to a dotless "ı", which used to lose every MSP field
    /// whose name contains an I.
    /// </summary>
    [TestClass]
    public class RegionalFormatParsingTests
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

        [DataTestMethod]
        [DataRow("en-US")]
        [DataRow("fr-FR")]
        [DataRow("de-DE")]
        [DataRow("tr-TR")]
        public void GetIUPACDatabase_ReadsTheTableWhateverTheRegionalFormat(string culture) {
            var iupac = Under(culture, IupacResourceParser.GetIUPACDatabase);

            var hydrogen = iupac.ElementName2AtomElementProperties["H"];
            Assert.AreEqual(1.00782503207, hydrogen[0].ExactMass, 1e-11);
            Assert.AreEqual(99.9885, hydrogen[0].NaturalRelativeAbundance, 1e-9);
            Assert.AreEqual(1, hydrogen[0].NominalMass);
            var carbon = iupac.ElementName2AtomElementProperties["C"];
            Assert.AreEqual(13.0033548378, carbon[1].ExactMass, 1e-10);
        }

        // No PRECURSORTYPE: it would set the ion mode itself, and IONMODE, which contains an "I",
        // must be the only line that can.
        private const string Record =
            "NAME: Regional format test\n" +
            "PRECURSORMZ: 471.34798\n" +
            "INSTRUMENTTYPE: LC-ESI-QTOF\n" +
            "RETENTIONTIME: 12.5\n" +
            "CCS: 213.7653\n" +
            "COLLISIONENERGY: 6.5\n" +
            "INCHIKEY: RGHNJXZEOKUKBD-SQOUGZDYSA-N\n" +
            "SMILES: O=C(O)C(O)C(O)C(O)C(O)CO\n" +
            "IONMODE: Negative\n" +
            "Num Peaks: 2\n" +
            "75.0092\t1933\n" +
            "129.0206\t158.5\n" +
            "\n";

        [DataTestMethod]
        [DataRow("en-US")]
        [DataRow("fr-FR")]
        [DataRow("de-DE")]
        [DataRow("tr-TR")]
        public void MspFileReader_ReadsNumbersAndFieldsWhateverTheRegionalFormat(string culture) {
            var path = Path.GetTempFileName();
            try {
                File.WriteAllText(path, Record);

                var record = Under(culture, () => MspFileParser.MspFileReader(path)).Single();

                Assert.AreEqual(471.34798, record.PrecursorMz, 1e-4);
                Assert.AreEqual(12.5, record.ChromXs.RT.Value, 1e-6);
                Assert.AreEqual(213.7653, record.CollisionCrossSection, 1e-3);
                Assert.AreEqual(6.5, record.CollisionEnergy, 1e-6);
                Assert.AreEqual("RGHNJXZEOKUKBD-SQOUGZDYSA-N", record.InChIKey);
                Assert.AreEqual("O=C(O)C(O)C(O)C(O)C(O)CO", record.SMILES);
                Assert.AreEqual(IonMode.Negative, record.IonMode);
                Assert.AreEqual("LC-ESI-QTOF", record.InstrumentType);
                Assert.AreEqual(2, record.Spectrum.Count);
                Assert.AreEqual(75.0092, record.Spectrum[0].Mass, 1e-4);
                Assert.AreEqual(1933, record.Spectrum[0].Intensity, 1e-6);
                Assert.AreEqual(129.0206, record.Spectrum[1].Mass, 1e-4);
                Assert.AreEqual(158.5, record.Spectrum[1].Intensity, 1e-6);
            }
            finally {
                File.Delete(path);
            }
        }
    }
}
