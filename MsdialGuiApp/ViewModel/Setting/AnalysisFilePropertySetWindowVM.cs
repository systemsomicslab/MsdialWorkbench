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

namespace CompMs.App.Msdial.Property
{
    class AnalysisFilePropertySetWindowVM : ViewModelBase
    {
        public MachineCategory MachineCategory => Model.Category;
        public string ProjectFolderPath => Model.ProjectFolderPath;

        public AnalysisFilePropertySetWindowVM(AnalysisFilePropertySetModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            AnalysisFilePropertyCollection = Model.AnalysisFilePropertyCollection
                .ToReadOnlyReactiveCollection(v => new AnalysisFileBeanViewModel(v))
                .AddTo(Disposables);

            var analysisFileHasError = new[]
            {
                AnalysisFilePropertyCollection.ObserveAddChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveRemoveChanged().ToUnit(),
                AnalysisFilePropertyCollection.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => AnalysisFilePropertyCollection.Any(vm => vm.HasErrors.Value));
            analysisFileHasError.Subscribe(x => Console.WriteLine($"file has error: {x}"));

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
            analysisFileNameDuplicate.Subscribe(x => Console.WriteLine($"file name duplicates: {x}"));

            ObserveHasErrors = new[]
            {
                analysisFileHasError,
                analysisFileNameDuplicate,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ObserveHasErrors.Subscribe(x => Console.WriteLine($"something errors: {x}"));

            ContinueProcessCommand = ObserveHasErrors.Inverse()
                .ToReactiveCommand<Window>()
                .WithSubscribe(ContinueProcess)
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
                InitialDirectory = ProjectFolderPath,
                RestoreDirectory = true,
                Multiselect = true,
            };
            if (Model.Category == MachineCategory.LCIMMS || Model.Category == MachineCategory.IMMS) {
                ofd.Filter = "IBF file(*.ibf)|*.ibf";
            }
            else {
                ofd.Filter = "ABF file(*.abf)|*.abf|mzML file(*.mzml)|*.mzml|netCDF file(*.cdf)|*.cdf|IBF file(*.ibf)|*.ibf|WIFF file(*.wiff)|*.wiff|WIFF2 file(*.wiff2)|*.wiff2|Raw file(*.raw)|*.raw";
            }

            if (ofd.ShowDialog() == true) {
                if (ofd.FileNames.Any(filename => Path.GetDirectoryName(filename) != ProjectFolderPath)) {
                    MessageBox.Show("The directory of analysis files should be where the project file is created.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;
                Model.ReadImportedFiles(ofd.FileNames);
                Mouse.OverrideCursor = null;
            }
        }

        public ReactiveCommand<Window> ContinueProcessCommand { get; }

        private void ContinueProcess(Window window) {
            Commit();
            window.DialogResult = true;
            window.Close();
        }

        public DelegateCommand<Window> CancelProcessCommand {
            get => cancelProcessCommand ?? (cancelProcessCommand = new DelegateCommand<Window>(CancelProcess));
        }

        private DelegateCommand<Window> cancelProcessCommand;

        private void CancelProcess(Window window) {
            window.DialogResult = false;
            window.Close();
        }

        public void Drop(string[] files) {
            if (files.IsEmptyOrNull()) {
                return;
            }

            var includedFiles = new List<string>();
            var excludedFiles = new List<string>();

            // Set BaseFileList Clone
            for (int i = 0; i < files.Length; i++) {
                if (IsAccepted(files[i])) {
                    includedFiles.Add(files[i]);
                }
                else {
                    excludedFiles.Add(System.IO.Path.GetFileName(files[i]));
                }
            }

            if (0 < excludedFiles.Count) {
                System.Windows.MessageBox.Show("The following file(s) cannot be converted because they are not acceptable raw files\n" +
                    String.Join("\n", excludedFiles.ToArray()),
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
            var extension = System.IO.Path.GetExtension(file).ToLower();
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
                    return true;
                default:
                    return false;
            }
        }

        public void Commit() {
            foreach (var file in AnalysisFilePropertyCollection) {
                file.Commit();
            }
        }

        private static readonly string FileNameDuplicateErrorMessage = "File name duplicated.";
    }
}
