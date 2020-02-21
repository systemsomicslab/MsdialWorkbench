using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [Serializable()]
    [DataContract]
    [MessagePackObject]
    public class AnalysisFilePropertyBean
    {
        [DataMember]
        private string analysisFilePath;
        [DataMember]
        private string analysisFileName;
        [DataMember]
        private AnalysisFileType analysisFileType;
        [DataMember]
        private string analysisFileClass;
        private string analysisFileSuperClass;
        [DataMember]
        private int analysisFileId;
        [DataMember]
        private int analysisFileAnalyticalOrder;
        [DataMember]
        private bool analysisFileIncluded;
        [DataMember]
        private string deconvolutionFilePath;
        [DataMember]
        private List<string> deconvolutionFilePathList;
        [DataMember]
        private string peakAreaBeanInformationFilePath;
        [DataMember]
        private string riDictionaryFilePath;
        [DataMember]
        private double responseVariable;
        [DataMember]
        private double injectionVolume;

        public AnalysisFilePropertyBean() {
            injectionVolume = 1.0;
        }

        //Properties
        #region
        [Key(0)]
        public string AnalysisFilePath
        {
            get { return analysisFilePath; }
            set { analysisFilePath = value; }
        }

        [Key(1)]
        public string AnalysisFileName
        {
            get { return analysisFileName; }
            set { analysisFileName = value; }
        }

        [Key(2)]
        public AnalysisFileType AnalysisFileType
        {
            get { return analysisFileType; }
            set { analysisFileType = value; }
        }

        [Key(3)]
        public string AnalysisFileClass
        {
            get { return analysisFileClass; }
            set { analysisFileClass = value; }
        }

        [Key(4)]
        public int AnalysisFileAnalyticalOrder
        {
            get { return analysisFileAnalyticalOrder; }
            set { analysisFileAnalyticalOrder = value; }
        }

        [Key(5)]
        public int AnalysisFileId
        {
            get { return analysisFileId; }
            set { analysisFileId = value; }
        }

        [Key(6)]
        public bool AnalysisFileIncluded
        {
            get { return analysisFileIncluded; }
            set { analysisFileIncluded = value; }
        }

        [Key(7)]
        public string DeconvolutionFilePath
        {
            get { return deconvolutionFilePath; }
            set { deconvolutionFilePath = value; }
        }

        [Key(8)]
        public List<string> DeconvolutionFilePathList {
            get { return deconvolutionFilePathList; }
            set { deconvolutionFilePathList = value; }
        }

        [Key(9)]
        public string PeakAreaBeanInformationFilePath
        {
            get { return peakAreaBeanInformationFilePath; }
            set { peakAreaBeanInformationFilePath = value; }
        }

        [Key(10)]
        public string RiDictionaryFilePath
        {
            get { return riDictionaryFilePath; }
            set { riDictionaryFilePath = value; }
        }

        [Key(11)]
        public int AnalysisBatch { get; set; } = 1;

        [Key(12)]
        public double ResponseVariable {
            get {
                return responseVariable;
            }

            set {
                responseVariable = value;
            }
        }

        [Key(13)]
        public double InjectionVolume {
            get {
                return injectionVolume;
            }

            set {
                injectionVolume = value;
            }
        }

        [Key(14)]
        public string AnalysisFileSuperClass {
            get {
                return analysisFileSuperClass;
            }

            set {
                analysisFileSuperClass = value;
            }
        }
        #endregion
    }
}
