using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Lcms {
    sealed class LcmsFastaAnnotationSettingViewModel : ViewModelBase {
        public LcmsFastaAnnotationSettingViewModel(FastaAnnotationSettingModel other) {
            model = new LcmsFastaAnnotationSettingModel(other);
            ParameterVM = new MsRefSearchParameterBaseViewModel(other.MsRefSearchParameter).AddTo(Disposables);
            ProteomicsParameterVM = new ProteomicsParameterVM(other.ProteomicsParameter).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = Observable.Return("LcmsFastaAnnotator").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.Ms2Tolerance.ObserveHasErrors,
                ParameterVM.RtTolerance.ObserveHasErrors,
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
                ProteomicsParameterVM.AndromedaDelta.ObserveHasErrors,
                ProteomicsParameterVM.AndromedaMaxPeaks.ObserveHasErrors,
                ProteomicsParameterVM.FalseDiscoveryRateForPeptide.ObserveHasErrors,
                ProteomicsParameterVM.FalseDiscoveryRateForProtein.ObserveHasErrors
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly LcmsFastaAnnotationSettingModel model;

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        public ProteomicsParameterVM ProteomicsParameterVM { get; }
        public ProteomicsParameter ProteomicsParam { get => model.ProteomicsParameter; }
        public List<Enzyme> Enzymes { get => ProteomicsParam.EnzymesForDigestion; }
        public int MaxMissedCleavage { get => ProteomicsParam.MaxMissedCleavage; }
        public List<Modification> VariableModifications { get => ProteomicsParam.VariableModifications; }
        public List<Modification> FixedModifications { get => ProteomicsParam.FixedModifications; }
        public int MaxNumberOfModificationsPerPeptide { get => ProteomicsParam.MaxNumberOfModificationsPerPeptide; }

        public IAnnotationSettingModel Model => model;

        //ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public ReactivePropertySlim<string> AnnotatorID { get; }

        public ReadOnlyReactivePropertySlim<string> Label { get; }

        public DelegateCommand EnzymeSetCommand => enzymeSetCommand ?? (enzymeSetCommand = new DelegateCommand(EnzymeSet));
        private DelegateCommand enzymeSetCommand;

        private void EnzymeSet() {
            using (var vm = new EnzymeSettingViewModel(ProteomicsParam)) {
                var window = new EnzymeSettingWin {
                    DataContext = vm,
                };
                window.ShowDialog();
            }
        }
    }
}
