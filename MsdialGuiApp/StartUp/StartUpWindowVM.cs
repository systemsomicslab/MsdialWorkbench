using CompMs.App.Msdial.Common;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.StartUp
{
    class StartUpWindowVM : ViewModelBase {
        public string ProjectFilePath {
            get => projectFilePath;
            set => SetProperty(ref projectFilePath, value);
        }
        private string projectFilePath;

        public Ionization Ionization {
            get => ionization;
            set => SetProperty(ref ionization, value);
        }
        private Ionization ionization = Ionization.ESI;

        public SeparationType SeparationType {
            get => separationType;
            set => SetProperty(ref separationType, value);
        }
        private SeparationType separationType = SeparationType.Chromatography;

        public bool SeparationChromatography {
            get => GetSeparation(SeparationType.Chromatography);
            set {
                SetSepration(SeparationType.Chromatography, value);
                OnPropertyChanged(nameof(SeparationChromatography));
            }
        }
        public bool SeparationIonMobility {
            get => GetSeparation(SeparationType.IonMobility);
            set {
                SetSepration(SeparationType.IonMobility, value);
                OnPropertyChanged(nameof(SeparationIonMobility));
            }
        }

        private bool GetSeparation(SeparationType flag) => (separationType & flag) != 0;
        private void SetSepration(SeparationType flag, bool value) {
            if (value)
                separationType |= flag;
            else
                separationType &= ~flag;
        }

        public AcquisitionType AcquisitionType {
            get => acquisitionType;
            set => SetProperty(ref acquisitionType, value);
        }
        private AcquisitionType acquisitionType = AcquisitionType.DDA;

        public MSDataType MS1DataType {
            get => ms1DataType;
            set => SetProperty(ref ms1DataType, value);
        }
        private MSDataType ms1DataType = MSDataType.Profile;

        public MSDataType MS2DataType {
            get => ms2DataType;
            set => SetProperty(ref ms2DataType, value);
        }
        private MSDataType ms2DataType = MSDataType.Profile;

        public IonMode IonMode {
            get => ionMode;
            set => SetProperty(ref ionMode, value);
        }
        private IonMode ionMode = IonMode.Positive;


        public TargetOmics TargetOmics {
            get => targetOmics;
            set => SetProperty(ref targetOmics, value);
        }
        private TargetOmics targetOmics = TargetOmics.Metabolomics;

        public string InstrumentType {
            get => instrumentType;
            set => SetProperty(ref instrumentType, value);
        }
        private string instrumentType = string.Empty;

        public string Instrument {
            get => instrument;
            set => SetProperty(ref instrument, value);
        }
        private string instrument = string.Empty;

        public string Authors {
            get => authors;
            set => SetProperty(ref authors, value);
        }
        private string authors = string.Empty;

        public string License {
            get => license;
            set => SetProperty(ref license, value);
        }
        private string license = string.Empty;

        public string CollisionEnergy {
            get => collisionEnergy;
            set => SetProperty(ref collisionEnergy, value);
        }
        private string collisionEnergy = string.Empty;

        public string Comment {
            get => comment;
            set => SetProperty(ref comment, value);
        }
        private string comment = string.Empty;

        public StartUpWindowVM() {
            PropertyChanged += OnProjectPropertyChanged;
            //ProjectFilePath = @"E:\6_Projects\PROJECT_ChemProteomics\20210217_Imami_Lox15\jPOST_files_JPST001097.0\test.mtd";
        }

        #region Commnad
        public DelegateCommand ProjectFolderSelectCommand {
            get => projectFolderSelectCommand ?? (projectFolderSelectCommand = new DelegateCommand(ProjectFolderSelect));
        }
        private DelegateCommand projectFolderSelectCommand;

        private void ProjectFolderSelect() {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Choose a project folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                var dt = DateTime.Now;
                ProjectFilePath = Path.Combine(fbd.SelectedPath, dt.ToString("yyyy_MM_dd_HH_mm_ss") + "." + SaveFileFormat.mtd);
            }
        }

        public DelegateCommand<Window> ContinueProcessCommand {
            get => continueProcessCommand ?? (continueProcessCommand = new DelegateCommand<Window>(ContinueProcess, ValidateStartUpWindow));
        }
        private DelegateCommand<Window> continueProcessCommand;

        private void ContinueProcess(Window window) {
            if (TryContiueProcess()) {
                window.DialogResult = true;
                window.Close();
            }
        }

        public bool HasViewError {
            get => hasViewError;
            set => SetProperty(ref hasViewError, value);
        }
        private bool hasViewError = false;

        private bool ValidateStartUpWindow(Window window) {
            if (HasViewError)
                return false;
            if (HasValidationErrors)
                return false;
            if (ProjectFilePath != null && Path.GetInvalidPathChars().Any(invalidChar => ProjectFilePath.Contains(invalidChar)))
                return false;
            if (string.IsNullOrEmpty(ProjectFilePath))
                return false;
            else if (!Directory.Exists(Path.GetDirectoryName(ProjectFilePath)))
                return false;
            return true;
        }
        #endregion

        #region Event
        private void OnProjectPropertyChanged(object sender, PropertyChangedEventArgs e) {
            ContinueProcessCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region validation
        private bool TryContiueProcess()
        {
            if (!Directory.Exists(Path.GetDirectoryName(ProjectFilePath))) {
                MessageBox.Show("Your project folder is not existed. Browse a folder containing ABF, mzML, or netCDF files.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Ionization == Ionization.ESI && TargetOmics == TargetOmics.Lipidomics)
            {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly).Length != 1)
                {
                    MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(Comment))
                Comment = Comment.Replace("\r", "").Replace("\n", " ");

            return true;
        }
        #endregion
    }
}
