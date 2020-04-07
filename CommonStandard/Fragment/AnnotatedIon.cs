using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
     /// This class is the storage of adduct or isotope ion assignment used in MS-FINDER program.
     /// </summary>
    public class AnnotatedIon
    {
        public enum AnnotationType { Precursor, Product, Isotope, Adduct }
        public AnnotationType PeakType { get; set; }
        public double AccurateMass { get; set; }
        public double Intensity { get; set; }
        public double LinkedAccurateMass { get; set; }
        public double LinkedIntensity { get; set; }
        public AdductIon AdductIon { get; set; }
        public int IsotopeWeightNumber { get; set; }
        /// <summary> C-13, O-18, and something </summary>
        public string IsotopeName { get; set; }
        public AnnotatedIon() { PeakType = AnnotationType.Product; }

        public void SetIsotopeC13(double linkedMz) {
            LinkedAccurateMass = linkedMz;
            PeakType = AnnotationType.Isotope;
            IsotopeWeightNumber = 1;
            IsotopeName = "C-13";
        }

        public void SetIsotope(double linkedMz, double intensity, double linkedIntensity, string isotomeName, int weightNumber) {
            PeakType = AnnotationType.Isotope;
            LinkedAccurateMass = linkedMz;
            Intensity = intensity;
            LinkedIntensity = linkedIntensity;
            IsotopeWeightNumber = weightNumber;
            IsotopeName = isotomeName;
        }

        public void SetAdduct(double linkedMz, double intensity, double linkedIntensity, AdductIon adduct) {
            PeakType = AnnotationType.Adduct;
            Intensity = intensity;
            LinkedIntensity = linkedIntensity;
            LinkedAccurateMass = linkedMz;
            AdductIon = adduct;            
        }
    }
}
