using CompMs.Common.MessagePack;
using Microsoft.Win32;
using Riken.Metabolomics.Msdial.Pathway;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for LipidPathwaySetWin.xaml
    /// </summary>
    public partial class MetabolicPathwaySetWin : Window {
        public MetabolicPathwaySetWin(MainWindow mainWindow) {
            InitializeComponent();
            this.DataContext = new MetabolicPathwaySetVM(mainWindow);
        }
    }

    public class MetabolicPathwaySetVM : ViewModelBase {
        MainWindow MainWindow;
        public MetabolicPathwaySetVM(MainWindow mainWindow) {
            this.MainWindow = mainWindow;
            this.AnimalPathway = true;
            if (this.MainWindow.ProjectProperty.TargetOmics == TargetOmics.Metablomics)
                this.IsMainInChIKey = true;
            else
                this.IsMainInChIKey = false;
        }

        private bool animalPathway;
        public bool AnimalPathway {
            get { return animalPathway; }
            set { animalPathway = value; OnPropertyChanged("AnimalPathway"); }
        }

        private bool plantPathway;
        public bool PlantPathway {
            get { return plantPathway; }
            set { plantPathway = value; OnPropertyChanged("PlantPathway"); }
        }

        private bool userPathway;
        public bool UserPathway {
            get { return userPathway; }
            set { userPathway = value; OnPropertyChanged("UserPathway"); }
        }

        private string userPathwayFilePath;
        public string UserPathwayFilePath {
            get { return userPathwayFilePath; }
            set { userPathwayFilePath = value; OnPropertyChanged("UserPathwayFilePath"); }
        }

        private int project1SelectedResultIndex;
        public int Project1SelectedResultIndex {
            get { return project1SelectedResultIndex; }
            set { project1SelectedResultIndex = value; OnPropertyChanged("Project1SelectedResultIndex"); }
        }

        private int project2SelectedResultIndex;
        public int Project2SelectedResultIndex {
            get { return project2SelectedResultIndex; }
            set { project2SelectedResultIndex = value; OnPropertyChanged("Project2SelectedResultIndex"); }
        }

        private int project3SelectedResultIndex;
        public int Project3SelectedResultIndex {
            get { return project3SelectedResultIndex; }
            set { project3SelectedResultIndex = value; OnPropertyChanged("Project3SelectedResultIndex"); }
        }

        private int project4SelectedResultIndex;
        public int Project4SelectedResultIndex {
            get { return project4SelectedResultIndex; }
            set { project4SelectedResultIndex = value; OnPropertyChanged("Project4SelectedResultIndex"); }
        }

        private bool isMainInChIKey;
        public bool IsMainInChIKey {
            get { return isMainInChIKey; }
            set { isMainInChIKey = value; OnPropertyChanged("IsMainInChIKey"); }
        }

        private ObservableCollection<AlignmentFileBean> alignedResults1;
        public ObservableCollection<AlignmentFileBean> AlignedResults1 {
            get { return alignedResults1; }
            set { alignedResults1 = value; OnPropertyChanged("AlignedResults1"); }
        }
        public ObservableCollection<AnalysisFileBean> OtherFiles1 { get; set; }
        public List<MspFormatCompoundInformationBean> MspDB1 { get; set; }
        public List<PostIdentificatioinReferenceBean> TextDB1 { get; set; }
        private bool isProject1InChIKey;
        public bool IsProject1InChIKey {
            get { return isProject1InChIKey; }
            set { isProject1InChIKey = value; OnPropertyChanged("IsProject1InChIKey"); }
        }

        private ObservableCollection<AlignmentFileBean> alignedResults2;
        public ObservableCollection<AlignmentFileBean> AlignedResults2 {
            get { return alignedResults2; }
            set { alignedResults2 = value; OnPropertyChanged("AlignedResults2"); }
        }
        public ObservableCollection<AnalysisFileBean> OtherFiles2 { get; set; }
        public List<MspFormatCompoundInformationBean> MspDB2 { get; set; }
        public List<PostIdentificatioinReferenceBean> TextDB2 { get; set; }
        private bool isProject2InChIKey;
        public bool IsProject2InChIKey {
            get { return isProject2InChIKey; }
            set { isProject2InChIKey = value; OnPropertyChanged("IsProject2InChIKey"); }
        }

        private ObservableCollection<AlignmentFileBean> alignedResults3;
        public ObservableCollection<AlignmentFileBean> AlignedResults3 {
            get { return alignedResults3; }
            set { alignedResults3 = value; OnPropertyChanged("AlignedResults3"); }
        }
        public ObservableCollection<AnalysisFileBean> OtherFiles3 { get; set; }
        public List<MspFormatCompoundInformationBean> MspDB3 { get; set; }
        public List<PostIdentificatioinReferenceBean> TextDB3 { get; set; }
        private bool isProject3InChIKey;
        public bool IsProject3InChIKey {
            get { return isProject3InChIKey; }
            set { isProject3InChIKey = value; OnPropertyChanged("IsProject3InChIKey"); }
        }

        private ObservableCollection<AlignmentFileBean> alignedResults4;
        public ObservableCollection<AlignmentFileBean> AlignedResults4 {
            get { return alignedResults4; }
            set { alignedResults4 = value; OnPropertyChanged("AlignedResults4"); }
        }
        public ObservableCollection<AnalysisFileBean> OtherFiles4 { get; set; }
        public List<MspFormatCompoundInformationBean> MspDB4 { get; set; }
        public List<PostIdentificatioinReferenceBean> TextDB4 { get; set; }
        private bool isProject4InChIKey;
        public bool IsProject4InChIKey {
            get { return isProject4InChIKey; }
            set { isProject4InChIKey = value; OnPropertyChanged("IsProject4InChIKey"); }
        }

        //delegate
        private DelegateCommand loadUserPathway;
        public DelegateCommand LoadUserPathway {
            get {
                return loadUserPathway ?? (loadUserPathway = new DelegateCommand(obj => {
                    var ofd = new OpenFileDialog();
                    ofd.Filter = "Graphml file(*.graphml)|*.graphml|Gpml file(*.gpml)|*.gpml";
                    ofd.Title = "Import a pathway format file";
                    ofd.RestoreDirectory = true;
                    ofd.Multiselect = false;

                    if (ofd.ShowDialog() == true) {
                        this.UserPathwayFilePath = ofd.FileName;
                    }
                }, obj => { return true; }));
            }
        }

        private DelegateCommand mapping;
        public DelegateCommand Mapping {
            get {
                return mapping ?? (mapping = new DelegateCommand(obj => {
                    var view = (MetabolicPathwaySetWin)obj;
                    AlignmentResultBean OtherResult1 = null;
                    AlignmentResultBean OtherResult2 = null;
                    AlignmentResultBean OtherResult3 = null;
                    AlignmentResultBean OtherResult4 = null;
                    if (alignedResults1 != null && alignedResults1.Count != 0 && project1SelectedResultIndex <= alignedResults1.Count -1) {
                        var filepath = alignedResults1[project1SelectedResultIndex].FilePath;
                        var errorString = string.Empty;
                        OtherResult1 = MessagePackHandler.LoadFromFile<AlignmentResultBean>(filepath);
                    }

                    if (alignedResults2 != null && alignedResults2.Count != 0 && project2SelectedResultIndex <= alignedResults2.Count - 1) {
                        var filepath = alignedResults2[project2SelectedResultIndex].FilePath;
                        var errorString = string.Empty;
                        OtherResult2 = MessagePackHandler.LoadFromFile<AlignmentResultBean>(filepath);
                    }

                    if (alignedResults3 != null && alignedResults3.Count != 0 && project3SelectedResultIndex <= alignedResults3.Count - 1) {
                        var filepath = alignedResults3[project3SelectedResultIndex].FilePath;
                        var errorString = string.Empty;
                        OtherResult3 = MessagePackHandler.LoadFromFile<AlignmentResultBean>(filepath);
                    }

                    if (alignedResults4 != null && alignedResults4.Count != 0 && project4SelectedResultIndex <= alignedResults4.Count - 1) {
                        var filepath = alignedResults4[project4SelectedResultIndex].FilePath;
                        var errorString = string.Empty;
                        OtherResult4 = MessagePackHandler.LoadFromFile<AlignmentResultBean>(filepath);
                    }

                    Mouse.OverrideCursor = Cursors.Wait;
                    if (this.userPathway) {
                        MappingToPathways.PathwayMapping(this.UserPathwayFilePath, this.MainWindow, this.IsMainInChIKey,
                               OtherFiles1, OtherResult1, MspDB1, TextDB1, this.IsProject1InChIKey,
                               OtherFiles2, OtherResult2, MspDB2, TextDB2, this.IsProject2InChIKey,
                               OtherFiles3, OtherResult3, MspDB3, TextDB3, this.IsProject3InChIKey,
                               OtherFiles4, OtherResult4, MspDB4, TextDB4, this.IsProject4InChIKey);
                    }
                    else {
                        var gpmlMap = getGpmlFile();
                        MappingToPathways.PathwayMapping(gpmlMap, ".gpml", this.MainWindow, this.IsMainInChIKey,
                               OtherFiles1, OtherResult1, MspDB1, TextDB1, this.IsProject1InChIKey,
                               OtherFiles2, OtherResult2, MspDB2, TextDB2, this.IsProject2InChIKey,
                               OtherFiles3, OtherResult3, MspDB3, TextDB3, this.IsProject3InChIKey,
                               OtherFiles4, OtherResult4, MspDB4, TextDB4, this.IsProject4InChIKey);
                    }
                  
                    Mouse.OverrideCursor = null;
                    view.Close();

                }, obj => { return true; }));
            }
        }

        private Stream getGpmlFile() {

            var inchikeyCounter = 0;
            var metaboliteNameCounter = 0;
            if (this.IsMainInChIKey) inchikeyCounter++;
            else metaboliteNameCounter++;

            if (OtherFiles1 != null && OtherFiles1.Count != 0) {
                if (this.IsProject1InChIKey) inchikeyCounter++;
                else metaboliteNameCounter++;
            }

            if (OtherFiles2 != null && OtherFiles2.Count != 0) {
                if (this.IsProject2InChIKey) inchikeyCounter++;
                else metaboliteNameCounter++;
            }

            if (OtherFiles3 != null && OtherFiles3.Count != 0) {
                if (this.IsProject3InChIKey) inchikeyCounter++;
                else metaboliteNameCounter++;
            }

            if (OtherFiles4 != null && OtherFiles4.Count != 0) {
                if (this.IsProject4InChIKey) inchikeyCounter++;
                else metaboliteNameCounter++;
            }

            if (metaboliteNameCounter == 0) { // inchikey name base
                if (this.AnimalPathway) {
                    var fileUri = new Uri("/Resources/MetabolicMapForAnimal.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
                else {
                    var fileUri = new Uri("/Resources/MetabolicMapForPlant.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
            }
            else if (inchikeyCounter == 0) { // lipid name base
                if (this.AnimalPathway) {
                    var fileUri = new Uri("/Resources/LipidMapForAnimal.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
                else {
                    var fileUri = new Uri("/Resources/LipidMapForPlant.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
            }
            else {
                if (this.AnimalPathway) {
                    var fileUri = new Uri("/Resources/GlobalMapForAnimal.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
                else {
                    var fileUri = new Uri("/Resources/GlobalMapForPlant.gpml", UriKind.Relative);
                    var info = Application.GetResourceStream(fileUri);
                    return info.Stream;
                }
            }
        }

        private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        private DelegateCommand loadProject1;
        public DelegateCommand LoadProject1 {
            get {
                return loadProject1 ?? (loadProject1 = new DelegateCommand(obj => {
                    var ofd = new OpenFileDialog();
                    ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2";
                    ofd.Title = "Import a project file";
                    ofd.RestoreDirectory = true;

                    if (ofd.ShowDialog() == true) {
                        var filepath = ofd.FileName;
                        var errorString = string.Empty;
                        if (ErrorHandler.IsFileLocked(filepath, out errorString)) {
                            MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        var saveProp = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                        if (saveProp == null) {
                            MessageBox.Show(this.MainWindow.Title + " cannot open the project: \n" + filepath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else {
                            this.AlignedResults1 = saveProp.AlignmentFileBeanCollection;
                            this.Project1SelectedResultIndex = 0;
                            this.OtherFiles1 = saveProp.AnalysisFileBeanCollection;
                            this.MspDB1 = saveProp.MspFormatCompoundInformationBeanList;
                            this.TextDB1 = saveProp.PostIdentificationReferenceBeanList;

                            if (saveProp.ProjectPropertyBean.TargetOmics == TargetOmics.Metablomics) {
                                this.IsProject1InChIKey = true;
                            }
                            else {
                                this.IsProject1InChIKey = false;
                            }
                        }
                        Mouse.OverrideCursor = null;
                    }


                }, obj => { return true; }));
            }
        }

        private DelegateCommand loadProject2;
        public DelegateCommand LoadProject2 {
            get {
                return loadProject2 ?? (loadProject2 = new DelegateCommand(obj => {
                    var ofd = new OpenFileDialog();
                    ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2";
                    ofd.Title = "Import a project file";
                    ofd.RestoreDirectory = true;

                    if (ofd.ShowDialog() == true) {
                        var filepath = ofd.FileName;
                        var errorString = string.Empty;
                        if (ErrorHandler.IsFileLocked(filepath, out errorString)) {
                            MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        var saveProp = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                        if (saveProp == null) {
                            MessageBox.Show(this.MainWindow.Title + " cannot open the project: \n" + filepath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else {
                            this.AlignedResults2 = saveProp.AlignmentFileBeanCollection;
                            this.Project2SelectedResultIndex = 0;
                            this.OtherFiles2 = saveProp.AnalysisFileBeanCollection;
                            this.MspDB2 = saveProp.MspFormatCompoundInformationBeanList;
                            this.TextDB2 = saveProp.PostIdentificationReferenceBeanList;

                            if (saveProp.ProjectPropertyBean.TargetOmics == TargetOmics.Metablomics) {
                                this.IsProject2InChIKey = true;
                            }
                            else {
                                this.IsProject2InChIKey = false;
                            }
                        }
                        Mouse.OverrideCursor = null;
                    }


                }, obj => { return true; }));
            }
        }

        private DelegateCommand loadProject3;
        public DelegateCommand LoadProject3 {
            get {
                return loadProject3 ?? (loadProject3 = new DelegateCommand(obj => {
                    var ofd = new OpenFileDialog();
                    ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2";
                    ofd.Title = "Import a project file";
                    ofd.RestoreDirectory = true;

                    if (ofd.ShowDialog() == true) {
                        var filepath = ofd.FileName;
                        var errorString = string.Empty;
                        if (ErrorHandler.IsFileLocked(filepath, out errorString)) {
                            MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        var saveProp = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                        if (saveProp == null) {
                            MessageBox.Show(this.MainWindow.Title + " cannot open the project: \n" + filepath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else {
                            this.AlignedResults3 = saveProp.AlignmentFileBeanCollection;
                            this.Project3SelectedResultIndex = 0;
                            this.OtherFiles3 = saveProp.AnalysisFileBeanCollection;
                            this.MspDB3 = saveProp.MspFormatCompoundInformationBeanList;
                            this.TextDB3 = saveProp.PostIdentificationReferenceBeanList;

                            if (saveProp.ProjectPropertyBean.TargetOmics == TargetOmics.Metablomics) {
                                this.IsProject3InChIKey = true;
                            }
                            else {
                                this.IsProject3InChIKey = false;
                            }
                        }
                        Mouse.OverrideCursor = null;
                    }


                }, obj => { return true; }));
            }
        }

        private DelegateCommand loadProject4;
        public DelegateCommand LoadProject4 {
            get {
                return loadProject4 ?? (loadProject4 = new DelegateCommand(obj => {
                    var ofd = new OpenFileDialog();
                    ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2";
                    ofd.Title = "Import a project file";
                    ofd.RestoreDirectory = true;

                    if (ofd.ShowDialog() == true) {
                        var filepath = ofd.FileName;
                        var errorString = string.Empty;
                        if (ErrorHandler.IsFileLocked(filepath, out errorString)) {
                            MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        var saveProp = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                        if (saveProp == null) {
                            MessageBox.Show(this.MainWindow.Title + " cannot open the project: \n" + filepath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else {
                            this.AlignedResults4 = saveProp.AlignmentFileBeanCollection;
                            this.Project4SelectedResultIndex = 0;
                            this.OtherFiles4 = saveProp.AnalysisFileBeanCollection;
                            this.MspDB4 = saveProp.MspFormatCompoundInformationBeanList;
                            this.TextDB4 = saveProp.PostIdentificationReferenceBeanList;

                            if (saveProp.ProjectPropertyBean.TargetOmics == TargetOmics.Metablomics) {
                                this.IsProject4InChIKey = true;
                            }
                            else {
                                this.IsProject4InChIKey = false;
                            }
                        }
                        Mouse.OverrideCursor = null;
                    }


                }, obj => { return true; }));
            }
        }

      
    }
}
