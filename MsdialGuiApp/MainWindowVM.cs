using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Property;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel;
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
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial
{
    class MainWindowVM : ViewModelBase {

        public MainWindowVM(
            IWindowService<StartUpWindowVM> startUpService,
            IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
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

            this.startUpService = startUpService;
            this.analysisFilePropertySetService = analysisFilePropertySetService;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
        }

        private readonly IWindowService<StartUpWindowVM> startUpService;
        private readonly IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public MethodViewModel MethodVM {
            get => methodVM;
            set => SetProperty(ref methodVM, value);
        }
        private MethodViewModel methodVM;

        public MsdialDataStorage Storage {
            get => storage;
            set {
                if (SetProperty(ref storage, value)) {
                    OnPropertyChanged(nameof(ProjectFile));
                }
            }
        }
        private MsdialDataStorage storage;

        public string ProjectFile => Storage?.ParameterBase?.ProjectFilePath ?? string.Empty;


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

            var storage = new MsdialDataStorage();
            storage.AnalysisFiles = analysisFiles;
            storage.ParameterBase = parameter;
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase(); //Get IUPAC reference
            storage.DataBaseMapper = new DataBaseMapper();
            storage.DataBases = new DataBaseStorage();


            RunProcessAll(window, storage);

            Storage = storage;
        }

        public DelegateCommand<Window> RunProcessAllCommand => runProcessAllCommand ?? (runProcessAllCommand = new DelegateCommand<Window>(owner => RunProcessAll(owner, Storage)));

        private DelegateCommand<Window> runProcessAllCommand;

        private void RunProcessAll(Window window, MsdialDataStorage storage) {
            MethodVM?.Dispose();

            var analysisFiles = storage.AnalysisFiles; // TODO: temporary
            storage.AnalysisFiles = storage.AnalysisFiles.Select(file => new AnalysisFileBean(file)).ToList();
            var dt = DateTime.Now;
            foreach (var file in storage.AnalysisFiles) {
                file.DeconvolutionFilePath = Path.Combine(storage.ParameterBase.ProjectFolderPath, $"{file.AnalysisFileName}_{dt:yyyyMMddHHmm}.{MsdialDataStorageFormat.dcl}");
                file.PeakAreaBeanInformationFilePath = Path.Combine(storage.ParameterBase.ProjectFolderPath, $"{file.AnalysisFileName}_{dt:_yyyyMMddHHmm}.{MsdialDataStorageFormat.pai}");
            }
            var method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
            if (method.InitializeNewProject(window) != 0) {
                //storage.AnalysisFiles = analysisFiles;
                //method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
                //method.LoadProject();
                //MethodVM = method;
                return;
            }

#if DEBUG
            Console.WriteLine(string.Join("\n", storage.ParameterBase.ParametersAsText()));
#endif

            MethodVM = method;

            SaveProject(method, storage);
        }

        private MethodViewModel CreateNewMethodVM(MachineCategory category, MsdialDataStorage storage) {
            switch (category) {
                case MachineCategory.LCMS:
                    return new ViewModel.Lcms.LcmsMethodVM(storage, compoundSearchService, peakSpotTableService);
                case MachineCategory.IFMS:
                    return new ViewModel.Dims.DimsMethodVM(storage, compoundSearchService, peakSpotTableService);
                case MachineCategory.IMMS:
                    return new ViewModel.Imms.ImmsMethodVM(storage, compoundSearchService, peakSpotTableService);
                case MachineCategory.LCIMMS:
                    return new ViewModel.Lcimms.LcimmsMethodVM(storage, compoundSearchService, peakSpotTableService);
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
            IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService,
            ParameterBase parameter) {

            var analysisFilePropertySetModel = new AnalysisFilePropertySetModel(parameter.ProjectFolderPath, parameter.MachineCategory);
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetWindowVM(analysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertySetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result != true) {
                    return new List<AnalysisFileBean>();
                }

                return analysisFilePropertySetModel.GetAnalysisFileBeanCollection();
            }
        }

        public DelegateCommand<Window> OpenProjectCommand {
            get => openProjectCommand ?? (openProjectCommand = new DelegateCommand<Window>(OpenProject));
        }
        private DelegateCommand<Window> openProjectCommand;

        private void OpenProject(Window owner) {
            var ofd = new OpenFileDialog
            {
                Filter = "MTD2 file(*.mtd2)|*.mtd2|All(*)|*",
                Title = "Import a project file",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow() {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Loading project...",
                };
                message.Show();

                Storage = LoadProjectFromPath(ofd.FileName);
                if (Storage == null) {
                    MessageBox.Show("Msdial cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MethodVM = CreateNewMethodVM(Storage.ParameterBase.MachineCategory, Storage);
                MethodVM.LoadProject();

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        // TODO: Move this method. MainWindowVM shouldn't know each analysis and alignment files.
        private static MsdialDataStorage LoadProjectFromPath(string projectfile) {
            var projectFolder = System.IO.Path.GetDirectoryName(projectfile);

            var serializer = SerializerResolver.ResolveMsdialSerializer(projectfile);
            var storage = serializer.LoadMsdialDataStorageBase(projectfile);

            var previousFolder = storage.ParameterBase.ProjectFolderPath;

            if (projectFolder == previousFolder)
                return storage;

            storage.ParameterBase.ProjectFolderPath = projectFolder;

            storage.ParameterBase.ProjectFilePath = ReplaceFolderPath(storage.ParameterBase.ProjectFilePath, previousFolder, projectFolder);
            // storage.ParameterBase.MspFilePath = replaceFolderPath(storage.ParameterBase.MspFilePath, previousFolder, projectFolder);
            storage.ParameterBase.TextDBFilePath = ReplaceFolderPath(storage.ParameterBase.TextDBFilePath, previousFolder, projectFolder);
            storage.ParameterBase.IsotopeTextDBFilePath = ReplaceFolderPath(storage.ParameterBase.IsotopeTextDBFilePath, previousFolder, projectFolder);

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
                return System.IO.Path.Combine(current, path.Substring(previous.Length).TrimStart('\\', '/'));
            if (!System.IO.Path.IsPathRooted(path))
                return System.IO.Path.Combine(current, path);
            throw new ArgumentException("Invalid path or directory.");
        }

        public DelegateCommand SaveProjectCommand {
            get => saveProjectCommand ?? (saveProjectCommand = new DelegateCommand(() => SaveProject(MethodVM, Storage)));
        }
        private DelegateCommand saveProjectCommand;

        private static void SaveProject(MethodViewModel methodVM, MsdialDataStorage storage) {
            // TODO: implement process when project save failed.
            methodVM.Serializer.SaveMsdialDataStorage(storage.ParameterBase.ProjectFilePath, storage);
            methodVM?.SaveProject();
        }

        public DelegateCommand<Window> SaveAsProjectCommand {
            get => saveAsProjectCommand ?? (saveAsProjectCommand = new DelegateCommand<Window>(owner => SaveAsProject(owner, methodVM, Storage)));
        }
        private DelegateCommand<Window> saveAsProjectCommand;

        private static void SaveAsProject(Window owner, MethodViewModel methodVM, MsdialDataStorage storage) {
            var sfd = new SaveFileDialog
            {
                Filter = "MTD file(*.mtd2)|*.mtd2",
                Title = "Save project dialog",
                InitialDirectory = storage.ParameterBase.ProjectFolderPath
            };

            if (sfd.ShowDialog() == true) {
                if (System.IO.Path.GetDirectoryName(sfd.FileName) != storage.ParameterBase.ProjectFolderPath) {
                    MessageBox.Show("Save folder should be the same folder as analysis files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow() {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the project as...",
                };
                message.Show();

                storage.ParameterBase.ProjectFilePath = sfd.FileName;
                SaveProject(methodVM, storage);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand<Window> SaveParameterCommand => saveParameterCommand ?? (saveParameterCommand = new DelegateCommand<Window>(owner => SaveParameter(owner, Storage)));
        private DelegateCommand<Window> saveParameterCommand;

        private static void SaveParameter(Window owner, MsdialDataStorage storage) {
            // TODO: implement process when parameter save failed.
            var sfd = new SaveFileDialog
            {
                Filter = "MED file(*.med)|*.med",
                Title = "Save file dialog",
                InitialDirectory = storage.ParameterBase.ProjectFolderPath
            };

            if (sfd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow() {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the parameter...",
                };
                message.Show();

                MessagePackHandler.SaveToFile(storage.ParameterBase, sfd.FileName);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand GoToTutorialCommand {
            get => goToTutorialCommand ?? (goToTutorialCommand = new DelegateCommand(GoToTutorial));
        }

        private void GoToTutorial() {
            System.Diagnostics.Process.Start("https://mtbinfo-team.github.io/mtbinfo.github.io/MS-DIAL/tutorial.html");
        }

        private DelegateCommand goToTutorialCommand;

    }
}
