using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    /// <summary>
    /// Processes demo data from every vendor format and processing mode the console supports, and
    /// checks that each run finishes with peaks in every file. Reading a vendor format is its own
    /// code path, and the vendor libraries ship as their own package, so either can break a release
    /// without any change in this repository.
    /// </summary>
    [TestClass]
    [TestCategory("Release")]
    public class ProcessingTests
    {
        private static readonly Dictionary<string, string> NominalMass = new() {
            ["Accuracy type"] = "IsNominal",
            ["MS1 tolerance for centroid"] = "0.5",
            ["MS1 tolerance for alignment"] = "0.5",
        };

        // Paths are relative to the demo data directory. The method entries are what the data
        // needs beyond the template's defaults for its mode and ion mode.
        public static IEnumerable<ConsoleCase> Cases => new[] {
            // LC-MS
            new ConsoleCase("thermo_qe_dda", "lcms", IonMode.Negative, new[] { @"thermo\qe_dda\raw\CB1.raw", @"thermo\qe_dda\raw\CB2.raw" }),
            new ConsoleCase("thermo_qe_swath", "lcms", IonMode.Positive, new[] { @"thermo\qe_swath\DIA_Lipid_Pos_50Da.raw" }) {
                Method = new Dictionary<string, string> { ["Acquisition type"] = "SWATH", ["Target omics"] = "Lipidomics" },
            },
            new ConsoleCase("thermo_lumos_polarityswitch", "lcms", IonMode.Positive, new[] { @"thermo\lumos_polarityswitch\220511_st320_Lipidomics_03_st300_n1.raw" }),
            new ConsoleCase("thermo_ltq", "lcms", IonMode.Positive, new[] { @"thermo\ltq\raw\BERRY_POS_02282016_05.raw" }) {
                Method = new Dictionary<string, string> { ["MS2 data type"] = "Centroid" },
            },
            new ConsoleCase("sciex_wiff", "lcms", IonMode.Negative, new[] {
                @"sciex\wiff1\raw\0718_kinetex_wine_50_4min_neg_IDA_A1.wiff", @"sciex\wiff1\raw\0718_kinetex_wine_50_4min_neg_IDA_A1.wiff.scan",
                @"sciex\wiff1\raw\0718_kinetex_wine_50_4min_neg_IDA_B1.wiff", @"sciex\wiff1\raw\0718_kinetex_wine_50_4min_neg_IDA_B1.wiff.scan",
            }),
            new ConsoleCase("sciex_wiff2", "lcms", IonMode.Positive, new[] {
                @"sciex\wiff2\03  1.wiff2", @"sciex\wiff2\03  1.wiff.scan",
                @"sciex\wiff2\04  10.wiff2", @"sciex\wiff2\04  10.wiff.scan",
            }),
            new ConsoleCase("sciex_wiff2_samples", "lcms", IonMode.Positive, new[] {
                @"sciex\wiff2\samples  03_rucola.wiff2", @"sciex\wiff2\samples  03_rucola.wiff.scan",
                @"sciex\wiff2\samples  05_avocado.wiff2", @"sciex\wiff2\samples  05_avocado.wiff.scan",
            }),
            new ConsoleCase("sciex_ead", "lcms", IonMode.Positive, new[] {
                @"ead\20250723_LightSPLASHx0001_Na_NH4HCO3_DDA_EAD_POS_1.wiff", @"ead\20250723_LightSPLASHx0001_Na_NH4HCO3_DDA_EAD_POS_1.wiff.scan",
            }) {
                Method = new Dictionary<string, string> { ["Target omics"] = "Lipidomics", ["Solvent type"] = "NH4HCO3" },
            },
            new ConsoleCase("waters_xevo_dda", "lcms", IonMode.Negative, new[] { @"waters\xevo_dda\R00_1_FDDAn.raw", @"waters\xevo_dda\R00_2_FDDAn.raw" }),
            new ConsoleCase("waters_premier_dda", "lcms", IonMode.Positive, new[] { @"waters\premier_dda_pos\18Mar2016_7.raw" }),
            // MS1 base peaks here are in the tens, so the default height would find nothing.
            new ConsoleCase("waters_mse", "lcms", IonMode.Positive, new[] { @"waters\premier_mse_pos\19Sep16_A17-MU_r1_300_uM_Root.raw\19Sep16_A17-MU_r1_50_uM_Root.raw" }) {
                Method = new Dictionary<string, string> { ["Acquisition type"] = "AIF", ["Minimum peak height"] = "10" },
            },
            new ConsoleCase("agilent_lc", "lcms", IonMode.Positive, new[] { @"agilent\qrai\posHILIC_QRAI_75Da_ISTD.d" }),
            new ConsoleCase("shimadzu_dda", "lcms", IonMode.Positive, new[] { @"shimadzu\dda_plasma\DDA-Plasma_25ng-mL_009.lcd", @"shimadzu\dda_plasma\DDA-Plasma_50ng-mL_008.lcd" }),
            new ConsoleCase("shimadzu_dia", "lcms", IonMode.Negative, new[] { @"shimadzu\dia\P_DIAneg_35_15_1.lcd" }) {
                Method = new Dictionary<string, string> { ["Acquisition type"] = "SWATH" },
            },
            new ConsoleCase("bruker_qtof", "lcms", IonMode.Positive, new[] { @"bruker\swath\SRM 1950_21_01_1195.d" }) {
                Method = new Dictionary<string, string> { ["Acquisition type"] = "SWATH" },
            },
            // The intensities in these files peak in the hundreds, far below the default height.
            new ConsoleCase("mzml", "lcms", IonMode.Negative, new[] { @"mzml\Nega_Ida_QC_1_1.mzML", @"mzml\Nega_Ida_QC_1_5.mzML" }) {
                Method = new Dictionary<string, string> { ["Minimum peak height"] = "10" },
            },
            new ConsoleCase("hive_hmd", "lcms", IonMode.Positive, new[] { @"hive\Mx DDA CID POS Chiron_1.hmd" }),

            // LC-IM-MS, direct infusion and ion mobility
            new ConsoleCase("bruker_lcimms", "lcimms", IonMode.Positive, new[] {
                @"LC-MS\4D-Lipidmics_pos_5times_2uL-inj\kidney1_5tims_pos_P2-E-2_1_15262.d", @"LC-MS\4D-Lipidmics_pos_5times_2uL-inj\plasma1_5tims_pos_P2-F-2_1_15252.d",
            }) {
                Method = new Dictionary<string, string> { ["Target omics"] = "Lipidomics" },
            },
            new ConsoleCase("bruker_dims", "dims", IonMode.Negative, new[] {
                @"infusion\infusion\timsOFF_neg_20uLinj\kidney1_1-17_1_15397.d", @"infusion\infusion\timsOFF_neg_20uLinj\plasma1_1-2_1_15392.d",
            }),
            new ConsoleCase("bruker_imms", "imms", IonMode.Positive, new[] {
                @"infusion\infusion\timsON_pos_20uLinj\kidney1_1-17_1_15366.d", @"infusion\infusion\timsON_pos_20uLinj\plasma1_1-2_1_15361.d",
            }),

            // GC-MS
            new ConsoleCase("gcms_cdf_fiehnri", "gcms", IonMode.Positive, new[] { @"gcms\fiehnri\140428actsa25_1.cdf", @"gcms\fiehnri\140428actsa26_1.cdf" }) {
                Method = new Dictionary<string, string>(NominalMass) {
                    ["Retention type"] = "RI",
                    ["RI compound type"] = "Fames",
                    ["Alignment index type"] = "RI",
                    ["Retention index tolerance for alignment"] = "2000",
                    ["MSP file path"] = @"{DemoDataDir}\gcms\fiehnri\GCMS DB_FiehnBinbase-FiehnRI-VS2.msp",
                },
                RiCompoundFile = @"gcms\fiehnri\fames_rt_info.txt",
            },
            new ConsoleCase("gcms_netcdf", "gcms", IonMode.Positive, new[] { @"netcdf\210830_suntory-test.CDF" }) { Method = NominalMass },
            new ConsoleCase("gcms_agilent_masshunter", "gcms", IonMode.Positive, new[] { @"agilent\gcms\data\natane_chem2MHconverted.D" }) { Method = NominalMass },
            new ConsoleCase("gcms_shimadzu_qgd", "gcms", IonMode.Positive, new[] { @"shimadzu\gcms\scan_pest.QGD" }) { Method = NominalMass },
            new ConsoleCase("gcms_thermo_gcqe", "gcms", IonMode.Positive, new[] { @"thermo\gc-qe\2ppm_Pesticides_50-700_Standard_GC-A.raw" }),
            new ConsoleCase("gcms_abf", "gcms", IonMode.Positive, new[] { @"gcms\kovatsri\Alkane_mix.abf" }) {
                Method = NominalMass,
                KnownIssue = "RawDataHandler ships the ABF reader as lib/Reifycs/RDAM.dll while its assembly is named RDAM_DLL, so it loads only when the console is started from its own folder.",
            },
        };

        public static IEnumerable<object[]> CaseData => Cases.Select(consoleCase => new object[] { consoleCase });

        public static string CaseName(MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
        public void ProcessesDemoData(ConsoleCase consoleCase) {
            RunParameters.SkipUnlessSelected(TestContext, consoleCase);
            var exe = RunParameters.RequireConsoleExe(TestContext);
            var demoDataDir = RunParameters.RequireDemoDataDir(TestContext);
            if (consoleCase.KnownIssue is { } issue) {
                Assert.Inconclusive("Known issue: " + issue);
            }

            var directory = Path.Combine(Path.GetTempPath(), "MsdialConsoleReleaseTests", $"{consoleCase.Name}-{DateTime.Now:yyyyMMdd-HHmmss}");
            TestContext.WriteLine($"Working in {directory}. It is kept when the test fails.");
            var run = new ConsoleRun(TestContext, exe, demoDataDir, consoleCase, directory);
            run.Prepare();
            run.RunAndCheck();

            Directory.Delete(directory, recursive: true);
        }
    }
}
