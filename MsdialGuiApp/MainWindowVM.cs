using CompMs.App.Msdial.Property;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial
{
    class MainWindowVM : ViewModelBase {

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

            //Get IUPAC reference
            var iupacdb = IupacResourceParser.GetIUPACDatabase();
            storage.IupacDatabase = iupacdb;

            // Set parameterbase
            var parameter = ProcessStartUp(window);
            if (parameter == null)
                return;
            storage.ParameterBase = parameter;

            // Set analysis file property
            if (!ProcessSetAnalysisFile(window, storage))
                return;

            RunProcessAll(window, storage);

            Storage = storage;
        }

        public DelegateCommand<Window> RunProcessAllCommand => runProcessAllCommand ?? (runProcessAllCommand = new DelegateCommand<Window>(owner => RunProcessAll(owner, Storage)));

        private DelegateCommand<Window> runProcessAllCommand;

        private void RunProcessAll(Window window, MsdialDataStorage storage) {
            var method = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
            if (method.InitializeNewProject(window) != 0)
                return;

#if DEBUG
            Console.WriteLine(string.Join("\n", storage.ParameterBase.ParametersAsText()));
#endif

            MethodVM = method;
            SaveProject(method, storage);
        }

        private static MethodVM CreateNewMethodVM(MachineCategory category, MsdialDataStorage storage) {
            switch (category) {
                case MachineCategory.LCMS:
                    return new ViewModel.Lcms.LcmsMethodVM(storage, storage.AnalysisFiles, storage.AlignmentFiles);
                case MachineCategory.IFMS:
                    return new ViewModel.Dims.DimsMethodVM(storage, storage.AnalysisFiles, storage.AlignmentFiles);
            }
            throw new NotImplementedException("This method is not implemented");
        }

        private static ParameterBase ProcessStartUp(Window owner) {
            var startUpWindowVM = new StartUpWindowVM();
            var suw = new StartUpWindow()
            {
                DataContext = startUpWindowVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var suw_result = suw.ShowDialog();
            if (suw_result != true) return null;

            ParameterBase parameter = ParameterFactory.CreateParameter(startUpWindowVM.Ionization, startUpWindowVM.SeparationType);
            if (parameter == null) return null;

            ParameterFactory.SetParameterFromStartUpVM(parameter, startUpWindowVM);
            return parameter;
        }

        private static bool ProcessSetAnalysisFile(Window owner, MsdialDataStorage storage) {
            var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetWindowVM
            {
                ProjectFolderPath = storage.ParameterBase.ProjectFolderPath,
                MachineCategory = storage.ParameterBase.MachineCategory,
            };
            var afpsw = new AnalysisFilePropertySetWindow()
            {
                DataContext = analysisFilePropertySetWindowVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var afpsw_result = afpsw.ShowDialog();
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
            var ofd = new OpenFileDialog();
            ofd.Filter = "MTD2 file(*.mtd2)|*.mtd2|All(*)|*";
            ofd.Title = "Import a project file";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow() {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Loading project...",
                };
                message.Show();

                Storage = loadProjectFromPath(ofd.FileName);
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
        private static MsdialDataStorage loadProjectFromPath(string projectfile) {
            var projectFolder = System.IO.Path.GetDirectoryName(projectfile);

            var serializer = SerializerResolver.ResolveMsdialSerializer(projectfile);
            var storage = serializer.LoadMsdialDataStorageBase(projectfile);

            var previousFolder = storage.ParameterBase.ProjectFolderPath;

            if (projectFolder == previousFolder)
                return storage;

            storage.ParameterBase.ProjectFolderPath = projectFolder;

            storage.ParameterBase.ProjectFilePath = replaceFolderPath(storage.ParameterBase.ProjectFilePath, previousFolder, projectFolder);
            // storage.ParameterBase.MspFilePath = replaceFolderPath(storage.ParameterBase.MspFilePath, previousFolder, projectFolder);
            storage.ParameterBase.TextDBFilePath = replaceFolderPath(storage.ParameterBase.TextDBFilePath, previousFolder, projectFolder);
            storage.ParameterBase.IsotopeTextDBFilePath = replaceFolderPath(storage.ParameterBase.IsotopeTextDBFilePath, previousFolder, projectFolder);

            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFilePath = replaceFolderPath(file.AnalysisFilePath, previousFolder, projectFolder);
                file.DeconvolutionFilePath = replaceFolderPath(file.DeconvolutionFilePath, previousFolder, projectFolder);
                file.PeakAreaBeanInformationFilePath = replaceFolderPath(file.PeakAreaBeanInformationFilePath, previousFolder, projectFolder);
                file.RiDictionaryFilePath = replaceFolderPath(file.RiDictionaryFilePath, previousFolder, projectFolder);

                file.DeconvolutionFilePathList = file.DeconvolutionFilePathList.Select(decfile => replaceFolderPath(decfile, previousFolder, projectFolder)).ToList();
            }

            foreach (var file in storage.AlignmentFiles) {
                file.FilePath = replaceFolderPath(file.FilePath, previousFolder, projectFolder);
                file.EicFilePath = replaceFolderPath(file.EicFilePath, previousFolder, projectFolder);
                file.SpectraFilePath = replaceFolderPath(file.SpectraFilePath, previousFolder, projectFolder);
            }

            return storage;
        }

        private static string replaceFolderPath(string path, string previous, string current) {
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
            var sfd = new SaveFileDialog();
            sfd.Filter = "MTD file(*.mtd2)|*.mtd2";
            sfd.Title = "Save project dialog";
            sfd.InitialDirectory = storage.ParameterBase.ProjectFolderPath;

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
            var sfd = new SaveFileDialog();
            sfd.Filter = "MED file(*.med)|*.med";
            sfd.Title = "Save file dialog";
            sfd.InitialDirectory = storage.ParameterBase.ProjectFolderPath;

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

        public DelegateCommand<Window> GoToTutorialCommand {
            get => goToTutorialCommand ?? (goToTutorialCommand = new DelegateCommand<Window>(GoToTutorial));
        }

        private void GoToTutorial(Window obj) {
            System.Diagnostics.Process.Start("https://mtbinfo-team.github.io/mtbinfo.github.io/MS-DIAL/tutorial.html");
        }

        private DelegateCommand<Window> goToTutorialCommand;

    }
}
