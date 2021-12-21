using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    class MainWindowVM : ViewModelBase
    {

        public MainWindowVM(
            IWindowService<StartUpWindowVM> startUpService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertySetService,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService) {
            if (startUpService is null) {
                throw new ArgumentNullException(nameof(startUpService));
            }

            if (analysisFilePropertySetService is null) {
                throw new ArgumentNullException(nameof(analysisFilePropertySetService));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (analysisFilePropertyResetService is null) {
                throw new ArgumentNullException(nameof(analysisFilePropertyResetService));
            }

            Model = new MainWindowModel();

            this.startUpService = startUpService;
            this.analysisFilePropertySetService = analysisFilePropertySetService;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.analysisFilePropertyResetService = analysisFilePropertyResetService;
        }

        private readonly IWindowService<StartUpWindowVM> startUpService;
        private readonly IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertySetService;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService;

        public MainWindowModel Model { get; }

        public MethodViewModel MethodVM {
            get => methodVM;
            set => SetProperty(ref methodVM, value);
        }
        private MethodViewModel methodVM;

        public IMsdialDataStorage<ParameterBase> Storage {
            get => storage;
            set {
                if (SetProperty(ref storage, value)) {
                    OnPropertyChanged(nameof(ProjectFile));
                }
            }
        }
        private IMsdialDataStorage<ParameterBase> storage;

        public string ProjectFile => Storage?.Parameter is null ? string.Empty : Storage.Parameter.ProjectParam.ProjectFilePath;

        public DelegateCommand<Window> CreateNewProjectCommand {
            get => createNewProjectCommand ?? (createNewProjectCommand = new DelegateCommand<Window>(CreateNewProject));
        }
        private DelegateCommand<Window> createNewProjectCommand;

        private void CreateNewProject(Window window) {
            // Set parameterbase
            var parameter = ProcessStartUp(startUpService);
            if (parameter == null)
                return;


            // Set analysis file property
            var analysisFiles = ProcessSetAnalysisFile(analysisFilePropertySetService, parameter);
            if (!analysisFiles.Any()) {
                return;
            }
            ParameterFactory.SetParameterFromAnalysisFiles(parameter, analysisFiles);

            var storage = CreateDataStorage(parameter);
            storage.AnalysisFiles = analysisFiles;
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase(); //Get IUPAC reference
            storage.DataBaseMapper = new DataBaseMapper();
            storage.DataBases = DataBaseStorage.CreateEmpty();


            RunProcessAll(window, storage);

            Storage = storage;
        }

        private IMsdialDataStorage<ParameterBase> CreateDataStorage(ParameterBase parameter) {
            if (parameter is MsdialLcImMsParameter lcimmsParameter) {
                return new MsdialLcImMsDataStorage() { MsdialLcImMsParameter = lcimmsParameter };
            }
            if (parameter is MsdialLcmsParameter lcmsParameter) {
                return new MsdialLcmsDataStorage() { MsdialLcmsParameter = lcmsParameter };
            }
            if (parameter is MsdialImmsParameter immsParameter) {
                return new MsdialImmsDataStorage() { MsdialImmsParameter = immsParameter };
            }
            if (parameter is MsdialDimsParameter dimsParameter) {
                return new MsdialDimsDataStorage() { MsdialDimsParameter = dimsParameter };
            }
            throw new NotImplementedException("This method is not implemented");
        }

        public DelegateCommand<Window> RunProcessAllCommand => runProcessAllCommand ?? (runProcessAllCommand = new DelegateCommand<Window>(owner => RunProcessAll(owner, Storage)));

        private DelegateCommand<Window> runProcessAllCommand;

        private void RunProcessAll(Window window, IMsdialDataStorage<ParameterBase> storage) {
            MethodVM?.Dispose();

            var analysisFiles = storage.AnalysisFiles; // TODO: temporary
            storage.AnalysisFiles = storage.AnalysisFiles.Select(file => new AnalysisFileBean(file)).ToList();
            var dt = DateTime.Now;
            foreach (var file in storage.AnalysisFiles) {
                file.DeconvolutionFilePath = Path.Combine(storage.Parameter.ProjectFolderPath, $"{file.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}");
                file.PeakAreaBeanInformationFilePath = Path.Combine(storage.Parameter.ProjectFolderPath, $"{file.AnalysisFileName}_{dt:_yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}");
            }
            var method = CreateNewMethodVM(storage);
            if (method.InitializeNewProject(window) != 0) {
                //storage.AnalysisFiles = analysisFiles;
                //method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
                //method.LoadProject();
                //MethodVM = method;
                return;
            }

#if DEBUG
            Console.WriteLine(string.Join("\n", storage.Parameter.ParametersAsText()));
#endif

            MethodVM = method;

            SaveProject(method, storage);
        }

        private MethodViewModel CreateNewMethodVM(IMsdialDataStorage<ParameterBase> storage) {
            if (storage is MsdialLcImMsDataStorage lcimmsStorage) {
                return new Lcimms.LcimmsMethodVM(lcimmsStorage, compoundSearchService, peakSpotTableService);
            }
            if (storage is MsdialLcmsDataStorage lcmsStorage) {
                return new Lcms.LcmsMethodVM(lcmsStorage, compoundSearchService, peakSpotTableService);
            }
            if (storage is MsdialImmsDataStorage immsStorage) {
                return new Imms.ImmsMethodVM(immsStorage, compoundSearchService, peakSpotTableService);
            }
            if (storage is MsdialDimsDataStorage dimsStorage) {
                return new Dims.DimsMethodVM(dimsStorage, compoundSearchService, peakSpotTableService);
            }
            throw new NotImplementedException("This method is not implemented");
        }

        

        private static ParameterBase ProcessStartUp(IWindowService<StartUpWindowVM> service) {
            var startUpWindowVM = new StartUpWindowVM();
            var suw_result = service.ShowDialog(startUpWindowVM);
            if (suw_result != true) return null;

            ParameterBase parameter = ParameterFactory.CreateParameter(startUpWindowVM.Ionization, startUpWindowVM.SeparationType);
            if (parameter == null) return null;

            ParameterFactory.SetParameterFromStartUpVM(parameter, startUpWindowVM);
            return parameter;
        }

        private static List<AnalysisFileBean> ProcessSetAnalysisFile(
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertySetService,
            ParameterBase parameter) {

            var analysisFilePropertySetModel = new AnalysisFilePropertySetModel(parameter.ProjectFolderPath, parameter.MachineCategory);
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetViewModel(analysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertySetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result != true) {
                    return new List<AnalysisFileBean>();
                }
            }
            return analysisFilePropertySetModel.GetAnalysisFileBeanCollection();
        }

        public DelegateCommand<Window> OpenProjectCommand {
            get => openProjectCommand ?? (openProjectCommand = new DelegateCommand<Window>(OpenProject));
        }
        private DelegateCommand<Window> openProjectCommand;

        private void OpenProject(Window owner) {
            var ofd = new OpenFileDialog
            {
                Filter = "MTD3 file(*.mtd3)|*.mtd3|MTD2 file(*.mtd2)|*.mtd2|All(*)|*",
                Title = "Import a project file",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow()
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Loading project...",
                };
                message.Show();

                Storage = LoadProjectFromPath(ofd.FileName);
                if (Storage == null) {
                    MessageBox.Show("Msdial cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MethodVM = CreateNewMethodVM(Storage);
                MethodVM.LoadProject();

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        // TODO: Move this method. MainWindowVM shouldn't know each analysis and alignment files.
        private static IMsdialDataStorage<ParameterBase> LoadProjectFromPath(string projectfile) {
            var projectFolder = Path.GetDirectoryName(projectfile);

            var serializer = new MsdialIntegrateSerializer();
            var streamManager = new DirectoryTreeStreamManager(Path.GetDirectoryName(projectfile));
            var storage = serializer.LoadAsync(streamManager, Path.GetFileName(projectfile), Path.GetDirectoryName(projectfile), string.Empty).Result;
            storage.Parameter.ProjectFileName = Path.GetFileName(storage.Parameter.ProjectFileName);

            var previousFolder = storage.Parameter.ProjectFolderPath;
            if (projectFolder == previousFolder)
                return storage;

            storage.Parameter.ProjectFolderPath = projectFolder;

            storage.Parameter.TextDBFilePath = ReplaceFolderPath(storage.Parameter.TextDBFilePath, previousFolder, projectFolder);
            storage.Parameter.IsotopeTextDBFilePath = ReplaceFolderPath(storage.Parameter.IsotopeTextDBFilePath, previousFolder, projectFolder);

            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFilePath = ReplaceFolderPath(file.AnalysisFilePath, previousFolder, projectFolder);
                file.DeconvolutionFilePath = ReplaceFolderPath(file.DeconvolutionFilePath, previousFolder, projectFolder);
                file.PeakAreaBeanInformationFilePath = ReplaceFolderPath(file.PeakAreaBeanInformationFilePath, previousFolder, projectFolder);
                file.RiDictionaryFilePath = ReplaceFolderPath(file.RiDictionaryFilePath, previousFolder, projectFolder);

                file.DeconvolutionFilePathList = file.DeconvolutionFilePathList.Select(decfile => ReplaceFolderPath(decfile, previousFolder, projectFolder)).ToList();
            }

            foreach (var file in storage.AlignmentFiles) {
                file.FilePath = ReplaceFolderPath(file.FilePath, previousFolder, projectFolder);
                file.EicFilePath = ReplaceFolderPath(file.EicFilePath, previousFolder, projectFolder);
                file.SpectraFilePath = ReplaceFolderPath(file.SpectraFilePath, previousFolder, projectFolder);
            }

            return storage;
        }

        private static string ReplaceFolderPath(string path, string previous, string current) {
            if (string.IsNullOrEmpty(path))
                return path;
            if (path.StartsWith(previous))
                return Path.Combine(current, path.Substring(previous.Length).TrimStart('\\', '/'));
            if (!Path.IsPathRooted(path))
                return Path.Combine(current, path);
            throw new ArgumentException("Invalid path or directory.");
        }

        public DelegateCommand SaveProjectCommand {
            get => saveProjectCommand ?? (saveProjectCommand = new DelegateCommand(() => SaveProject(MethodVM, Storage)));
        }
        private DelegateCommand saveProjectCommand;

        private static void SaveProject(MethodViewModel methodVM, IMsdialDataStorage<ParameterBase> storage) {
            // TODO: implement process when project save failed.
            var streamManager = new DirectoryTreeStreamManager(storage.Parameter.ProjectFolderPath);
            storage.Save(streamManager, storage.Parameter.ProjectFileName, string.Empty);
            // storage.Save(storage.Parameter.ProjectFilePath);
            methodVM?.SaveProject();
        }

        public DelegateCommand<Window> SaveAsProjectCommand {
            get => saveAsProjectCommand ?? (saveAsProjectCommand = new DelegateCommand<Window>(owner => SaveAsProject(owner, methodVM, Storage)));
        }
        private DelegateCommand<Window> saveAsProjectCommand;

        private static void SaveAsProject(Window owner, MethodViewModel methodVM, IMsdialDataStorage<ParameterBase> storage) {
            var sfd = new SaveFileDialog
            {
                Filter = "MTD file(*.mtd2)|*.mtd2",
                Title = "Save project dialog",
                InitialDirectory = storage.Parameter.ProjectFolderPath,
            };

            if (sfd.ShowDialog() == true) {
                if (Path.GetDirectoryName(sfd.FileName) != storage.Parameter.ProjectFolderPath) {
                    MessageBox.Show("Save folder should be the same folder as analysis files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow()
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the project as...",
                };
                message.Show();

                storage.Parameter.ProjectFileName = Path.GetFileName(sfd.FileName);
                SaveProject(methodVM, storage);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand<Window> SaveParameterCommand => saveParameterCommand ?? (saveParameterCommand = new DelegateCommand<Window>(owner => SaveParameter(owner, Storage)));
        private DelegateCommand<Window> saveParameterCommand;

        private static void SaveParameter(Window owner, IMsdialDataStorage<ParameterBase> storage) {
            // TODO: implement process when parameter save failed.
            var sfd = new SaveFileDialog
            {
                Filter = "MED file(*.med)|*.med",
                Title = "Save file dialog",
                InitialDirectory = storage.Parameter.ProjectFolderPath
            };

            if (sfd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow()
                {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the parameter...",
                };
                message.Show();

                MessagePackHandler.SaveToFile(storage.Parameter, sfd.FileName);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand FilePropertyResetCommand {
            get => filePropertyResetCommand ?? (filePropertyResetCommand = new DelegateCommand(FilePropertyResettingWindow));
        }

        private void FilePropertyResettingWindow() {
            var files = Storage.AnalysisFiles;
            var analysisFilePropertySetModel = new AnalysisFilePropertySetModel(files);
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetViewModel(analysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertyResetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result != true) {
                    return;
                }
            }
        }

        private DelegateCommand filePropertyResetCommand;

        public DelegateCommand GoToTutorialCommand {
            get => goToTutorialCommand ?? (goToTutorialCommand = new DelegateCommand(GoToTutorial));
        }

        private void GoToTutorial() {
            System.Diagnostics.Process.Start("https://mtbinfo-team.github.io/mtbinfo.github.io/MS-DIAL/tutorial.html");
        }

        private DelegateCommand goToTutorialCommand;
    }
}
