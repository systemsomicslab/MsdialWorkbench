using CompMs.App.Msdial.Lipidomics;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.DataObj.Result;
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
        IReadOnlyReactiveProperty<string?> DataBaseID { get; }
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
            DataBaseID = model.ToReactivePropertyAsSynchronized(m => m.DataBaseID).SetValidateAttribute(() => DataBaseID).AddTo(Disposables);
            DataBasePath.Subscribe(path => DataBaseID.Value = Path.GetFileNameWithoutExtension(path)).AddTo(Disposables);

            var validDBSources = DBSources.Where(src => src != DataBaseSource.None).ToArray();
            DBSource = model.ToReactivePropertyAsSynchronized(m => m.DBSource)
                .SetValidateNotifyError(src => validDBSources.Contains(src) ? null : "Unknown database")
                .AddTo(Disposables);
            DBSource.Where(src => src == DataBaseSource.EieioLipid)
                .Subscribe(_ => DataBaseID.Value = "EieioDatabase")
                .AddTo(Disposables);
            DBSource.Where(src => src == DataBaseSource.OadLipid)
                .Subscribe(_ => DataBaseID.Value = "OadDatabase")
                .AddTo(Disposables);
            DBSource.Where(src => src == DataBaseSource.EidLipid)
                .Subscribe(_ => DataBaseID.Value = "EidDatabase")
                .AddTo(Disposables);
            DBSource.Where(src => src == DataBaseSource.Lbm)
                .Subscribe(_ => {
                    if (string.IsNullOrEmpty(DataBasePath.Value)) {
                        model.TrySetLbmLibrary();
                    }
                }).AddTo(Disposables);
            IsDataBasePathEnabled = DBSource.Select(src => (src != DataBaseSource.EieioLipid && src != DataBaseSource.OadLipid && src != DataBaseSource.EidLipid)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            DataBasePath
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => Path.GetExtension(path))
                .Subscribe(ext => {
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        model.DBSource = DataBaseSource.Msp;
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        model.DBSource = DataBaseSource.Lbm;
                    }
                    else if (Regex.IsMatch(ext, @"\.txt")) {
                        model.DBSource = DataBaseSource.Text;
                    }
                    else if (Regex.IsMatch(ext, @"\.fa(sta)?")) {
                        model.DBSource = DataBaseSource.Fasta;
                    }
                }).AddTo(Disposables);

            ProteomicsParameterVM = new ProteomicsParameterVM(model.ProteomicsParameter);

            //MassRangeBegin = Model.ToReactivePropertyAsSynchronized(
            //    m => m.MassRangeBegin,
            //    m => m.ToString(),
            //    mv => double.TryParse(mv, out double result) ? result : 0)
            //    .SetValidateAttribute(() => MassRangeBegin)
            //    .AddTo(Disposables);

            //MassRangeEnd = Model.ToReactivePropertyAsSynchronized(
            //    m => m.MassRangeEnd,
            //    m => m.ToString(),
            //    mv => double.TryParse(mv, out double result) ? result : 0)
            //    .SetValidateAttribute(() => MassRangeEnd)
            //    .AddTo(Disposables);

            var commonNoError = new[]
            {
                DataBasePath.ObserveHasErrors.CombineLatest(IsDataBasePathEnabled, (a, b) => a && b),
                DataBaseID.ObserveHasErrors,
                DBSource.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse();

            var proteomicsNoError = new[]
            {
                //MassRangeBegin.ObserveHasErrors,
                //MassRangeEnd.ObserveHasErrors,
                ProteomicsParameterVM.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                commonNoError,
                DBSource.SelectSwitch(src => src == DataBaseSource.Fasta ? proteomicsNoError : Observable.Return(true)),
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

            IsEditable = isEditableModel.Where(m => m == Model).ToConstant(true)
                .Merge(notEditableModel.Where(m => m == Model).ToConstant(false))
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

        public ReadOnlyCollection<DataBaseSource> DBSources { get; } = new ReadOnlyCollection<DataBaseSource>([
            DataBaseSource.None,
            DataBaseSource.Msp,
            DataBaseSource.Lbm,
            DataBaseSource.Text,
            DataBaseSource.Fasta,
            DataBaseSource.EieioLipid,
            DataBaseSource.OadLipid,
            DataBaseSource.EidLipid
        ]);

        public ProteomicsParameterVM ProteomicsParameterVM { get; }

        //[RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        //public ReactiveProperty<string> MassRangeBegin { get; }

        //[RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        //public ReactiveProperty<string> MassRangeEnd { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEditable { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand BrowseCommand { get; }

        private void Browse() {

            var filter = string.Join("|", OrderingExtensions());

            var ofd = new OpenFileDialog
            {
                Title = "Import a library file",
                Filter = filter,
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                DataBasePath.Value = ofd.FileName;
            }
        }

        private static readonly string MspExtension = "MSP file(*.msp)|*.msp?";
        private static readonly string LbmExtension = "Lbm file(*.lbm)|*.lbm?";
        private static readonly string FastaExtension = "FASTA file(*.fasta)|*.fasta";
        private static readonly string TextExtension = "Text library(*.txt)|*.txt";
        private List<string> OrderingExtensions() {
            var filters = new List<string>
            {
                 MspExtension,
                 LbmExtension,
                 FastaExtension,
                 TextExtension,
                 "All(*)|*"
            };
            switch (DBSource.Value) {
                case DataBaseSource.Text:
                    filters.Remove(TextExtension);
                    filters.Insert(0, TextExtension);
                    break;
                case DataBaseSource.Fasta:
                    filters.Remove(FastaExtension);
                    filters.Insert(0, FastaExtension);
                    break;
                case DataBaseSource.Lbm:
                    filters.Remove(LbmExtension);
                    filters.Insert(0, LbmExtension);
                    break;
                case DataBaseSource.Msp:
                default:
                    filters.Remove(MspExtension);
                    filters.Insert(0, MspExtension);
                    break;
            }
            return filters;
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

        public ReadOnlyReactivePropertySlim<string?> DataBaseID { get; }

        public ReadOnlyReactivePropertySlim<DataBaseSource> DBSource { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        // IDataBaseSettingViewModel interface
        IReadOnlyReactiveProperty<string?> IDataBaseSettingViewModel.DataBaseID => DataBaseID;
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
