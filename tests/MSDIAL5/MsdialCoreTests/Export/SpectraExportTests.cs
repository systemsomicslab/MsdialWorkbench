using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass()]
public class SpectraExportTests
{
    [TestMethod()]
    public void SaveSpectraTableAsNistFormatTest()
    {
        var expects =
$@"NAME: XXX
PRECURSORMZ: 200
PRECURSORTYPE: [M+H]+
IONMODE: Positive
RETENTIONTIME: 3
FORMULA: C6
ONTOLOGY: Molecule ontology
INCHIKEY: ABCDEFG
SMILES: CCCCCC
COMMENT: Here is peak comment.|PEAKID=100|MS1SCAN=1000|MS2SCAN=2000|PEAKHEIGHT=10000|PEAKAREA=20000|ISOTOPE=M+1
AUTHORS: A. BCD, E. FGD
LICENSE: 2021, A. BCD
COLLISIONENERGY: 1000
INSTRUMENTTYPE: LC-MS/MS
INSTRUMENT: YYYYY
PARAMETERCOMMENT: Here is parameter comment.
Num Peaks: 4
100{"\t"}100
118{"\t"}1000
200{"\t"}2000
300{"\t"}500

";

        var basePeak = new BaseChromatogramPeakFeature
        {
            ChromXsTop = new ChromXs(3),
            Mass = 200,
            PeakHeightTop = 10_000,
            PeakAreaAboveZero = 20_000,
        };
        var chromPeakFeature = new ChromatogramPeakFeature(basePeak)
        {
            Name = "XXX",
            MasterPeakID = 100,
            MS1RawSpectrumIdTop = 1000,
            MS2RawSpectrumID = 2000,
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            IonMode = Common.Enum.IonMode.Positive,
            PeakCharacter = new IonFeatureCharacter
            {
                IsotopeWeightNumber = 1,
            },
            Comment = "Here is peak comment.",
        };
        var reference = new MoleculeMsReference
        {
            Ontology = "Molecule ontology",
            InChIKey = "ABCDEFG",
            SMILES = "CCCCCC",
            Formula = Common.FormulaGenerator.Parser.FormulaStringParcer.Convert2FormulaObjV2("C6"),
        };
        var parameter = new ParameterBase
        {
            Authors = "A. BCD, E. FGD",
            License = "2021, A. BCD",
            CollisionEnergy = "1000",
            InstrumentType = "LC-MS/MS",
            Instrument = "YYYYY",
            Comment = "Here is parameter comment.",
        };
        var msdecResult = new MSDecResult
        {
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 100, Intensity = 100 },
                new SpectrumPeak { Mass = 118, Intensity = 1000 },
                new SpectrumPeak { Mass = 200, Intensity = 2000 },
                new SpectrumPeak { Mass = 300, Intensity = 500 },
            }
        };
        var refer = new MockRefer(reference);

        var stream = new MemoryStream();
        SpectraExport.SaveSpectraTableAsNistFormat(stream, chromPeakFeature, msdecResult.Spectrum, refer, parameter);
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
$@"NAME: Unknown|ID=0|MZ=200|RT=3
PRECURSORMZ: 200
PRECURSORTYPE: [M+H]+
IONMODE: Positive
RETENTIONTIME: 3
FORMULA: C6
ONTOLOGY: Molecule ontology
INCHIKEY: ABCDEFG
SMILES: CCCCCC
COMMENT: |PEAKID=0|MS1SCAN=0|MS2SCAN=-1|PEAKHEIGHT=10000|PEAKAREA=0|ISOTOPE=M+1
AUTHORS: A. BCD, E. FGD
LICENSE: 2021, A. BCD
COLLISIONENERGY: 1000
INSTRUMENTTYPE: LC-MS/MS
INSTRUMENT: YYYYY
PARAMETERCOMMENT: Here is parameter comment.
Num Peaks: 4
100{"\t"}100
118{"\t"}1000
200{"\t"}2000
300{"\t"}500

