using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialCore.Export;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class SpectraExportTests
    {
        [TestMethod()]
        public void SaveSpectraTableAsNistFormatTest()
        {
            var expects =
$@"NAME: XXX
SCANNUMBER: 100
RETENTIONTIME: 3
PRECURSORMZ: 200
PRECURSORTYPE: [M+H]+
IONMODE: Positive
INTENSITY: 10000
ISOTOPE: M + 1
INCHIKEY: ABCDEFG
SMILES: CCCCCC
FORMULA: C6
AUTHORS: A. BCD, E. FGD
LICENSE: 2021, A. BCD
COLLISIONENERGY: 1000
INSTRUMENTTYPE: LC-MS/MS
INSTRUMENT: YYYYY
COMMENT: Here is comment.
Num Peaks: 4
100{"\t"}100
118{"\t"}1000
200{"\t"}2000
300{"\t"}500

";

            var chromPeakFeature = new ChromatogramPeakFeature
            {
                Name = "XXX",
                ChromScanIdTop = 100,
                ChromXsTop = new Common.Components.ChromXs(3),
                PrecursorMz = 200,
                AdductType = Common.Parser.AdductIonParser.GetAdductIonBean("[M+H]+"),
                IonMode = Common.Enum.IonMode.Positive,
                PeakHeightTop = 10_000,
                PeakCharacter = new IonFeatureCharacter
                {
                    IsotopeWeightNumber = 1,
                },
                InChIKey = "ABCDEFG",
                SMILES = "CCCCCC",
                Formula = new Common.DataObj.Property.Formula("C6"),
            };
            var parameter = new ParameterBase
            {
                Authors = "A. BCD, E. FGD",
                License = "2021, A. BCD",
                CollisionEnergy = "1000",
                InstrumentType = "LC-MS/MS",
                Instrument = "YYYYY",
                Comment = "Here is comment.",
            };
            var msdecResult = new MSDecResult
            {
                Spectrum = new List<Common.Components.SpectrumPeak>
                {
                    new Common.Components.SpectrumPeak { Mass = 100, Intensity = 100 },
                    new Common.Components.SpectrumPeak { Mass = 118, Intensity = 1000 },
                    new Common.Components.SpectrumPeak { Mass = 200, Intensity = 2000 },
                    new Common.Components.SpectrumPeak { Mass = 300, Intensity = 500 },
                }
            };

            var stream = new MemoryStream();
            SpectraExport.SaveSpectraTableAsNistFormat(stream, chromPeakFeature, msdecResult.Spectrum, parameter);
            var buffer = stream.GetBuffer();
            var actual = System.Text.Encoding.ASCII.GetString(buffer);
            Console.WriteLine($"Expect:\n{expects}");
            Console.WriteLine($"Actual:\n{actual}");
            Assert.AreEqual(expects, actual);
        }

        [TestMethod()]
        public void SaveSpectraTableAsNistFormatUnknownTest()
        {
            var expects =
$@"NAME: Unknown
SCANNUMBER: 100
RETENTIONTIME: 3
PRECURSORMZ: 200
PRECURSORTYPE: [M+H]+
IONMODE: Positive
INTENSITY: 10000
ISOTOPE: M + 1
INCHIKEY: ABCDEFG
SMILES: CCCCCC
FORMULA: C6
AUTHORS: A. BCD, E. FGD
LICENSE: 2021, A. BCD
COLLISIONENERGY: 1000
INSTRUMENTTYPE: LC-MS/MS
INSTRUMENT: YYYYY
COMMENT: Here is comment.
Num Peaks: 4
100{"\t"}100
118{"\t"}1000
200{"\t"}2000
300{"\t"}500

";

            var chromPeakFeature = new ChromatogramPeakFeature
            {
                Name = string.Empty,
                ChromScanIdTop = 100,
                ChromXsTop = new Common.Components.ChromXs(3),
                PrecursorMz = 200,
                AdductType = Common.Parser.AdductIonParser.GetAdductIonBean("[M+H]+"),
                IonMode = Common.Enum.IonMode.Positive,
                PeakHeightTop = 10_000,
                PeakCharacter = new IonFeatureCharacter
                {
                    IsotopeWeightNumber = 1,
                },
                InChIKey = "ABCDEFG",
                SMILES = "CCCCCC",
                Formula = new Common.DataObj.Property.Formula("C6"),
            };
            var parameter = new ParameterBase
            {
                Authors = "A. BCD, E. FGD",
                License = "2021, A. BCD",
                CollisionEnergy = "1000",
                InstrumentType = "LC-MS/MS",
                Instrument = "YYYYY",
                Comment = "Here is comment.",
            };
            var msdecResult = new MSDecResult
            {
                Spectrum = new List<Common.Components.SpectrumPeak>
                {
                    new Common.Components.SpectrumPeak { Mass = 100, Intensity = 100 },
                    new Common.Components.SpectrumPeak { Mass = 118, Intensity = 1000 },
                    new Common.Components.SpectrumPeak { Mass = 200, Intensity = 2000 },
                    new Common.Components.SpectrumPeak { Mass = 300, Intensity = 500 },
                }
            };

            var stream = new MemoryStream();
            SpectraExport.SaveSpectraTableAsNistFormat(stream, chromPeakFeature, msdecResult.Spectrum, parameter);
            var buffer = stream.GetBuffer();
            var actual = System.Text.Encoding.ASCII.GetString(buffer);
            Console.WriteLine($"Expect:\n{expects}");
            Console.WriteLine($"Actual:\n{actual}");
            Assert.AreEqual(expects, actual);
        }
    }
}