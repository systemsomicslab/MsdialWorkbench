using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {

    public class ModificationContainer {
        public List<Modification> ProteinNtermMods { get; set; }
        public List<Modification> ProteinCtermMods { get; set; }
        public List<Modification> AnyNtermMods { get; set; }
        public List<Modification> AnyCtermMods { get; set; }
        public List<Modification> AnywhereMods { get; set; }
        public List<Modification> NotCtermMods { get; set; }

        public Dictionary<char, ModificationProtocol> AnywehereSite2Mod { get; set; }

        public ModificationContainer(List<Modification> modifications) {
            ProteinNtermMods = modifications.Where(n => n.Position == "proteinNterm").ToList(); 
            ProteinCtermMods = modifications.Where(n => n.Position == "proteinCterm").ToList(); 
            AnyCtermMods = modifications.Where(n => n.Position == "anyNterm").ToList(); 
            AnyCtermMods = modifications.Where(n => n.Position == "anyCterm").ToList(); 
            AnywhereMods = modifications.Where(n => n.Position == "anywhere").ToList();
            NotCtermMods = modifications.Where(n => n.Position == "notCterm").ToList();

            AnywehereSite2Mod = GetModificationProtocolDict(AnywhereMods);
        }

        public Dictionary<char, ModificationProtocol> GetModificationProtocolDict(List<Modification> modifications) {
            var dict = GetInitializeObject();
            foreach (var mod in modifications) {
                foreach (var site in mod.ModificationSites) {



                }
            }
            return dict;
        }

        public Dictionary<char, ModificationProtocol> GetInitializeObject() {
            var aaletters = AminoAcidObjUtility.AminoAcidLetters;
            var dict = new Dictionary<char, ModificationProtocol>();

            foreach (var oneletter in aaletters) {
                dict[oneletter] = new ModificationProtocol() {
                    OriginalAA = new AminoAcid(oneletter)
                };
            }
            return dict;
        }
    }

    public class ModificationProtocol {
        public AminoAcid OriginalAA { get; set; }

        public bool IsModified() { return !ModSequence.IsEmptyOrNull();  }
        public string ModifiedAACode { get; set; } //Tp, K(Acethyl)
        public AminoAcid ModifiedAA { get; set; }
        public Formula ModifiedComposition { get; set; }
        public List<Modification> ModSequence { get; set; } = new List<Modification>(); // A -> K -> K(Acethyl)

        public void UpdateProtocol(char oneletter, Modification mod) {
            var threeletter2onechar = AminoAcidObjUtility.ThreeLetter2OneChar;
            if (IsModified()) {
                if (ModSequence[ModSequence.Count - 1].Type == "AaSubstitution") {
                    if (AminoAcidObjUtility.IsAAEqual(oneletter, ModifiedAACode)) {
                        ModifiedAACode += ";" + mod.Title.Split('(')[0].Trim();
                        ModSequence.Add(mod);
                    }
                }
                else {
                    if (AminoAcidObjUtility.IsAAEqual(oneletter, OriginalAA.OneLetter)) {
                        ModifiedAACode += ";" + mod.Title.Split('(')[0].Trim();
                        ModSequence.Add(mod);
                    }
                }
            }
            else {
                if (AminoAcidObjUtility.IsAAEqual(OriginalAA.OneLetter, oneletter)) {
                    if (mod.Type == "AaSubstitution") {
                        var convertedAA = mod.Title.Replace("->", "_").Split('_')[1];
                        if (convertedAA == "CamCys") ModifiedAACode = "CamCys";
                        else {
                            if (threeletter2onechar.ContainsKey(convertedAA)) {
                                ModifiedAACode = threeletter2onechar[convertedAA].ToString();
                            }
                        }
                    }
                    else {
                        ModifiedAACode = oneletter + ";" + mod.Title.Split('(')[0].Trim();
                    }
                    ModSequence.Add(mod);
                }
            }
            if (IsModified()) {
                UpdateObjects();
            }
        }

        private void UpdateObjects() {
            var dict = new Dictionary<string, int>();
            foreach (var mod in ModSequence) {
                var formula = mod.Composition;
                foreach (var pair in formula.Element2Count) {
                    if (dict.ContainsKey(pair.Key)) {
                        dict[pair.Key] += pair.Value;
                    }
                    else {
                        dict[pair.Key] = pair.Value;
                    }
                }
            }
            ModifiedComposition = new Formula(dict);

            foreach (var pair in OriginalAA.Formula.Element2Count) {
                if (dict.ContainsKey(pair.Key)) {
                    dict[pair.Key] += pair.Value;
                }
                else {
                    dict[pair.Key] = pair.Value;
                }
            }
            ModifiedAA = new AminoAcid('0', this.ModifiedAACode, new Formula(dict));
        }
    }

    //proteinNterm modification is allowed only once.
    //proteinCterm modification is allowed only once.
    //anyCterm modification is allowed only once.
    //anyNterm modification is allowed only once.
    public class Modification {
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreateDate { get; set; }
        public string LastModifiedDate { get; set; }
        public string User { get; set; }
        public int ReporterCorrectionM2 { get; set; }
        public int ReporterCorrectionM1 { get; set; }
        public int ReporterCorrectionP1 { get; set; }
        public int ReporterCorrectionP2 { get; set; }
        public bool ReporterCorrectionType { get; set; }
        public Formula Composition { get; set; } // only derivative moiety 
        public List<ModificationSite> ModificationSites { get; set; } = new List<ModificationSite>();
        public string Position { get; set; } // anyCterm, anyNterm, anywhere, notCterm, proteinCterm, proteinNterm
        public string Type { get; set; } // Standard, Label, IsobaricLabel, Glycan, AaSubstitution, CleavedCrosslink, NeuCodeLabel
        public string TerminusType { get; set; }

    }

    public class ModificationSite {
        public string Site { get; set; }
        public List<ProductIon> DiagnosticIons { get; set; } = new List<ProductIon>();
        public List<NeutralLoss> DiagnosticNLs { get; set; } = new List<NeutralLoss>();
    }
}
