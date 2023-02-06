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
    public class MsmsSearchReferenceVM : ViewModelBase
    {
        #region // members
        private int id;
        private int libraryId;
        private string compoundName;
        private float retentionTime;
        private float ccsSimilarity;
        private float ccsValue;
        private float accurateMass;
        private float dotProduct;
        private float reverseDotProduct;
        private float presenseSimilarity;
        private float retentionTimeSimlarity;
        private float accurateMassSimilarity;
        private float totalSimilarity;
        private string instrument;
        private string comment;
        private string adduct;

        #endregion

        #region // properties

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public int LibraryId
        {
            get { return libraryId; }
            set { if (libraryId == value) return; libraryId = value; OnPropertyChanged("LibraryId"); }
        }

        public string CompoundName
        {
            get { return compoundName; }
            set { if (compoundName == value) return; compoundName = value; OnPropertyChanged("CompoundName"); }
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

        public float DotProduct
        {
            get { return dotProduct; }
            set { if (dotProduct == value) return; dotProduct = value; OnPropertyChanged("DotProduct"); }
        }

        public float ReverseDotProduct
        {
            get { return reverseDotProduct; }
            set { if (reverseDotProduct == value) return; reverseDotProduct = value; OnPropertyChanged("ReverseDotProduct"); }
        }

        public float RetentionTimeSimlarity
        {
            get { return retentionTimeSimlarity; }
            set { if (retentionTimeSimlarity == value) return; retentionTimeSimlarity = value; OnPropertyChanged("RetentionTimeSimlarity"); }
        }

        public float PresenseSimilarity
        {
            get { return presenseSimilarity; }
            set { if (presenseSimilarity == value) return; presenseSimilarity = value; OnPropertyChanged("PresenseSimilarity"); }
        }

        public float AccurateMassSimilarity
        {
            get { return accurateMassSimilarity; }
            set { if (accurateMassSimilarity == value) return; accurateMassSimilarity = value; OnPropertyChanged("AccurateMassSimilarity"); }
        }

        public float TotalSimilarity
        {
            get { return totalSimilarity; }
            set { if (totalSimilarity == value) return; totalSimilarity = value; OnPropertyChanged("TotalSimilarity"); }
        }

        public string Instrument {
            get {
                return instrument;
            }

            set {
                instrument = value;
                OnPropertyChanged("Instrument");
            }
        }

        public string Comment {
            get {
                return comment;
            }

            set {
                comment = value;
                OnPropertyChanged("Comment");
            }
        }

        public float CcsSimilarity {
            get {
                return ccsSimilarity;
            }

            set {
                if (ccsSimilarity == value) return; ccsSimilarity = value; OnPropertyChanged("CcsSimilarity");
            }
        }

        public float CcsValue {
            get {
                return ccsValue;
            }

            set {
                if (ccsValue == value) return; ccsValue = value; OnPropertyChanged("CcsValue");
            }
        }

        public string Adduct {
            get {
                return adduct;
            }

            set {
                adduct = value; OnPropertyChanged("Adduct");
            }
        }
        #endregion
    }
}
