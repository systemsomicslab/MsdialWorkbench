using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class MsScanSearchTests
{
    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    public void SearchPreservesEveryRankedResultAndTheOriginalSpectrum(int spectrumKind) {
        var target = new MSScanProperty {
            ChromXs = new ChromXs(), PrecursorMz = 300,
            Spectrum = spectrumKind == 2 ? [] : spectrumKind == 3 ? null! : Peaks(),
        };
        var original = target.Spectrum?.Select(peak => (peak.Mass, peak.Intensity, peak.Comment)).ToArray();
        var records = Enumerable.Range(0, 20).Select(index => new MoleculeMsReference {
            Name = "Candidate " + index, ScanID = index,
            Spectrum = Peaks().Select(peak => new SpectrumPeak(peak.Mass + index * .001, peak.Intensity,
                index % 2 == 0 ? null : "fragment")).ToList(),
        }).ToList();
        foreach (var record in records) { record.TryCompactSpectrum(); }
        var database = new MoleculeDataBase(records, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var search = new MsScanCompoundSearchUsecase();
        search.AddDataBase(database);
        search.SearchMethod = database;
        var parameter = search.SearchParameter;
        parameter.AbsoluteAmpCutoff = spectrumKind == 1 ? 20 : 0;
        parameter.RelativeAmpCutoff = spectrumKind == 1 ? .2f : 0;
        parameter.MassRangeBegin = 50;
        parameter.MassRangeEnd = 200;
        var expected = records.Select(record => {
            var normalized = DataAccess.GetNormalizedMSScanProperty(target, parameter);
            var score = new Ms2MatchCalculator().Calculate(new MSScanMatchQuery(normalized, parameter), record);
            var result = new MsScanMatchResult { TotalScore = (float)score.Scores.Average() };
            score.Assign(result);
            result.EvidenceSource = AnnotationEvidence.WhenSpectrumCompared(result.MeasuredTerms, AnnotationEvidenceSource.ReferenceSpectrum);
            return (record, result);
        }).ToList();
        expected.Sort((left, right) => right.result.TotalScore.CompareTo(left.result.TotalScore));

        var actual = search.Search(target);

        Assert.AreEqual(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++) {
            Assert.AreSame(expected[index].record, actual[index].MsReference);
            CollectionAssert.AreEqual(MessagePackSerializer.Serialize(expected[index].result),
                MessagePackSerializer.Serialize(actual[index].MatchResult));
        }
        if (original is null) { Assert.IsNull(target.Spectrum); }
        else { CollectionAssert.AreEqual(original, target.Spectrum.Select(peak => (peak.Mass, peak.Intensity, peak.Comment)).ToArray()); }
    }

    [TestMethod]
    public void EmptyDatabaseDoesNotAccessTheQuery() {
        var database = new MoleculeDataBase(new List<MoleculeMsReference>(), "Empty", DataBaseSource.Msp, SourceType.MspDB, "");
        var search = new MsScanCompoundSearchUsecase();
        search.AddDataBase(database);
        search.SearchMethod = database;
        Assert.AreEqual(0, search.Search(null!).Count);
    }

    [TestMethod]
    public void EachSearchUsesTheUpdatedQueryAndCutoffs() {
        var target = new MSScanProperty { ChromXs = new ChromXs(), Spectrum = Peaks() };
        var reference = new MoleculeMsReference { Spectrum = Peaks() };
        var database = new MoleculeDataBase(new[] { reference }, "Test", DataBaseSource.Msp, SourceType.MspDB, "");
        var search = new MsScanCompoundSearchUsecase();
        search.AddDataBase(database);
        search.SearchMethod = database;
        Assert.IsTrue(search.Search(target)[0].MatchResult.MatchedPeaksCount > 0);
        search.SearchParameter.AbsoluteAmpCutoff = 2000;
        Assert.AreEqual(0, search.Search(target)[0].MatchResult.MatchedPeaksCount);
        search.SearchParameter.AbsoluteAmpCutoff = 0;
        target.Spectrum = new List<SpectrumPeak> { new SpectrumPeak(400, 100) };
        Assert.AreEqual(0, search.Search(target)[0].MatchResult.MatchedPeaksCount);
    }

    private static List<SpectrumPeak> Peaks() => [
        new SpectrumPeak(73, 1000, "base"), new SpectrumPeak(100, 100), new SpectrumPeak(147, 500), new SpectrumPeak(205, 10),
    ];

}
