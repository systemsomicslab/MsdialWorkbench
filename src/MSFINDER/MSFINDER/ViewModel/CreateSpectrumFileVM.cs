using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class CreateSpectrumFileVM : ViewModelBase
    {
        private Window window;
        private MainWindowVM mainWindowVM;
        private RawData rawData;

        private int precursorTypeId;
        private List<string> precursorTypeList;

        private string folderPath;
        private string fileName;

        private string ms1Spectrum;
        private string ms2Spectrum;

        public CreateSpectrumFileVM(MainWindowVM mainWindowVM, Window window)
        {
            this.window = window;
            this.mainWindowVM = mainWindowVM;
            this.rawData = new RawData();
            propertyChangedPrecursorType();
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            closingMethod();
            this.window.Close();
        }

        private void closingMethod()
        {
            if (!System.IO.Directory.Exists(this.folderPath))
            {
                MessageBox.Show("This program cannot find the directry. Please check your folder path again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.rawData.Name = this.fileName;
            if (this.ms1Spectrum != null && this.ms1Spectrum != string.Empty)
            {
                var peaklist = RawDataParcer.SpectrumStringParcer(this.ms1Spectrum);
                this.rawData.Ms1PeakNumber = peaklist.Count;
                this.rawData.Ms1Spectrum.PeakList = peaklist;
            }

            if (this.ms2Spectrum != null && this.ms2Spectrum != string.Empty)
            {
                var peaklist = RawDataParcer.SpectrumStringParcer(this.ms2Spectrum);
                this.rawData.Ms2PeakNumber = peaklist.Count;
                this.rawData.Ms2Spectrum.PeakList = peaklist;
            }

            var filePath = Path.Combine(this.folderPath, this.fileName + "." + SaveFileFormat.mat);
            RawDataParcer.RawDataFileWriter(filePath, this.rawData);

            this.mainWindowVM.Refresh_ImportFolder(this.folderPath);
        }

        public int ScanNumber
        {
            get { return this.rawData.ScanNumber; }
            set { if (this.rawData.ScanNumber == value) return; this.rawData.ScanNumber = value; OnPropertyChanged("ScanNumber"); }
        }

        public double CollisionEnergy
        {
            get { return this.rawData.CollisionEnergy; }
            set { if (this.rawData.CollisionEnergy == value) return; this.rawData.CollisionEnergy = value; OnPropertyChanged("CollisionEnergy"); }
        }

        public string Formula
        {
            get { return this.rawData.Formula; }
            set { if (this.rawData.Formula == value) return; this.rawData.Formula = value; OnPropertyChanged("Formula"); }
        }

        public string Smiles
        {
            get { return this.rawData.Smiles; }
            set { if (this.rawData.Smiles == value) return; this.rawData.Smiles = value; OnPropertyChanged("Smiles"); }
        }

        public double RetentionTime
        {
            get { return this.rawData.RetentionTime; }
            set { if (this.rawData.RetentionTime == value) return; this.rawData.RetentionTime = value; OnPropertyChanged("RetentionTime"); }
        }

        public int Intensity
        {
            get { return this.rawData.Intensity; }
            set { if (this.rawData.Intensity == value) return; this.rawData.Intensity = value; OnPropertyChanged("Intensity"); }
        }

        [Required(ErrorMessage = "Enter the precursor m/z value.")]
        [Range(20, 2000)]
        public double PrecursorMz
        {
            get { return this.rawData.PrecursorMz; }
            set { if (this.rawData.PrecursorMz == value) return; this.rawData.PrecursorMz = value; OnPropertyChanged("PrecursorMz"); }
        }

        [Required(ErrorMessage = "Select ion mode type.")]
        public IonMode IonMode
        {
            get { return this.rawData.IonMode; }
            set { if (this.rawData.IonMode == value) return; this.rawData.IonMode = value; OnPropertyChanged("IonMode"); propertyChangedPrecursorType(); }
        }

        private void propertyChangedPrecursorType()
        {
            this.precursorTypeList = new List<string>();

            if (IonMode == IonMode.Positive) { foreach (var type in this.mainWindowVM.AdductPositiveResources) this.precursorTypeList.Add(type.AdductIonName); }
            else { foreach (var type in this.mainWindowVM.AdductNegativeResources) this.precursorTypeList.Add(type.AdductIonName); }

            PrecursorType = this.precursorTypeList[0];
            OnPropertyChanged("PrecursorTypeList");
            OnPropertyChanged("PrecursorType");

            this.precursorTypeId = 0;
            OnPropertyChanged("PrecursorTypeId");
        }

        public int PrecursorTypeId
        {
            get { return precursorTypeId; }
            set
            {
                if (precursorTypeId == value) return; precursorTypeId = value; OnPropertyChanged("PrecursorTypeId");
                if (precursorTypeList != null && precursorTypeId >= 0 && precursorTypeId < precursorTypeList.Count) PrecursorType = precursorTypeList[precursorTypeId];
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
            get { return this.rawData.PrecursorType; }
            set { if (this.rawData.PrecursorType == value) return; this.rawData.PrecursorType = value; OnPropertyChanged("PrecursorType"); }
        }

        [Required(ErrorMessage = "Select spectrum type (centroid or profile).")]
        public DataType SpectrumType
        {
            get { return this.rawData.SpectrumType; }
            set { if (this.rawData.SpectrumType == value) return; this.rawData.SpectrumType = value; OnPropertyChanged("SpectrumType"); }
        }

        [Required(ErrorMessage = "Select a folder path.")]
        public string FolderPath
        {
            get { return folderPath; }
            set { if (folderPath == value) return; folderPath = value; OnPropertyChanged("FolderPath"); }
        }

        [Required(ErrorMessage = "Enter a file name.")]
        public string FileName
        {
            get { return fileName; }
            set { if (fileName == value) return; fileName = value; OnPropertyChanged("FileName"); }
        }

        [CustomValidation(typeof(CreateSpectrumFileVM), "ValidateSpectrum")]
        public string Ms1Spectrum
        {
            get { return ms1Spectrum; }
            set { if (ms1Spectrum == value) return; ms1Spectrum = value; OnPropertyChanged("Ms1Spectrum"); }
        }

        [CustomValidation(typeof(CreateSpectrumFileVM), "ValidateSpectrum")]
        public string Ms2Spectrum
        {
            get { return ms2Spectrum; }
            set { if (ms2Spectrum == value) return; ms2Spectrum = value; OnPropertyChanged("Ms2Spectrum"); }
        }

        public static ValidationResult ValidateSpectrum(string test, ValidationContext context)
        {
            if (test == null || test == string.Empty) return ValidationResult.Success;
            else if (!spectrumFormatCheck(test)) return new ValidationResult("Add spectrum correctly.");
            else return ValidationResult.Success;
        }

        private static bool spectrumFormatCheck(string spectrum)
        {
            var peaklist = RawDataParcer.SpectrumStringParcer(spectrum);
            if (peaklist == null || peaklist.Count == 0) return false;

            var flg = true;

            foreach (var peak in peaklist)
                if (peak.Mz <= 0 || peak.Intensity <= 0) flg = false;

            return flg;
        }
    }
}
