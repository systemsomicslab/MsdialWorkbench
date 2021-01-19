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
            set => SetProperty(ref storage, value);
        }
        private MsdialDataStorage storage;


        public DelegateCommand<Window> CreateNewProjectCommand {
            get => createNewProjectCommand ?? (createNewProjectCommand = new DelegateCommand<Window>(CreateNewProject));
        }
        private DelegateCommand<Window> createNewProjectCommand;

        private void CreateNewProject(Window window) {
            Storage = new MsdialDataStorage();

            //Get IUPAC reference
            var iupacdb = IupacResourceParser.GetIUPACDatabase();
            Storage.IupacDatabase = iupacdb;

            // Set parameterbase
            var parameter = ProcessStartUp(window);
            if (parameter == null) return;
            Storage.ParameterBase = parameter;

            // Set analysis file property
            var success = ProcessSetAnalysisFile(window, Storage);
            if (!success) return;

            MethodVM = CreateNewMethodVM(storage.ParameterBase.MachineCategory, storage);
            MethodVM.InitializeNewProject(window);

#if DEBUG
            Console.WriteLine(string.Join("\n", Storage.ParameterBase.ParametersAsText()));
#endif
            SaveProject();
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

        private ParameterBase ProcessStartUp(Window owner) {
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

        private bool ProcessSetAnalysisFile(Window owner, MsdialDataStorage storage) {
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

            Storage.AnalysisFiles = analysisFilePropertySetWindowVM.AnalysisFilePropertyCollection.ToList();
            ParameterFactory.SetParameterFromAnalysisFiles(storage.ParameterBase, Storage.AnalysisFiles);

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

                var serializer = SerializerResolver.ResolveMsdialSerializer(ofd.FileName);
                Storage = serializer.LoadMsdialDataStorageBase(ofd.FileName);
                if (Storage == null) {
                    MessageBox.Show("Msdial cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MethodVM = CreateNewMethodVM(Storage.ParameterBase.MachineCategory, Storage);
                MethodVM.LoadProject();

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand SaveProjectCommand {
            get => saveProjectCommand ?? (saveProjectCommand = new DelegateCommand(SaveProject));
        }
        private DelegateCommand saveProjectCommand;

        private void SaveProject() {
            // TODO: implement process when project save failed.
            MethodVM.Serializer.SaveMsdialDataStorage(Storage.ParameterBase.ProjectFilePath, Storage);
            MethodVM?.SaveProject();
        }

        public DelegateCommand<Window> SaveAsProjectCommand {
            get => saveAsProjectCommand ?? (saveAsProjectCommand = new DelegateCommand<Window>(SaveAsProject));
        }
        private DelegateCommand<Window> saveAsProjectCommand;

        private void SaveAsProject(Window owner) {
            var sfd = new SaveFileDialog();
            sfd.Filter = "MTD file(*.mtd2)|*.mtd2";
            sfd.Title = "Save project dialog";
            sfd.InitialDirectory = Storage.ParameterBase.ProjectFolderPath;

            if (sfd.ShowDialog() == true) {
                if (System.IO.Path.GetDirectoryName(sfd.FileName) != Storage.ParameterBase.ProjectFolderPath) {
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

                Storage.ParameterBase.ProjectFilePath = sfd.FileName;
                SaveProject();

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

        public DelegateCommand<Window> SaveParameterCommand => saveParameterCommand ?? (saveParameterCommand = new DelegateCommand<Window>(SaveParameter));
        private DelegateCommand<Window> saveParameterCommand;

        private void SaveParameter(Window owner) {
            // TODO: implement process when parameter save failed.
            var sfd = new SaveFileDialog();
            sfd.Filter = "MED file(*.med)|*.med";
            sfd.Title = "Save file dialog";
            sfd.InitialDirectory = Storage.ParameterBase.ProjectFolderPath;

            if (sfd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var message = new ShortMessageWindow() {
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Text = "Saving the parameter...",
                };
                message.Show();

                MessagePackHandler.SaveToFile(Storage.ParameterBase, sfd.FileName);

                message.Close();
                Mouse.OverrideCursor = null;
            }
        }

    }
}
