using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    //[Serializable()]
    [DataContract]
    [MessagePackObject]
    public class RdamFileContentBean
    {
        [DataMember]
        private string rdamFilePath;
        [DataMember]
        private string rdamFileName;
        [DataMember]
        private int rdamFileID;
        [DataMember]
        private int measurementNumber;
        [DataMember]
        private Dictionary<int, int> fileID_MeasurementID;
        [DataMember]
        private Dictionary<int, int> measurementID_FileID;

        public RdamFileContentBean()
        {
            fileID_MeasurementID = new Dictionary<int, int>();
            measurementID_FileID = new Dictionary<int, int>();
        }

        [Key(0)]
        public string RdamFilePath
        {
            get { return rdamFilePath; }
            set { rdamFilePath = value; }
        }

        [Key(1)]
        public string RdamFileName
        {
            get { return rdamFileName; }
            set { rdamFileName = value; }
        }

        [Key(2)]
        public int RdamFileID
        {
            get { return rdamFileID; }
            set { rdamFileID = value; }
        }

        [Key(3)]
        public int MeasurementNumber
        {
            get { return measurementNumber; }
            set { measurementNumber = value; }
        }

        [Key(4)]
        public Dictionary<int, int> FileID_MeasurementID
        {
            get { return fileID_MeasurementID; }
            set { fileID_MeasurementID = value; }
        }

        [Key(5)]
        public Dictionary<int, int> MeasurementID_FileID
        {
            get { return measurementID_FileID; }
            set { measurementID_FileID = value; }
        }
    }
}
