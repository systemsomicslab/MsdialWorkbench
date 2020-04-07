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
    public class RdamPropertyBean
    {
        [DataMember]
        private Dictionary<string, int> rdamFilePath_RdamFileID;
        [DataMember]
        private Dictionary<int, string> rdamFileID_RdamFilePath;
        [DataMember]
        private ObservableCollection<RdamFileContentBean> rdamFileContentBeanCollection;

        public RdamPropertyBean()
        {
            rdamFilePath_RdamFileID = new Dictionary<string, int>();
            rdamFileID_RdamFilePath = new Dictionary<int, string>();
            rdamFileContentBeanCollection = new ObservableCollection<RdamFileContentBean>();
        }

        [Key(0)]
        public Dictionary<string, int> RdamFilePath_RdamFileID
        {
            get { return rdamFilePath_RdamFileID; }
            set { rdamFilePath_RdamFileID = value; }
        }

        [Key(1)]
        public Dictionary<int, string> RdamFileID_RdamFilePath
        {
            get { return rdamFileID_RdamFilePath; }
            set { rdamFileID_RdamFilePath = value; }
        }

        [Key(2)]
        public ObservableCollection<RdamFileContentBean> RdamFileContentBeanCollection
        {
            get { return rdamFileContentBeanCollection; }
            set { rdamFileContentBeanCollection = value; }
        }
    }
}
