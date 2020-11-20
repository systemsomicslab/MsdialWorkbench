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

namespace CompMs.App.Msdial
{
    class MainWindowVM : ViewModelBase
    {
        #region property
        public MsdialDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        #endregion

        #region field
        private MsdialDataStorage storage;
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

            Console.WriteLine(string.Join("\n", Storage.ParameterBase.ParametersAsText()));
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
        #endregion
    }
}
