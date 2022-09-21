using CompMs.Common.MessagePack;
using Msdial.Lcms.Dataprocess.Utility;
using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for SplashSetWin.xaml
    /// </summary>
    public partial class SplashSetWin : Window {

        public SplashSetVM SplashSetVM { get; set; }

        public SplashSetWin() {
            InitializeComponent();
        }

        public SplashSetWin(MainWindow mainWindow) {
            InitializeComponent();
            
            this.SplashSetVM = new SplashSetVM(mainWindow);
            this.DataContext = this.SplashSetVM;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void ComboBox_SplashProducts_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataContext == null) return;
            var index = ((ComboBox)sender).SelectedIndex;
            this.SplashSetVM.SplashProductSelectionChanged(index);
        }

        private void ComboBox_OutputUnit_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataContext == null) return;
            var index = ((ComboBox)sender).SelectedIndex;
            this.SplashSetVM.OutputUnitChanged(index);
        }

        private void Datagrid_SplashProperty_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_SplashProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_SplashProperty.BeginEdit();
        }

        private void Datagrid_SplashProperty_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;

                var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                var clipTextList = new List<string[]>();

                foreach (var clip in clipText) { clipTextList.Add(clip.Split('\t')); }

                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                var startRow = this.Datagrid_SplashProperty.Items.IndexOf(this.Datagrid_SplashProperty.SelectedCells[0].Item);
                double doubleValue;

                if (clipTextList.Count > 1 && this.Datagrid_SplashProperty.SelectedCells[0].Column.DisplayIndex == 0) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_SplashProperty.Items.Count - 1) break;

                        ((StandardCompound)this.Datagrid_SplashProperty.Items[startRow + i]).StandardName = clipTextList[i][0];

                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out doubleValue)) {
                                ((StandardCompound)this.Datagrid_SplashProperty.Items[startRow + i]).Concentration = doubleValue;
                            }
                        }
                    }
                }
                else if(clipTextList.Count > 1 && this.Datagrid_SplashProperty.SelectedCells[0].Column.DisplayIndex == 1) {
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.Datagrid_SplashProperty.Items.Count - 1) break;
                        if (double.TryParse(clipTextList[i][0], out doubleValue)) {
                            ((StandardCompound)this.Datagrid_SplashProperty.Items[startRow + i]).Concentration = doubleValue;
                        }
                    }
                }

                this.Datagrid_SplashProperty.UpdateLayout();

            }
        }

        private void MenuItem_AutoFill_Click(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_SplashProperty;
            var currentItem = (StandardCompound)grid.CurrentItem;
            var currentID = grid.Items.IndexOf(currentItem);

            for (int i = currentID; i < grid.Items.Count; i++) {
                var propVM = (StandardCompound)grid.Items[i];
                propVM.Concentration = currentItem.Concentration;
            }

            this.Datagrid_SplashProperty.CommitEdit();
            this.Datagrid_SplashProperty.CommitEdit();
            this.Datagrid_SplashProperty.Items.Refresh();
        }

        private void MenuItem_CopyToClipboard(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_SplashProperty;
            var currentItem = (StandardCompound)grid.CurrentItem;

            Clipboard.SetText(currentItem.Concentration.ToString());
        }

        private void MenuItem_Paste(object sender, RoutedEventArgs e) {
            var grid = this.Datagrid_SplashProperty;
            var currentItem = (StandardCompound)grid.CurrentItem;

            e.Handled = true;
            var clipboardArrayList = getClipboardContentAsStringArrayList();
            if (clipboardArrayList == null || clipboardArrayList.Count == 0 || clipboardArrayList[0].Length == 0) return;

            double volume = 0;
            if (double.TryParse(clipboardArrayList[0][0], out volume)) {
                currentItem.Concentration = volume;
            }
        }

        private List<string[]> getClipboardContentAsStringArrayList() {
            var clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
            var clipTextList = new List<string[]>();
            for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
            if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

            return clipTextList;
        }
    }

    public class SplashSetVM : ViewModelBase {

        public MainWindow MainWindow { get; set; }
        public ProjectPropertyBean ProjectProp { get; set; }
        public AlignmentResultBean AlignmentResult { get; set; }
        public AnalysisParametersBean ParamLC { get; set; }
        public AnalysisParamOfMsdialGcms ParamGC { get; set; }
        public List<object> GlobalAlignedSpots { get; set; } // merged list of alignmentpropertybean (RT) and aligneddriftspotpropertybean (ion mobility)

        private List<string> splashProducts;
        public List<string> SplashProducts {
            get {
                return splashProducts;
            }

            set {
                splashProducts = value;
                OnPropertyChanged("SplashProducts");
            }
        }

        private int splashProductID;
        public int SplashProductID {
            get {
                return splashProductID;
            }

            set {
                splashProductID = value;
                OnPropertyChanged("SplashProductID");
            }
        }

        private List<StandardCompound> standardCompounds;
        public List<StandardCompound> StandardCompounds {
            get {
                return standardCompounds;
            }

            set {
                standardCompounds = value;
                OnPropertyChanged("StandardCompounds");
            }
        }

        private List<string> targetClasses;
        public List<string> TargetClasses {
            get {
                return targetClasses;
            }

            set {
                targetClasses = value;
                OnPropertyChanged("TargetClasses");
            }
        }

        private List<string> outputUnits;
        public List<string> OutputUnits {
            get {
                return outputUnits;
            }

            set {
                outputUnits = value;
                OnPropertyChanged("OutputUnits");
            }
        }

        private int outputUnitID;
        public int OutputUnitID {
            get {
                return outputUnitID;
            }

            set {
                outputUnitID = value;
                OnPropertyChanged("OutputUnitID");
            }
        }

      

        public SplashSetVM(MainWindow mainWindow) {
            this.MainWindow = mainWindow;
            this.AlignmentResult = mainWindow.FocusedAlignmentResult;
            this.GlobalAlignedSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(this.AlignmentResult.AlignmentPropertyBeanCollection);
            this.ProjectProp = mainWindow.ProjectProperty;
            if (this.ProjectProp.Ionization == Ionization.ESI) {
                this.ParamLC = this.MainWindow.AnalysisParamForLC;
            }
            else {
                this.ParamGC = this.MainWindow.AnalysisParamForGC;
            }

            var targetMetabolites = LipidomicsConverter.GetLipidClasses();
            targetMetabolites.Add("Any others");

            this.TargetClasses = targetMetabolites;

            this.SplashProducts = new List<string>() { "EquiSPLASH", "SPLASH LIPIDOMIX", "SPLASH II LIPIDOMIX" };
            if (this.ProjectProp.IsLabPrivateVersion) {
                this.SplashProducts = new List<string>() { 
                    "EquiSPLASH", "SPLASH LIPIDOMIX", "SPLASH II LIPIDOMIX", "EquiSPLASH(TUAT)"
                };
            }
            this.OutputUnits = new List<string>() {
                "nmol/μL plasma", "pmol/μL plasma", "fmol/μL plasma",
                "nmol/mg tissue", "pmol/mg tissue", "fmol/mg tissue",
                "nmol/10^6 cells", "pmol/10^6 cells", "fmol/10^6 cells",
                "nmol/individual", "pmol/individual", "fmol/individual",
                "nmol/μg protein", "pmol/μg protein", "fmol/μg protein"
            };
            this.OutputUnitID = 1;
            if (this.ProjectProp.Ionization == Ionization.ESI) {
                if (this.ParamLC.StandardCompounds == null || this.ParamLC.StandardCompounds.Count == 0) {
                    SplashProductSelectionChanged(0);
                }
                else {
                    var compounds = getStandardsCopy(this.ParamLC.StandardCompounds);
                    this.StandardCompounds = compounds;
                }
            }
            else {
                if (this.ParamGC.StandardCompounds == null || this.ParamGC.StandardCompounds.Count == 0) {
                    SplashProductSelectionChanged(0);
                }
                else {
                    var compounds = getStandardsCopy(this.ParamGC.StandardCompounds);
                    this.StandardCompounds = compounds;
                }
            }
        }

        private List<StandardCompound> getStandardsCopy(List<StandardCompound> standardCompounds) {
            var compounds = new List<StandardCompound>();
            foreach (var compound in standardCompounds) {
                compounds.Add(new StandardCompound() {
                    Concentration = compound.Concentration,
                    DilutionRate = compound.DilutionRate,
                    MolecularWeight = compound.MolecularWeight,
                    PeakID = compound.PeakID,
                    StandardName = compound.StandardName,
                    TargetClass = compound.TargetClass
                });
            }

            return compounds;
        }

        public void OutputUnitChanged(int id) {
            this.OutputUnitID = id;
        }

        private DelegateCommand finish;
        public DelegateCommand Finish {
            get {
                return finish ?? (finish = new DelegateCommand(obj => {
                    var view = (SplashSetWin)obj;

                    var compounds = new List<StandardCompound>();
                   
                    foreach (var comp in this.StandardCompounds) {
                        if (isRequiredFieldFilled(comp)) {
                            compounds.Add(comp);
                        }
                    }

                    if (compounds.Count == 0) {
                        MessageBox.Show("Please fill the required fields for normalization", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // ion mobility data check
                    if (this.ParamLC != null && this.ParamLC.IsIonMobility) {
                        foreach (var comp in compounds) {
                            var gSpot = this.GlobalAlignedSpots[comp.PeakID];
                            if (gSpot.GetType() == typeof(AlignmentPropertyBean)) {
                                MessageBox.Show("Please add the peak ID of ion mobility axis instead of that of retention time axis", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        var compoundsOnRt = new List<StandardCompound>();
                        foreach (var comp in compounds) {
                            var dSpot = (AlignedDriftSpotPropertyBean)this.GlobalAlignedSpots[comp.PeakID];
                            var idOnAlignedSpots = dSpot.AlignmentSpotID;
                            var masterIdOnAlignedSpots = this.AlignmentResult.AlignmentPropertyBeanCollection[idOnAlignedSpots].MasterID;
                            compoundsOnRt.Add(new StandardCompound() {
                                Concentration = comp.Concentration,
                                DilutionRate = comp.DilutionRate,
                                MolecularWeight = comp.MolecularWeight,
                                StandardName = comp.StandardName,
                                TargetClass = comp.TargetClass,
                                PeakID = masterIdOnAlignedSpots
                            });
                        }
                        foreach (var comp in compoundsOnRt) compounds.Add(comp);
                    }


                    if (this.ProjectProp.Ionization == Ionization.EI) {
                        this.ParamGC.StandardCompounds = compounds;
                    }
                    else {
                        this.ParamLC.StandardCompounds = compounds;
                    }
                    //MsDialDataNormalization.SplashNormalization(this.MainWindow.AnalysisFiles, this.AlignmentResult, this.MainWindow.MspDB, this.ParamLC, this.OutputUnits[this.OutputUnitID]);
                    var unitObj = MsDialStatistics.GetAbundanceUnitEnum(this.OutputUnits[this.OutputUnitID]);
                    MsDialDataNormalization.SplashNormalization(this.MainWindow.AnalysisFiles, this.AlignmentResult, this.MainWindow.MspDB, 
                        this.MainWindow.PostIdentificationTxtDB, this.ParamLC, unitObj);

                    #region
                    this.AlignmentResult.Normalized = true;
                    MessagePackHandler.SaveToFile<AlignmentResultBean>(this.AlignmentResult, this.MainWindow.AlignmentFiles[this.MainWindow.FocusedAlignmentFileID].FilePath);
                    this.MainWindow.BarChartDisplayMode = BarChartDisplayMode.NormalizedHeight;
                    this.MainWindow.menuItemRefresh();
                    this.MainWindow.Update_BarChart();
                    this.MainWindow.Reset_AlignmentTableViewer();
                    #endregion

                    view.Close();
                }, CanRun));
            }
        }

      

        private DelegateCommand find;
        public DelegateCommand Find {
            get {
                return find ?? (find = new DelegateCommand(obj => {
                    var view = (SplashSetWin)obj;
                    var compounds = new List<StandardCompound>();
                    foreach (var comp in this.StandardCompounds) {
                        if (comp.StandardName == null || comp.StandardName == string.Empty) continue;
                        var standard = comp.StandardName;
                        foreach (var spot in this.GlobalAlignedSpots) {
                            var id = -1;
                            var metabolite = string.Empty;
                            if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                                var rSpot = (AlignmentPropertyBean)spot;
                                if (this.ParamLC != null && this.ParamLC.IsIonMobility) continue;
                                metabolite = rSpot.MetaboliteName;
                                id = this.ParamLC != null && this.ParamLC.IsIonMobility ? rSpot.MasterID : rSpot.AlignmentID;
                            }
                            else {
                                var dSpot = (AlignedDriftSpotPropertyBean)spot;
                                metabolite = dSpot.MetaboliteName;
                                id = this.ParamLC != null && this.ParamLC.IsIonMobility ? dSpot.MasterID : dSpot.AlignmentID;
                            }

                            if (metabolite == null || metabolite == string.Empty || metabolite.Contains("w/o")) continue;
                            if (metabolite.Contains(standard)) {
                                comp.PeakID = id;
                                break;
                            }
                        }
                    }
                    //view.Datagrid_SplashProperty.Items.Refresh();
                    view.Datagrid_SplashProperty.CommitEdit();
                    view.Datagrid_SplashProperty.CommitEdit();
                    view.Datagrid_SplashProperty.Items.Refresh();
                    Finish.RaiseCanExecuteChanged();

                }, (obj) => { return true; }));
            }
        }

        private bool CanRun(object obj) {
            var compounds = this.standardCompounds;
            var flg = false;
            foreach (var compound in compounds) {
                if (isRequiredFieldFilled(compound)) {
                    flg = true;
                }
            }

            if (flg) return true;
            else return false;
        }

        private bool isRequiredFieldFilled(StandardCompound compound) {
            //if (compound.MolecularWeight <= 0) return false;
            if (compound.Concentration <= 0) return false;
            if (compound.TargetClass == null || compound.TargetClass == string.Empty) return false;
            //if (compound.DilutionRate <= 0) return false;
            if (compound.PeakID < 0 || compound.PeakID > this.GlobalAlignedSpots.Count - 1) return false;
            return true;
        }

        public void SplashProductSelectionChanged(int id) {
            if (id != 0 && id != 1 && id != 2 && id != 3) return;
            var compounds = new List<StandardCompound>();
            if (id == 0) {

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 33.19585,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "PC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 35.1605,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "PE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 33.19585,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "EtherPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 35.1605,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "EtherPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                    Concentration = 32.1742,
                    DilutionRate = 0.015625,
                    MolecularWeight = 777.02,
                    PeakID = -1,
                    TargetClass = "PS"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                    Concentration = 32.7217,
                    DilutionRate = 0.015625,
                    MolecularWeight = 764.02,
                    PeakID = -1,
                    TargetClass = "PG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                    Concentration = 29.5114,
                    DilutionRate = 0.015625,
                    MolecularWeight = 847.13,
                    PeakID = -1,
                    TargetClass = "PI"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 47.284175,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "LPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 47.284175,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "EtherLPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 51.372975,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "LPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 51.372975,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "EtherLPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "CE 18:1(d7)",
                    Concentration = 37.98468,
                    DilutionRate = 0.015625,
                    MolecularWeight = 658.16,
                    PeakID = -1,
                    TargetClass = "CE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "MG 18:1(d7)",
                    Concentration = 68.758775,
                    DilutionRate = 0.015625,
                    MolecularWeight = 363.59,
                    PeakID = -1,
                    TargetClass = "MG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                    Concentration = 42.5186,
                    DilutionRate = 0.015625,
                    MolecularWeight = 587.978,
                    PeakID = -1,
                    TargetClass = "DG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                    Concentration = 30.7743,
                    DilutionRate = 0.015625,
                    MolecularWeight = 812.366,
                    PeakID = -1,
                    TargetClass = "TG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                    Concentration = 33.8691,
                    DilutionRate = 0.015625,
                    MolecularWeight = 738.136,
                    PeakID = -1,
                    TargetClass = "SM"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                    Concentration = 47.087175,
                    DilutionRate = 0.015625,
                    MolecularWeight = 530.93,
                    PeakID = -1,
                    TargetClass = "Cer-NS"
                });


                if (this.ProjectProp.IsLabPrivateVersion) {
                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 160,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "FA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 160,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "OxFA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 160,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "FAHFA"
                    });
                }
                else {
                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "FA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "OxFA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "FAHFA"
                    });
                }



                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 47.284175,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "Any others"
                });

                if (this.ProjectProp.IsLabPrivateVersion) {
                    // level 3
                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CAR"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "ADGGA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BASulfate"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BileAcid"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "ST"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CerP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Cholesterol"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CoQ"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGCC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGGA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGTS"
                    });

                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGTA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "EtherMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "EtherDGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GM3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GM1"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD1a"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD2"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GQ1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GT1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NGcGM3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGCC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGTS"
                    });

                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGTA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LPA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "MGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "MGMG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGMG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAGly"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAGlySer"
                    });

                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAOrn"
                    });
                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAPhe"
                    });
                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NATau"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GPNAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PEtOH"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PMeOH"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BAHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPEHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPGHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BRSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CASLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SISLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "STSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BRSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CASPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SISPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "STSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SQDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SSulfate"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "VAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Vitamin_D"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 47.284175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Vitamin_E"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                        Concentration = 33.19585,
                        DilutionRate = 0.015625,
                        MolecularWeight = 753.106,
                        PeakID = -1,
                        TargetClass = "OxPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                        Concentration = 33.19585,
                        DilutionRate = 0.015625,
                        MolecularWeight = 753.106,
                        PeakID = -1,
                        TargetClass = "EtherOxPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 35.1605,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "OxPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 35.1605,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "LNAPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 35.1605,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "EtherOxPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "BMP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "CL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "EtherLPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "EtherPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "HBMP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "MLCL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "LPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 32.7217,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "OxPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 29.5114,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "EtherPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 29.5114,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "LPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 29.5114,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "OxPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 32.1742,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "EtherPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 32.1742,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "LNAPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 32.1742,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "LPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 32.1742,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "OxPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                        Concentration = 33.8691,
                        DilutionRate = 0.015625,
                        MolecularWeight = 738.136,
                        PeakID = -1,
                        TargetClass = "ASM"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 30.7743,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "EtherTG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexCAS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexCS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexSIS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexSTS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexBRS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "BRSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "CASE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "GDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "GLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "TDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "TLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "SISE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "STSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "LCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "KLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "KDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "EGSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DEGSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 37.98468,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DSMSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 35.1605,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "MMPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 35.1605,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "DMPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 28.91795,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "MIPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 30.7743,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "OxTG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 30.7743,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "TG_EST"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "AHexCer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-ADS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-AP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-AS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-BDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-BS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EBDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EOS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EODS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-HDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-HS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-NDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-NP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "EtherSMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-AP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-EOS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-HDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-HS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-NDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-NS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Hex2Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Hex3Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PE-Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PhytoSph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PI-Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SHexCer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "DHSph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 47.087175,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Sph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                        Concentration = 42.5186,
                        DilutionRate = 0.015625,
                        MolecularWeight = 587.978,
                        PeakID = -1,
                        TargetClass = "EtherDG"
                    });
                }
            }
            else if (id == 1) {

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 106.224,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "PC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 3.52,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "PE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 106.224,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "EtherPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 3.52,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "EtherPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                    Concentration = 3.216,
                    DilutionRate = 0.015625,
                    MolecularWeight = 755.034,
                    PeakID = -1,
                    TargetClass = "PS"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                    Concentration = 19.632,
                    DilutionRate = 0.015625,
                    MolecularWeight = 742.035,
                    PeakID = -1,
                    TargetClass = "PG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                    Concentration = 5.904,
                    DilutionRate = 0.015625,
                    MolecularWeight = 830.097,
                    PeakID = -1,
                    TargetClass = "PI"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PA 33:1(d7)|PA 15:0_18:1(d7)",
                    Concentration = 5.072,
                    DilutionRate = 0.015625,
                    MolecularWeight = 667.956,
                    PeakID = -1,
                    TargetClass = "PA"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 23.648,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "LPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 5.136,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "LPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 23.648,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "EtherLPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 5.136,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "EtherLPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "CE 18:1(d7)",
                    Concentration = 265.904,
                    DilutionRate = 0.015625,
                    MolecularWeight = 658.16,
                    PeakID = -1,
                    TargetClass = "CE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "MG 18:1(d7)",
                    Concentration = 2.752,
                    DilutionRate = 0.015625,
                    MolecularWeight = 363.59,
                    PeakID = -1,
                    TargetClass = "MG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                    Concentration = 8.496,
                    DilutionRate = 0.015625,
                    MolecularWeight = 587.978,
                    PeakID = -1,
                    TargetClass = "DG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                    Concentration = 33.856,
                    DilutionRate = 0.015625,
                    MolecularWeight = 812.366,
                    PeakID = -1,
                    TargetClass = "TG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                    Concentration = 20.32,
                    DilutionRate = 0.015625,
                    MolecularWeight = 738.136,
                    PeakID = -1,
                    TargetClass = "SM"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "Cholesterol(d7)",
                    Concentration = 127.008,
                    DilutionRate = 0.015625,
                    MolecularWeight = 393.707,
                    PeakID = -1,
                    TargetClass = "Cholesterol"
                });
            }
            else if (id == 2) {
                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 106.224,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "PC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 3.52,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "PE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                    Concentration = 5.1456,
                    DilutionRate = 0.015625,
                    MolecularWeight = 755.034,
                    PeakID = -1,
                    TargetClass = "PS"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                    Concentration = 4.7232,
                    DilutionRate = 0.015625,
                    MolecularWeight = 830.097,
                    PeakID = -1,
                    TargetClass = "PI"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 23.648,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "LPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 0.5136,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "LPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 23.648,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "EtherLPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 0.5136,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "EtherLPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "CE 18:1(d7)",
                    Concentration = 265.904,
                    DilutionRate = 0.015625,
                    MolecularWeight = 658.16,
                    PeakID = -1,
                    TargetClass = "CE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PC O-36:2(d7)|PC O-18:1_18:1(d7)",
                    Concentration = 5.1203,
                    DilutionRate = 0.015625,
                    MolecularWeight = 781.201,
                    PeakID = -1,
                    TargetClass = "EtherPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                    Concentration = 10.1952,
                    DilutionRate = 0.015625,
                    MolecularWeight = 587.978,
                    PeakID = -1,
                    TargetClass = "DG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                    Concentration = 33.856,
                    DilutionRate = 0.015625,
                    MolecularWeight = 812.366,
                    PeakID = -1,
                    TargetClass = "TG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                    Concentration = 20.32,
                    DilutionRate = 0.015625,
                    MolecularWeight = 738.136,
                    PeakID = -1,
                    TargetClass = "SM"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE P-18:0_18:1(d9)",
                    Concentration = 0.04735,
                    DilutionRate = 0.015625,
                    MolecularWeight = 739.12,
                    PeakID = -1,
                    TargetClass = "EtherPE"
                });
            }
            else if (id == 3) {

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 132.7834,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "PC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 140.642,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "PE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                    Concentration = 132.7834,
                    DilutionRate = 0.015625,
                    MolecularWeight = 753.106,
                    PeakID = -1,
                    TargetClass = "EtherPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                    Concentration = 140.642,
                    DilutionRate = 0.015625,
                    MolecularWeight = 711.025,
                    PeakID = -1,
                    TargetClass = "EtherPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                    Concentration = 128.6968,
                    DilutionRate = 0.015625,
                    MolecularWeight = 777.02,
                    PeakID = -1,
                    TargetClass = "PS"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                    Concentration = 130.8868,
                    DilutionRate = 0.015625,
                    MolecularWeight = 764.02,
                    PeakID = -1,
                    TargetClass = "PG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                    Concentration = 118.0456,
                    DilutionRate = 0.015625,
                    MolecularWeight = 847.13,
                    PeakID = -1,
                    TargetClass = "PI"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 189.1367,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "LPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 189.1367,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "EtherLPC"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 205.4919,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "LPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "LPE 18:1(d7)",
                    Concentration = 205.4919,
                    DilutionRate = 0.015625,
                    MolecularWeight = 486.637,
                    PeakID = -1,
                    TargetClass = "EtherLPE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "CE 18:1(d7)",
                    Concentration = 151.93872,
                    DilutionRate = 0.015625,
                    MolecularWeight = 658.16,
                    PeakID = -1,
                    TargetClass = "CE"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "MG 18:1(d7)",
                    Concentration = 275.0351,
                    DilutionRate = 0.015625,
                    MolecularWeight = 363.59,
                    PeakID = -1,
                    TargetClass = "MG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                    Concentration = 170.0744,
                    DilutionRate = 0.015625,
                    MolecularWeight = 587.978,
                    PeakID = -1,
                    TargetClass = "DG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                    Concentration = 123.0972,
                    DilutionRate = 0.015625,
                    MolecularWeight = 812.366,
                    PeakID = -1,
                    TargetClass = "TG"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                    Concentration = 135.4764,
                    DilutionRate = 0.015625,
                    MolecularWeight = 738.136,
                    PeakID = -1,
                    TargetClass = "SM"
                });

                compounds.Add(new StandardCompound() {
                    StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                    Concentration = 188.3487,
                    DilutionRate = 0.015625,
                    MolecularWeight = 530.93,
                    PeakID = -1,
                    TargetClass = "Cer-NS"
                });


                if (this.ProjectProp.IsLabPrivateVersion) {
                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 100,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "FA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 100,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "OxFA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "FA 18:0(d3)",
                        Concentration = 100,
                        DilutionRate = 0.001,
                        MolecularWeight = 287.502,
                        PeakID = -1,
                        TargetClass = "FAHFA"
                    });
                }
                else {
                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "FA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "OxFA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "FAHFA"
                    });
                }



                compounds.Add(new StandardCompound() {
                    StandardName = "LPC 18:1(d7)",
                    Concentration = 189.1367,
                    DilutionRate = 0.015625,
                    MolecularWeight = 528.718,
                    PeakID = -1,
                    TargetClass = "Any others"
                });

                if (this.ProjectProp.IsLabPrivateVersion) {
                    // level 3
                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CAR"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "ADGGA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BASulfate"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BileAcid"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "ST"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CerP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Cholesterol"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CoQ"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGCC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGGA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGTS"
                    });

                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGTA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "EtherMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "EtherDGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GM3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GM1"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD1a"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD2"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GD3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GQ1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GT1b"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NGcGM3"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGCC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGTS"
                    });

                    compounds.Add(new StandardCompound()
                    {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LDGTA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "LPA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "MGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "MGMG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "DGMG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAGly"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAGlySer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAOrn"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "NAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "GPNAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PA"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PEtOH"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "PMeOH"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BAHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPEHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPGHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BRSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CASLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SISLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "STSLPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "BRSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "CASPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SISPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "STSPHex"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SQDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "SSulfate"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "VAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Vitamin_D"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "LPC 18:1(d7)",
                        Concentration = 189.1367,
                        DilutionRate = 0.015625,
                        MolecularWeight = 528.718,
                        PeakID = -1,
                        TargetClass = "Vitamin_E"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                        Concentration = 132.7834,
                        DilutionRate = 0.015625,
                        MolecularWeight = 753.106,
                        PeakID = -1,
                        TargetClass = "OxPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PC 33:1(d7)|PC 15:0_18:1(d7)",
                        Concentration = 132.7834,
                        DilutionRate = 0.015625,
                        MolecularWeight = 753.106,
                        PeakID = -1,
                        TargetClass = "EtherOxPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 140.642,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "OxPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 140.642,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "LNAPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 140.642,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "EtherOxPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "BMP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "CL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "EtherLPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "EtherPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "HBMP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "MLCL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "LPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PG 33:1(d7)|PG 15:0_18:1(d7)",
                        Concentration = 130.8868,
                        DilutionRate = 0.015625,
                        MolecularWeight = 764.02,
                        PeakID = -1,
                        TargetClass = "OxPG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 118.0456,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "EtherPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 118.0456,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "LPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 118.0456,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "OxPI"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 128.6968,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "EtherPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 128.6968,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "LNAPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 128.6968,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "LPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PS 33:1(d7)|PS 15:0_18:1(d7)",
                        Concentration = 128.6968,
                        DilutionRate = 0.015625,
                        MolecularWeight = 777.02,
                        PeakID = -1,
                        TargetClass = "OxPS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)",
                        Concentration = 135.4764,
                        DilutionRate = 0.015625,
                        MolecularWeight = 738.136,
                        PeakID = -1,
                        TargetClass = "ASM"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 123.0972,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "EtherTG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexCAS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexCS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexSIS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexSTS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "AHexBRS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "BRSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "CASE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "GDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "GLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "TDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "TLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "SISE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "STSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "LCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "KLCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "KDCAE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "EGSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DEGSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "CE 18:1(d7)",
                        Concentration = 151.93872,
                        DilutionRate = 0.015625,
                        MolecularWeight = 658.16,
                        PeakID = -1,
                        TargetClass = "DSMSE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 140.642,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "MMPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PE 33:1(d7)|PE 15:0_18:1(d7)",
                        Concentration = 140.642,
                        DilutionRate = 0.015625,
                        MolecularWeight = 711.025,
                        PeakID = -1,
                        TargetClass = "DMPE"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "PI 33:1(d7)|PI 15:0_18:1(d7)",
                        Concentration = 115.6718,
                        DilutionRate = 0.015625,
                        MolecularWeight = 847.13,
                        PeakID = -1,
                        TargetClass = "MIPC"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 123.0972,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "OxTG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0",
                        Concentration = 123.0972,
                        DilutionRate = 0.015625,
                        MolecularWeight = 812.366,
                        PeakID = -1,
                        TargetClass = "TG_EST"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "AHexCer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-ADS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-AP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-AS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-BDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-BS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EBDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EOS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-EODS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-HDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-HS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-NDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Cer-NP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "EtherSMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SMGDG"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-AP"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-EOS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-HDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-HS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-NDS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "HexCer-NS"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Hex2Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Hex3Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PE-Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PhytoSph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "PI-Cer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SHexCer"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "SL"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "DHSph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)",
                        Concentration = 188.3487,
                        DilutionRate = 0.015625,
                        MolecularWeight = 530.93,
                        PeakID = -1,
                        TargetClass = "Sph"
                    });

                    compounds.Add(new StandardCompound() {
                        StandardName = "DG 33:1(d7)|DG 15:0_18:1(d7)",
                        Concentration = 170.0744,
                        DilutionRate = 0.015625,
                        MolecularWeight = 587.978,
                        PeakID = -1,
                        TargetClass = "EtherDG"
                    });
                }
            }

            for (int i = 0; i < 100; i++) {
                compounds.Add(new StandardCompound());
            }

            this.StandardCompounds = compounds;
        }
    }
}
