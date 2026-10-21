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
    public class PeakDetectionSettingViewModelTests : ExclusionMassListInputTests<PeakDetectionSettingViewModel>
    {
        protected override PeakDetectionSettingViewModel CreateViewModel(PeakPickBaseParameter parameter, out IPeakDetectionSettingModel model) {
            var settingModel = new PeakDetectionSettingModel(new PeakPickBaseParameterModel(parameter), ProcessOption.All);
            model = settingModel;
            return new PeakDetectionSettingViewModel(settingModel, Observable.Return(true));
        }

        protected override void SetNewMass(PeakDetectionSettingViewModel viewModel, double mass) => viewModel.NewMass = mass;
        protected override void SetNewTolerance(PeakDetectionSettingViewModel viewModel, double tolerance) => viewModel.NewTolerance = tolerance;
        protected override ReactiveCommand GetAddCommand(PeakDetectionSettingViewModel viewModel) => viewModel.AddCommand;
        protected override IReadOnlyList<MzSearchQueryViewModel> GetExcludedMassList(PeakDetectionSettingViewModel viewModel) => viewModel.ExcludedMassList;
        protected override bool HasErrors(PeakDetectionSettingViewModel viewModel) => viewModel.ObserveHasErrors.Value;
        protected override ReactiveProperty<string> GetMinimumAmplitude(PeakDetectionSettingViewModel viewModel) => viewModel.MinimumAmplitude;
        protected override ReactiveProperty<string> GetMassSliceWidth(PeakDetectionSettingViewModel viewModel) => viewModel.MassSliceWidth;
        protected override ReactiveProperty<string> GetSmoothingLevel(PeakDetectionSettingViewModel viewModel) => viewModel.SmoothingLevel;
        protected override ReactiveProperty<string> GetPeakWidth(PeakDetectionSettingViewModel viewModel) => viewModel.MinimumDatapoints;
    }
}
