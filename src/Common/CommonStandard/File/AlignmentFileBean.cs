using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    //[Serializable]
    [DataContract]
    [MessagePackObject]
    public class AlignmentFileBean
    {
        [DataMember]
        private int fileID;
        [DataMember]
        private string filePath;
        [DataMember]
        private string fileName;
        [DataMember]
        private string spectraFilePath;
        [DataMember]
        private string eicFilePath;

        [Key(0)]
        public int FileID
        {
            get { return fileID; }
            set { fileID = value; }
        }

        [Key(1)]
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        [Key(2)]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        [Key(3)]
        public string SpectraFilePath
        {
            get { return spectraFilePath; }
            set { spectraFilePath = value; }
        }

        [Key(4)]
        public string EicFilePath {
            get { return eicFilePath; }
            set { eicFilePath = value; }
        }
    }
}
