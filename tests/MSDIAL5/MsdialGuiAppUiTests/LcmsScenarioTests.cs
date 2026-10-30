using CompMs.App.Msdial.UiTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.UiTests
{
    /// <summary>
    /// The path a person takes through an LC-MS project, end to end: create it, process it, look at
    /// the results, export them, close, and open the project again. It runs once per vendor format,
    /// because reading each format is its own code path that a release can break.
    /// </summary>
    [TestClass]
    [TestCategory("UI")]
    public class LcmsScenarioTests
    {
        // LC-MS/MS DDA runs from the demo data the console checks use, one dataset per vendor
        // format. Polarity comes from the file names, earlier parameter files in the folders or
        // the scans themselves; the peak heights are set below the intensities in each dataset.
        public static IEnumerable<object[]> Datasets => new[] {
            new RawDataset("mzML", "mzml", Polarity.Negative, 10,
                "Nega_Ida_QC_1_1.mzML", "Nega_Ida_QC_1_5.mzML", "Nega_Ida_QC_1_9.mzML"),
            new RawDataset("Sciex WIFF", "lipidomics_dda", Polarity.Negative, 1000,
                "20231113_feces_DDA_top30_NEG_1.wiff", "20231113_feces_DDA_top30_NEG_2.wiff", "20231113_feces_DDA_top30_NEG_3.wiff"),
            new RawDataset("Sciex WIFF2", @"sciex\wiff2", Polarity.Positive, 1000,
                "03  1.wiff2", "04  10.wiff2", "05  100.wiff2"),
            new RawDataset("Waters Xevo RAW", @"waters\xevo_dda", Polarity.Negative, 500,
                "R00_1_FDDAn.raw", "R00_2_FDDAn.raw", "R00_3_FDDAn.raw"),
            new RawDataset("Waters Premier RAW", @"waters\premier_dda_pos", Polarity.Positive, 50,
                "18Mar2016_7.raw"),
            new RawDataset("Agilent D", @"agilent\qrai", Polarity.Positive, 1000,
                "posHILIC_QRAI_75Da_ISTD.d"),
            new RawDataset("Shimadzu LCD", @"shimadzu\dda_plasma", Polarity.Positive, 1000,
                "DDA-Plasma_5ng-mL_010.lcd", "DDA-Plasma_25ng-mL_009.lcd", "DDA-Plasma_50ng-mL_008.lcd"),
            new RawDataset("Thermo RAW", @"thermo\lumos_polarityswitch", Polarity.Positive, 1000,
                "220511_st320_Lipidomics_03_st300_n1.raw"),
            new RawDataset("Hive HMD", "hive", Polarity.Positive, 1000,
                "Mx DDA CID POS Chiron_1.hmd"),
        }.Select(dataset => new object[] { dataset });

        public static string DatasetName(MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        [DynamicData(nameof(Datasets), DynamicDataDisplayName = nameof(DatasetName))]
        public void CreatesProcessesExportsAndReopensAProject(RawDataset dataset) {
            RunParameters.SkipUnlessSelected(TestContext, dataset);
            var exe = RunParameters.RequireMsdialExe(TestContext);
            var demoDataDir = RunParameters.RequireDemoDataDir(TestContext);
            var work = WorkDirectory.Prepare(TestContext, demoDataDir, dataset);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.CreateProjectAndRun(session, dataset, work, dataset.NeedsDragAndDrop(demoDataDir));
                Workflow.WaitForResults(session, dataset, "processing");
                Workflow.BrowseResults(session, dataset);
                if (Workflow.HasAlignment(dataset)) {
                    Workflow.ExportAlignment(session, Path.Combine(work, "export"));
                }
                session.Close();
            }

            var project = WorkDirectory.ProjectFile(work);

            using (var session = MsdialSession.Launch(TestContext, exe)) {
                Workflow.LoadProject(session, dataset, project);
                Workflow.BrowseResults(session, dataset);
                session.Close();
            }

            Directory.Delete(work, recursive: true);
        }
    }
}
