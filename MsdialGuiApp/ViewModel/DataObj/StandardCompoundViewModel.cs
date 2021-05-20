using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class StandardCompoundViewModel : ViewModelBase
    {
        public StandardCompoundViewModel() : this(new StandardCompound()) {

        }

        public StandardCompoundViewModel(StandardCompound compound) {
            this.Compound = compound;
        }

        public StandardCompound Compound { get; }

        public void Refresh() {
            StandardName = Compound.StandardName;
            MolecularWeight = Compound.MolecularWeight;
            Concentration = Compound.Concentration;
            TargetClass = Compound.TargetClass;
            DilutionRate = Compound.DilutionRate;
            PeakID = Compound.PeakID;
        }

        [Required]
        public string StandardName {
            get => standardName;
            set {
                if (SetProperty(ref standardName, value)) {
                    if (!ContainsError(nameof(StandardName))) {
                        Compound.StandardName = standardName;
                    }
                }
            }
        }
        private string standardName;

        [Range(0, double.MaxValue)]
        public double MolecularWeight {
            get => molecularWeight;
            set {
                if (SetProperty(ref molecularWeight, value)) {
                    if (!ContainsError(nameof(MolecularWeight))) {
                        Compound.MolecularWeight = molecularWeight;
                    }
                }
            }
        }
        private double molecularWeight;

        [Range(0, double.MaxValue)]
        public double Concentration {
            get => concentration;
            set {
                if (SetProperty(ref concentration, value)) {
                    if (!ContainsError(nameof(Concentration))) {
                        Compound.Concentration = concentration;
                    }
                }
            }
        }
        private double concentration;

        [Required]
        public string TargetClass {
            get => targetClass;
            set {
                if (SetProperty(ref targetClass, value)) {
                    if (!ContainsError(nameof(targetClass))) {
                        Compound.TargetClass = targetClass;
                    }
                }
            }
        }
        private string targetClass;

        [Range(0, double.MaxValue)]
        public double DilutionRate {
            get => dilutionRate;
            set {
                if (SetProperty(ref dilutionRate, value)) {
                    if (!ContainsError(nameof(DilutionRate))) {
                        Compound.DilutionRate = dilutionRate;
                    }
                }
            }
        }
        private double dilutionRate;

        [Range(0, int.MaxValue)]
        public int PeakID {
            get => peakID;
            set {
                if (SetProperty(ref peakID, value)) {
                    if (!ContainsError(nameof(PeakID))) {
                        Compound.PeakID = peakID;
                    }
                }
            }
        }
        private int peakID;
    }
}
