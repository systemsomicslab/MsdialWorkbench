using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MsdialGuiAppTests.Model.Gcms;

[TestClass]
public sealed class CompactLibraryTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ConcurrentPublicReadersShareOneEditableSpectrum(bool integral) {
        var reference = Reference();
        if (integral) { foreach (var peak in reference.Spectrum) { peak.Mass = Math.Round(peak.Mass); peak.Intensity = Math.Round(peak.Intensity); } }
        Assert.IsTrue(reference.TryCompactSpectrum());
        var readers = new List<SpectrumPeak>[64];
        Parallel.For(0, readers.Length, index => readers[index] = reference.Spectrum);
        foreach (var spectrum in readers) { Assert.AreSame(readers[0], spectrum); }
        readers[0][0].Intensity = 321;
        Assert.AreEqual(321d, reference.GetSpectrumPeakData()[0].Intensity);
        Assert.IsFalse(reference.IsSpectrumCompact);
    }

    [TestMethod]
    public void CompactionPreservesDoubleBitsAndPublicListEdits() {
        var reference = Reference();
        var bits = reference.Spectrum.SelectMany(peak => new[] { BitConverter.DoubleToInt64Bits(peak.Mass), BitConverter.DoubleToInt64Bits(peak.Intensity) }).ToArray();
        Assert.IsTrue(reference.TryCompactSpectrum());
        var data = reference.GetSpectrumPeakData();
        CollectionAssert.AreEqual(bits, Enumerable.Range(0, data.Count).SelectMany(i => new[] { BitConverter.DoubleToInt64Bits(data[i].Mass), BitConverter.DoubleToInt64Bits(data[i].Intensity) }).ToArray());
        Assert.IsTrue(reference.IsSpectrumCompact);
        var editable = reference.Spectrum;
        Assert.IsFalse(reference.IsSpectrumCompact);
        Assert.AreSame(editable, reference.Spectrum);
        editable[0].Intensity = 321;
        editable.Add(new SpectrumPeak(400, 4));
        Assert.AreEqual(321d, reference.GetSpectrumPeakData()[0].Intensity);
        Assert.AreEqual(4, reference.GetSpectrumPeakData().Count);
    }

    [DataTestMethod]
    [DataRow(.5f, false)] [DataRow(.5f, true)]
    [DataRow(.01f, false)] [DataRow(.01f, true)]
    public void EveryEiScoreIsIdenticalWithoutExpandingReferences(float tolerance, bool integral) {
        var query = Reference();
        var reference = Reference();
        if (integral) {
            foreach (var peak in query.Spectrum.Concat(reference.Spectrum)) { peak.Mass = Math.Round(peak.Mass); peak.Intensity = Math.Round(peak.Intensity); }
        }
        reference.Spectrum[1].Intensity = 39;
        var parameter = new MsRefSearchParameterBase { Ms1Tolerance = tolerance };
        var expected = MsScanMatching.CompareEIMSScanProperties(query, reference, parameter);
        Assert.IsTrue(reference.TryCompactSpectrum());
        var actual = MsScanMatching.CompareEIMSScanProperties(query, reference, parameter);
        Assert.IsTrue(reference.IsSpectrumCompact, "Scoring must not recreate the retained peak-object graph.");
        Assert.AreEqual(expected.TotalScore, actual.TotalScore);
        Assert.AreEqual(expected.SquaredWeightedDotProduct, actual.SquaredWeightedDotProduct);
        Assert.AreEqual(expected.SquaredSimpleDotProduct, actual.SquaredSimpleDotProduct);
        Assert.AreEqual(expected.SquaredReverseDotProduct, actual.SquaredReverseDotProduct);
        Assert.AreEqual(expected.MatchedPeaksCount, actual.MatchedPeaksCount);
        Assert.AreEqual(expected.MatchedPeaksPercentage, actual.MatchedPeaksPercentage);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)]
    [DataRow(7)] [DataRow(8)] [DataRow(9)] [DataRow(10)] [DataRow(11)] [DataRow(12)]
    public void NondefaultPeakMetadataIsNeverDiscarded(int field) {
        var reference = Reference();
        var peak = reference.Spectrum[0];
        switch (field) {
            case 0: peak.Comment = ""; break;
            case 1: peak.Resolution = 1; break;
            case 2: peak.Charge = 1; break;
            case 3: peak.IsotopeFrag = true; break;
            case 4: peak.PeakQuality = (PeakQuality)1; break;
            case 5: peak.PeakID = 1; break;
            case 6: peak.IsotopeParentPeakID = 1; break;
            case 7: peak.IsotopeWeightNumber = 1; break;
            case 8: peak.IsMatched = true; break;
            case 9: peak.SpectrumComment = SpectrumComment.reference; break;
            case 10: peak.IsAbsolutelyRequiredFragmentForAnnotation = true; break;
            case 11: peak.FragmentationScore = 1; break;
            case 12: reference.Spectrum[0] = new DerivedPeak(); break;
        }
        var original = reference.Spectrum;
        Assert.IsFalse(reference.TryCompactSpectrum());
        Assert.AreSame(original, reference.Spectrum);
    }

    [TestMethod]
    public void LegacyAndCompactLibrariesHaveTheSameContentAndFingerprint() {
        var plain = Reference();
        // Exercise the desktop CLR's numeric conversions through the public database save/load path.
        var values = new[] { BitConverter.Int64BitsToDouble(long.MinValue), double.NaN, double.PositiveInfinity,
            double.NegativeInfinity, double.Epsilon, double.MaxValue, -1d, 127d, 128d, 255d, 256d,
            int.MinValue, uint.MaxValue, (double)uint.MaxValue + 1, 73.25d, float.Epsilon, float.MaxValue };
        plain.Spectrum.AddRange(values.Select(value => new SpectrumPeak(value, value)));
        var random = new Random(924);
        var bits = new byte[8];
        for (var i = 0; i < 1000; i++) {
            random.NextBytes(bits);
            var value = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(bits, 0));
            plain.Spectrum.Add(new SpectrumPeak(value, value));
        }
        var annotated = Reference();
        annotated.ScanID = 1;
        annotated.Spectrum[0].Comment = "Keep annotation";
        annotated.Spectrum[0].SpectrumComment = SpectrumComment.reference;
        var references = new List<MoleculeMsReference> { plain, annotated };
        var digest = LibraryFingerprint.Digest(references);
        using var legacy = new MemoryStream();
        LargeListMessagePack.Serialize(legacy, references);
        var database = new MoleculeDataBase("Test", SourceType.MspDB, DataBaseSource.Msp, "local.msp");
        legacy.Position = 0;
        database.Load(legacy, "");
        Assert.IsTrue(database.Database[0].IsSpectrumCompact);
        Assert.IsFalse(database.Database[1].IsSpectrumCompact);
        Assert.AreEqual(digest, database.ContentDigest);
        Assert.IsTrue(database.Database[0].IsSpectrumCompact, "Computing provenance must not expand every spectrum.");
        using var saved = new MemoryStream();
        database.Save(saved);
        Assert.IsTrue(database.Database[0].IsSpectrumCompact, "Saving must not pin all mutable peak lists.");
        saved.Position = 0;
        var oldReader = LargeListMessagePack.Deserialize<MoleculeMsReference>(saved);
        Assert.AreEqual(references.Count, oldReader.Count);
        for (var i = 0; i < references.Count; i++) {
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(references[i]), MessagePackSerializer.Serialize(oldReader[i]));
        }
        Assert.AreEqual("Keep annotation", oldReader[1].Spectrum[0].Comment);
        Assert.AreEqual(SpectrumComment.reference, oldReader[1].Spectrum[0].SpectrumComment);
        Assert.AreEqual(plain.Spectrum[0].Mass, oldReader[0].Spectrum[0].Mass);
    }

    [TestMethod]
    public void TextExportDoesNotExpandCompactSpectraOrChangeItsBytes() {
        var reference = Reference();
        using var original = new MemoryStream();
        MspFileParser.WriteAsMsp(original, new[] { reference });
        reference.TryCompactSpectrum();
        using var compact = new MemoryStream();
        MspFileParser.WriteAsMsp(compact, new[] { reference });
        Assert.IsTrue(reference.IsSpectrumCompact);
        CollectionAssert.AreEqual(original.ToArray(), compact.ToArray());
    }

    [TestMethod]
    public void ExplicitSmallBlocksRoundTripThroughTheLegacyReader() {
        var references = Enumerable.Range(0, 40).Select(_ => Reference()).ToArray();
        using var stream = new MemoryStream();
        LargeListMessagePack.Serialize(stream, references, null, 1024);
        stream.Position = 0;
        var result = LargeListMessagePack.Deserialize<MoleculeMsReference>(stream);
        Assert.AreEqual(references.Length, result.Count);
        Assert.AreEqual(references.Last().Spectrum[2].Mass, result.Last().Spectrum[2].Mass);
    }

    private static MoleculeMsReference Reference() => new() {
        Name = "Reference", ChromXs = new ChromXs(),
        Spectrum = [new SpectrumPeak(73.0000000000123, 100.00000000004), new SpectrumPeak(147.01, 40), new SpectrumPeak(205, 20)],
    };

    private sealed class DerivedPeak : SpectrumPeak { }
}
