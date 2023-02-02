using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public enum DataType { Centroid, Profile }
    public enum MethodType { ddMSMS, diMSMS }
    public enum IonMode { Positive, Negative, Both }
    public enum AnalysisFileType { Sample, Standard, QC, Blank }
    public enum CollisionType { CID, HCD }
    public enum SolventType { CH3COONH4, HCOONH4 }

    public enum SeparationType { Chromatography, IonMobility, Infusion }
    /// <summary>
    /// This is the storage of raw data (MS1 and MS2 spectrum) of a detected peak.
    /// The MS1 and MS2 spectrum will be used to calculate formula or structure in MS-FINDER program.
    /// </summary>
    public class RawData
    {
        // file path
        private string rawdataFilePath;
        private string formulaFilePath;

        // data
        private string name;
        private int scanNumber;
        private double retentionTime;
        private double retentionIndex;
        private double ccs;

        private double precursorMz;
        private double accumulationTime;
        private string precursorType;
        private string instrumentType;
        private string instrument;
        private string authors;
        private string license;
        private string ontology;
        private string smiles;
        private string inchi;
        private string inchiKey;
        private string ionization;
        private string comment;
        private double similarity;
        private bool isMarked;

        private DataType spectrumType;
        private IonMode ionMode;
        private int intensity;
        private double collisionEnergy;
        private string metaboliteName;
        private string formula;
        private int ms1PeakNumber;
        private Spectrum ms1Spectrum;
        private int ms2PeakNumber;
        private Spectrum ms2Spectrum;
        private List<Peak> nominalIsotopicPeakList;

        //specific fields from labeled experiments,
        private int carbonNumberFromLabeledExperiment;
        private int nitrogenNumberFromLabeledExperiment;
        private int sulfurNumberFromLabeledExperiment;
        private int oxygenNumberFromLabeledExperiment;
        private int carbonNitrogenNumberFromLabeledExperiment;
        private int carbonSulfurNumberFromLabeledExperiment;
        private int nitrogenSulfurNumberFromLabeledExperiment;
        private int carbonNitrogenSulfurNumberFromLabeledExperiment;

        public RawData()
        {
            scanNumber = -1;
            spectrumType = DataType.Centroid;
            ionMode = IonMode.Positive;
            ms1Spectrum = new Spectrum();
            ms2Spectrum = new Spectrum();
            nominalIsotopicPeakList = new List<Peak>();
            comment = string.Empty;
            collisionEnergy = 40.0;
            ms1PeakNumber = 0;
            ms2PeakNumber = 0;
            carbonNumberFromLabeledExperiment = -1;
            nitrogenNumberFromLabeledExperiment = -1;
            sulfurNumberFromLabeledExperiment = -1;
            oxygenNumberFromLabeledExperiment = -1;
            carbonNitrogenNumberFromLabeledExperiment = -1;
            carbonSulfurNumberFromLabeledExperiment = -1;
            nitrogenSulfurNumberFromLabeledExperiment = -1;
            carbonNitrogenSulfurNumberFromLabeledExperiment = -1;
            isMarked = false;
        }

        public string RawdataFilePath
        {
            get { return rawdataFilePath; }
            set { rawdataFilePath = value; }
        }

        public string FormulaFilePath
        {
            get { return formulaFilePath; }
            set { formulaFilePath = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int ScanNumber
        {
            get { return scanNumber; }
            set { scanNumber = value; }
        }

        public double RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        public double RetentionIndex
        {
            get { return retentionIndex; }
            set { retentionIndex = value; }
        }

        public double AccumulationTime
        {
            get { return accumulationTime; }
            set { accumulationTime = value; }
        }

        public double PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public string PrecursorType
        {
            get { return precursorType; }
            set { precursorType = value; }
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public DataType SpectrumType
        {
            get { return spectrumType; }
            set { spectrumType = value; }
        }

        public int Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        public string InstrumentType
        {
            get { return instrumentType; }
            set { instrumentType = value; }
        }

        public string Ionization
        {
            get { return ionization; }
            set { ionization = value; }
        }
       
        public string Instrument
        {
            get { return instrument; }
            set { instrument = value; }
        }

        public string Authors
        {
            get { return authors; }
            set { authors = value; }
        }

        public string License
        {
            get { return license; }
            set { license = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Inchi
        {
            get { return inchi; }
            set { inchi = value; }
        }

        public string InchiKey
        {
            get { return inchiKey; }
            set { inchiKey = value; }
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public double CollisionEnergy
        {
            get { return collisionEnergy; }
            set { collisionEnergy = value; }
        }

        public int Ms1PeakNumber
        {
            get { return ms1PeakNumber; }
            set { ms1PeakNumber = value; }
        }

        public Spectrum Ms1Spectrum
        {
            get { return ms1Spectrum; }
            set { ms1Spectrum = value; }
        }

        public int Ms2PeakNumber
        {
            get { return ms2PeakNumber; }
            set { ms2PeakNumber = value; }
        }

        public Spectrum Ms2Spectrum
        {
            get { return ms2Spectrum; }
            set { ms2Spectrum = value; }
        }

        public List<Peak> NominalIsotopicPeakList
        {
            get { return nominalIsotopicPeakList; }
            set { nominalIsotopicPeakList = value; }
        }

        public int CarbonNumberFromLabeledExperiment {
            get { return carbonNumberFromLabeledExperiment; }
            set { carbonNumberFromLabeledExperiment = value; }
        }

        public int NitrogenNumberFromLabeledExperiment {
            get {
                return nitrogenNumberFromLabeledExperiment;
            }

            set {
                nitrogenNumberFromLabeledExperiment = value;
            }
        }

        public int SulfurNumberFromLabeledExperiment {
            get {
                return sulfurNumberFromLabeledExperiment;
            }

            set {
                sulfurNumberFromLabeledExperiment = value;
            }
        }

        public int OxygenNumberFromLabeledExperiment {
            get {
                return oxygenNumberFromLabeledExperiment;
            }

            set {
                oxygenNumberFromLabeledExperiment = value;
            }
        }

        public int CarbonNitrogenNumberFromLabeledExperiment {
            get {
                return carbonNitrogenNumberFromLabeledExperiment;
            }

            set {
                carbonNitrogenNumberFromLabeledExperiment = value;
            }
        }

        public int CarbonSulfurNumberFromLabeledExperiment {
            get {
                return carbonSulfurNumberFromLabeledExperiment;
            }

            set {
                carbonSulfurNumberFromLabeledExperiment = value;
            }
        }

        public int NitrogenSulfurNumberFromLabeledExperiment {
            get {
                return nitrogenSulfurNumberFromLabeledExperiment;
            }

            set {
                nitrogenSulfurNumberFromLabeledExperiment = value;
            }
        }

        public int CarbonNitrogenSulfurNumberFromLabeledExperiment {
            get {
                return carbonNitrogenSulfurNumberFromLabeledExperiment;
            }

            set {
                carbonNitrogenSulfurNumberFromLabeledExperiment = value;
            }
        }

        public string Comment {
            get {
                return comment;
            }

            set {
                comment = value;
            }
        }

        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        public double Similarity {
            get {
                return similarity;
            }

            set {
                similarity = value;
            }
        }

        public double Ccs {
            get {
                return ccs;
            }

            set {
                ccs = value;
            }
        }

        public bool IsMarked {
            get {
                return isMarked;
            }

            set {
                isMarked = value;
            }
        }
    }
}
