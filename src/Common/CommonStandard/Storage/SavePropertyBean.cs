using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class SavePropertyBean
    {
        [DataMember]
        private ProjectPropertyBean projectPropertyBean;
        [DataMember]
        private ObservableCollection<AnalysisFileBean> analysisFileBeanCollection;
        [DataMember]
        private ObservableCollection<AlignmentFileBean> alignmentFileBeanCollection;
        [DataMember]
        private AnalysisParametersBean analysisParametersBean;
        [DataMember]
        private AnalysisParamOfMsdialGcms analysisParamForGC;
        [DataMember]
        private RdamPropertyBean rdamPropertyBean;
        [DataMember]
        private List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList;
        [DataMember]
        private IupacReferenceBean iupacReferenceBean;
        [DataMember]
        private List<PostIdentificatioinReferenceBean> postIdentificationReferenceBeanList;
        [DataMember]
        private List<PostIdentificatioinReferenceBean> targetFormulaLibrary;

        [Key(0)]
        public ProjectPropertyBean ProjectPropertyBean
        {
            get { return projectPropertyBean; }
            set { projectPropertyBean = value; }
        }

        [Key(1)]
        public ObservableCollection<AnalysisFileBean> AnalysisFileBeanCollection
        {
            get { return analysisFileBeanCollection; }
            set { analysisFileBeanCollection = value; }
        }

        [Key(2)]
        public ObservableCollection<AlignmentFileBean> AlignmentFileBeanCollection
        {
            get { return alignmentFileBeanCollection; }
            set { alignmentFileBeanCollection = value; }
        }

        [Key(3)]
        public AnalysisParametersBean AnalysisParametersBean
        {
            get { return analysisParametersBean; }
            set { analysisParametersBean = value; }
        }

        [Key(4)]
        public AnalysisParamOfMsdialGcms AnalysisParamForGC
        {
            get { return analysisParamForGC; }
            set { analysisParamForGC = value; }
        }

        [Key(5)]
        public RdamPropertyBean RdamPropertyBean
        {
            get { return rdamPropertyBean; }
            set { rdamPropertyBean = value; }
        }

        [Key(6)]
        public List<MspFormatCompoundInformationBean> MspFormatCompoundInformationBeanList
        {
            get { return mspFormatCompoundInformationBeanList; }
            set { mspFormatCompoundInformationBeanList = value; }
        }

        [Key(7)]
        public IupacReferenceBean IupacReferenceBean
        {
            get { return iupacReferenceBean; }
            set { iupacReferenceBean = value; }
        }

        [Key(8)]
        public List<PostIdentificatioinReferenceBean> PostIdentificationReferenceBeanList
        {
            get { return postIdentificationReferenceBeanList; }
            set { postIdentificationReferenceBeanList = value; }
        }

        [Key(9)]
        public List<PostIdentificatioinReferenceBean> TargetFormulaLibrary
        {
            get { return targetFormulaLibrary; }
            set { targetFormulaLibrary = value; }
        }

    }
}
