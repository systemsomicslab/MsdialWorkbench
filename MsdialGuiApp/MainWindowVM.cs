using CompMs.App.Msdial.Property;
using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CompMs.App.Msdial.LC;
using CompMs.App.Msdial.Common;
using CompMs.MsdialCore.Utility;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.MsdialLcMsApi.Algorithm;
using System.Threading;
using CompMs.Graphics.UI.ProgressBar;
using System.Collections.ObjectModel;
using CompMs.App.Msdial.ViewModel;
using Microsoft.Win32;
using System.Windows.Input;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Parser;

namespace CompMs.App.Msdial
{
    class MainWindowVM : ViewModelBase
    {
        #region property
        public MsdialDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }

        public AnalysisFileVM FileVM {
            get => fileVM;
            set => SetProperty(ref fileVM, value);
        }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }

        public bool RefMatchedChecked {
            get => refMatchedChecked;
            set => SetProperty(ref refMatchedChecked, value);
        }
        public bool SuggestedChecked {
            get => suggestedChecked;
            set => SetProperty(ref suggestedChecked, value);
        }
        public bool UnknownChecked {
            get => unknownChecked;
            set => SetProperty(ref unknownChecked, value);
        }
        public bool CcsChecked {
            get => ccsChecked;
            set => SetProperty(ref ccsChecked, value);
        }
        public bool Ms2AcquiredChecked {
            get => ms2AcquiredChecked;
            set => SetProperty(ref ms2AcquiredChecked, value);
        }
        public bool MolecularIonChecked {
            get => molecularIonChecked;
            set => SetProperty(ref molecularIonChecked, value);
        }
        public bool BlankFilterChecked {
            get => blankFilterChecked;
            set => SetProperty(ref blankFilterChecked, value);
        }
        public bool UniqueIonsChecked {
            get => uniqueIonsChecked;
            set => SetProperty(ref uniqueIonsChecked, value);
        }

        private MsdialSerializer Serializer {
            get => serializer ?? (serializer = new MsdialLcmsSerializer());
        }
        #endregion

        #region field
        private MsdialDataStorage storage;
        private bool refMatchedChecked = true, suggestedChecked = true, unknownChecked = true,
            ccsChecked, ms2AcquiredChecked, molecularIonChecked, blankFilterChecked, uniqueIonsChecked;
        // private AlignmentVM alignmentVM;
        private AnalysisFileVM fileVM;
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private MsdialSerializer serializer;
        #endregion

        public MainWindowVM() { }

        #region Command
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

            // Set analysis param
            success = ProcessSetAnalysisParameter(window);
            if (!success) return;

            // Run Identification
            ProcessAnnotaion(window, Storage);
            LoadInitialFiles();

            // Run Alignment
            // ProcessAlignment();

            Console.WriteLine(string.Join("\n", Storage.ParameterBase.ParametersAsText()));
            SaveProject();
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

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new AnalysisParamSetForLcVM(Storage.ParameterBase as MsdialLcmsParameter, Storage.AnalysisFiles);
            var apsw = new AnalysisParamSetForLcWindow
            {
                DataContext = analysisParamSetVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            var apsw_result = apsw.ShowDialog();
            if (apsw_result != true) return false;

            if (Storage.AlignmentFiles == null)
                Storage.AlignmentFiles = new List<AlignmentFileBean>();
            var filename = analysisParamSetVM.AlignmentResultFileName;
            Storage.AlignmentFiles.Add(
                new AlignmentFileBean
                {
                    FileID = Storage.AlignmentFiles.Count,
                    FileName = filename,
                    FilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + "." + SaveFileFormat.arf),
                    EicFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + ".EIC.aef"),
                }
            );
            Storage.MspDB = analysisParamSetVM.MspDB;
            Storage.TextDB = analysisParamSetVM.TextDB;

            return true;
        }

        private bool ProcessAnnotaion(Window owner, MsdialDataStorage storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                var numThreads = Math.Min(Math.Min(storage.ParameterBase.NumThreads, storage.AnalysisFiles.Count), Environment.ProcessorCount);
                var semaphore = new SemaphoreSlim(0, numThreads);
                var tasks = new Task[storage.AnalysisFiles.Count];
                var counter = 0;
                foreach (((var analysisfile, var pbvm), var idx) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs).WithIndex()) {
                    tasks[idx] = Task.Run(async () => {
                        await semaphore.WaitAsync();
                        MsdialLcMsApi.Process.FileProcess.Run(analysisfile, storage, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v);
                        Interlocked.Increment(ref counter);
                        vm.CurrentValue = counter;
                        semaphore.Release();
                    });
                }
                semaphore.Release(numThreads);

                await Task.WhenAll(tasks);

                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        public DelegateCommand<Window> OpenProjectCommand {
            get => openProjectCommand ?? (openProjectCommand = new DelegateCommand<Window>(OpenProject));
        }
        private DelegateCommand<Window> openProjectCommand;

        private void OpenProject(Window owner) {
            var ofd = new OpenFileDialog();
            ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2|All(*)|*";
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

                Storage = Serializer.LoadMsdialDataStorageBase(ofd.FileName);
                if (Storage == null) {
                    MessageBox.Show("Msdial cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                LoadInitialFiles();

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
            Serializer.SaveMsdialDataStorage(Storage.ParameterBase.ProjectFilePath, Storage);
        }

        #endregion

        #region
        private void LoadInitialFiles() {
            FileVM = new AnalysisFileVM(Storage.AnalysisFiles.FirstOrDefault(), Storage.ParameterBase, Storage.MspDB);
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(Storage.AnalysisFiles);
        }
        #endregion
    }
}
