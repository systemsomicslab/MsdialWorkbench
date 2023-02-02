using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class EimsSearchReferenceVM : ViewModelBase
    {
        #region // members
        private int id;
        private int mspID;
        private string compoundName;
        private float retentionTime;
        private float retentionIndex;

        private float quantMass;
        private float dotProduct;
        private float reverseDotProduct;
        private float presenseSimilarity;
        private float retentionTimeSimlarity;
        private float retentionIndexSimilarity;
        private float eiSpectraSimilarity;

        private float totalSimilarity;
        #endregion

        #region // properties

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public int MspID
        {
            get { return mspID; }
            set { if (mspID == value) return; mspID = value; OnPropertyChanged("MspID"); }
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

        public float RetentionIndexSimilarity
        {
            get { return retentionIndexSimilarity; }
            set { if (retentionIndexSimilarity == value) return; retentionIndexSimilarity = value; OnPropertyChanged("RetentionIndexSimilarity"); }
        }

        public float EiSpectraSimilarity
        {
            get { return eiSpectraSimilarity; }
            set { if (eiSpectraSimilarity == value) return; eiSpectraSimilarity = value; OnPropertyChanged("EiSpectraSimilarity"); }
        }

        public float PresenseSimilarity
        {
            get { return presenseSimilarity; }
            set { if (presenseSimilarity == value) return; presenseSimilarity = value; OnPropertyChanged("PresenseSimilarity"); }
        }

        public float TotalSimilarity
        {
            get { return totalSimilarity; }
            set { if (totalSimilarity == value) return; totalSimilarity = value; OnPropertyChanged("TotalSimilarity"); }
        }
        #endregion
    }
}
