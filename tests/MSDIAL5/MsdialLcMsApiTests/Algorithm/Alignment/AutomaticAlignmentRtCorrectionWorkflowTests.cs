using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests;

/// <summary>
/// The settings check and the anchor library reading the Console and the GUI share.
/// </summary>
[TestClass]
public class AutomaticAlignmentRtCorrectionWorkflowTests
{
    private const string Header = "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n";

    private static MsdialLcmsParameter Parameter(AutomaticRtCorrectionAnchorMode mode, string library) {
        var parameter = new MsdialLcmsParameter();
        parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute = true;
        parameter.AlignmentBaseParam.AutomaticRtCorrection.AnchorMode = mode;
        parameter.AlignmentBaseParam.AutomaticRtCorrection.AnchorLibraryFilePath = library;
        return parameter;
    }

    private static string Library(string body) {
        var path = Path.Combine(Path.GetTempPath(), "msdial-anchors-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, Header + body);
        return path;
    }

    [TestMethod]
    public void BothRtCorrectionsTogetherAreRefused() {
        var parameter = Parameter(AutomaticRtCorrectionAnchorMode.Automatic, string.Empty);
        parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = true;
        StringAssert.Contains(AutomaticAlignmentRtCorrectionWorkflow.ExclusiveWithLegacyRtCorrection(parameter), "correct the RT axis twice");
        StringAssert.Contains(AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(parameter), "correct the RT axis twice");
    }

    [TestMethod]
    public void NothingIsCheckedWhileTheCorrectionIsOff() {
        var parameter = Parameter(AutomaticRtCorrectionAnchorMode.UserOnly, "missing.txt");
        parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute = false;
        parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = true;
        Assert.IsNull(AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(parameter));
    }

    [TestMethod]
    public void ALibraryTheConsoleRefusesIsRefusedHereToo() {
        var path = Library("IS1\t2\t1\t200.1\t0.01\t1000\n");
        try {
            var message = AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(Parameter(AutomaticRtCorrectionAnchorMode.UserOnly, path));
            StringAssert.StartsWith(message, "Automatic RT correction anchor library could not be used:");
            var reporter = new CollectingAutomaticRtCorrectionReporter();
            Assert.IsNull(AutomaticAlignmentRtCorrectionWorkflow.PrepareInputs(Parameter(AutomaticRtCorrectionAnchorMode.UserOnly, path), reporter));
            Assert.AreEqual(message, reporter.Errors.Single());
        }
        finally {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void SettingsTheConsoleRefusesAreRefusedHereToo() {
        var parameter = Parameter(AutomaticRtCorrectionAnchorMode.Automatic, string.Empty);
        parameter.AlignmentBaseParam.AutomaticRtCorrection.MinimumAnchorCount = 1;
        StringAssert.StartsWith(AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(parameter), "Automatic alignment RT correction settings cannot be used:");
    }

    [TestMethod]
    public void AUsableLibraryIsReadWithItsNameAndChecksumAndReported() {
        var path = Library("IS1\t2\t1\t200.1\t0.01\t1000\tTrue\nIS2\t5\t1\t300.2\t0.01\t1000\tTrue\nIS3\t8\t1\t400.3\t0.01\t1000\tTrue\nX\t6\t0.2\t500\t0.01\t0\tFalse\n");
        try {
            var parameter = Parameter(AutomaticRtCorrectionAnchorMode.Hybrid, path);
            Assert.IsNull(AutomaticAlignmentRtCorrectionWorkflow.CheckSettings(parameter));
            var reporter = new CollectingAutomaticRtCorrectionReporter();
            var inputs = AutomaticAlignmentRtCorrectionWorkflow.PrepareInputs(parameter, reporter);
            Assert.IsNotNull(inputs);
            Assert.AreEqual(4, inputs.AnchorLibrary.Count);
            Assert.AreEqual(Path.GetFileName(path), inputs.AnchorLibraryName);
            Assert.AreEqual(64, inputs.AnchorLibrarySha256.Length);
            StringAssert.Contains(reporter.Lines[0].Line, "anchor mode Hybrid: 3 user anchor(s) and 1 exclusion(s)");
            Assert.IsFalse(reporter.Errors.Any());
        }
        finally {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ALibraryLeftInPlaceInAutomaticModeIsWarnedAboutAndNotRead() {
        var parameter = Parameter(AutomaticRtCorrectionAnchorMode.Automatic, "left-over.txt");
        var reporter = new CollectingAutomaticRtCorrectionReporter();
        var inputs = AutomaticAlignmentRtCorrectionWorkflow.PrepareInputs(parameter, reporter);
        Assert.AreSame(AutomaticRtCorrectionInputs.None, inputs);
        StringAssert.Contains(reporter.Lines.Single().Line, "the anchor mode is Automatic, so the library is not used");
        Assert.IsTrue(reporter.HasWarnings);
    }
}
