using CompMs.App.MsdialConsole.Process;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class MainProcessCommandTests
{
    [TestMethod]
    public void RtCorrection_IsTopLevelAndLegacyEicPathIsHidden() {
        var root = BuildRoot();

        Assert.IsNotNull(root.Subcommands.SingleOrDefault(command => command.Name == "rtcorrection"));
        var eic = root.Subcommands.Single(command => command.Name == "eic");
        var legacy = eic.Subcommands.Single(command => command.Name == "rtcorrection");
        Assert.IsTrue(legacy.Hidden);
        Assert.AreEqual(0, root.Parse(["rtcorrection", "--help"]).Errors.Count);
        Assert.AreEqual(0, root.Parse(["eic", "rtcorrection", "--help"]).Errors.Count);
    }

    [TestMethod]
    public void Root_DoesNotExposePartialFeatureInventoryCommands() {
        var root = BuildRoot();

        Assert.IsNull(root.Subcommands.SingleOrDefault(command => command.Name == "info"));
        Assert.IsTrue(root.Parse(["capabilities"]).Errors.Count > 0);
        Assert.IsTrue(root.Parse(["info"]).Errors.Count > 0);
    }

    [TestMethod]
    public void Msn_AcceptsStringInputPathWithoutValidatorCastError() {
        var root = BuildRoot();
        var existingPath = typeof(MainProcessCommandTests).Assembly.Location;

        var parseResult = root.Parse([
            "msn",
            "-i", existingPath,
            "-o", existingPath,
            "-m", existingPath,
        ]);

        Assert.AreEqual(0, parseResult.Errors.Count);
    }

    [TestMethod]
    public void Msn_HelpDoesNotRequireExecutionOptions() {
        var root = BuildRoot();

        var parseResult = root.Parse(["msn", "--help"]);

        Assert.AreEqual(0, parseResult.Errors.Count);
    }

    [TestMethod]
    public void Msn_ExposesSpectrumOptionWithShortAlias() {
        var root = BuildRoot();
        var msn = root.Subcommands.Single(command => command.Name == "msn");

        var spectrum = msn.Options.SingleOrDefault(option => option.Name.Contains("spectrum") || option.Aliases.Contains("--spectrum"));
        Assert.IsNotNull(spectrum);
        Assert.IsTrue(spectrum!.Aliases.Contains("-s"));
    }

    [TestMethod]
    public void Msn_HelpDescriptionsDocumentInputTypesAndOptionPrecedence() {
        var root = BuildRoot();
        var msn = root.Subcommands.Single(command => command.Name == "msn");

        var descriptions = msn.Options.ToDictionary(
            option => option.Aliases.FirstOrDefault(alias => alias.StartsWith("--"))?.TrimStart('-') ?? option.Name.TrimStart('-'),
            option => option.Description);

        StringAssert.Contains(descriptions["input"], ".msp");
        StringAssert.Contains(descriptions["spectrum"], ".dcl");
        StringAssert.Contains(descriptions["targetFile"], "MSP");
        StringAssert.Contains(descriptions["analysis-file"], "Takes precedence over --alignment");
        StringAssert.Contains(descriptions["alignment"], "Ignored when --analysis-file is specified");
        StringAssert.Contains(descriptions["all-edge-export"], "[default: false]");
        Assert.IsFalse(descriptions["ionmode"].Contains("Default:"));
    }

    private static RootCommand BuildRoot() {
        var root = new RootCommand("test");
        MainProcess.SetEicCommand(root);
        MainProcess.SetRtCorrectionCommand(root);
        MainProcess.SetMsnCommand(root);
        return root;
    }
}
