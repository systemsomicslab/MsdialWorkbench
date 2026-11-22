using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System.IO;

namespace Riken.Metabolomics.MsfinderCommon.Tests
{
    /// <summary>
    /// Characterization of <see cref="MsFinderIniParcer.Write"/> as it was before the diagnostic-ion rule
    /// settings existed (design 6.4 item 3). The goldens were taken on that code; the settings file and the
    /// GUI batch parameter file must keep these bytes while the new settings are absent or at their defaults.
    /// </summary>
    /// <remarks>
    /// The full-settings golden equals its input in every value but one: the reader has no pattern for
    /// CanExcuteMS1AdductSearch, which the writer writes, so it comes back False. That is recorded as it is.
    /// </remarks>
    [TestClass]
    public class IniCharacterizationTests
    {
        [TestMethod]
        public void WriteOfDefaultParametersMatchesGolden() {
            var output = IniTestFiles.NewTempFile();
            MsFinderIniParcer.Write(new AnalysisParamOfMsfinder(), output);
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("Default.ini")), output, "Write(default)");
        }

        [TestMethod]
        public void ReadThenWriteOfDefaultGoldenIsStable() {
            var output = IniTestFiles.NewTempFile();
            MsFinderIniParcer.Write(MsFinderIniParcer.Read(IniTestFiles.Golden("Default.ini")), output);
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("Default.ini")), output, "Write(Read(default golden))");
        }

        [TestMethod]
        public void ReadThenWriteOfFullSettingsMatchesGolden() {
            var output = IniTestFiles.NewTempFile();
            MsFinderIniParcer.Write(MsFinderIniParcer.Read(IniTestFiles.Input("FullSettings.ini")), output);
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettings.ini")), output, "Write(Read(full settings))");
        }

        [TestMethod]
        public void ReadThenWriteOfFullSettingsWithUnknownKeysMatchesGolden() {
            var output = IniTestFiles.NewTempFile();
            MsFinderIniParcer.Write(MsFinderIniParcer.Read(IniTestFiles.Input("FullSettingsWithUnknownKeys.ini")), output);
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettingsWithUnknownKeys.ini")), output, "Write(Read(full settings with unknown keys))");
        }
    }
}
