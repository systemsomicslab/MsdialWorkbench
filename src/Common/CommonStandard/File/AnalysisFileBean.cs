using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using Rfx.Riken.OsakaUniv.RetentionTimeCorrection;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class AnalysisFileBean
    {
        [DataMember]
        private AnalysisFilePropertyBean analysisFilePropertyBean;
        [DataMember]
        private DataSummaryBean dataSummaryBean;
        [DataMember]
        private ObservableCollection<PeakAreaBean> peakAreaBeanCollection;
        private RetentionTimeCorrectionBean retentionTimeCorrectionBean;

        public AnalysisFileBean()
        {
            this.analysisFilePropertyBean = new AnalysisFilePropertyBean();
            this.peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
            this.dataSummaryBean = new DataSummaryBean();
            this.retentionTimeCorrectionBean = new RetentionTimeCorrectionBean();
        }

        #region
        [Key(0)]
        public AnalysisFilePropertyBean AnalysisFilePropertyBean
        {
            get { return analysisFilePropertyBean; }
            set { analysisFilePropertyBean = value; }
        }

        [Key(1)]
        public ObservableCollection<PeakAreaBean> PeakAreaBeanCollection
        {
            get { return peakAreaBeanCollection; }
            set { peakAreaBeanCollection = value; }
        }

        [Key(2)]
        public DataSummaryBean DataSummaryBean
        {
            get { return dataSummaryBean; }
            set { dataSummaryBean = value; }
        }

        [Key(3)]
        public RetentionTimeCorrectionBean RetentionTimeCorrectionBean {
            get { return retentionTimeCorrectionBean; }
            set { retentionTimeCorrectionBean = value; }
        }

        #endregion
    }

    public class SampleInfomation
    {
        private string sampleName;
        private string sampleSource;
        private string sampleType;
        private string mutantType;
        private string metabolomeExtractionMethod;
        private string chromatogramCondition;
        private string massspecCondition;
        private string ionMode;
        private string quantification;
        private string date;
        private string methodLink;

        #region property
        public string SampleName
        {
            get {
                return sampleName;
            }

            set {
                sampleName = value;
            }
        }

        public string SampleSource
        {
            get {
                return sampleSource;
            }

            set {
                sampleSource = value;
            }
        }

        public string SampleType
        {
            get {
                return sampleType;
            }

            set {
                sampleType = value;
            }
        }

        public string MutantType
        {
            get {
                return mutantType;
            }

            set {
                mutantType = value;
            }
        }

        public string MetabolomeExtractionMethod
        {
            get {
                return metabolomeExtractionMethod;
            }

            set {
                metabolomeExtractionMethod = value;
            }
        }

        public string ChromatogramCondition
        {
            get {
                return chromatogramCondition;
            }

            set {
                chromatogramCondition = value;
            }
        }

        public string MassspecCondition
        {
            get {
                return massspecCondition;
            }

            set {
                massspecCondition = value;
            }
        }

        public string IonMode
        {
            get {
                return ionMode;
            }

            set {
                ionMode = value;
            }
        }

        public string Quantification
        {
            get {
                return quantification;
            }

            set {
                quantification = value;
            }
        }

        public string Date
        {
            get {
                return date;
            }

            set {
                date = value;
            }
        }

        public string MethodLink
        {
            get {
                return methodLink;
            }

            set {
                methodLink = value;
            }
        }

        #endregion
    }

    public sealed class AnalysisFileClassUtility
    {
        private AnalysisFileClassUtility()
        {
        }

        public static Dictionary<string, List<AnalysisFileBean>> GetClassIdAnalysisFileBeansDictionary(ObservableCollection<AnalysisFileBean> files)
        {
            Dictionary<string, List<AnalysisFileBean>> classIdAnalysisFileBeans = new Dictionary<string, List<AnalysisFileBean>>();

            for (int i = 0; i < files.Count; i++) {
                var classId = files[i].AnalysisFilePropertyBean.AnalysisFileClass;
                if (!classIdAnalysisFileBeans.ContainsKey(classId)) {
                    classIdAnalysisFileBeans[classId] = new List<AnalysisFileBean>() { files[i] };
                }
                else {
                    classIdAnalysisFileBeans[classId].Add(files[i]);
                }
            }

            return classIdAnalysisFileBeans;
        }

    }
}
