using Rfx.Riken.OsakaUniv;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ColorPickerWpfApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            var classProperties = new ObservableCollection<ClassProperty>() {
                new ClassProperty() { ClassOrder = 1, ClassName = "No 1", ClassColor = Brushes.Red.Color, ClassRgb = Brushes.Red.Color.ToString() },
                new ClassProperty() { ClassOrder = 2, ClassName = "No 2", ClassColor = Brushes.Blue.Color, ClassRgb = Brushes.Blue.Color.ToString() },
                new ClassProperty() { ClassOrder = 3, ClassName = "No 3", ClassColor = Brushes.Green.Color, ClassRgb = Brushes.Green.Color.ToString() },
                new ClassProperty() { ClassOrder = 4, ClassName = "No 4", ClassColor = Brushes.Yellow.Color, ClassRgb = Brushes.Yellow.Color.ToString() },
                new ClassProperty() { ClassOrder = 5, ClassName = "No 5", ClassColor = Brushes.Pink.Color, ClassRgb = Brushes.Pink.Color.ToString() }
            };
            var vm = new MainWindowVM() { ClassProperties = classProperties };
            this.DataContext = vm;
        }

        private void Datagrid_FileProperty_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_FileProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_FileProperty.BeginEdit();
        }
    }

    public class MainWindowVM : ViewModelBase {
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
    }

    public class ClassProperty : ViewModelBase {
        private int classOrder;
        private string className;
        private Color classColor;
        private string classRgb;

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
                ClassRgb = classColor.ToString();
            }
        }

        public string ClassRgb {
            get {
                return classRgb;
            }

            set {
                classRgb = value;
                OnPropertyChanged("ClassRgb");
            }
        }
    }
}
