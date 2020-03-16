using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class RawDataVM : ViewModelBase
    {
        private RawData rawData;
        
        private IonMode ionMode;
        private DataType spectrumType;
        
        private int precursorMzId;
        private double precursorMz;
        private List<double> precursorList;
        
        private string precursorType;
        private int precursorTypeId;
        private List<string> precursorTypeList;

        private double collisionEnergy;
        private string formula;
        private string ontology;
        private string smiles;
        private string inchiKey;
        private string comment;
        private bool isMarked;

        private List<AdductIon> adductPositives;
        private List<AdductIon> adductNegatives;

        public RawDataVM(RawData rawData, List<AdductIon> adductPositives, List<AdductIon> adductNegatives)
        {
            this.adductPositives = adductPositives;
            this.adductNegatives = adductNegatives;

            this.rawData = rawData;
            this.collisionEnergy = rawData.CollisionEnergy;
            this.formula = rawData.Formula;
            this.smiles = rawData.Smiles;
            this.inchiKey = rawData.InchiKey;
            this.comment = rawData.Comment;
            this.ontology = rawData.Ontology;
            this.isMarked = rawData.IsMarked;

            this.precursorMzId = 0;
            this.precursorMz = rawData.PrecursorMz;
            this.precursorList = new List<double>() { this.precursorMz };
            foreach (var peak in rawData.Ms1Spectrum.PeakList) { precursorList.Add(peak.Mz); }
            if (this.precursorMz < 0.1 && this.precursorList.Count == 1 && rawData.Ms2Spectrum != null && rawData.Ms2Spectrum.PeakList != null && rawData.Ms2Spectrum.PeakList.Count > 0) {
                this.precursorList = new List<double>();
                foreach (var peak in rawData.Ms2Spectrum.PeakList) { precursorList.Add(peak.Mz); }
                this.precursorList = this.precursorList.OrderByDescending(n => n).ToList();
                this.precursorMz = precursorList.Max();
            }

            this.precursorTypeId = 0;
            this.precursorType = rawData.PrecursorType;
            this.precursorTypeList = new List<string>() { this.precursorType };

            if (rawData.IonMode == IonMode.Positive) { foreach (var type in this.adductPositives) this.precursorTypeList.Add(type.AdductIonName); }
            else if (rawData.IonMode == IonMode.Negative) { foreach (var type in this.adductNegatives) this.precursorTypeList.Add(type.AdductIonName); }

            this.ionMode = rawData.IonMode;
            this.spectrumType = rawData.SpectrumType;

        }

        private void propertyChangedPrecursorType()
        {
            this.precursorTypeList = new List<string>();

            if (this.ionMode == IonMode.Positive) { foreach (var type in this.adductPositives) this.precursorTypeList.Add(type.AdductIonName); }
            else { foreach (var type in this.adductNegatives) this.precursorTypeList.Add(type.AdductIonName); }
            
            this.precursorType = this.precursorTypeList[0];
            OnPropertyChanged("PrecursorTypeList");
            OnPropertyChanged("PrecursorType");

            this.precursorTypeId = 0;
            OnPropertyChanged("PrecursorTypeId");
        }

        public RawData RawData
        {
            get { return rawData; }
        } 

        public string Name
        {
            get { return this.rawData.Name; }
            set { if (this.rawData.Name == value) return; this.rawData.Name = value; OnPropertyChanged("Name"); }
        }

        public double CollisionEnergy
        {
            get { return collisionEnergy; }
            set { if (collisionEnergy == value) return; collisionEnergy = value; OnPropertyChanged("CollisionEnergy"); }
        }

        public string Formula
        {
            get { return formula; }
            set { if (formula == value) return; formula = value; OnPropertyChanged("Formula"); }
        }

        public string Smiles
        {
            get { return smiles; }
            set { if (smiles == value) return; smiles = value; OnPropertyChanged("Smiles"); }
        }

        public string InchiKey {
            get { return inchiKey; }
            set { if (inchiKey == value) return; inchiKey = value; OnPropertyChanged("InchiKey"); }
        }

        public double RetentionTime
        {
            get { return this.rawData.RetentionTime; }
            set { if (this.rawData.RetentionTime == value) return; this.rawData.RetentionTime = value; OnPropertyChanged("RetentionTime"); }
        }

        public double Ccs {
            get { return this.rawData.Ccs; }
            set { if (this.rawData.Ccs == value) return; this.rawData.Ccs = value; OnPropertyChanged("Ccs"); }
        }

        public int ScanNumber
        {
            get { return this.rawData.ScanNumber; }
            set { if (this.rawData.ScanNumber == value) return; this.rawData.ScanNumber = value; OnPropertyChanged("ScanNumber"); }
        }

        public int Intensity
        {
            get { return this.rawData.Intensity; }
            set { if (this.rawData.Intensity == value) return; this.rawData.Intensity = value; OnPropertyChanged("Intensity"); }
        }

        public int Ms1PeakNum
        {
            get { return this.rawData.Ms1PeakNumber; }
        }

        public int Ms2PeakNum
        {
            get { return this.rawData.Ms2PeakNumber; }
        }

        [Required(ErrorMessage="Select ion mode type.")]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { if (ionMode == value) return; ionMode = value; OnPropertyChanged("IonMode"); propertyChangedPrecursorType(); }
        }

        public int PrecursorMzId
        {
            get { return precursorMzId; }
            set 
            { 
                if (precursorMzId == value) return; precursorMzId = value; OnPropertyChanged("PrecursorMzId");
                if (precursorList != null && precursorMzId >= 0 && precursorMzId < precursorList.Count) precursorMz = precursorList[precursorMzId];
            }
        }

        [Required(ErrorMessage="Enter the precursor m/z value.")]
        public double PrecursorMz
        {
            get { return precursorMz; }
            set { if (precursorMz == value) return; precursorMz = value; OnPropertyChanged("PrecursorMz"); }
        }

        public int PrecursorTypeId
        {
            get { return precursorTypeId; }
            set 
            {
                if (precursorTypeId == value) return; precursorTypeId = value; OnPropertyChanged("PrecursorTypeId");
                if (precursorTypeList != null && precursorTypeId >= 0 && precursorTypeId < precursorTypeList.Count) precursorType = precursorTypeList[precursorTypeId];
            }
        }

        public List<string> PrecursorTypeList
        {
            get { return precursorTypeList; }
            set { if (precursorTypeList == value) return; precursorTypeList = value; OnPropertyChanged("PrecursorTypeList"); }
        }

        [Required(ErrorMessage = "Enter the adduct ion.")]
        public string PrecursorType
        {
            get { return precursorType; }
            set { if (precursorType == value) return; precursorType = value; OnPropertyChanged("PrecursorType"); }
        }

        [Required(ErrorMessage = "Select spectrum type (centroid or profile).")]
        public DataType SpectrumType
        {
            get { return spectrumType; }
            set { if (spectrumType == value) return; spectrumType = value; OnPropertyChanged("SpectrumType"); }
        }

        public List<double> PrecursorList
        {
            get { return precursorList; }
            set { if (precursorList == value) return; precursorList = value; OnPropertyChanged("PrecursorList"); }
        }

        public string Comment {
            get {
                return comment;
            }

            set { if (comment == value) return; comment = value; OnPropertyChanged("Comment"); }
        }

        public bool IsMarked {
            get {
                return isMarked;
            }

            set {
                isMarked = value; OnPropertyChanged("IsMarked");
            }
        }

        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value; OnPropertyChanged("Ontology");
            }
        }
    }
}
