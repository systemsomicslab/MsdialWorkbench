using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class IsotopeTrackSettingViewModel : ViewModelBase
    {
        public IsotopeTrackSettingViewModel(IsotopeTrackSettingModel model) {
            Model = model;

            TrackingIsotopeLabels = model.ToReactivePropertySlimAsSynchronized(m => m.TrackingIsotopeLabels).AddTo(Disposables);
            NonLabeledReference = model.ToReactivePropertySlimAsSynchronized(m => m.NonLabeledReference).AddTo(Disposables);
            UseTargetFormulaLibrary = model.ToReactivePropertySlimAsSynchronized(m => m.UseTargetFormulaLibrary).AddTo(Disposables);
            IsotopeTextDBFilePath = model.ToReactivePropertyAsSynchronized(m => m.IsotopeTextDBFilePath).AddTo(Disposables);
            SetFullyLabeledReferenceFile = model.ToReactivePropertySlimAsSynchronized(m => m.SetFullyLabeledReferenceFile).AddTo(Disposables);
            FullyLabeledReference = model.ToReactivePropertySlimAsSynchronized(m => m.FullyLabeledReference).AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> TrackingIsotopeLabels { get; }

        public IsotopeTrackingDictionary IsotopeTrackingDictionary => Model.IsotopeTrackingDictionary;

        public ReactivePropertySlim<AnalysisFileBean> NonLabeledReference { get; }

        public ReactivePropertySlim<bool> UseTargetFormulaLibrary { get; }

        public ReactiveProperty<string> IsotopeTextDBFilePath { get; }

        public ReactivePropertySlim<bool> SetFullyLabeledReferenceFile { get; }

        public ReactivePropertySlim<AnalysisFileBean> FullyLabeledReference { get; }

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles => Model.AnalysisFiles;

        public IsotopeTrackSettingModel Model { get; }
    }
}
