using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Export.Tests;

[TestClass()]
public class NistRecordBuilderTests
{
    [TestMethod]
    public void Export_WritesExpectedNistFormat()
    {
        // Arrange
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
        var builder = new NistRecordBuilder();
        builder.SetNameProperty(testPeakFeature.Name);
        builder.SetChromatogramPeakFeatureProperties(testPeakFeature, testPeakFeature.MasterPeakID);
        builder.SetChromatogramPeakProperties(testPeakFeature);
        builder.SetComment(testPeakFeature);
        builder.SetMoleculeProperties(testPeakFeature.Refer(refer));
        builder.SetIonPropertyProperties(testPeakFeature);
        builder.SetProjectParameterProperties(parameter.ProjectParam);
        builder.SetScan(msdecResult);

        var stream = new MemoryStream();
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

        // Act
        builder.Export(stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(stream, Encoding.ASCII);
        var actual = reader.ReadToEnd();
        Assert.AreEqual(expects, actual, "The exported NIST format data does not match the expected output.");
    }

    [TestMethod]
    public void Export_WriteCCSProperty()
    {
        // Arrange
        var testFeature = new BaseChromatogramPeakFeature
        {
            Mass = 250.15,
            ChromXsTop = new ChromXs(new RetentionTime(5.5)) { Drift = new DriftTime(6d) },
        };
        var testPeakFeature = new ChromatogramPeakFeature(testFeature)
        {
            MasterPeakID = 1,
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
        var builder = new NistRecordBuilder();
        builder.SetNameProperty(testPeakFeature.Name);
        builder.SetChromatogramPeakFeatureProperties(testPeakFeature, testPeakFeature.MasterPeakID);
        builder.SetChromatogramPeakProperties(testPeakFeature);
        builder.SetComment(testPeakFeature);
        builder.SetMoleculeProperties(testPeakFeature.Refer(refer));
        builder.SetIonPropertyProperties(testPeakFeature);
        builder.SetProjectParameterProperties(parameter.ProjectParam);
        builder.SetScan(msdecResult);

        var stream = new MemoryStream();
        var expects =
$@"NAME: Unknown|ID=1|MZ=250.15|RT=5.5|DT=6
PRECURSORMZ: 250.15
PRECURSORTYPE: [M+H]+
IONMODE: Positive
RETENTIONTIME: 5.5
MOBILITY: 6
CCS: 150.5
FORMULA: C10H20
ONTOLOGY: Test Ontology
INCHIKEY: KWIUHFFTVRNATP-UHFFFAOYSA-N
SMILES: CCCCCCCCCC
COMMENT: Test compound with basic properties.|PEAKID=1|MS1SCAN=0|MS2SCAN=-1|PEAKHEIGHT=0|PEAKAREA=0|ISOTOPE=M+1
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

        // Act
        builder.Export(stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(stream, Encoding.ASCII);
        var actual = reader.ReadToEnd();
        //for (int i = 0; i < actual.Length; i++) {
        //    Assert.AreEqual(expects[i], actual[i], $"Charactor {i}: {expects.Substring(System.Math.Max(i-10, 0), 10)}'{expects[i]}'{expects.Substring(i+1, 10)}");
        //}
        Assert.AreEqual(expects, actual, "The exported NIST format data does not match the expected output.");
    }

    [TestMethod]
    public void Export_NoName()
    {
        // Arrange
        var msdecResult = new MSDecResult
        {
            Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 50, Intensity = 500 },
                new SpectrumPeak { Mass = 150, Intensity = 1500 },
                new SpectrumPeak { Mass = 250, Intensity = 2500 }
            }
        };
        var builder = new NistRecordBuilder();
        builder.SetScan(msdecResult);

        var stream = new MemoryStream();
        var expects =
$@"NAME: Unknown
Num Peaks: 3
50{'\t'}500
150{'\t'}1500
250{'\t'}2500

";

        // Act
        builder.Export(stream);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(stream, Encoding.ASCII);
        var actual = reader.ReadToEnd();
        Assert.AreEqual(expects, actual, "The exported NIST format data does not match the expected output.");
    }

    [TestMethod]
    public void SetMoleculeProperties_NullFormula()
    {
        var builder = new NistRecordBuilder();
        var molecule = new MoleculeProperty(
            name:string.Empty, formula:null, ontology:string.Empty, smiles:string.Empty, inchikey:string.Empty
            );
        builder.SetMoleculeProperties(molecule);
    }

    class MockRefer(MoleculeMsReference reference) : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public string Key => "";

        public MoleculeMsReference Reference { get; } = reference;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return Reference;
        }
    }
}
