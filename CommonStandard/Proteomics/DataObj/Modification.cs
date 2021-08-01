using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.Function;
using CompMs.Common.Proteomics.Parser;
using MessagePack;
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
        public Dictionary<char, ModificationProtocol> NotCtermSite2Mod { get; set; }
        public Dictionary<char, ModificationProtocol> AnyCtermSite2Mod { get; set; }
        public Dictionary<char, ModificationProtocol> AnyNtermSite2Mod { get; set; }
        public Dictionary<char, ModificationProtocol> ProteinCtermSite2Mod { get; set; }
        public Dictionary<char, ModificationProtocol> ProteinNterm2Mod { get; set; }

        public bool IsEmptyOrNull() {
            return ProteinNtermMods.IsEmptyOrNull() && ProteinCtermMods.IsEmptyOrNull() &&
                AnyNtermMods.IsEmptyOrNull() && AnyCtermMods.IsEmptyOrNull() &&
                AnywhereMods.IsEmptyOrNull() && NotCtermMods.IsEmptyOrNull() &&
                NotCtermSite2Mod.IsEmptyOrNull() && AnyCtermSite2Mod.IsEmptyOrNull() &&
                AnyNtermSite2Mod.IsEmptyOrNull() && ProteinCtermSite2Mod.IsEmptyOrNull() && ProteinNterm2Mod.IsEmptyOrNull();
        }

        public ModificationContainer(List<Modification> modifications) {
            ProteinNtermMods = modifications.Where(n => n.Position == "proteinNterm").ToList(); 
            ProteinCtermMods = modifications.Where(n => n.Position == "proteinCterm").ToList(); 
            AnyNtermMods = modifications.Where(n => n.Position == "anyNterm").ToList(); 
            AnyCtermMods = modifications.Where(n => n.Position == "anyCterm").ToList(); 
            AnywhereMods = modifications.Where(n => n.Position == "anywhere").ToList();
            NotCtermMods = modifications.Where(n => n.Position == "notCterm").ToList();

            AnywehereSite2Mod = GetModificationProtocolDict(AnywhereMods);
            NotCtermSite2Mod = GetModificationProtocolDict(NotCtermMods);
            AnyCtermSite2Mod = GetModificationProtocolDict(AnyCtermMods);
            AnyNtermSite2Mod = GetModificationProtocolDict(AnyNtermMods);
            ProteinCtermSite2Mod = GetModificationProtocolDict(ProteinCtermMods);
            ProteinNterm2Mod = GetModificationProtocolDict(ProteinNtermMods);
        }

        public Dictionary<char, ModificationProtocol> GetModificationProtocolDict(List<Modification> modifications) {
            var dict = GetInitializeObject();
            foreach (var mod in modifications) {
                foreach (var site in mod.ModificationSites) {
                    dict[site.Site[0]].UpdateProtocol(site.Site[0], mod);
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

    public sealed class ModificationUtility {
        private ModificationUtility() { }

        public static Formula GetModifiedComposition(List<Modification> modseqence) {
            if (modseqence.IsEmptyOrNull()) return null;
            var dict = new Dictionary<string, int>();
            foreach (var mod in modseqence) {
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
            return new Formula(dict);
        }

        public static string GetModifiedAminoacidCode(string originalcode, List<Modification> modseqence) {
            if (modseqence.IsEmptyOrNull()) return string.Empty;
            var code = originalcode;
            foreach (var mod in modseqence) {
                code += "[" + mod.Title.Split('(')[0].Trim() + "]";
            }
            return code;
        }

        public static (string, Formula) GetModifiedCompositions(string originalcode, List<Modification> modseqence) {
            if (modseqence.IsEmptyOrNull()) return (string.Empty, null);
            var dict = new Dictionary<string, int>();
            var code = originalcode;
            var modCodes = new List<string>();
            foreach (var mod in modseqence) {
                modCodes.Add(mod.Title.Split('(')[0].Trim());

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

            if (!modCodes.IsEmptyOrNull()) {
                if (modCodes.Count == 1) {
                    code = code + "[" + modCodes[0] + "]";
                }
                else {
                    code = code + "[" + String.Join("][", modCodes) + "]";
                }
            }

            return (code, new Formula(dict));
        }

        public static ModificationContainer GetModificationContainer(List<string> selectedModifications) {
            var mParser = new ModificationsXmlRefParser();
            mParser.Read();

            var modifications = mParser.Modifications;
            return GetModificationContainer(modifications, selectedModifications);
        }

        public static ModificationContainer GetModificationContainer(List<Modification> modifications, List<string> selectedModifications) {
            var sModifications = new List<Modification>();
            foreach (var modString in selectedModifications) {
                foreach (var modObj in modifications) {
                    if (modString == modObj.Title) {
                        sModifications.Add(modObj);
                        break;
                    }
                }
            }

            return new ModificationContainer(sModifications);
        }

        public static List<Peptide> GetModifiedPeptides(List<Peptide> peptides, ModificationContainer modContainer) {
            var mPeptides = new List<Peptide>();
            foreach (var peptide in peptides) {
                mPeptides.Add(PeptideCalc.Sequence2ModifiedPeptide(peptide, modContainer));
            }
            return mPeptides;
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
                        ModifiedAACode += "[" + mod.Title.Split('(')[0].Trim() + "]";
                        ModSequence.Add(mod);
                    }
                }
                else {
                    if (AminoAcidObjUtility.IsAAEqual(oneletter, OriginalAA.OneLetter)) {
                        ModifiedAACode += "[" + mod.Title.Split('(')[0].Trim() + "]";
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
                        ModifiedAACode = oneletter + "[" + mod.Title.Split('(')[0].Trim() + "]";
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
    [MessagePackObject]
    public class Modification {
        [Key(0)]
        public string Title { get; set; }
        [Key(1)]
        public string Description { get; set; }
        [Key(2)]
        public string CreateDate { get; set; }
        [Key(3)]
        public string LastModifiedDate { get; set; }
        [Key(4)]
        public string User { get; set; }
        [Key(5)]
        public int ReporterCorrectionM2 { get; set; }
        [Key(6)]
        public int ReporterCorrectionM1 { get; set; }
        [Key(7)]
        public int ReporterCorrectionP1 { get; set; }
        [Key(8)]
        public int ReporterCorrectionP2 { get; set; }
        [Key(9)]
        public bool ReporterCorrectionType { get; set; }
        [Key(10)]
        public Formula Composition { get; set; } // only derivative moiety 
        [Key(11)]
        public List<ModificationSite> ModificationSites { get; set; } = new List<ModificationSite>();
        [Key(12)]
        public string Position { get; set; } // anyCterm, anyNterm, anywhere, notCterm, proteinCterm, proteinNterm
        [Key(13)]
        public string Type { get; set; } // Standard, Label, IsobaricLabel, Glycan, AaSubstitution, CleavedCrosslink, NeuCodeLabel
        [Key(14)]
        public string TerminusType { get; set; }

    }

    [MessagePackObject]
    public class ModificationSite {
        [Key(0)]
        public string Site { get; set; }
        [Key(1)]
        public List<ProductIon> DiagnosticIons { get; set; } = new List<ProductIon>();
        [Key(2)]
        public List<NeutralLoss> DiagnosticNLs { get; set; } = new List<NeutralLoss>();
    }
}
