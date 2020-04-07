using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    //[Serializable()]
    [DataContract]
    [MessagePackObject]
    public class AlignmentResultBean
    {
        [DataMember]
        int sampleNumber;
        [DataMember]
        int alignmentIdNumber;
        [DataMember]
        bool normalized;
        [DataMember]
        ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection;
        [DataMember]
        AnalysisParametersBean analysisParamForLC;
        [DataMember]
        AnalysisParamOfMsdialGcms analysisParamForGC;
        [DataMember]
        Ionization ionizationType;

        [Key(0)]
        public Ionization IonizationType
        {
            get { return ionizationType; }
            set { ionizationType = value; }
        }

        [Key(1)]
        public AnalysisParametersBean AnalysisParamForLC
        {
            get { return analysisParamForLC; }
            set { analysisParamForLC = value; }
        }

        [Key(2)]
        public AnalysisParamOfMsdialGcms AnalysisParamForGC
        {
            get { return analysisParamForGC; }
            set { analysisParamForGC = value; }
        }

        [Key(3)]
        public int SampleNumber
        {
            get { return sampleNumber; }
            set { sampleNumber = value; }
        }

        [Key(4)]
        public int AlignmentIdNumber
        {
            get { return alignmentIdNumber; }
            set { alignmentIdNumber = value; }
        }

        [Key(5)]
        public ObservableCollection<AlignmentPropertyBean> AlignmentPropertyBeanCollection
        {
            get {
                if (alignmentPropertyBeanCollection == null) return null;
                return alignmentPropertyBeanCollection;
            }
            set { alignmentPropertyBeanCollection = value; }
        }

        [Key(6)]
        public bool Normalized
        {
            get { return normalized; }
            set { normalized = value; }
        }

        public AlignmentResultBean() 
        { 
            alignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>();
        }
    }
}
