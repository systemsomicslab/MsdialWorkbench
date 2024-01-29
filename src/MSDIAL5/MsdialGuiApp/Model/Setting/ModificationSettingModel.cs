using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.Common.Extension;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CompMs.Common.Proteomics.Parser;
using CompMs.Common.Proteomics.DataObj;

namespace CompMs.App.Msdial.Model.Setting
{
    class ModificationSettingModel : ValidatableBase {
        private ProteomicsParameter Parameter { get; set; }
        public ModificationSettingModel(ProteomicsParameter Parameter) {
            this.Parameter = Parameter;
            var isFirstApp = false;
            if (Parameter.VariableModifications.IsEmptyOrNull()) {
                isFirstApp = true;  
                var mParser = new ModificationsXmlRefParser();
                mParser.Read();
                Parameter.VariableModifications = mParser.Modifications;
            }
            if (Parameter.FixedModifications.IsEmptyOrNull()) {
                var mParser = new ModificationsXmlRefParser();
                mParser.Read();
                Parameter.FixedModifications = mParser.Modifications;
            }

            var vModifications = Parameter.VariableModifications;
            var selectedVModifications = vModifications.Where(n => n.IsSelected).ToList();
            var unselectedVModifications = vModifications.Where(n => n.IsSelected == false).ToList();

            var fModifications = Parameter.FixedModifications;
            var selectedFModifications = fModifications.Where(n => n.IsSelected).ToList();
            var unselectedFModifications = fModifications.Where(n => n.IsSelected == false).ToList();

            if (isFirstApp && selectedVModifications.IsEmptyOrNull()) {
                var defaultVMods = new List<string> { "Acetyl (Protein N-term)", "Oxidation (M)" };
                selectedVModifications = new List<Modification>();
                unselectedVModifications = new List<Modification>();
                foreach (var modification in vModifications) {
                    var frag = false;
                    foreach (var dVMod in defaultVMods) {
                        if (dVMod == modification.Title) {
                            selectedVModifications.Add(modification);
                            frag = true; 
                            break;
                        }
                    }
                    if (!frag) unselectedVModifications.Add(modification);
                }
            }

            unSelectedVariableModifications = new ObservableCollection<Modification>(unselectedVModifications);
            selectedVariableModifications = new ObservableCollection<Modification>(selectedVModifications);

            if (isFirstApp && selectedFModifications.IsEmptyOrNull()) {
                var defaultFMods = new List<string> { "Carbamidomethyl (C)" };
                selectedFModifications = new List<Modification>();
                unselectedFModifications = new List<Modification>();
                foreach (var modification in fModifications) {
                    var frag = false;
                    foreach (var fVMod in defaultFMods) {
                        if (fVMod == modification.Title) {
                            selectedFModifications.Add(modification);
                            frag = true;
                            break;
                        }
                    }
                    if (!frag) unselectedFModifications.Add(modification);
                }
            }

            unSelectedFixedModifications = new ObservableCollection<Modification>(unselectedFModifications);
            selectedFixedModifications = new ObservableCollection<Modification>(selectedFModifications);

            MaxNumberOfModificationsPerPeptide = Parameter.MaxNumberOfModificationsPerPeptide;
        }

        public void Set() {
            Set(Parameter, 
                SelectedVariableModifications, UnSelectedVariableModifications,
                SelectedFixedModifications, UnSelectedFixedModifications,
                MaxNumberOfModificationsPerPeptide);
        }

        private void Set(ProteomicsParameter parameter,
            ObservableCollection<Modification> selectedVariableMods,
            ObservableCollection<Modification> unSelectedVariableMods,
            ObservableCollection<Modification> selectedFixedMods,
            ObservableCollection<Modification> unSelectedFixedMods,
            int maxNumberOfModificationsPerPeptide) {
            var vmods = new List<Modification>();
            foreach (var mod in selectedVariableMods) {
                mod.IsSelected = true;
                mod.IsVariable = true;
                vmods.Add(mod);
            }
            foreach (var mod in unSelectedVariableMods) {
                mod.IsSelected = false;
                mod.IsVariable = true;
                vmods.Add(mod);
            }
            parameter.VariableModifications = vmods;

            var fmods = new List<Modification>();
            foreach (var mod in selectedFixedMods) {
                mod.IsSelected = true;
                mod.IsVariable = false;
                fmods.Add(mod);
            }
            foreach (var mod in unSelectedFixedMods) {
                mod.IsSelected = false;
                mod.IsVariable = false;
                fmods.Add(mod);
            }
            parameter.FixedModifications = fmods;
            parameter.MaxNumberOfModificationsPerPeptide = maxNumberOfModificationsPerPeptide;
        }

        public ObservableCollection<Modification> SelectedVariableModifications {
            get => selectedVariableModifications;
            set => SetProperty(ref selectedVariableModifications, value);
        }

        private ObservableCollection<Modification> selectedVariableModifications;

        public ObservableCollection<Modification> UnSelectedVariableModifications {
            get => unSelectedVariableModifications;
            set => SetProperty(ref unSelectedVariableModifications, value);
        }

        private ObservableCollection<Modification> unSelectedVariableModifications;

        public void SelectsVariableModifications(IEnumerable<Modification> modifications) {
            foreach (var mod in modifications) {
                UnSelectedVariableModifications.Remove(mod);
                SelectedVariableModifications.Add(mod);
            }
        }

        public void UnSelectsVariableModifications(IEnumerable<Modification> modifications) {
            foreach (var mod in modifications) {
                SelectedVariableModifications.Remove(mod);
                UnSelectedVariableModifications.Add(mod);
            }
        }

        public ObservableCollection<Modification> SelectedFixedModifications {
            get => selectedFixedModifications;
            set => SetProperty(ref selectedFixedModifications, value);
        }

        private ObservableCollection<Modification> selectedFixedModifications;

        public ObservableCollection<Modification> UnSelectedFixedModifications {
            get => unSelectedFixedModifications;
            set => SetProperty(ref unSelectedFixedModifications, value);
        }

        private ObservableCollection<Modification> unSelectedFixedModifications;

        public void SelectsFixedModifications(IEnumerable<Modification> modifications) {
            foreach (var mod in modifications) {
                UnSelectedFixedModifications.Remove(mod);
                SelectedFixedModifications.Add(mod);
            }
        }

        public void UnSelectsFixedModifications(IEnumerable<Modification> modifications) {
            foreach (var mod in modifications) {
                SelectedFixedModifications.Remove(mod);
                UnSelectedFixedModifications.Add(mod);
            }
        }

        public int MaxNumberOfModificationsPerPeptide {
            get => maxNumberOfModificationsPerPeptide;
            set => SetProperty(ref maxNumberOfModificationsPerPeptide, value);
        }

        private int maxNumberOfModificationsPerPeptide;

    }
}

