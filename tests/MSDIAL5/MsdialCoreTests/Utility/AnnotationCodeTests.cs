using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Utility.Tests;

/// <summary>
/// "Annotation tag (VS1.0)": an MS/MS match against a spectrum predicted in silico is 450, not the acquired-spectrum code
/// 430 (author decision D-C, 2026-10-28). Every other code is as it was.
/// </summary>
[TestClass]
public class AnnotationCodeTests
{
    [DataTestMethod]
    [DataRow(MachineCategory.LCMS)]
    [DataRow(MachineCategory.IFMS)]
    [DataRow(MachineCategory.IMMS)]
    [DataRow(MachineCategory.LCIMMS)]
    public void AnInSilicoMsmsMatchIs450AndAnAcquiredOneStays430(MachineCategory category) {
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(Match(AnnotationEvidenceSource.BySpectrumPredictionTool), category));
        Assert.AreEqual(430, DataAccess.GetAnnotationCode(Match(AnnotationEvidenceSource.ReferenceSpectrum), category));
        Assert.AreEqual(430, DataAccess.GetAnnotationCode(Match(AnnotationEvidenceSource.Unspecified), category),
            "a project saved before the evidence record existed reads as before");
    }

    [TestMethod]
    public void AnInSilicoCandidateWithoutASpectrumMatchKeepsItsCode() {
        var precursorOnly = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        precursorOnly.IsSpectrumMatch = false;
        Assert.AreEqual(530, DataAccess.GetAnnotationCode(precursorOnly, MachineCategory.LCMS));

        var nothing = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        nothing.IsSpectrumMatch = false;
        nothing.IsPrecursorMzMatch = false;
        Assert.AreEqual(999, DataAccess.GetAnnotationCode(nothing, MachineCategory.LCMS));
    }

    /// <summary>
    /// An in-silico match also supported by RT or CCS is 450 (author, 2026-10-28): 330/230/130 would claim an acquired
    /// reference spectrum, and 350/250/150 are not defined now. An acquired match with RT is 330 as before.
    /// </summary>
    [TestMethod]
    public void AnInSilicoMatchWithRetentionTimeStays450() {
        var predicted = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        predicted.IsRtMatch = true;
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(predicted, MachineCategory.LCMS));

        var acquired = Match(AnnotationEvidenceSource.ReferenceSpectrum);
        acquired.IsRtMatch = true;
        Assert.AreEqual(330, DataAccess.GetAnnotationCode(acquired, MachineCategory.LCMS));

        var mobility = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        mobility.IsRtMatch = true;
        mobility.IsCcsMatch = true;
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(mobility, MachineCategory.LCIMMS));
    }

    /// <summary>
    /// An in-silico hit on a lipid is 450 whatever lipid level is flagged, so it never falls into the 400-420 band of
    /// rule-based lipid annotation (author, 2026-10-28).
    /// </summary>
    [DataTestMethod]
    [DataRow(MachineCategory.LCMS)]
    [DataRow(MachineCategory.LCIMMS)]
    public void AnInSilicoLipidHitIs450WhateverItsLipidLevel(MachineCategory category) {
        var classLevel = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        classLevel.IsLipidClassMatch = true;
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(classLevel, category));

        var chainLevel = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        chainLevel.IsLipidChainsMatch = true;
        chainLevel.IsLipidClassMatch = true;
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(chainLevel, category));

        var positionLevel = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        positionLevel.IsLipidPositionMatch = true;
        positionLevel.IsRtMatch = true;
        Assert.AreEqual(450, DataAccess.GetAnnotationCode(positionLevel, category));
    }

    /// <summary>Rule-based lipid annotation (the LBM, recorded RuleBased) keeps the 400-420 band and its RT promotion.</summary>
    [TestMethod]
    public void ARuleBasedLipidKeepsItsCodes() {
        var position = Match(AnnotationEvidenceSource.RuleBased);
        position.IsLipidPositionMatch = true;
        Assert.AreEqual(400, DataAccess.GetAnnotationCode(position, MachineCategory.LCMS));

        var chains = Match(AnnotationEvidenceSource.RuleBased);
        chains.IsLipidChainsMatch = true;
        Assert.AreEqual(410, DataAccess.GetAnnotationCode(chains, MachineCategory.LCMS));

        var lipidClass = Match(AnnotationEvidenceSource.RuleBased);
        lipidClass.IsLipidClassMatch = true;
        Assert.AreEqual(420, DataAccess.GetAnnotationCode(lipidClass, MachineCategory.LCMS));

        var noLevel = Match(AnnotationEvidenceSource.RuleBased);
        Assert.AreEqual(430, DataAccess.GetAnnotationCode(noLevel, MachineCategory.LCMS));

        lipidClass.IsRtMatch = true;
        Assert.AreEqual(320, DataAccess.GetAnnotationCode(lipidClass, MachineCategory.LCMS));
    }

    /// <summary>
    /// Peptides are out of scope: LcmsFastaAnnotator records the b/y ladder as BySpectrumPredictionTool, and a FASTA result
    /// still takes the acquired-spectrum codes, with their RT and CCS promotions.
    /// </summary>
    [DataTestMethod]
    [DataRow(MachineCategory.LCMS)]
    [DataRow(MachineCategory.LCIMMS)]
    public void APeptideIsNotReportedAs450(MachineCategory category) {
        var peptide = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        peptide.Source = SourceType.FastaDB;
        Assert.AreEqual(430, DataAccess.GetAnnotationCode(peptide, category));

        peptide.IsRtMatch = true;
        Assert.AreEqual(330, DataAccess.GetAnnotationCode(peptide, category));
    }

    [TestMethod]
    public void GcmsIsNotTouched() {
        var predicted = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        Assert.AreEqual(440, DataAccess.GetAnnotationCode(predicted, MachineCategory.GCMS));
        predicted.IsRiMatch = true;
        Assert.AreEqual(340, DataAccess.GetAnnotationCode(predicted, MachineCategory.GCMS));
    }

    private static MsScanMatchResult Match(AnnotationEvidenceSource evidence) => new() {
        Source = SourceType.MspDB,
        IsPrecursorMzMatch = true,
        IsSpectrumMatch = true,
        IsReferenceMatched = true,
        EvidenceSource = evidence,
    };
}
