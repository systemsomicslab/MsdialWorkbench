using CompMs.App.RawDataViewer.Model;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.RawDataViewer.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel() {
            model = new MainWindowModel().AddTo(Disposables);

            FilePath = model.ToReactivePropertyAsSynchronized(
                m => m.File,
                m => m?.AnalysisFilePath ?? string.Empty,
                vm => new AnalysisFileBean { 
                    AnalysisFilePath = vm,
                    AnalysisFileId = 0,
                    AnalysisFileName = Path.GetFileName(vm),
                    AcquisitionType = AcquisitionType.DDA,
                },
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => FilePath)
                .AddTo(Disposables);

            IsLcms = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            IsGcms = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
            IsDims = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
            IsImms = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
            IsLcimms = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
            new[]
            {
                IsLcms.Where(isLcms => isLcms).Select(_ => MachineCategory.LCMS),
                IsGcms.Where(isGcms => isGcms).Select(_ => MachineCategory.GCMS),
                IsDims.Where(isDims => isDims).Select(_ => MachineCategory.IFMS),
                IsImms.Where(isImms => isImms).Select(_ => MachineCategory.IMMS),
                IsLcimms.Where(isLcimms => isLcimms).Select(_ => MachineCategory.LCIMMS),
            }.Merge()
            .Subscribe(category => model.MachineCategory = category)
            .AddTo(Disposables);

            IsPositive = new ReactivePropertySlim<bool>(true).AddTo(Disposables);
            IsNegative = new ReactivePropertySlim<bool>(false).AddTo(Disposables);
            new[]
            {
                IsPositive.Where(isPositive => isPositive).Select(_ => IonMode.Positive),
                IsNegative.Where(isNegative => isNegative).Select(_ => IonMode.Negative),
            }.Merge()
            .Subscribe(ionMode => model.IonMode = ionMode)
            .AddTo(Disposables);

            SummarizedDataViewModels = model.SummarizedDataModels
                .ToReadOnlyReactiveCollection(m => new SummarizedDataViewModel(m))
                .AddTo(Disposables);
            SelectedSummarizedDataViewModel = model
                .ToReactivePropertySlimAsSynchronized(
                    m => m.SelectedSummarizedDataModel,
                    m => SummarizedDataViewModels.FirstOrDefault(vm => vm.Model == m),
                    vm => vm?.Model)
                .AddTo(Disposables);

            var commandEnable = new[]
            {
                FilePath.ObserveHasErrors,
                new[]
                {
                    IsLcms,
                    IsGcms,
                    IsDims,
                    IsImms,
                    IsLcimms,
                }.CombineLatestValuesAreAllFalse(),
                new[]
                {
                    IsPositive,
                    IsNegative,
                }.CombineLatestValuesAreAllFalse(),
            }.CombineLatestValuesAreAllFalse();

            LoadAnalysisDataCommand = commandEnable.ToReactiveCommand().AddTo(Disposables);
            LoadAnalysisDataCommand
                .Subscribe(LoadAnalysisData)
                .AddTo(Disposables);

            ShowSummarizedDataCommand = new ReactiveCommand().AddTo(Disposables);
            ShowSummarizedDataCommand
                .WithLatestFrom(SelectedSummarizedDataViewModel, (_, b) => b)
                .Subscribe(ShowSummarizedData)
                .AddTo(Disposables);

            RemoveSummarizedDataCommand = new ReactiveCommand()
                .WithSubscribe(model.RemoveAnalysisDataModel)
                .AddTo(Disposables);
        }

        private readonly MainWindowModel model;

        [Required(ErrorMessage = "Please input raw data file.")]
        [PathExists(IsFile = true, IsDirectory = true, ErrorMessage = "Unknown file selected.")]
        public ReactiveProperty<string> FilePath { get; }

        public ReactivePropertySlim<bool> IsLcms { get; }
        public ReactivePropertySlim<bool> IsGcms { get; }
        public ReactivePropertySlim<bool> IsDims { get; }
        public ReactivePropertySlim<bool> IsImms { get; }
        public ReactivePropertySlim<bool> IsLcimms { get; }

        public ReactivePropertySlim<bool> IsPositive { get; }
        public ReactivePropertySlim<bool> IsNegative { get; }

        public ReadOnlyReactiveCollection<SummarizedDataViewModel> SummarizedDataViewModels { get; }
        public ReactivePropertySlim<SummarizedDataViewModel> SelectedSummarizedDataViewModel { get; }

        public ReactiveCommand LoadAnalysisDataCommand { get; }

        private void LoadAnalysisData() {
            model.AddAnalysisDataModel();
            MessageBroker.Default.Publish(SelectedSummarizedDataViewModel.Value);
        }

        public ReactiveCommand ShowSummarizedDataCommand { get; }

        private void ShowSummarizedData(SummarizedDataViewModel vm) {
            MessageBroker.Default.Publish(vm);
        }

        public ReactiveCommand RemoveSummarizedDataCommand { get; }
    }
}
