using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class MassBankRecordHandlerTests
    {
        [TestMethod()]
        public void WriteRecordTest_InChIKeyLinkHasNoColonAfterDatabaseName() {
            var handler = new MassBankRecordHandler(IonMode.Positive, "LC-ESI-QTOF", _ => "NA");
            var peak = new ChromatogramPeak(1, 76.03930, 1000d, new ChromXs(1.5));
            var reference = new MoleculeMsReference
            {
                Name = "Glycine",
                Formula = new Formula(),
                SMILES = "NCC(=O)O",
                InChIKey = "DHMQDGOQFOQNFH-UHFFFAOYSA-N",
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak(30.03383, 100d), },
            };

            string[] lines;
            using (var stream = new MemoryStream()) {
                handler.WriteRecord(stream, peak, reference, reference, reference);
                lines = Encoding.UTF8.GetString(stream.ToArray()).Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            }

            // MassBank Record Format 2.6.0, 2.2.8: "CH$LINK: INCHIKEY UFFBMTHBGFGIHF-UHFFFAOYSA-N"
            var link = lines.Single(line => line.StartsWith("CH$LINK: INCHIKEY"));
            Assert.AreEqual("CH$LINK: INCHIKEY DHMQDGOQFOQNFH-UHFFFAOYSA-N", link);
        }
    }
}
