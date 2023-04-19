using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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

        public RiDictionaryModel Model => _model;

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

    public sealed class RiDictionarySettingViewModel : ViewModelBase {
        private readonly RiDictionarySettingModel _model;

        public RiDictionarySettingViewModel(RiDictionarySettingModel model, IMessageBroker broker) {
            _model = model;
            RetentionIndexFiles = model.RetentionIndexFiles.ToReadOnlyReactiveCollection(m => new RiDictionaryViewModel(m, broker)).AddTo(Disposables);
            SelectedRetentionIndexFile = model.ToReactivePropertySlimAsSynchronized(
                m => m.SelectedRetentionIndexFile,
                op => op.Select(p => RetentionIndexFiles.FirstOrDefault(f => f.Model == p)),
                op => op.Select(p => p?.Model)).AddTo(Disposables);
            IsImported = model.ObserveProperty(m => m.IsImported).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ApplyCommand = RetentionIndexFiles.Select(ri => ri.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default).Select(_ => ri.HasValidationErrors))
                .CombineLatestValuesAreAllFalse()
                .ToReactiveCommand().WithSubscribe(Apply).AddTo(Disposables);
            AutoFillCommand = new ReactiveCommand().WithSubscribe(model.AutoFill).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<RiDictionaryViewModel> RetentionIndexFiles { get; }
        public ReactivePropertySlim<RiDictionaryViewModel> SelectedRetentionIndexFile { get; }
        public ReadOnlyReactivePropertySlim<bool> IsImported { get; }

        public ReactiveCommand AutoFillCommand { get; }

        public ReactiveCommand ApplyCommand { get; }

        private void Apply() {
            _model.TrySet();
        }
    }
    
    public sealed class GcmsIdentificationSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly Subject<Unit> _decide;

        public GcmsIdentificationSettingViewModel(GcmsIdentificationSettingModel model, IMessageBroker broker, IObservable<bool> isEnabled) {
            IsReadOnly = model.IsReadOnly;
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

            RiDictionarySettingViewModel = new RiDictionarySettingViewModel(model.RiDictionarySettingModel, broker).AddTo(Disposables);
            IndexFileSetCommand = UseRI.ToReactiveCommand().WithSubscribe(() => broker.Publish(RiDictionarySettingViewModel)).AddTo(Disposables);
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
            BrowseMspCommand = new ReactiveCommand().WithSubscribe(Browse).AddTo(Disposables);
            SearchParameter = new MsRefSearchParameterBaseViewModel(model.SearchParameter).AddTo(Disposables);
            UseQuantMassesDefinedInMsp = model.ToReactivePropertySlimAsSynchronized(m => m.UseQuantmassDefinedInLibrary).AddTo(Disposables);
            OnlyReportTopHit = model.ToReactivePropertySlimAsSynchronized(m => m.OnlyReportTopHit).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                RiDictionarySettingViewModel.IsImported,
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
                RiDictionarySettingViewModel.RetentionIndexFiles.Select(f => f.DictionaryPath.ToUnit()).Merge(),
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

        public bool IsReadOnly { get; }
        public ReactivePropertySlim<bool> UseRI { get; }
        public ReactivePropertySlim<bool> UseRT { get; }
        public ReactiveCommand IndexFileSetCommand { get; }
        public RiDictionarySettingViewModel RiDictionarySettingViewModel { get; }
        public ReadOnlyReactivePropertySlim<bool> IsIndexImported => RiDictionarySettingViewModel.IsImported;
        public ReactivePropertySlim<bool> UseAlkanes { get; }
        public ReactivePropertySlim<bool> UseFAMEs { get; }

        [PathExists(ErrorMessage = "Msp file does not exist.", IsFile = true)]
        public ReactiveProperty<string> MspFilePath { get; }
        public ReactiveCommand BrowseMspCommand { get; }
        public MsRefSearchParameterBaseViewModel SearchParameter { get; }
        public ReactivePropertySlim<bool> UseQuantMassesDefinedInMsp { get; }
        public ReactivePropertySlim<bool> OnlyReportTopHit { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        private void Browse() {
            var filter = "MSP file(*.msp)|*.msp?";

            var request = new OpenFileRequest(file => MspFilePath.Value = file)
            {
                Title = "Import a library file",
                Filter = filter,
                RestoreDirectory = true,
            };
        }

        public IObservable<bool> ObserveHasErrors { get; }

        public IObservable<bool> ObserveChangeAfterDecision { get; }

        public IObservable<Unit> ObserveChanges { get; }

        public ISettingViewModel Next(ISettingViewModel selected) {
            _decide.OnNext(Unit.Default);
            return null;
        }
    }
}
