using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class IsotopeTrackSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public IsotopeTrackSettingViewModel(IsotopeTrackSettingModel model, IObservable<bool> isEnabled) {
            Model = model;
            IsReadOnly = model.IsReadOnly;

            TrackingIsotopeLabels = model.ToReactivePropertySlimAsSynchronized(m => m.TrackingIsotopeLabels).AddTo(Disposables);
            NonLabeledReference = model.ToReactivePropertySlimAsSynchronized(m => m.NonLabeledReference).AddTo(Disposables);
            UseTargetFormulaLibrary = model.ToReactivePropertySlimAsSynchronized(m => m.UseTargetFormulaLibrary).AddTo(Disposables);
            IsotopeTextDBFilePath = model.ToReactivePropertyAsSynchronized(m => m.IsotopeTextDBFilePath).AddTo(Disposables);
            SetFullyLabeledReferenceFile = model.ToReactivePropertySlimAsSynchronized(m => m.SetFullyLabeledReferenceFile).AddTo(Disposables);
            FullyLabeledReference = model.ToReactivePropertySlimAsSynchronized(m => m.FullyLabeledReference).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = Observable.Return(false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveChanges = new[]
            {
                TrackingIsotopeLabels.ToUnit(),
                NonLabeledReference.ToUnit(),
                UseTargetFormulaLibrary.ToUnit(),
                IsotopeTextDBFilePath.ToUnit(),
                SetFullyLabeledReferenceFile.ToUnit(),
                FullyLabeledReference.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = new[]
            {
                ObserveChanges.TakeFirstAfterEach(decide).ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public IsotopeTrackSettingModel Model { get; }

        public bool IsReadOnly { get; }

        public ReactivePropertySlim<bool> TrackingIsotopeLabels { get; }

        public IsotopeTrackingDictionary IsotopeTrackingDictionary => Model.IsotopeTrackingDictionary;

        public ReactivePropertySlim<AnalysisFileBean?> NonLabeledReference { get; }

        public ReactivePropertySlim<bool> UseTargetFormulaLibrary { get; }

        public ReactiveProperty<string> IsotopeTextDBFilePath { get; }

        public ReactivePropertySlim<bool> SetFullyLabeledReferenceFile { get; }

        public ReactivePropertySlim<AnalysisFileBean?> FullyLabeledReference { get; }

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles => Model.AnalysisFiles;

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        private readonly Subject<Unit> decide;
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            return null;
        }
    }
}
