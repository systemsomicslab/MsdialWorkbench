using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel.DataAnnotations;


namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// ExperimentConfigViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class ExperimentConfigViewer : Window
    {
        private ProjectPropertyBean projectProperty;
        public ExperimentConfigViewer() {
            InitializeComponent();
        }

        public ExperimentConfigViewer(ProjectPropertyBean projectProperty) {
            InitializeComponent();
            this.projectProperty = projectProperty;
            this.DataContext = new ExperrimentConfigVM(projectProperty, this);

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

        public void ClosingMethod() {
            var vm = (ExperrimentConfigVM)this.DataContext;
            var experimentID_AnalystExperimentInformationBean = new Dictionary<int, AnalystExperimentInformationBean>();
            var ceList = new List<float>();
            foreach (var row in vm.ExperrimentConfigRowVMCollection) {
                experimentID_AnalystExperimentInformationBean.Add(row.RowID, row.AnalystExperimentInformationBean);
                ceList.Add(row.CE);
            }
            projectProperty.ExperimentID_AnalystExperimentInformationBean = experimentID_AnalystExperimentInformationBean;
            projectProperty.CollisionEnergyList = ceList;
        }
    }

    public class ExperrimentConfigVM : ViewModelBase
    {
        private ObservableCollection<ExperrimentConfigRowVM> experrimentConfigRowVM;
        private ExperimentConfigViewer window;
        public ObservableCollection<ExperrimentConfigRowVM> ExperrimentConfigRowVMCollection {
            get { return experrimentConfigRowVM; }
            set { experrimentConfigRowVM = value; }
        }
        public ExperrimentConfigVM() { }
        public ExperrimentConfigVM(ProjectPropertyBean projectProperty, ExperimentConfigViewer window) {
            this.window = window;
            var ce = 0f;
            this.ExperrimentConfigRowVMCollection = new ObservableCollection<ExperrimentConfigRowVM>();
            for (int i = 0; i < projectProperty.ExperimentID_AnalystExperimentInformationBean.Count; i++) {
                if (projectProperty.CollisionEnergyList != null && projectProperty.CollisionEnergyList.Count == projectProperty.ExperimentID_AnalystExperimentInformationBean.Count) ce = projectProperty.CollisionEnergyList[0];

                ExperrimentConfigRowVMCollection.Add(new ExperrimentConfigRowVM(projectProperty.ExperimentID_AnalystExperimentInformationBean[i], i, ce));
            }
        }

        protected override void executeCommand(object parameter) {
            base.executeCommand(parameter);

            window.ClosingMethod();
            window.DialogResult = true;
            window.Close();
        }
    }

    public class ExperrimentConfigRowVM : ViewModelBase
    {
        private AnalystExperimentInformationBean analystExperimentInformationBean;
        private int rowID;
        private float ce;

        public ExperrimentConfigRowVM() {
            this.analystExperimentInformationBean = new AnalystExperimentInformationBean();
            rowID = 0;
        }

        public ExperrimentConfigRowVM(AnalystExperimentInformationBean analystExperimentInformationBean, int id, float ce = 0) {
            this.analystExperimentInformationBean = analystExperimentInformationBean;
            this.rowID = id;
            this.ce = ce;
        }

        #region // Properties
        public AnalystExperimentInformationBean AnalystExperimentInformationBean {
            get { return analystExperimentInformationBean; }
            set { analystExperimentInformationBean = value; }
        }

        public int RowID {
            get { return rowID; }
            set { rowID = value; }
        }

        public int ExperimentNumber {
            get { return analystExperimentInformationBean.ExperimentNumber; }
            set { analystExperimentInformationBean.ExperimentNumber = value; }
        }

        public MsType MsType {
            get { return analystExperimentInformationBean.MsType; }
            set { analystExperimentInformationBean.MsType = value; }

        }

        [Required(ErrorMessage = "Enter start m/z")]
        public float StartMz {
            get { return analystExperimentInformationBean.StartMz; }
            set { if (analystExperimentInformationBean.StartMz == value) return; analystExperimentInformationBean.StartMz = value; OnPropertyChanged("StartMz"); }
        }

        [Required(ErrorMessage = "Enter end m/z")]
        public float EndMz {
            get { return analystExperimentInformationBean.EndMz; }
            set { if (analystExperimentInformationBean.EndMz == value) return; analystExperimentInformationBean.EndMz = value; OnPropertyChanged("EndMz"); }
        }

        [Required(ErrorMessage = "Enter a name")]
        public string Name {
            get { return analystExperimentInformationBean.Name; }
            set { if (analystExperimentInformationBean.Name == value) return; analystExperimentInformationBean.Name = value; OnPropertyChanged("Name"); }
        }

        [Required(ErrorMessage = "Enter 1/0")]
        public bool Check {
            get { var res = analystExperimentInformationBean.CheckDecTarget > 0 ? true : false; return res ; }
            set { analystExperimentInformationBean.CheckDecTarget = value == true ? 1 : 0; OnPropertyChanged("Check"); }
        }

        [Required(ErrorMessage = "Enter collision energy")]
        public float CE {
            get { return ce; }
            set { if (ce == value) return; ce = value; OnPropertyChanged("Check"); }
        }

        #endregion

    }
}
