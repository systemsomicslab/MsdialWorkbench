using CompMs.App.Msdial.Property;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial
{
    class MainWindowVM : ViewModelBase {

        public MainWindowVM(
            IWindowService<StartUpWindowVM> startUpService,
            IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService,
            IWindowService<CompoundSearchVM<AlignmentSpotProperty>> alignmentCompoundSearchService) {
            if (startUpService is null) {
                throw new ArgumentNullException(nameof(startUpService));
            }

            if (analysisFilePropertySetService is null) {
                throw new ArgumentNullException(nameof(analysisFilePropertySetService));
            }

            if (alignmentCompoundSearchService is null) {
                throw new ArgumentNullException(nameof(alignmentCompoundSearchService));
            }

            this.startUpService = startUpService;
            this.analysisFilePropertySetService = analysisFilePropertySetService;
            this.alignmentCompoundSearchService = alignmentCompoundSearchService;
        }

        private readonly IWindowService<StartUpWindowVM> startUpService;
        private readonly IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService;
        private readonly IWindowService<CompoundSearchVM<AlignmentSpotProperty>> alignmentCompoundSearchService;

        public MethodVM MethodVM {
            get => methodVM;
            set => SetProperty(ref methodVM, value);
        }
        private MethodVM methodVM;

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
            var storage = new MsdialDataStorage();
            storage.DataBaseMapper = new DataBaseMapper();

            //Get IUPAC reference
            var iupacdb = IupacResourceParser.GetIUPACDatabase();
            storage.IupacDatabase = iupacdb;

            // Set parameterbase
            var parameter = ProcessStartUp(startUpService);
            if (parameter == null)
                return;
            storage.ParameterBase = parameter;

            // Set analysis file property
            if (!ProcessSetAnalysisFile(analysisFilePropertySetService, storage))
                return;

            RunProcessAll(window, storage);

            Storage = storage;
        }

        public DelegateCommand<Window> RunProcessAllCommand => runProcessAllCommand ?? (runProcessAllCommand = new DelegateCommand<Window>(owner => RunProcessAll(owner, Storage)));

        private DelegateCommand<Window> runProcessAllCommand;

        private void RunProcessAll(Window window, MsdialDataStorage storage) {
            MethodVM?.Dispose();

            var method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
            if (method.InitializeNewProject(window) != 0) {

                method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
                method.LoadProject();
                MethodVM = method;
                return;
            }

#if DEBUG
            Console.WriteLine(string.Join("\n", storage.ParameterBase.ParametersAsText()));
#endif

            MethodVM = method;

            SaveProject(method, storage);
        }

        private MethodVM CreateNewMethodVM(MachineCategory category, MsdialDataStorage storage) {
            switch (category) {
                case MachineCategory.LCMS:
                    return new ViewModel.Lcms.LcmsMethodVM(storage, storage.AnalysisFiles, storage.AlignmentFiles);
                case MachineCategory.IFMS:
                    return new ViewModel.Dims.DimsMethodVM(storage, alignmentCompoundSearchService);
                case MachineCategory.IMMS:
                    return new ViewModel.Imms.ImmsMethodVM(storage, alignmentCompoundSearchService);
                case MachineCategory.LCIMMS:
                    throw new NotImplementedException("Lcimms method is working now.");
                    
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

        private static bool ProcessSetAnalysisFile(
            IWindowService<AnalysisFilePropertySetWindowVM> analysisFilePropertySetService,
            MsdialDataStorage storage) {

            var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetWindowVM
            {
                ProjectFolderPath = storage.ParameterBase.ProjectFolderPath,
                MachineCategory = storage.ParameterBase.MachineCategory,
            };

            var afpsw_result = analysisFilePropertySetService.ShowDialog(analysisFilePropertySetWindowVM);
            if (afpsw_result != true) return false;

            storage.AnalysisFiles = analysisFilePropertySetWindowVM.AnalysisFilePropertyCollection.ToList();
            ParameterFactory.SetParameterFromAnalysisFiles(storage.ParameterBase, storage.AnalysisFiles);

            return true;
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

        private static void SaveProject(MethodVM methodVM, MsdialDataStorage storage) {
            // TODO: implement process when project save failed.
            methodVM.Serializer.SaveMsdialDataStorage(storage.ParameterBase.ProjectFilePath, storage);
            methodVM?.SaveProject();
        }

        public DelegateCommand<Window> SaveAsProjectCommand {
            get => saveAsProjectCommand ?? (saveAsProjectCommand = new DelegateCommand<Window>(owner => SaveAsProject(owner, methodVM, Storage)));
        }
        private DelegateCommand<Window> saveAsProjectCommand;

        private static void SaveAsProject(Window owner, MethodVM methodVM, MsdialDataStorage storage) {
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
