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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class DataBaseSettingViewModel : ViewModelBase
    {
        public DataBaseSettingViewModel(DataBaseSettingModel model) {
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

            DBSources = new List<DataBaseSource> { DataBaseSource.Msp, DataBaseSource.Lbm, DataBaseSource.Text, DataBaseSource.Fasta, }.AsReadOnly();
            DBSource = DataBasePath
                .Select(path => Path.GetExtension(path))
                .Select(ext => {
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        return DataBaseSource.Msp;
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        return DataBaseSource.Lbm;
                    }
                    else if (Regex.IsMatch(ext, @"\.txt\d*")) {
                        return DataBaseSource.Text;
                    }
                    else if (Regex.IsMatch(ext, @"\.fasta\d*")) {
                        return DataBaseSource.Fasta;
                    }
                    return DataBaseSource.None;
                })
                .ToReactiveProperty(DataBaseSource.None)
                .SetValidateNotifyError(src => DBSources.Contains(src) ? null : "Unknown database")
                .AddTo(Disposables);
            DBSource.Where(_ => !DBSource.HasErrors).Subscribe(source => Model.DBSource = source).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                DataBasePath.ObserveHasErrors,
                DataBaseID.ObserveHasErrors,
                DBSource.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            BrowseCommand = new ReactiveCommand()
                .WithSubscribe(Browse)
                .AddTo(Disposables);

            IsLipidDataBase = Model.ObserveProperty(m => m.DBSource)
                .Select(src => src == DataBaseSource.Lbm)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            LipidDBSetCommand = IsLipidDataBase
                .ToReactiveCommand()
                .WithSubscribe(LipidDBSet)
                .AddTo(Disposables);
        }

        public DataBaseSettingModel Model { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReactiveProperty<DataBaseSource> DBSource { get; }

        public ReadOnlyCollection<DataBaseSource> DBSources { get; }

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
    }
}
