using CompMs.App.Msdial.Lipidomics;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public interface IDataBaseSettingViewModel : INotifyPropertyChanged
    {
        DataBaseSettingModel Model { get; }
        IReadOnlyReactiveProperty<string> DataBaseID { get; }
        IReadOnlyReactiveProperty<DataBaseSource> DBSource { get; }
        IReadOnlyReactiveProperty<bool> ObserveHasErrors { get; }
    }

    public class DataBaseSettingViewModel : ViewModelBase, IDataBaseSettingViewModel
    {
        public DataBaseSettingViewModel(
            DataBaseSettingModel model,
            IObservable<DataBaseSettingModel> notEditableModel,
            IObservable<DataBaseSettingModel> isEditableModel) {
            Model = model;

            DataBasePath = Model.ToReactivePropertyAsSynchronized(m => m.DataBasePath)
                .SetValidateAttribute(() => DataBasePath)
                .AddTo(Disposables);
            DataBaseID = new[]
            {
                Model.ObserveProperty(m => m.DataBaseID),
                DataBasePath.Merge(Observable.Return(Model.DataBasePath)).Select(path => Path.GetFileNameWithoutExtension(path)),
            }.Merge()
            .ToReactiveProperty()
            .SetValidateAttribute(() => DataBaseID)
            .AddTo(Disposables);
            DataBaseID.Where(_ => !DataBaseID.HasErrors).Subscribe(id => Model.DataBaseID = id).AddTo(Disposables);

            DBSources = new List<DataBaseSource> { DataBaseSource.Msp, DataBaseSource.Lbm, DataBaseSource.Text, DataBaseSource.Fasta, DataBaseSource.EadLipid, }.AsReadOnly();
            DBSource = DataBasePath
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => Path.GetExtension(path))
                .Select(ext => {
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        return DataBaseSource.Msp;
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        return DataBaseSource.Lbm;
                    }
                    else if (Regex.IsMatch(ext, @"\.txt")) {
                        return DataBaseSource.Text;
                    }
                    else if (Regex.IsMatch(ext, @"\.fa(sta)?")) {
                        return DataBaseSource.Fasta;
                    }
                    return DataBaseSource.None;
                })
                .ToReactiveProperty(Model.DBSource)
                .SetValidateNotifyError(src => DBSources.Contains(src) ? null : "Unknown database")
                .AddTo(Disposables);
            DBSource.Where(src => src == DataBaseSource.EadLipid)
                .Subscribe(_ => DataBaseID.Value = "LipidDatabase")
                .AddTo(Disposables);
            IsDataBasePathEnabled = DBSource.Select(src => src != DataBaseSource.EadLipid).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            DBSource.Where(_ => !DBSource.HasErrors)
                .Subscribe(source => Model.DBSource = source)
                .AddTo(Disposables);

            ProteomicsParameterVM = new ProteomicsParameterVM(model.ProteomicsParameter);

            MassRangeBegin = Model.ToReactivePropertyAsSynchronized(
                m => m.MassRangeBegin,
                m => m.ToString(),
                mv => double.Parse(mv))
                .SetValidateAttribute(() => MassRangeBegin)
                .AddTo(Disposables);

            MassRangeEnd = Model.ToReactivePropertyAsSynchronized(
                m => m.MassRangeEnd,
                m => m.ToString(),
                mv => double.Parse(mv))
                .SetValidateAttribute(() => MassRangeEnd)
                .AddTo(Disposables);

            var commonNoError = new[]
            {
                DataBasePath.ObserveHasErrors.CombineLatest(IsDataBasePathEnabled, (a, b) => a && b),
                DataBaseID.ObserveHasErrors,
                DBSource.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse();

            var proteomicsNoError = new[]
            {
                MassRangeBegin.ObserveHasErrors,
                MassRangeEnd.ObserveHasErrors,
                ProteomicsParameterVM.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                commonNoError,
                DBSource.Select(src => src == DataBaseSource.Fasta ? proteomicsNoError : Observable.Return(true)).Switch(),
            }.CombineLatestValuesAreAllTrue()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            BrowseCommand = new ReactiveCommand()
                .WithSubscribe(Browse)
                .AddTo(Disposables);

            IsLipidDataBase = DBSource
                .Select(src => src == DataBaseSource.Lbm)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            LipidDBSetCommand = IsLipidDataBase
                .ToReactiveCommand()
                .WithSubscribe(LipidDBSet)
                .AddTo(Disposables);

            IsProteomicsDataBase = DBSource
                .Select(src => src == DataBaseSource.Fasta)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            IsEditable = isEditableModel.Where(m => m == Model).Select(_ => true)
                .Merge(notEditableModel.Where(m => m == Model).Select(_ => false))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public DataBaseSettingModel Model { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        public ReadOnlyReactivePropertySlim<bool> IsDataBasePathEnabled { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReactiveProperty<DataBaseSource> DBSource { get; }

        public ReadOnlyCollection<DataBaseSource> DBSources { get; }

        public ProteomicsParameterVM ProteomicsParameterVM { get; }

        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeBegin { get; }

        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeEnd { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEditable { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand BrowseCommand { get; }

        private void Browse() {

            var filter = "MSP file(*.msp)|*.msp?|Lbm file(*.lbm)|*.lbm?|FASTA file(*.fasta)|*.fasta|Text library(*.txt)|*.txt|All(*)|*";
            if (Model.TargetOmics == TargetOmics.Proteomics) {
                filter = "FASTA file(*.fasta)|*.fasta|MSP file(*.msp)|*.msp?|Lbm file(*.lbm)|*.lbm?|Text library(*.txt)|*.txt|All(*)|*";
            }

            var ofd = new OpenFileDialog
            {
                Title = "Import alibrary file",
                Filter = filter,
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                DataBasePath.Value = ofd.FileName;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> IsLipidDataBase { get; }
        public ReactiveCommand LipidDBSetCommand { get; }

        private void LipidDBSet() {
            using (var vm = new LipidDbSetVM(Model.LipidQueryContainer, Model.LipidQueryContainer.IonMode)) {
                var window = new LipidDbSetWindow
                {
                    DataContext = vm,
                };
                window.ShowDialog();
            }
        }

        public ReadOnlyReactivePropertySlim<bool> IsProteomicsDataBase { get; }

        // IDataBaseSettingViewModel interface
        IReadOnlyReactiveProperty<string> IDataBaseSettingViewModel.DataBaseID => DataBaseID;
        IReadOnlyReactiveProperty<DataBaseSource> IDataBaseSettingViewModel.DBSource => DBSource;
        IReadOnlyReactiveProperty<bool> IDataBaseSettingViewModel.ObserveHasErrors => ObserveHasErrors;
    }

    public class LoadedDataBaseSettingViewModel : ViewModelBase, IDataBaseSettingViewModel
    {
        public LoadedDataBaseSettingViewModel(DataBaseSettingModel model) {
            Model = model;
            DataBaseID = Observable.Return(model.DataBaseID).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            DBSource = Observable.Return(model.DBSource).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ObserveHasErrors = Observable.Return(false).ToReadOnlyReactivePropertySlim(false).AddTo(Disposables);
        }

        public DataBaseSettingModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> DataBaseID { get; }

        public ReadOnlyReactivePropertySlim<DataBaseSource> DBSource { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        // IDataBaseSettingViewModel interface
        IReadOnlyReactiveProperty<string> IDataBaseSettingViewModel.DataBaseID => DataBaseID;
        IReadOnlyReactiveProperty<DataBaseSource> IDataBaseSettingViewModel.DBSource => DBSource;
        IReadOnlyReactiveProperty<bool> IDataBaseSettingViewModel.ObserveHasErrors => ObserveHasErrors;
    }

    public class DataBaseSettingViewModelFactory
    {
        private readonly IObservable<DataBaseSettingModel> notEditableModel;
        private readonly IObservable<DataBaseSettingModel> isEditableModel;

        public DataBaseSettingViewModelFactory(IObservable<DataBaseSettingModel> isEditableModel, IObservable<DataBaseSettingModel> notEditableModel) {
            this.isEditableModel = isEditableModel;
            this.notEditableModel = notEditableModel;
        }

        public IDataBaseSettingViewModel Create(DataBaseSettingModel model) {
            if (model.IsLoaded) {
                return new LoadedDataBaseSettingViewModel(model);
            }
            else {
                return new DataBaseSettingViewModel(model, notEditableModel, isEditableModel.StartWith(model));
            }
        }  
    }
}
