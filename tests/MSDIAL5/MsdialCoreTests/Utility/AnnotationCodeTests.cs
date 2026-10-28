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
    /// No code was decided for an in-silico match also supported by RT; 330 would claim an acquired reference spectrum, so
    /// it reports 450. An acquired match with RT is 330 as before.
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

    [TestMethod]
    public void ALipidLevelResolvedByTheRulesKeepsItsCode() {
        var predicted = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        predicted.IsLipidClassMatch = true;
        Assert.AreEqual(420, DataAccess.GetAnnotationCode(predicted, MachineCategory.LCMS));
    }

    [TestMethod]
    public void APeptideKeeps430() {
        var peptide = Match(AnnotationEvidenceSource.BySpectrumPredictionTool);
        peptide.Source = SourceType.FastaDB;
        Assert.AreEqual(430, DataAccess.GetAnnotationCode(peptide, MachineCategory.LCMS));
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
