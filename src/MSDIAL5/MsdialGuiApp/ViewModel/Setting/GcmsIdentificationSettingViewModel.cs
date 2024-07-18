using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Data;
using System.Windows.Input;

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

    public sealed class RiDictionarySettingViewModel : SettingDialogViewModel {
        private readonly RiDictionarySettingModel _model;
        private readonly ICollectionView _retentionIndexFilesView;

        public RiDictionarySettingViewModel(RiDictionarySettingModel model, IMessageBroker broker) {
            _model = model;
            RetentionIndexFiles = model.RetentionIndexFiles.ToReadOnlyReactiveCollection(m => new RiDictionaryViewModel(m, broker)).AddTo(Disposables);
            _retentionIndexFilesView = CollectionViewSource.GetDefaultView(RetentionIndexFiles);
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
            IsImported = model.ObserveProperty(m => m.IsImported).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            _finishCommand = RetentionIndexFiles.Select(ri => ri.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default).Select(_ => ri.HasValidationErrors))
                .CombineLatestValuesAreAllFalse()
                .ToReactiveCommand().WithSubscribe(Set).AddTo(Disposables);
            AutoFillCommand = new ReactiveCommand<RiDictionaryViewModel>().WithSubscribe(vm => model.AutoFill(vm.Model, GetBelowModels(vm))).AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<RiDictionaryViewModel> RetentionIndexFiles { get; }
        public ReactivePropertySlim<bool> UseAlkanes { get; }
        public ReactivePropertySlim<bool> UseFAMEs { get; }
        public ReadOnlyReactivePropertySlim<bool> IsImported { get; }

        public ReactiveCommand<RiDictionaryViewModel> AutoFillCommand { get; }

        public override ICommand FinishCommand => _finishCommand;
        private readonly ReactiveCommand _finishCommand;

        private void Set() {
            _model.TrySet();
        }

        private RiDictionaryModel[] GetBelowModels(RiDictionaryViewModel vm)
        {
            var index = _retentionIndexFilesView.Cast<RiDictionaryViewModel>().ToList().IndexOf(vm);
            if (index >= 0 && index < RetentionIndexFiles.Count)
            {
                return _retentionIndexFilesView.Cast<RiDictionaryViewModel>().Skip(index + 1).Select(v => v.Model).ToArray();
            }
            return [];
        }
    }
    
    public sealed class GcmsIdentificationSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly Subject<Unit> _decide;
        private readonly IMessageBroker _broker;

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
            MspFilePath = model.ToReactivePropertyAsSynchronized(m => m.MspFilePath).AddTo(Disposables);
            BrowseMspCommand = new ReactiveCommand().WithSubscribe(Browse).AddTo(Disposables);
            SearchParameter = new MsRefSearchParameterBaseViewModel(model.SearchParameter).AddTo(Disposables);
            UseQuantMassesDefinedInMsp = model.ToReactivePropertySlimAsSynchronized(m => m.UseQuantmassDefinedInLibrary).AddTo(Disposables);
            OnlyReportTopHit = model.ToReactivePropertySlimAsSynchronized(m => m.OnlyReportTopHit).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                UseRI.SelectSwitch(p => p ? RiDictionarySettingViewModel.IsImported.Inverse() : Observable.Return(false)),
                MspFilePath.ObserveHasErrors,
                SearchParameter.RiTolerance.ObserveHasErrors,
                SearchParameter.RtTolerance.ObserveHasErrors,
                SearchParameter.Ms1Tolerance.ObserveHasErrors,
                SearchParameter.MassRangeBegin.ObserveHasErrors,
                SearchParameter.MassRangeEnd.ObserveHasErrors,
                SearchParameter.WeightedDotProductCutOff.ObserveHasErrors,
                SearchParameter.SimpleDotProductCutOff.ObserveHasErrors,
                SearchParameter.ReverseDotProductCutOff.ObserveHasErrors,
                SearchParameter.TotalScoreCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                UseRI.ToUnit(),
                UseRT.ToUnit(),
                RiDictionarySettingViewModel.RetentionIndexFiles.Select(f => f.DictionaryPath.ToUnit()).Merge(),
                MspFilePath.ToUnit(),
                SearchParameter.RiTolerance.ToUnit(),
                SearchParameter.RtTolerance.ToUnit(),
                SearchParameter.MassRangeBegin.ToUnit(),
                SearchParameter.MassRangeEnd.ToUnit(),
                SearchParameter.Ms1Tolerance.ToUnit(),
                SearchParameter.WeightedDotProductCutOff.ToUnit(),
                SearchParameter.SimpleDotProductCutOff.ToUnit(),
                SearchParameter.ReverseDotProductCutOff.ToUnit(),
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
            _broker = broker;
        }

        public bool IsReadOnly { get; }
        public ReactivePropertySlim<bool> UseRI { get; }
        public ReactivePropertySlim<bool> UseRT { get; }
        public ReactiveCommand IndexFileSetCommand { get; }
        public RiDictionarySettingViewModel RiDictionarySettingViewModel { get; }
        public ReadOnlyReactivePropertySlim<bool> IsIndexImported => RiDictionarySettingViewModel.IsImported;

        //[PathExists(ErrorMessage = "Msp file does not exist.", IsFile = true)]
        public ReactiveProperty<string?> MspFilePath { get; }
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
            _broker.Publish(request);
        }

        public IObservable<bool> ObserveHasErrors { get; }

        public IObservable<bool> ObserveChangeAfterDecision { get; }

        public IObservable<Unit> ObserveChanges { get; }

        public ISettingViewModel? Next(ISettingViewModel selected) {
            _decide.OnNext(Unit.Default);
            return null;
        }
    }
}
