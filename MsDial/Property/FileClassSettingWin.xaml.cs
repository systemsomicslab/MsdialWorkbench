using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    /// Interaction logic for FileClassSettingWin.xaml
    /// </summary>
    public partial class FileClassSettingWin : Window {
        public FileClassSettingWin(ProjectPropertyBean project) {
            InitializeComponent();
            var classProperties = getClassProperties(project);
            var vm = new FileClassSettingVM() {
                ClassProperties = classProperties,
                Project = project
            };
            this.DataContext = vm;
        }

        private ObservableCollection<ClassProperty> getClassProperties(ProjectPropertyBean project) {
            var classnameToOrder = project.ClassnameToOrder;
            var classnameToBrush = project.ClassnameToColorBytes;

            var classProperties = new ObservableCollection<ClassProperty>();
            foreach (var pair in classnameToBrush) {
                var classname = pair.Key;
                var colorBytes = pair.Value;
                var colorprop = new Color() { R = colorBytes[0], G = colorBytes[1], B = colorBytes[2], A = colorBytes[3] };

                var classProp = new ClassProperty() {
                    ClassColor = colorprop,
                    ClassName = classname,
                    ClassOrder = classnameToOrder[classname]
                };
                classProperties.Add(classProp);
            }
            return classProperties;
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Datagrid_FileProperty_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_FileProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_FileProperty.BeginEdit();
        }
    }

    public class FileClassSettingVM : ViewModelBase {
        private ObservableCollection<ClassProperty> classProperties;
        public ObservableCollection<ClassProperty> ClassProperties {
            get {
                return classProperties;
            }

            set {
                classProperties = value;
                OnPropertyChanged("ClassProperties");
            }
        }
        public ProjectPropertyBean Project { get; set; }

        /// <summary>
        /// search BinBase spectra
        /// </summary>
        private DelegateCommand finish;
        public DelegateCommand Finish {
            get {
                return finish ?? (finish = new DelegateCommand(winobj => {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var classPropList = classProperties.OrderBy(n => n.ClassOrder).ToList();
                    var classnameToBrush = new Dictionary<string, List<byte>>();
                    var classnameToOrder = new Dictionary<string, int>();
                    var classid = 0;
                    foreach (var prop in classPropList) {
                        classnameToBrush[prop.ClassName] = new List<byte>() { prop.ClassColor.R, prop.ClassColor.G, prop.ClassColor.B, prop.ClassColor.A };
                        classnameToOrder[prop.ClassName] = classid;
                        classid++;
                    }

                    this.Project.ClassnameToColorBytes = classnameToBrush;
                    this.Project.ClassnameToOrder = classnameToOrder;

                    var mainWindow = (MainWindow)((FileClassSettingWin)winobj).Owner;
                    if (mainWindow.FocusedAlignmentResult != null) {

                        if (this.Project.Ionization == Ionization.EI)
                            TableViewerUtility.CalcStatisticsForAlignmentProperty(mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection, mainWindow.AnalysisFiles, false);
                        else {
                            var param = mainWindow.AnalysisParamForLC;
                            TableViewerUtility.CalcStatisticsForAlignmentProperty(mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection, mainWindow.AnalysisFiles, param.IsIonMobility);
                        }
                    }

                    Mouse.OverrideCursor = null;

                    var window = (FileClassSettingWin)winobj;
                    window.Close();

                }, CanApplyClassSetting));
            }
        }

        /// <summary>
        /// search BinBase spectra
        /// </summary>
        private DelegateCommand ordering;
        public DelegateCommand Ordering {
            get {
                return ordering ?? (ordering = new DelegateCommand(winobj => {
                    Mouse.OverrideCursor = Cursors.Wait;

                    classProperties = new ObservableCollection<ClassProperty>(classProperties.OrderBy(n => n.ClassOrder).ToList());
                    OnPropertyChanged("ClassProperties");

                    Mouse.OverrideCursor = null;

                    var window = (FileClassSettingWin)winobj;
                    window.UpdateLayout();
                }, CanApplyClassSetting));
            }
        }

        /// <summary>
        /// Checks whether the BinVestigate search can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanApplyClassSetting(object arg) {
            if (this.HasViewError) return false;
            else return true;
        }
    }

    public class ClassProperty : ViewModelBase {
        private int classOrder;
        private string className;
        private Color classColor;
        private byte classRgbA;
        private byte classRgbB;
        private byte classRgbR;
        private byte classRgbG;

        public int ClassOrder {
            get {
                return classOrder;
            }

            set {
                classOrder = value;
                OnPropertyChanged("ClassOrder");
            }
        }

        public string ClassName {
            get {
                return className;
            }

            set {
                className = value;
                OnPropertyChanged("ClassName");
            }
        }

        public Color ClassColor {
            get {
                return classColor;
            }

            set {
                classColor = value;
                OnPropertyChanged("ClassColor");

                ClassRgbA = classColor.A;
                ClassRgbG = classColor.G;
                ClassRgbB = classColor.B;
                ClassRgbR = classColor.R;
            }
        }

        public byte ClassRgbA {
            get {
                return classRgbA;
            }

            set {
                classRgbA = value;
            }
        }

        public byte ClassRgbB {
            get {
                return classRgbB;
            }

            set {
                classRgbB = value;
            }
        }

        public byte ClassRgbR {
            get {
                return classRgbR;
            }

            set {
                classRgbR = value;
            }
        }

        public byte ClassRgbG {
            get {
                return classRgbG;
            }

            set {
                classRgbG = value;
            }
        }
    }
}
