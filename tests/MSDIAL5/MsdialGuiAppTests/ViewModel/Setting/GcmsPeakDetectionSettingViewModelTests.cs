using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    [TestClass]
    public class GcmsPeakDetectionSettingViewModelTests : ExclusionMassListInputTests<GcmsPeakDetectionSettingViewModel>
    {
        protected override GcmsPeakDetectionSettingViewModel CreateViewModel(PeakPickBaseParameter parameter, out IPeakDetectionSettingModel model) {
            var chromDecParameter = new ChromDecBaseParameter { AccuracyType = AccuracyType.IsAccurate, };
            var settingModel = new GcmsPeakDetectionSettingModel(new PeakPickBaseParameterModel(parameter), chromDecParameter, ProcessOption.All);
            model = settingModel;
            return new GcmsPeakDetectionSettingViewModel(settingModel, Observable.Return(true));
        }

        protected override void SetNewMass(GcmsPeakDetectionSettingViewModel viewModel, double mass) => viewModel.NewMass = mass;
        protected override void SetNewTolerance(GcmsPeakDetectionSettingViewModel viewModel, double tolerance) => viewModel.NewTolerance = tolerance;
        protected override ReactiveCommand GetAddCommand(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.AddCommand;
        protected override IReadOnlyList<MzSearchQueryViewModel> GetExcludedMassList(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.ExcludedMassList;
        protected override bool HasErrors(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.ObserveHasErrors.Value;
        protected override ReactiveProperty<string> GetMinimumAmplitude(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.MinimumAmplitude;
        protected override ReactiveProperty<string> GetMassSliceWidth(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.MassSliceWidth;
        protected override ReactiveProperty<string> GetSmoothingLevel(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.SmoothingLevel;
        protected override ReactiveProperty<string> GetPeakWidth(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.AveragePeakWidth;

        // The mass slice width and the centroid tolerance count only for accurate masses; a nominal run sets both to 0.5.
        [TestMethod]
        public void ASavedMassWidthBelowTheMinimumBlocksOnlyAnAccurateRun() {
            var parameter = new PeakPickBaseParameter { MassSliceWidth = 0.00005f, CentroidMs1Tolerance = 0.00005f, };
            var chromDecParameter = new ChromDecBaseParameter { AccuracyType = AccuracyType.IsNominal, };
            var settingModel = new GcmsPeakDetectionSettingModel(new PeakPickBaseParameterModel(parameter), chromDecParameter, ProcessOption.All);
            var viewModel = new GcmsPeakDetectionSettingViewModel(settingModel, Observable.Return(true));
            Assert.IsTrue(viewModel.MassSliceWidth.HasErrors);
            Assert.IsTrue(viewModel.MassAccuracy.HasErrors);
            Assert.IsFalse(viewModel.ObserveHasErrors.Value);

            viewModel.IsAccurateMS.Value = true;
            Assert.IsTrue(viewModel.ObserveHasErrors.Value);
        }
    }
}
