using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Lcms.Tests
{
    /// <summary>
    /// "Re-run lipid RT check" as the LC-MS method opens it on a loaded alignment result: with the project's databases, so a
    /// match from a library of predicted spectra is not evaluated there, as it is not in processing (Q5).
    /// </summary>
    /// <remarks>
    /// The setting model's own tests hand it their databases; this one goes through the method, which is where a project's
    /// databases could be dropped without any other test noticing.
    /// </remarks>
    [TestClass]
    public class LcmsMethodModelLipidRtCheckTests
    {
        private const string PredictedAnnotator = "glacier_lipid";

        private string _folder = string.Empty;

        [TestInitialize]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-lcms-lipidrt-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            TestApplication.Ensure();
        }

        [TestCleanup]
        public void Cleanup() {
            TestApplication.WaitForBackgroundWork();
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod]
        public async Task TheRerunFromTheMethodDoesNotEvaluateAMatchFromTheProjectsPredictedLibrary() {
            var tg521 = await RunFromTheMethod(LipidRtTestSpots.WithPredictedLibrary(PredictedAnnotator));

            Assert.AreEqual(LipidRtOutcome.NotEvaluable, tg521.Verdict.Outcome);
            Assert.AreEqual(LipidRtReason.Ineligible, tg521.Verdict.Reason);
            StringAssert.Contains(tg521.Verdict.Evidence, "in silico");
            Assert.IsFalse(tg521.Confirmed);
            Assert.IsTrue(tg521.NeighbourConfirmed, "the LBM-named neighbour is still confirmed: the run evaluated the result");
        }

        [TestMethod]
        public async Task WithoutThePredictedLibraryTheSameMatchIsConfirmed() {
            // The control: the match's own record says RuleBased, as the LBM's does, so only the databases keep it out.
            var tg521 = await RunFromTheMethod(DataBaseStorage.CreateEmpty());

            Assert.AreEqual(LipidRtOutcome.Confirmed, tg521.Verdict.Outcome);
            Assert.IsTrue(tg521.Confirmed);
        }

        private sealed class Observed
        {
            public LipidRtVerdict Verdict = null!;
            public bool Confirmed;
            public bool NeighbourConfirmed;
        }

        /// <summary>
        /// Saves the TG grid as an alignment result whose TG 52:1 was named by <see cref="PredictedAnnotator"/>, loads it in an
        /// LC-MS method over <paramref name="dataBases"/>, and runs the check from the setting model the method opens.
        /// </summary>
        private async Task<Observed> RunFromTheMethod(DataBaseStorage dataBases) {
            var spots = LipidRtTestSpots.Spots();
            LipidRtTestSpots.MatchFromLibrary(LipidRtTestSpots.Find(spots, 52, 1), PredictedAnnotator);
            var alignmentFile = new AlignmentFileBean
            {
                FileID = 0,
                FileName = "alignment",
                FilePath = Path.Combine(_folder, "alignment.arf2"),
                EicFilePath = Path.Combine(_folder, "alignment.EIC.aef"),
                SpectraFilePath = Path.Combine(_folder, "alignment.dcl"),
            };
            new AlignmentResultContainer { AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(spots), }.Save(alignmentFile);

            var broker = new MessageBroker();
            var failures = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(failures.Add);
            LcmsMethodModel? method = null;
            LipidRtCheckSettingModel? setting = null;
            try {
                Application.Current.Dispatcher.Invoke(() => {
                    method = CreateMethod(dataBases, _folder, broker);
                    var model = new AlignmentFileBeanModel(alignmentFile, new List<AnalysisFileBean>());
                    method.LoadAlignmentFileAsync(model, CancellationToken.None).Wait();
                    setting = method.ShowLipidRtCheckSettingView();
                });
                Assert.IsNotNull(setting, "an alignment result is loaded: " + string.Join("; ", failures.Select(f => f.Content)));

                var run = await setting!.RunAsync();
                Assert.IsNotNull(run);
                Assert.IsTrue(run!.Ran, run.Describe());

                var loaded = method!.AlignmentModel!.Ms1Spots.Select(spot => spot.innerModel).ToList();
                var tg521 = LipidRtTestSpots.Find(loaded, 52, 1);
                return new Observed
                {
                    Verdict = tg521.LipidRtVerdict,
                    Confirmed = tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
                    NeighbourConfirmed = LipidRtTestSpots.Find(loaded, 52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED),
                };
            }
            finally {
                Application.Current.Dispatcher.Invoke(() => method?.Dispose());
            }
        }

        // An LC-MS lipidomics method with no sample, over the given databases, in the given project folder.
        private static LcmsMethodModel CreateMethod(DataBaseStorage dataBases, string folder, IMessageBroker broker) {
            var parameter = new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics, IonMode = IonMode.Positive, MachineCategory = MachineCategory.LCMS, };
            parameter.ProjectParam.ProjectFolderPath = folder;
            var storage = new MsdialLcmsDataStorage
            {
                MsdialLcmsParameter = parameter,
                AnalysisFiles = new List<AnalysisFileBean>(),
                DataBases = dataBases,
                DataBaseMapper = new DataBaseMapper(),
            };
            var analysisFiles = new AnalysisFileBeanModelCollection();
            var alignmentFiles = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), new List<AnalysisFileBean>());
            return new LcmsMethodModel(analysisFiles, alignmentFiles, storage, new NoRawData(), new FilePropertiesModel(parameter.ProjectParam), new StudyContextModel(parameter.ProjectParam), broker);
        }

        private sealed class NoRawData : IDataProviderFactory<AnalysisFileBean>
        {
            public IDataProvider Create(AnalysisFileBean source) => throw new AssertFailedException("The raw data were asked for.");
        }
    }
}
