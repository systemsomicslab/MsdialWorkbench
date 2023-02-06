using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class MsmsSearchMeasurmentVM : ViewModelBase
    {
        #region // members
        private int id;
        private int fileId;
        private string fileName;
        private string filePath;
        private float retentionTime;
        private float accurateMass;
        private string adductIonName;
        private int libraryId;
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

        public float AccurateMass
        {
            get { return accurateMass; }
            set { if (accurateMass == value) return; accurateMass = value; OnPropertyChanged("AccurateMass"); }
        }

        public string AdductIonName
        {
            get { return adductIonName; }
            set { if (adductIonName == value) return; adductIonName = value; OnPropertyChanged("AdductIonName"); }
        }

        public int LibraryId
        {
            get { return libraryId; }
            set { if (libraryId == value) return; libraryId = value; OnPropertyChanged("LibraryId"); }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { if (metaboliteName == value) return; metaboliteName = value; OnPropertyChanged("MetaboliteName"); }
        }

        #endregion
    }
}
