using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public sealed class StandardCompoundViewModel : ViewModelBase
    {
        public StandardCompoundViewModel(StandardCompoundModel compound) {
            _compound = compound;
            Refresh();
        }

        private readonly StandardCompoundModel _compound;

        public StandardCompoundModel Compound => _compound;

        public void Refresh() {
            StandardName = _compound.StandardName;
            MolecularWeight = _compound.MolecularWeight;
            Concentration = _compound.Concentration;
            TargetClass = _compound.TargetClass;
            DilutionRate = _compound.DilutionRate;
            PeakID = _compound.PeakID;
        }

        [Required]
        public string? StandardName {
            get => _standardName;
            set {
                if (SetProperty(ref _standardName, value)) {
                    if (!ContainsError(nameof(StandardName))) {
                        _compound.StandardName = _standardName;
                    }
                }
            }
        }
        private string? _standardName;

        [Range(0, double.MaxValue)]
        public double MolecularWeight {
            get => _molecularWeight;
            set {
                if (SetProperty(ref _molecularWeight, value)) {
                    if (!ContainsError(nameof(MolecularWeight))) {
                        _compound.MolecularWeight = _molecularWeight;
                    }
                }
            }
        }
        private double _molecularWeight;

        [Range(0, double.MaxValue)]
        public double Concentration {
            get => _concentration;
            set {
                if (SetProperty(ref _concentration, value)) {
                    if (!ContainsError(nameof(Concentration))) {
                        _compound.Concentration = _concentration;
                    }
                }
            }
        }
        private double _concentration;

        [Required]
        public string? TargetClass {
            get => _targetClass;
            set {
                if (SetProperty(ref _targetClass, value)) {
                    if (!ContainsError(nameof(_targetClass))) {
                        _compound.TargetClass = _targetClass;
                    }
                }
            }
        }
        private string? _targetClass;

        [Range(0, double.MaxValue)]
        public double DilutionRate {
            get => _dilutionRate;
            set {
                if (SetProperty(ref _dilutionRate, value)) {
                    if (!ContainsError(nameof(DilutionRate))) {
                        _compound.DilutionRate = _dilutionRate;
                    }
                }
            }
        }
        private double _dilutionRate;

        [Range(-1, int.MaxValue)]
        public int PeakID {
            get => _peakID;
            set {
                if (SetProperty(ref _peakID, value)) {
                    if (!ContainsError(nameof(PeakID))) {
                        _compound.PeakID = _peakID;
                    }
                }
            }
        }
        private int _peakID;
    }
}
