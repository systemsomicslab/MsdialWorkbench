using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class EimsSearchMeasurmentVM : ViewModelBase
    {
        #region // members
        private int id;
        private int fileId;
        private string fileName;
        private string filePath;
        private float retentionTime;
        private float retentionIndex;
        private float quantMass;
        private int mspID;
        private string metaboliteName;
        #endregion

        #region // properties
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public int FileId
        {
            get { return fileId; }
            set { if (fileId == value) return; fileId = value; OnPropertyChanged(" FileId"); }
        }

        public string FileName
        {
            get { return fileName; }
            set { if (fileName == value) return; fileName = value; OnPropertyChanged("FileName"); }
        }

        public string FilePath
        {
            get { return filePath; }
            set { if (filePath == value) return; filePath = value; OnPropertyChanged("FilePath"); }
        }

        public float RetentionTime
        {
            get { return retentionTime; }
            set { if (retentionTime == value) return; retentionTime = value; OnPropertyChanged("RetentionTime"); }
        }

        public float RetentionIndex
        {
            get { return retentionIndex; }
            set { if (retentionIndex == value) return; retentionIndex = value; OnPropertyChanged("RetentionIndex"); }
        }

        public float QuantMass
        {
            get { return quantMass; }
            set { if (quantMass == value) return; quantMass = value; OnPropertyChanged("QuantMass"); }
        }

        public int MspID
        {
            get { return mspID; }
            set { if (mspID == value) return; mspID = value; OnPropertyChanged("MspID"); }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { if (metaboliteName == value) return; metaboliteName = value; OnPropertyChanged("MetaboliteName"); }
        }

        #endregion
    }
}
