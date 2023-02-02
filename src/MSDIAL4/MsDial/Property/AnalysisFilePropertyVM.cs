using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisFilePropertyVM : ViewModelBase
    {
        private AnalysisFilePropertyBean analysisFilePropertyBean;

        public AnalysisFilePropertyVM()
        {
            this.analysisFilePropertyBean = new AnalysisFilePropertyBean();
        }

        public AnalysisFilePropertyVM(AnalysisFilePropertyBean prop)
        {
            this.analysisFilePropertyBean = prop;
        }

        #region // Properties
        public AnalysisFilePropertyBean AnalysisFilePropertyBean
        {
            get { return analysisFilePropertyBean; }
            set { analysisFilePropertyBean = value; }
        }

        public string AnalysisFilePath
        {
            get { return analysisFilePropertyBean.AnalysisFilePath; }
            set { analysisFilePropertyBean.AnalysisFilePath = value; }
        }

        [Required(ErrorMessage = "Enter a name.")]
        public string AnalysisFileName
        {
            get { return analysisFilePropertyBean.AnalysisFileName; }
            set { if (analysisFilePropertyBean.AnalysisFileName == value) return; analysisFilePropertyBean.AnalysisFileName = value; OnPropertyChanged("AnalysisFileName"); }
        }

        public AnalysisFileType AnalysisFileType
        {
            get { return analysisFilePropertyBean.AnalysisFileType; }
            set { analysisFilePropertyBean.AnalysisFileType = value; OnPropertyChanged("AnalysisFileType"); }
        }

        [Required(ErrorMessage = "Enter a class.")]
        public string AnalysisFileClass
        {
            get { return analysisFilePropertyBean.AnalysisFileClass; }
            set { if (analysisFilePropertyBean.AnalysisFileClass == value) return; analysisFilePropertyBean.AnalysisFileClass = value; OnPropertyChanged("AnalysisFileClass"); }
        }

        [Required(ErrorMessage = "Enter a value.")]
        public int AnalysisFileAnalyticalOrder
        {
            get { return analysisFilePropertyBean.AnalysisFileAnalyticalOrder; }
            set { if (analysisFilePropertyBean.AnalysisFileAnalyticalOrder == value)return; analysisFilePropertyBean.AnalysisFileAnalyticalOrder = value; OnPropertyChanged("AnalysisFileAnalyticalOrder"); }
        }

        public int AnalysisFileId
        {
            get { return analysisFilePropertyBean.AnalysisFileId; }
            set { analysisFilePropertyBean.AnalysisFileId = value; }
        }

        public bool AnalysisFileIncluded
        {
            get { return analysisFilePropertyBean.AnalysisFileIncluded; }
            set { if (analysisFilePropertyBean.AnalysisFileIncluded == value) return; analysisFilePropertyBean.AnalysisFileIncluded = value; OnPropertyChanged("AnalysisFileIncluded"); }
        }

        public string RiDictionaryFilePath
        {
            get { return analysisFilePropertyBean.RiDictionaryFilePath; }
            set { if (analysisFilePropertyBean.RiDictionaryFilePath == value) return; analysisFilePropertyBean.RiDictionaryFilePath = value; OnPropertyChanged("RiDictionaryFilePath"); }
        }

        [Required(ErrorMessage = "Enter a value.")]
        public int AnalysisBatch {
            get { return analysisFilePropertyBean.AnalysisBatch; }
            set { if (analysisFilePropertyBean.AnalysisBatch == value) return; analysisFilePropertyBean.AnalysisBatch = value; OnPropertyChanged("AnalysisBatch"); }
        }

        public double ResponseVariable {
            get { return analysisFilePropertyBean.ResponseVariable; }
            set { if (analysisFilePropertyBean.ResponseVariable == value) return; analysisFilePropertyBean.ResponseVariable = value; OnPropertyChanged("ResponseVariable"); }
        }

        public double InjectionVolume {
            get { return analysisFilePropertyBean.InjectionVolume; }
            set { if (analysisFilePropertyBean.InjectionVolume == value) return; analysisFilePropertyBean.InjectionVolume = value; OnPropertyChanged("InjectionVolume"); }
        }
        #endregion

    }
}
