using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO;
using System.ComponentModel.DataAnnotations;
using Accord.Math;
using CompMs.App.Msdial.Model.Service;
using Reactive.Bindings.Notifiers;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public sealed class RiDictionaryViewModel : ViewModelBase {
        private readonly RiDictionaryModel _model;
        private readonly IMessageBroker _broker;
        private readonly Subject<string> _fileSelect;

        public RiDictionaryViewModel(RiDictionaryModel model, IMessageBroker broker) {
            _model = model;
            _broker = broker;
            _fileSelect = new Subject<string>().AddTo(Disposables);
            SelectDictionaryCommand = new ReactiveCommand().WithSubscribe(SelectFile).AddTo(Disposables);
            DictionaryPath = model.ToReactivePropertyAsSynchronized(
                m => m.DictionaryPath,
                op => op.Merge(_fileSelect),
                op => op,
                ignoreValidationErrorValue: true
            ).AddTo(Disposables);
        }

        public string FilePath => _model.File;
        public string FileName => Path.GetFileName(_model.File);
        [Required(ErrorMessage = "Dictionary file is required")]
        [PathExists(ErrorMessage = "Dictionary file does not exist.", IsFile = true)]
        public ReactiveProperty<string> DictionaryPath { get; }

        public ReactiveCommand SelectDictionaryCommand { get; }

        private void SelectFile() {
            var request = new OpenFileRequest(_fileSelect.OnNext);
            _broker.Publish(request);
        }
    }
    
    public sealed class GcmsIdentificationSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly Subject<Unit> _decide;

        public GcmsIdentificationSettingViewModel(GcmsIdentificationSettingModel model, IMessageBroker broker, IObservable<bool> isEnabled) {
            UseRI = model.ToReactivePropertySlimAsSynchronized(
                m => m.RetentionType,
                op => op.Select(p => p == RetentionType.RI),
                op => op.Where(p => p).ToConstant(RetentionType.RI)
            ).AddTo(Disposables);
            UseRT = model.ToReactivePropertySlimAsSynchronized(
                m => m.RetentionType,
                op => op.Select(p => p == RetentionType.RT),
                op => op.Where(p => p).ToConstant(RetentionType.RT)
            ).AddTo(Disposables);

            RetentionIndexFiles = model.RetentionIndexFiles.ToReadOnlyReactiveCollection(m => new RiDictionaryViewModel(m, broker)).AddTo(Disposables);
            IsIndexImported = RetentionIndexFiles.Select(ri => ri.ObserveErrorInfo(vm => vm.DictionaryPath).ToUnit().StartWith(Unit.Default).Select(_ => ri.ContainsError(nameof(RiDictionaryViewModel.DictionaryPath))))
                .CombineLatestValuesAreAllFalse()
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            UseAlkanes = model.ToReactivePropertySlimAsSynchronized(
                m => m.CompoundType,
                op => op.Select(p => p == RiCompoundType.Alkanes),
                op => op.Where(p => p).ToConstant(RiCompoundType.Alkanes)
            ).AddTo(Disposables);
            UseFAMEs = model.ToReactivePropertySlimAsSynchronized(
                m => m.CompoundType,
                op => op.Select(p => p == RiCompoundType.Fames),
                op => op.Where(p => p).ToConstant(RiCompoundType.Fames)
            ).AddTo(Disposables);
            MspFilePath = model.ToReactivePropertyAsSynchronized(m => m.MspFilePath)
                .SetValidateAttribute(() => MspFilePath).AddTo(Disposables);
            SearchParameter = new MsRefSearchParameterBaseViewModel(model.SearchParameter).AddTo(Disposables);
            UseQuantMassesDefinedInMsp = model.ToReactivePropertySlimAsSynchronized(m => m.UseQuantmassDefinedInLibrary).AddTo(Disposables);
            OnlyReportTopHit = model.ToReactivePropertySlimAsSynchronized(m => m.OnlyReportTopHit).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MspFilePath.ObserveHasErrors,
                SearchParameter.RiTolerance.ObserveHasErrors,
                SearchParameter.RtTolerance.ObserveHasErrors,
                SearchParameter.Ms1Tolerance.ObserveHasErrors,
                SearchParameter.WeightedDotProductCutOff.ObserveHasErrors,
                SearchParameter.TotalScoreCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAllTrue()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                UseRI.ToUnit(),
                UseRT.ToUnit(),
                IsIndexImported.ToUnit(),
                UseAlkanes.ToUnit(),
                UseFAMEs.ToUnit(),
                MspFilePath.ToUnit(),
                SearchParameter.RiTolerance.ToUnit(),
                SearchParameter.RtTolerance.ToUnit(),
                SearchParameter.Ms1Tolerance.ToUnit(),
                SearchParameter.WeightedDotProductCutOff.ToUnit(),
                SearchParameter.TotalScoreCutoff.ToUnit(),
                SearchParameter.IsUseTimeForAnnotationScoring.ToUnit(),
                SearchParameter.IsUseTimeForAnnotationFiltering.ToUnit(),
                UseQuantMassesDefinedInMsp.ToUnit(),
                OnlyReportTopHit.ToUnit(),
            }.Merge();

            _decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(_decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                _decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReactivePropertySlim<bool> UseRI { get; }
        public ReactivePropertySlim<bool> UseRT { get; }
        public ReadOnlyReactiveCollection<RiDictionaryViewModel> RetentionIndexFiles { get; }
        public ReadOnlyReactivePropertySlim<bool> IsIndexImported { get; }
        public ReactivePropertySlim<bool> UseAlkanes { get; }
        public ReactivePropertySlim<bool> UseFAMEs { get; }

        [PathExists(ErrorMessage = "Msp file does not exist.", IsFile = true)]
        public ReactiveProperty<string> MspFilePath { get; }
        public MsRefSearchParameterBaseViewModel SearchParameter { get; }
        public ReactivePropertySlim<bool> UseQuantMassesDefinedInMsp { get; }
        public ReactivePropertySlim<bool> OnlyReportTopHit { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public IObservable<bool> ObserveHasErrors { get; }

        public IObservable<bool> ObserveChangeAfterDecision { get; }

        public IObservable<Unit> ObserveChanges { get; }

        public ISettingViewModel Next(ISettingViewModel selected) {
            _decide.OnNext(Unit.Default);
            return null;
        }
    }
}
