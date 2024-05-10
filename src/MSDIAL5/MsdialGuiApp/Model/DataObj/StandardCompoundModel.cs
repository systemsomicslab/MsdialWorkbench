using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class StandardCompoundModel : ViewModelBase
    {
        private readonly StandardCompound _compound;

        public StandardCompoundModel(StandardCompound compound) {
            _compound = compound ?? throw new ArgumentNullException(nameof(compound));
            Refresh();
        }

        public StandardCompound Compound => _compound;

        public void Commit() {
            _compound.StandardName = StandardName ?? string.Empty;
            _compound.MolecularWeight = MolecularWeight;
            _compound.Concentration = Concentration;
            _compound.TargetClass = TargetClass ?? string.Empty;
            _compound.DilutionRate = DilutionRate;
            _compound.PeakID = PeakID;
        }

        public void Refresh() {
            StandardName = _compound.StandardName;
            MolecularWeight = _compound.MolecularWeight;
            Concentration = _compound.Concentration;
            TargetClass = _compound.TargetClass;
            DilutionRate = _compound.DilutionRate;
            PeakID = _compound.PeakID;
        }

        public bool TrySetIdIfMatch(AlignmentSpotProperty spotProperty) {
            if (!string.IsNullOrEmpty(spotProperty.Name) && spotProperty.Name.Contains(StandardName)) {
                PeakID = spotProperty.MasterAlignmentID;
                return true;
            }
            return false;
        }

        public bool IsRequiredFieldFilled(IReadOnlyCollection<AlignmentSpotProperty> _spots) {
            if (Concentration <= 0) return false;
            if (string.IsNullOrEmpty(TargetClass)) return false;
            if (PeakID < 0 || PeakID >= _spots.Count) return false;
            return true;
        }

        public string? StandardName {
            get => _standardName;
            set => SetProperty(ref _standardName, value);
        }
        private string? _standardName;

        public double MolecularWeight {
            get => _molecularWeight;
            set => SetProperty(ref _molecularWeight, value);
        }
        private double _molecularWeight;

        public double Concentration {
            get => _concentration;
            set => SetProperty(ref _concentration, value);
        }
        private double _concentration;

        public string? TargetClass {
            get => _targetClass;
            set => SetProperty(ref _targetClass, value);
        }
        private string? _targetClass;

        public double DilutionRate {
            get => _dilutionRate;
            set => SetProperty(ref _dilutionRate, value);
        }
        private double _dilutionRate;

        public int PeakID {
            get => _peakID;
            set => SetProperty(ref _peakID, value);
        }
        private int _peakID;
    }
}
