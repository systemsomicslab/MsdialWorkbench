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

    private static RootCommand BuildRoot() {
        var root = new RootCommand("test");
        MainProcess.SetEicCommand(root);
        MainProcess.SetRtCorrectionCommand(root);
        return root;
    }
}