";

        var basePeak = new BaseChromatogramPeakFeature
        {
            ChromScanIdTop = 100,
            ChromXsTop = new ChromXs(3),
            PeakHeightTop = 10_000,
            Mass = 200,
        };
        var chromPeakFeature = new ChromatogramPeakFeature(basePeak)
        {
            Name = string.Empty,
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            IonMode = Common.Enum.IonMode.Positive,
            PeakCharacter = new IonFeatureCharacter
            {
                IsotopeWeightNumber = 1,
            },
        };
        var reference = new MoleculeMsReference
        {
            Ontology = "Molecule ontology",
            InChIKey = "ABCDEFG",
            SMILES = "CCCCCC",
            Formula = Common.FormulaGenerator.Parser.FormulaStringParcer.Convert2FormulaObjV2("C6"),
        };
        var parameter = new ParameterBase
        {
            Authors = "A. BCD, E. FGD",
            License = "2021, A. BCD",
            CollisionEnergy = "1000",
            InstrumentType = "LC-MS/MS",
            Instrument = "YYYYY",
            Comment = "Here is parameter comment.",
        };
        var msdecResult = new MSDecResult
        {
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 100, Intensity = 100 },
                new SpectrumPeak { Mass = 118, Intensity = 1000 },
                new SpectrumPeak { Mass = 200, Intensity = 2000 },
                new SpectrumPeak { Mass = 300, Intensity = 500 },
            }
        };
        var refer = new MockRefer(reference);

        var stream = new MemoryStream();
        SpectraExport.SaveSpectraTableAsNistFormat(stream, chromPeakFeature, msdecResult.Spectrum, refer, parameter);
        var buffer = stream.GetBuffer();
        var actual = System.Text.Encoding.ASCII.GetString(buffer);
        Console.WriteLine($"Expect:\n{expects}");
        Console.WriteLine($"Actual:\n{actual}");
        Assert.AreEqual(expects, actual);
    }

    [TestMethod()]
    public void SaveSpectraTableAsNistFormatGenericTest()
    {
        var expects =
$@"NAME: TestCompound
PRECURSORMZ: 250.15
PRECURSORTYPE: [M+H]+
IONMODE: Positive
RETENTIONTIME: 5.5
FORMULA: C10H20
ONTOLOGY: Test Ontology
INCHIKEY: KWIUHFFTVRNATP-UHFFFAOYSA-N
SMILES: CCCCCCCCCC
COMMENT: Test compound with basic properties.|PEAKID=0|MS1SCAN=0|MS2SCAN=-1|PEAKHEIGHT=0|PEAKAREA=0|ISOTOPE=M+1
AUTHORS: Test Author
LICENSE: Test License
COLLISIONENERGY: 30
INSTRUMENTTYPE: Test Instrument Type
INSTRUMENT: Test Instrument
PARAMETERCOMMENT: Test parameter comment.
Num Peaks: 3
50{'\t'}500
150{'\t'}1500
250{'\t'}2500

";
        var testFeature = new BaseChromatogramPeakFeature
        {
            Mass = 250.15,
            ChromXsTop = new ChromXs(new RetentionTime(5.5)),
        };
        var testPeakFeature = new ChromatogramPeakFeature(testFeature)
        {
            Name = "TestCompound",
            AdductType = AdductIon.GetAdductIon("[M+H]+"),
            CollisionCrossSection = 150.5,
            Comment = "Test compound with basic properties.",
            PeakCharacter = new IonFeatureCharacter
            {
                IsotopeWeightNumber = 1,
            },
        };
        var msdecResult = new MSDecResult
        {
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 50, Intensity = 500 },
                new SpectrumPeak { Mass = 150, Intensity = 1500 },
                new SpectrumPeak { Mass = 250, Intensity = 2500 }
            }
        };
        var stubReference = new MoleculeMsReference
        {
            Formula = FormulaStringParcer.Convert2FormulaObjV2("C10H20"),
            InChIKey = "KWIUHFFTVRNATP-UHFFFAOYSA-N",
            SMILES = "CCCCCCCCCC",
            Ontology = "Test Ontology"
        };
        var refer = new MockRefer(stubReference);
        var parameter = new ParameterBase
        {
            Authors = "Test Author",
            License = "Test License",
            CollisionEnergy = "30",
            InstrumentType = "Test Instrument Type",
            Instrument = "Test Instrument",
            Comment = "Test parameter comment."
        };

        var stream = new MemoryStream();
        SpectraExport.SaveSpectraTableAsNistFormat(stream, testPeakFeature, msdecResult.Spectrum, refer, parameter);
        stream.Seek(0, SeekOrigin.Begin);
        var actual = System.Text.Encoding.ASCII.GetString(stream.ToArray());

        Assert.AreEqual(expects.Trim(), actual.Trim(), "The generated NIST format data does not match the expected output.");
    }

    class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public MockRefer(MoleculeMsReference reference) {
            Reference = reference;
        }

        public string Key => "";

        public MoleculeMsReference Reference { get; }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return Reference;
        }
    }
}