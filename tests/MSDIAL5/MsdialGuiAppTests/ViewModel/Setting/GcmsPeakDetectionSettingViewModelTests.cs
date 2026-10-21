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
        protected override ReactiveProperty<string> GetMassSliceWidth(GcmsPeakDetectionSettingViewModel viewModel) => viewModel.MassSliceWidth;
    }
}
