using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class MspFileParserTests
    {
        [TestMethod()]
        public void WriteAsMspTest() {
            var records = new List<MoleculeMsReference>{
                new MoleculeMsReference
                {
                    Name = "Metabolite1",
                    PrecursorMz = 1234.567,
                    AdductType = AdductIon.GetAdductIon("[M+H]+"),
                    ChromXs = new ChromXs(12.34, ChromXType.RT, ChromXUnit.Min),
                    Formula = new Formula { FormulaString = "formula" },
                    SMILES = "CCCCCCCCC",
                    InChIKey = "ABCD1234",
                    Ontology = "Ontology1",
                    CollisionEnergy = 15f,
                    IonMode = IonMode.Positive,
                    InstrumentType = "Instrument1",
                    Comment = "Awesome comment",
                    DatabaseUniqueIdentifier = "DB1",
                    Spectrum = new List<SpectrumPeak>
                    {
                        new SpectrumPeak(123.45678d, 456d),
                        new SpectrumPeak(234.56789d, 567d),
                    },
                },

                new MoleculeMsReference
                {
                    Name = "Metabolite2",
                    PrecursorMz = 2345.678,
                    AdductType = AdductIon.GetAdductIon("[M+HCOO]-"),
                    ChromXs = new ChromXs(23.45, ChromXType.RT, ChromXUnit.Min),
                    Formula = new Formula { FormulaString = "formula" },
                    SMILES = "CCCCCCCCC",
                    InChIKey = "ABCD1234",
                    Ontology = "Ontology2",
                    CollisionEnergy = 15f,
                    IonMode = IonMode.Negative,
                    InstrumentType = "Instrument2",
                    DatabaseUniqueIdentifier = "DB2",
                    Spectrum = new List<SpectrumPeak>
                    {
                        new SpectrumPeak(345.67890d, 678d, "spectrum1"),
                        new SpectrumPeak(456.78901d, 789d, "spectrum2; abcdefg"),
                    },
                },

                new MoleculeMsReference
                {
                    Name = "Metabolite3",
                    PrecursorMz = 3456.789,
                    ChromXs = new ChromXs(34.56, ChromXType.RT, ChromXUnit.Min),
                    Formula = new Formula { FormulaString = "formula" },
                    SMILES = "CCCCCCCCC",
                    InChIKey = "ABCD1234",
                    CollisionEnergy = 15f,
                    IonMode = IonMode.Negative,
                },
            };

            var expected = string.Join(Environment.NewLine,
                new List<string>
                {
                    "NAME: Metabolite1",
                    "PRECURSORMZ: 1234.567",
                    "PRECURSORTYPE: [M+H]+",
                    "RETENTIONTIME: 12.34",
                    "FORMULA: formula",
                    "ONTOLOGY: Ontology1",
                    "SMILES: CCCCCCCCC",
                    "INCHIKEY: ABCD1234",
                    "INSTRUMENTTYPE: Instrument1",
                    "COLLISIONENERGY: 15",
                    "IONMODE: Positive",
                    "DatabaseUniqueIdentifier: DB1",
                    "Comment: Awesome comment",
                    "Num Peaks: 2",
                    "123.45678\t456",
                    "234.56789\t567",
                    "",
                    "NAME: Metabolite2",
                    "PRECURSORMZ: 2345.678",
                    "PRECURSORTYPE: [M+HCOO]-",
                    "RETENTIONTIME: 23.45",
                    "FORMULA: formula",
                    "ONTOLOGY: Ontology2",
                    "SMILES: CCCCCCCCC",
                    "INCHIKEY: ABCD1234",
                    "INSTRUMENTTYPE: Instrument2",
                    "COLLISIONENERGY: 15",
                    "IONMODE: Negative",
                    "DatabaseUniqueIdentifier: DB2",
                    "Comment: ",
                    "Num Peaks: 2",
                    "345.6789\t678",
                    "456.78901\t789\t\"spectrum2\"",
                    "",
                    "NAME: Metabolite3",
                    "PRECURSORMZ: 3456.789",
                    "PRECURSORTYPE: [M-H]-",
                    "RETENTIONTIME: 34.56",
                    "FORMULA: formula",
                    "ONTOLOGY: ",
                    "SMILES: CCCCCCCCC",
                    "INCHIKEY: ABCD1234",
                    "INSTRUMENTTYPE: ",
                    "COLLISIONENERGY: 15",
                    "IONMODE: Negative",
                    "DatabaseUniqueIdentifier: ",
                    "Comment: ",
                    "Num Peaks: 0",
                    "",
                    "",
                });

            var memory = new MemoryStream();
            MspFileParser.WriteAsMsp(memory, records);

            Assert.AreEqual(expected, Encoding.ASCII.GetString(memory.ToArray()));
        }

        [TestMethod]
        [DeploymentItem(@"Resources\Parser\msp_test_data.msp", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\msp_test_data.msp", 2)]
        [DeploymentItem(@"Resources\Parser\msp_name_check.msp", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\msp_name_check.msp", 12)]
        [DeploymentItem(@"Resources\Parser\msp_same_name.msp", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\msp_same_name.msp", 2)]
        [DeploymentItem(@"Resources\Parser\msp_spectra_check.msp", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\msp_spectra_check.msp", 2)]
        public void ReadMspFileTest(string testFile, int size) {
            var datas = MspFileParser.MspFileReader(testFile);
            Assert.AreEqual(size, datas.Count);
        }
    }
}