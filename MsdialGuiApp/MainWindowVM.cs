using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
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
            storage = new MsdialDataStorage();

            //Get IUPAC reference
            storage.IupacDatabase = IupacResourceParser.GetIUPACDatabase();

            // Set parameterbase
            storage.ParameterBase = ProcessStartUp(window);
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
        #endregion
    }
}
