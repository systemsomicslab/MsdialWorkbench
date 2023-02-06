using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj {
    /// <summary>
    /// This is the storage of raw data (MS1 and MS2 spectrum) of a detected peak.
    /// The MS1 and MS2 spectrum will be used to calculate formula or structure in MS-FINDER program.
    /// </summary>
    public class RawData
    {
        public RawData()
        {
            ScanNumber = -1;
            SpectrumType = MSDataType.Centroid;
            IonMode = IonMode.Positive;
            Comment = string.Empty;
            CollisionEnergy = 40.0;
            Ms1PeakNumber = 0;
            Ms2PeakNumber = 0;
            CarbonNumberFromLabeledExperiment = -1;
            NitrogenNumberFromLabeledExperiment = -1;
            SulfurNumberFromLabeledExperiment = -1;
            OxygenNumberFromLabeledExperiment = -1;
            CarbonNitrogenNumberFromLabeledExperiment = -1;
            CarbonSulfurNumberFromLabeledExperiment = -1;
            NitrogenSulfurNumberFromLabeledExperiment = -1;
            CarbonNitrogenSulfurNumberFromLabeledExperiment = -1;
            IsMarked = false;
        }

        // file path
        public string RawdataFilePath { get; set; }
        public string FormulaFilePath { get; set; }

        // data (basic msp field plus alpha
        public string Name { get; set; }
        public int ScanNumber { get; set; }
        public double RetentionTime { get; set; }
        public double RetentionIndex { get; set; }
        public double AccumulationTime { get; set; }
        public double PrecursorMz { get; set; }
        public string PrecursorType { get; set; }
        public IonMode IonMode { get; set; }
        public MSDataType SpectrumType { get; set; }
        public int Intensity { get; set; }
        public string MetaboliteName { get; set; }
        public string InstrumentType { get; set; }
        public string Ionization { get; set; }
        public string Instrument { get; set; }
        public string Authors { get; set; }
        public string License { get; set; }
        public string Smiles { get; set; }
        public string Inchi { get; set; }
        public string InchiKey { get; set; }
        public string Formula { get; set; }
        public double CollisionEnergy { get; set; }
        public string Comment { get; set; }
        public string Ontology { get; set; }
        public double Similarity { get; set; }
        public double Ccs { get; set; }
        public bool IsMarked { get; set; }
        public int Ms1PeakNumber { get; set; }
        public List<SpectrumPeak> Ms1Spectrum { get; set; } = new List<SpectrumPeak>();
        public int Ms2PeakNumber { get; set; }
        public List<SpectrumPeak> Ms2Spectrum { get; set; } = new List<SpectrumPeak>();
        public List<IsotopicPeak> NominalIsotopicPeakList { get; set; } = new List<IsotopicPeak>();

        //specific fields from labeled experiments,
        public int CarbonNumberFromLabeledExperiment { get; set; }
        public int NitrogenNumberFromLabeledExperiment { get; set; }
        public int SulfurNumberFromLabeledExperiment { get; set; }
        public int OxygenNumberFromLabeledExperiment { get; set; }
        public int CarbonNitrogenNumberFromLabeledExperiment { get; set; }
        public int CarbonSulfurNumberFromLabeledExperiment { get; set; }
        public int NitrogenSulfurNumberFromLabeledExperiment { get; set; }
        public int CarbonNitrogenSulfurNumberFromLabeledExperiment { get; set; }
        
    }
}
