using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class PeptideMsReference : IMSScanProperty, IIonProperty {
        public Peptide Peptide { get; }
        private Stream Fs { get; set; }
        private long SeekPoint { get; set; }

        public PeptideMsReference(Peptide peptide, Stream fs, long seekPoint, AdductIon adduct) {
            Peptide = peptide; Fs = fs; SeekPoint = seekPoint; AdductType = adduct;
            PrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adduct, peptide.ExactMass);
        }

        public int ScanID { get; set; }
        public List<SpectrumPeak> Spectrum { get => ReadSpectrum(Fs, SeekPoint); set => new NotSupportedException(); }

        private List<SpectrumPeak> ReadSpectrum(Stream fs, long seekPoint) {
            lock (fs) {
                return MsfFileParser.ReadSpectrumPeaks(fs, seekPoint);
            }
        }

        public void AddPeak(double mass, double intensity, string comment = null) {

        }

        public ChromXs ChromXs { get; set; }
        public IonMode IonMode { get; set; } = IonMode.Positive;
        public double PrecursorMz { get; set; }

        public AdductIon AdductType { get; set; }
        public double CollisionCrossSection { get; set; }

    }
}
