using CompMs.App.Msdial.Lipidomics;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public sealed class ImmsLbmAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel 
    {
        public ImmsLbmAnnotationSettingViewModel(DataBaseAnnotationSettingModelBase other, ParameterBase parameter) {
            model = new ImmsLbmAnnotationSettingModel(other, parameter);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.Parameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("ImmsLbmAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LipidQueryContainer = parameter.LipidQueryContainer;
            IonMode = parameter.IonMode;
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.Ms2Tolerance.ObserveHasErrors,
                ParameterVM.CcsTolerance.ObserveHasErrors,
                ParameterVM.RelativeAmpCutoff.ObserveHasErrors,
                ParameterVM.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterVM.MassRangeBegin.ObserveHasErrors,
                ParameterVM.MassRangeEnd.ObserveHasErrors,
                ParameterVM.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterVM.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterVM.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterVM.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterVM.MinimumSpectrumMatch.ObserveHasErrors,
                ParameterVM.TotalScoreCutoff.ObserveHasErrors,
                ParameterVM.IsUseCcsForAnnotationFiltering.ObserveHasErrors,
                ParameterVM.IsUseCcsForAnnotationScoring.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly ImmsLbmAnnotationSettingModel model;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public LipidQueryBean LipidQueryContainer { get; }

        public IonMode IonMode { get; }

        public IAnnotationSettingModel Model => model;

        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public ReactivePropertySlim<string> AnnotatorID { get; }

        public ReadOnlyReactivePropertySlim<string> Label { get; }

        public DelegateCommand LipidDBSetCommand => lipidDBSetCommand ?? (lipidDBSetCommand = new DelegateCommand(LipidDBSet));
        private DelegateCommand lipidDBSetCommand;

        private void LipidDBSet() {
            using (var vm = new LipidDbSetVM(LipidQueryContainer, IonMode)) {
                var window = new LipidDbSetWindow
                {
                    DataContext = vm,
                };
                window.ShowDialog();
            }
        }
    }
}
