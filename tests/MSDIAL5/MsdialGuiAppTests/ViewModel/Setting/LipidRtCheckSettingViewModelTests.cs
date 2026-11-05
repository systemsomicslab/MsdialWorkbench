using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>The "Re-run lipid RT check" window: what the person picks is what runs, and Run runs the check.</summary>
    [TestClass]
    public class LipidRtCheckSettingViewModelTests
    {
        [TestMethod]
        public async Task ThePickedSettingsAreWhatRuns() {
            var parameter = LipidRtTestSpots.Lipidomics();
            var spots = LipidRtTestSpots.Spots();
            var saves = 0;
            var model = new LipidRtCheckSettingModel(parameter, spots.Select(s => new AlignmentSpotPropertyModel(s)).ToList(), () => { saves++; return Task.CompletedTask; }, new UndoManager());
            using var viewModel = new LipidRtCheckSettingViewModel(model);

            CollectionAssert.AreEqual(new[] { LipidRtOperatingPoint.Strict, LipidRtOperatingPoint.HighPrecision, LipidRtOperatingPoint.Mid, LipidRtOperatingPoint.Balanced }, viewModel.OperatingPoints.ToArray());
            CollectionAssert.AreEqual(new[] { LipidRtPlausibilityProfileKind.None, LipidRtPlausibilityProfileKind.Mammalian }, viewModel.PlausibilityProfiles.ToArray());
            Assert.AreEqual(LipidRtClassLevelPolicy.Corroborated, viewModel.ClassLevelPolicy.Value);
            Assert.IsFalse(viewModel.CannotRun);
            Assert.IsTrue(viewModel.RunCommand.CanExecute());

            viewModel.PlausibilityProfile.Value = LipidRtPlausibilityProfileKind.Mammalian;
            viewModel.MinorDuplicatePolicy.Value = LipidRtMinorDuplicatePolicy.Block;
            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, model.PlausibilityProfile);

            await viewModel.RunCommand.ExecuteAsync();

            Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, parameter.LipidRtCheckParam.PlausibilityProfile);
            Assert.AreEqual(LipidRtMinorDuplicatePolicy.Block, parameter.LipidRtCheckParam.MinorDuplicatePolicy);
            Assert.AreEqual(1, saves);
            Assert.IsTrue(LipidRtTestSpots.Find(spots, 52, 1).TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            StringAssert.StartsWith(viewModel.Summary.Value, "Lipid RT check on 22 spots: ");
            Assert.IsFalse(viewModel.IsRunning.Value);
        }

        [TestMethod]
        public void RunIsDisabledWhereTheCheckCannotRun() {
            var parameter = LipidRtTestSpots.Lipidomics();
            parameter.TargetOmics = TargetOmics.Proteomics;
            var model = new LipidRtCheckSettingModel(parameter, new AlignmentSpotPropertyModel[0], () => Task.CompletedTask, new UndoManager());
            using var viewModel = new LipidRtCheckSettingViewModel(model);

            Assert.IsTrue(viewModel.CannotRun);
            StringAssert.Contains(viewModel.CannotRunReason, "lipidomics projects only");
            Assert.IsFalse(viewModel.RunCommand.CanExecute());
        }
    }
}
