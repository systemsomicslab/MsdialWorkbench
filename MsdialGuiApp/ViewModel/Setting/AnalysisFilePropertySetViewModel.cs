using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class AnalysisFilePropertySetViewModel : ViewModelBase
    {
        public AnalysisFilePropertySetViewModel(AnalysisFilePropertySetModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            AnalysisFilePropertyCollection = Model.AnalysisFilePropertyCollection
                .ToReadOnlyReactiveCollection(v => new AnalysisFileBeanViewModel(v))
                .AddTo(Disposables);

            DropFilesCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposables);
            DropFilesCommand.Select(e => e.Data.GetData(DataFormats.FileDrop) as string[])
                .Where(files => !files.IsEmptyOrNull())
                .Subscribe(Drop)
                .AddTo(Disposables);

            var analysisFileHasError = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Any(vm => vm.HasErrors.Value));

            var analysisFileNameDuplicate = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.AnalysisFileName).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Select(vm => vm.AnalysisFileName.Value).Distinct().Count() != AnalysisFilePropertyCollection.Count);
            analysisFileNameDuplicate.Subscribe(hasError => {
                if (hasError) {
                    AddError(nameof(AnalysisFilePropertyCollection), FileNameDuplicateErrorMessage);
                }
                else {
                    RemoveError(nameof(AnalysisFilePropertyCollection), FileNameDuplicateErrorMessage);
                }
            }).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                analysisFileHasError,
                analysisFileNameDuplicate,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ContinueProcessCommand = ObserveHasErrors.Inverse()
                .ToReactiveCommand()
                .WithSubscribe(Commit)
                .AddTo(Disposables);
        }

        public AnalysisFilePropertySetModel Model { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactiveCollection<AnalysisFileBeanViewModel> AnalysisFilePropertyCollection { get; }

        public DelegateCommand AnalysisFilesSelectCommand {
            get => analysisFilesSelectCommand ?? (analysisFilesSelectCommand = new DelegateCommand(AnalysisFilesSelect));
        }
        private DelegateCommand analysisFilesSelectCommand;

        private void AnalysisFilesSelect() {
            var ofd = new OpenFileDialog()
            {
                Title = "Import analysis files",
                InitialDirectory = Model.ProjectFolderPath,
                RestoreDirectory = true,
                Multiselect = true,
            };
            if (Model.Category == MachineCategory.LCIMMS || Model.Category == MachineCategory.IMMS) {
                ofd.Filter = "IBF file(*.ibf)|*.ibf";
            }
            else {
                ofd.Filter = "ABF file(*.abf)|*.abf|mzML file(*.mzml)|*.mzml|netCDF file(*.cdf)|*.cdf|IBF file(*.ibf)|*.ibf|WIFF file(*.wiff)|*.wiff|WIFF2 file(*.wiff2)|*.wiff2|Raw file(*.raw)|*.raw|LCD file(*.lcd)|*.lcd|QGD file(*.qgd)|*.qgd|All file(*.*)|*.*";
            }

            if (ofd.ShowDialog() == true) {
                if (ofd.FileNames.Any(filename => Path.GetDirectoryName(filename) != Model.ProjectFolderPath)) {
                    MessageBox.Show("The directory of analysis files should be where the project file is created.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ImportFiles(ofd.FileNames);
            }
        }

        public ReactiveCommand<DragEventArgs> DropFilesCommand { get; }

        public void Drop(string[] files) {
            ImportFiles(files);
        }

        private void ImportFiles(string[] files) {
            if (files.IsEmptyOrNull()) {
                return;
            }

            var includedFiles = new List<string>();
            var excludedFiles = new List<string>();

            foreach (var file in files) {
                if (IsAccepted(file)) {
                    includedFiles.Add(file);
                }
                else {
                    excludedFiles.Add(Path.GetFileName(file));
                }
            }

            if (excludedFiles.Count > 0) {
                MessageBox.Show("The following file(s) cannot be converted because they are not acceptable raw files\n" +
                    string.Join("\n", excludedFiles.ToArray()),
                    "Unacceptable Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (includedFiles.Count > 0) {
                Mouse.OverrideCursor = Cursors.Wait;
                Model.ReadImportedFiles(includedFiles);
                Mouse.OverrideCursor = null;
            }
        }

        private bool IsAccepted(string file) {
            var extension = Path.GetExtension(file).ToLower();
            switch (extension) {
                case ".abf":
                case ".mzml":
                case ".cdf":
                case ".raw":
                case ".d":
                case ".iabf":
                case ".ibf":
                case ".wiff":
                case ".wiff2":
                case ".qgd":
                case ".lcd":
                    return true;
                default:
                    return false;
            }
        }

        public ReactiveCommand ContinueProcessCommand { get; }

        public void Commit() {
            foreach (var file in AnalysisFilePropertyCollection) {
                file.Commit();
            }
        }

        private static readonly string FileNameDuplicateErrorMessage = "File name duplicated.";
    }
}
