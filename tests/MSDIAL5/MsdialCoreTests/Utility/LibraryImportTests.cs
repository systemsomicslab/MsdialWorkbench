using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.MessagePack;
using CompMs.Common.Query;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCoreTests.Utility;

[TestClass]
public sealed class LibraryImportTests
{
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(true, false)]
    [DataRow(false, true)] [DataRow(true, true)]
    public void LipidImportsCompactEligibleSpectraWithoutChangingSelectionOrContent(bool serialized, bool noSelection) {
        var textPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lbm");
        var binaryPath = textPath + "2";
        try {
            File.WriteAllText(textPath, @"NAME: Plain
PRECURSORTYPE: [M+H]+
IONMODE: Positive
COMPOUNDCLASS: PC
FORMULA: C8H20NO6P
SMILES: COP(=O)(O)OCC[N+](C)(C)C
INCHIKEY: TEST-STRUCTURE-UNCHANGED
Num Peaks: 1
73 100

NAME: Annotated
PRECURSORTYPE: [M+H]+
IONMODE: Positive
COMPOUNDCLASS: PC
Num Peaks: 1
147 40 ""Required fragment; qualifier""

NAME: Unselected class
PRECURSORTYPE: [M+H]+
IONMODE: Positive
COMPOUNDCLASS: PE
Num Peaks: 1
73 100

NAME: Other polarity
PRECURSORTYPE: [M-H]-
IONMODE: Negative
COMPOUNDCLASS: PC
Num Peaks: 1
73 100

");
            if (serialized) { MspFileParser.AsciiMspToSerializedMspObj(textPath, binaryPath); }
            var selected = new LbmQuery {
                LbmClass = LbmClass.PC, IonMode = IonMode.Positive,
                AdductType = CompMs.Common.DataObj.Property.AdductIon.GetAdductIon("[M+H]+"), IsSelected = !noSelection,
            };
            var queries = new LipidQueryBean {
                CollisionType = CollisionType.CID, SolventType = SolventType.HCOONH4,
                LbmQueries = new List<LbmQuery> {
                    selected,
                    new LbmQuery { LbmClass = LbmClass.PE, IonMode = IonMode.Positive, IsSelected = false },
                    new LbmQuery { LbmClass = LbmClass.PC, IonMode = IonMode.Negative, IsSelected = true },
                },
            };
            var path = serialized ? binaryPath : textPath;
            var filteredQueries = noSelection ? new List<LbmQuery>() : new List<LbmQuery> { selected };
            var expected = serialized
                ? MspFileParser.ReadSerializedLbmLibrary(path, filteredQueries, IonMode.Positive, queries.SolventType, queries.CollisionType)
                : MspFileParser.LbmFileReader(path, filteredQueries, IonMode.Positive, queries.SolventType, queries.CollisionType);
            var actual = LibraryHandler.ReadMsLibrary(path, new ParameterBase { IonMode = IonMode.Positive, LipidQueryContainer = queries }, out var error);
            Assert.AreEqual(string.Empty, error);
            if (noSelection) {
                if (serialized) { Assert.AreEqual(0, actual.Count); }
                else { Assert.IsNull(actual); }
                return;
            }
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual("Plain", actual[0].Name);
            Assert.AreEqual("Annotated", actual[1].Name);
            Assert.IsFalse(string.IsNullOrEmpty(expected[0].SMILES));
            Assert.IsFalse(string.IsNullOrEmpty(expected[0].InChIKey));
            Assert.IsFalse(string.IsNullOrEmpty(expected[1].Spectrum[0].Comment));
            Assert.IsTrue(actual[0].IsSpectrumCompact);
            Assert.IsFalse(actual[1].IsSpectrumCompact);
            for (var i = 0; i < expected.Count; i++) {
                CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[i]), MessagePackSerializer.Serialize(actual[i].CreateSerializationCopy()));
            }
        }
        finally {
            File.Delete(textPath);
            File.Delete(binaryPath);
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void SerializedImportsPreserveEmptyAndNullRecordLibraries(bool nullRecord) {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msp2");
        try {
            var references = nullRecord ? new List<MoleculeMsReference> { null } : new List<MoleculeMsReference>();
            MoleculeMsRefMethods.SaveMspToFile(references, path);
            var actual = LibraryHandler.ReadMspLibrary(path);
            Assert.AreEqual(references.Count, actual.Count);
            if (nullRecord) { Assert.IsNull(actual[0]); }
        }
        finally { File.Delete(path); }
    }

    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)]
    public void SharedMspImportsCompactEligibleSpectraWithoutChangingContent(bool serialized, bool generalLibraryEntry) {
        var textPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".msp");
        var binaryPath = textPath + "2";
        try {
            MspFileParser.WriteAsLbmMsp(textPath, new[] {
                new MoleculeMsReference { Name = "Plain", Spectrum = new List<SpectrumPeak> { new SpectrumPeak(73, 100) } },
                new MoleculeMsReference { Name = "Annotated", Spectrum = new List<SpectrumPeak> { new SpectrumPeak(147, 40, "Required fragment; qualifier") } },
            });
            if (serialized) { MspFileParser.AsciiMspToSerializedMspObj(textPath, binaryPath); }
            var path = serialized ? binaryPath : textPath;
            var expected = serialized ? MspFileParser.ReadSerializedMspObject(path) : MspFileParser.MspFileReader(path);
            Assert.IsFalse(string.IsNullOrEmpty(expected[1].Spectrum[0].Comment), "The fixture must contain an actual peak annotation.");
            var actual = generalLibraryEntry ? LibraryHandler.ReadMsLibrary(path, null, out _) : LibraryHandler.ReadMspLibrary(path);
            Assert.IsTrue(actual[0].IsSpectrumCompact);
            Assert.IsFalse(actual[1].IsSpectrumCompact);
            for (var i = 0; i < expected.Count; i++) {
                CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[i]), MessagePackSerializer.Serialize(actual[i].CreateSerializationCopy()));
            }
        }
        finally {
            File.Delete(textPath);
            File.Delete(binaryPath);
        }
    }
}
