using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class MoleculeModel : BindableBase, IMoleculeProperty
    {
        private IMoleculeProperty? _molecule;
        private readonly Func<IMoleculeProperty> _createMolecule;

        public MoleculeModel(IMoleculeProperty molecule) : this(molecule, () => new MoleculeProperty()) { }

        internal MoleculeModel(AnnotatedMSDecResult result) : this(result.Molecule, result.GetOrCreateMolecule) { }

        private MoleculeModel(IMoleculeProperty? molecule, Func<IMoleculeProperty> createMolecule) {
            _molecule = molecule;
            _createMolecule = createMolecule;
        }

        private IMoleculeProperty WritableMolecule => _molecule ??= _createMolecule();

        public string Name {
            get => _molecule?.Name ?? string.Empty;
            set {
                if (Name != value) {
                    WritableMolecule.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Formula {
            get => _molecule?.Formula?.FormulaString ?? string.Empty;
            set {
                if (value != null && Formula != value && FormulaStringParcer.Convert2FormulaObjV2(value) is Formula formula) {
                    WritableMolecule.Formula = formula;
                    OnPropertyChanged(nameof(Formula));
                }
            }
        }

        public string Ontology {
            get => _molecule?.Ontology ?? string.Empty;
            set {
                if (Ontology != value) {
                    WritableMolecule.Ontology = value;
                    OnPropertyChanged(nameof(Ontology));
                }
            }
        }

        public string SMILES {
            get => _molecule?.SMILES ?? string.Empty;
            set {
                if (SMILES != value) {
                    WritableMolecule.SMILES = value;
                    OnPropertyChanged(nameof(SMILES));
                }
            }
        }

        public string InChIKey {
            get => _molecule?.InChIKey ?? string.Empty;
            set {
                if (InChIKey != value) {
                    WritableMolecule.InChIKey = value;
                    OnPropertyChanged(nameof(InChIKey));
                }
            }
        }

        Formula IMoleculeProperty.Formula {
            get => _molecule?.Formula ?? new Formula();
            set {
                if (_molecule?.Formula != value) {
                    WritableMolecule.Formula = value;
                    OnPropertyChanged(nameof(Formula));
                }
            }
        }
    }
}
