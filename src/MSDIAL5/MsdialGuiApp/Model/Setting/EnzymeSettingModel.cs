using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using CompMs.MsdialCore.Parameter;
using CompMs.Common.Proteomics.Parser;

namespace CompMs.App.Msdial.Model.Setting
{
    class EnzymeSettingModel : ValidatableBase {
        private ProteomicsParameter Parameter { get; set; }
        public EnzymeSettingModel(ProteomicsParameter Parameter) {
            this.Parameter = Parameter;
            if (Parameter.EnzymesForDigestion.IsEmptyOrNull()) {
                var eParser = new EnzymesXmlRefParser();
                eParser.Read();
                Parameter.EnzymesForDigestion = eParser.Enzymes;
            }
            var enzymes = Parameter.EnzymesForDigestion;
            var selectedEnzymes = enzymes.Where(n => n.IsSelected).ToList();
            var unselectedEnzymes = enzymes.Where(n => n.IsSelected == false).ToList();
            
            if (selectedEnzymes.IsEmptyOrNull()) {
                var defaultEnzyme = "Trypsin/P";
                selectedEnzymes = new List<Enzyme>();
                unselectedEnzymes = new List<Enzyme>();
                foreach (var enzyme in enzymes) {
                    if (defaultEnzyme == enzyme.Title) {
                        selectedEnzymes.Add(enzyme);
                    }
                    else {
                        unselectedEnzymes.Add(enzyme);
                    }
                }
            }

            unSelectedEnzymes = new ObservableCollection<Enzyme>(unselectedEnzymes);
            this.selectedEnzymes = new ObservableCollection<Enzyme>(selectedEnzymes);

            MaxMissedCleavage = Parameter.MaxMissedCleavage;
        }

        public void Set() {
            Set(Parameter, SelectedEnzymes, UnSelectedEnzymes, MaxMissedCleavage);
        }

        private void Set(ProteomicsParameter parameter, 
            ObservableCollection<Enzyme> selectedEnzymes, 
            ObservableCollection<Enzyme> unSelectedEnzymes, int maxMissedCleavage) {
            var enzymes = new List<Enzyme>();
            foreach (var enzyme in selectedEnzymes) {
                enzyme.IsSelected = true;
                enzymes.Add(enzyme);
            }
            foreach (var enzyme in unSelectedEnzymes) {
                enzyme.IsSelected = false;
                enzymes.Add(enzyme);
            }
            parameter.EnzymesForDigestion = enzymes;
            parameter.MaxMissedCleavage = maxMissedCleavage;
        }

        public ObservableCollection<Enzyme> SelectedEnzymes {
            get => selectedEnzymes;
            set => SetProperty(ref selectedEnzymes, value);
        }

        private ObservableCollection<Enzyme> selectedEnzymes;

        public ObservableCollection<Enzyme> UnSelectedEnzymes {
            get => unSelectedEnzymes;
            set => SetProperty(ref unSelectedEnzymes, value);
        }

        private ObservableCollection<Enzyme> unSelectedEnzymes;

        public void Selects(IEnumerable<Enzyme> enzymes) {
            foreach (var enzyme in enzymes) {
                UnSelectedEnzymes.Remove(enzyme);
                SelectedEnzymes.Add(enzyme);
            }
        }

        public void UnSelects(IEnumerable<Enzyme> enzymes) {
            foreach (var enzyme in enzymes) {
                SelectedEnzymes.Remove(enzyme);
                UnSelectedEnzymes.Add(enzyme);
            }
        }

        public int MaxMissedCleavage {
            get => maxMissedCleavage;
            set => SetProperty(ref maxMissedCleavage, value);
        }

        private int maxMissedCleavage = 2;

    }
}
