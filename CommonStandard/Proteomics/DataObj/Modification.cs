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
    [MessagePackObject]
    public class ModificationContainer {
        // fixed modifications
        [Key(0)]
        public List<Modification> ProteinNtermFixedMods { get; set; }
        [Key(1)]
        public List<Modification> ProteinCtermFixedMods { get; set; }
        [Key(2)]
        public List<Modification> AnyNtermFixedMods { get; set; }
        [Key(3)]
        public List<Modification> AnyCtermFixedMods { get; set; }
        [Key(4)]
        public List<Modification> AnywhereFixedMods { get; set; }
        [Key(5)]
        public List<Modification> NotCtermFixedMods { get; set; }

        [Key(6)]
        public Dictionary<char, ModificationProtocol> AnywehereSite2FixedMod { get; set; }
        [Key(7)]
        public Dictionary<char, ModificationProtocol> NotCtermSite2FixedMod { get; set; }
        [Key(8)]
        public Dictionary<char, ModificationProtocol> AnyCtermSite2FixedMod { get; set; }
        [Key(9)]
        public Dictionary<char, ModificationProtocol> AnyNtermSite2FixedMod { get; set; }
        [Key(10)]
        public Dictionary<char, ModificationProtocol> ProteinCtermSite2FixedMod { get; set; }
        [Key(11)]
        public Dictionary<char, ModificationProtocol> ProteinNterm2FixedMod { get; set; }

        // variable modifications
        [Key(12)]
        public List<Modification> ProteinNtermVariableMods { get; set; }
        [Key(13)]
        public List<Modification> ProteinCtermVariableMods { get; set; }
        [Key(14)]
        public List<Modification> AnyNtermVariableMods { get; set; }
        [Key(15)]
        public List<Modification> AnyCtermVariableMods { get; set; }
        [Key(16)]
        public List<Modification> AnywhereVariableMods { get; set; }
        [Key(17)]
        public List<Modification> NotCtermVariableMods { get; set; }

        [Key(18)]
        public Dictionary<char, ModificationProtocol> AnywehereSite2VariableMod { get; set; }
        [Key(19)]
        public Dictionary<char, ModificationProtocol> NotCtermSite2VariableMod { get; set; }
        [Key(20)]
        public Dictionary<char, ModificationProtocol> AnyCtermSite2VariableMod { get; set; }
        [Key(21)]
        public Dictionary<char, ModificationProtocol> AnyNtermSite2VariableMod { get; set; }
        [Key(22)]
        public Dictionary<char, ModificationProtocol> ProteinCtermSite2VariableMod { get; set; }
        [Key(23)]
        public Dictionary<char, ModificationProtocol> ProteinNterm2VariableMod { get; set; }

        [Key(24)]
        public Dictionary<string, AminoAcid> Code2AminoAcidObj { get; set; }
        [Key(25)]
        public Dictionary<int, string> ID2Code { get; set; }
        [Key(26)]
        public Dictionary<string, int> Code2ID { get; set; }

        public bool IsEmptyOrNull() {
            return ProteinNtermFixedMods.IsEmptyOrNull() && ProteinCtermFixedMods.IsEmptyOrNull() &&
                AnyNtermFixedMods.IsEmptyOrNull() && AnyCtermFixedMods.IsEmptyOrNull() &&
                AnywhereFixedMods.IsEmptyOrNull() && NotCtermFixedMods.IsEmptyOrNull() &&
                NotCtermSite2FixedMod.IsEmptyOrNull() && AnyCtermSite2FixedMod.IsEmptyOrNull() &&
                AnyNtermSite2FixedMod.IsEmptyOrNull() && ProteinCtermSite2FixedMod.IsEmptyOrNull() && ProteinNterm2FixedMod.IsEmptyOrNull() &&
                ProteinNtermVariableMods.IsEmptyOrNull() && ProteinCtermVariableMods.IsEmptyOrNull() &&
                AnyNtermVariableMods.IsEmptyOrNull() && AnyCtermVariableMods.IsEmptyOrNull() &&
                AnywhereVariableMods.IsEmptyOrNull() && NotCtermVariableMods.IsEmptyOrNull() &&
                NotCtermSite2VariableMod.IsEmptyOrNull() && AnyCtermSite2VariableMod.IsEmptyOrNull() &&
                AnyNtermSite2VariableMod.IsEmptyOrNull() && ProteinCtermSite2VariableMod.IsEmptyOrNull() && ProteinNterm2VariableMod.IsEmptyOrNull();
        }

        [SerializationConstructor]
        public ModificationContainer()
        {

        }

        public ModificationContainer(List<Modification> modifications) {
            ProteinNtermFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "proteinNterm").ToList(); 
            ProteinCtermFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "proteinCterm").ToList(); 
            AnyNtermFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "anyNterm").ToList(); 
            AnyCtermFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "anyCterm").ToList(); 
            AnywhereFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "anywhere").ToList();
            NotCtermFixedMods = modifications.Where(n => !n.IsVariable && n.Position == "notCterm").ToList();

            AnywehereSite2FixedMod = GetModificationProtocolDict(AnywhereFixedMods);
            NotCtermSite2FixedMod = GetModificationProtocolDict(NotCtermFixedMods);
            AnyCtermSite2FixedMod = GetModificationProtocolDict(AnyCtermFixedMods);
            AnyNtermSite2FixedMod = GetModificationProtocolDict(AnyNtermFixedMods);
            ProteinCtermSite2FixedMod = GetModificationProtocolDict(ProteinCtermFixedMods);
            ProteinNterm2FixedMod = GetModificationProtocolDict(ProteinNtermFixedMods);

            ProteinNtermVariableMods = modifications.Where(n => n.IsVariable && n.Position == "proteinNterm").ToList();
            ProteinCtermVariableMods = modifications.Where(n => n.IsVariable && n.Position == "proteinCterm").ToList();
            AnyNtermVariableMods = modifications.Where(n => n.IsVariable && n.Position == "anyNterm").ToList();
            AnyCtermVariableMods = modifications.Where(n => n.IsVariable && n.Position == "anyCterm").ToList();
            AnywhereVariableMods = modifications.Where(n => n.IsVariable && n.Position == "anywhere").ToList();
            NotCtermVariableMods = modifications.Where(n => n.IsVariable && n.Position == "notCterm").ToList();

            AnywehereSite2VariableMod = GetModificationProtocolDict(AnywhereVariableMods);
            NotCtermSite2VariableMod = GetModificationProtocolDict(NotCtermVariableMods);
            AnyCtermSite2VariableMod = GetModificationProtocolDict(AnyCtermVariableMods);
            AnyNtermSite2VariableMod = GetModificationProtocolDict(AnyNtermVariableMods);
            ProteinCtermSite2VariableMod = GetModificationProtocolDict(ProteinCtermVariableMods);
            ProteinNterm2VariableMod = GetModificationProtocolDict(ProteinNtermVariableMods);

            Code2AminoAcidObj = GetAminoAcidDictionaryUsedInModificationProtocol();
            ID2Code = GetID2Code();
            Code2ID = GetCode2ID();
        }

        private Dictionary<string, int> GetCode2ID() {
            var dict = new Dictionary<string, int>();
            foreach (var item in ID2Code) {
                dict[item.Value] = item.Key;
            }
            return dict;
        }

        private Dictionary<int, string> GetID2Code() {
            var counter = 0;
            var dict = new Dictionary<int, string>();
            foreach (var item in Code2AminoAcidObj) {
                dict[counter] = item.Key;
                counter++;
            }
            return dict;
        }

        public Dictionary<string, AminoAcid> GetAminoAcidDictionaryUsedInModificationProtocol() {
            var aaletters = AminoAcidObjUtility.AminoAcidLetters;
            var dict = new Dictionary<string, AminoAcid>();
            var aminoacids = new List<AminoAcid>();
            foreach (var oneletter in aaletters) { // initialize normal amino acids
                var aa = new AminoAcid(oneletter);
                aminoacids.Add(aa);
            }

            // fixed modification's amino acids
            var fixedModificationAAs = new List<AminoAcid>();
            foreach (var aa in aminoacids) {
                var modseq1 = new List<Modification>();
                //isPeptideNTerminal, isPeptideCTerminal, isProteinNTerminal, isProteinCTerminal
                var mAA1 = PeptideCalc.GetAminoAcidByFixedModifications(modseq1, this, aa.OneLetter, false, false, false, false);
                fixedModificationAAs.Add(mAA1);
                var modseq2 = new List<Modification>();
                var mAA2 = PeptideCalc.GetAminoAcidByFixedModifications(modseq2, this, aa.OneLetter, true, false, false, false);
                fixedModificationAAs.Add(mAA2);

                var modseq3 = new List<Modification>();
                var mAA3 = PeptideCalc.GetAminoAcidByFixedModifications(modseq3, this, aa.OneLetter, false, true, false, false);
                fixedModificationAAs.Add(mAA3);

                var modseq4 = new List<Modification>();
                var mAA4 = PeptideCalc.GetAminoAcidByFixedModifications(modseq4, this, aa.OneLetter, true, false, true, false);
                fixedModificationAAs.Add(mAA4);

                var modseq5 = new List<Modification>();
                var mAA5 = PeptideCalc.GetAminoAcidByFixedModifications(modseq5, this, aa.OneLetter, false, true, false, true);
                fixedModificationAAs.Add(mAA5);
            }

            // fixed modification's amino acids
            var variableModificationAAs = new List<AminoAcid>();
            foreach (var aa in fixedModificationAAs) {
                var modseq1 = aa.Modifications.IsEmptyOrNull() ? new List<Modification>() : aa.Modifications.ToList();

                //isPeptideNTerminal, isPeptideCTerminal, isProteinNTerminal, isProteinCTerminal
                var mAA1 = PeptideCalc.GetAminoAcidByVariableModifications(modseq1, this, aa.OneLetter, false, false, false, false);
                variableModificationAAs.Add(mAA1);
                var modseq2 = aa.Modifications.IsEmptyOrNull() ? new List<Modification>() : aa.Modifications.ToList();
                var mAA2 = PeptideCalc.GetAminoAcidByVariableModifications(modseq2, this, aa.OneLetter, true, false, false, false);
                variableModificationAAs.Add(mAA2);

                var modseq3 = aa.Modifications.IsEmptyOrNull() ? new List<Modification>() : aa.Modifications.ToList();
                var mAA3 = PeptideCalc.GetAminoAcidByVariableModifications(modseq3, this, aa.OneLetter, false, true, false, false);
                variableModificationAAs.Add(mAA3);

                var modseq4 = aa.Modifications.IsEmptyOrNull() ? new List<Modification>() : aa.Modifications.ToList();
                var mAA4 = PeptideCalc.GetAminoAcidByVariableModifications(modseq4, this, aa.OneLetter, true, false, true, false);
                variableModificationAAs.Add(mAA4);

                var modseq5 = aa.Modifications.IsEmptyOrNull() ? new List<Modification>() : aa.Modifications.ToList();
                var mAA5 = PeptideCalc.GetAminoAcidByVariableModifications(modseq5, this, aa.OneLetter, false, true, false, true);
                variableModificationAAs.Add(mAA5);
            }

            // finalization
            foreach (var aa in aminoacids) {
                dict[aa.OneLetter.ToString()] = aa;
            }

            foreach (var aa in fixedModificationAAs) {
                var code = aa.Code().ToString();
                if (!dict.ContainsKey(code))
                    dict[code] = aa;
            }

            foreach (var aa in variableModificationAAs) {
                var code = aa.Code().ToString();
                if (!dict.ContainsKey(code))
                    dict[code] = aa;
            }

            return dict;
        }

      
        public Dictionary<char, ModificationProtocol> GetModificationProtocolDict(List<Modification> modifications) {
            var dict = GetInitializeObject();
            foreach (var mod in modifications) {
                foreach (var site in mod.ModificationSites) {
                    if (site.Site.Trim() == "-") { // meaning that the modification is executed in any of amion acid species
                        foreach (var pair in dict) {
                            pair.Value.UpdateProtocol(pair.Key, mod);
                        }
                    }
                    else if (dict.ContainsKey(site.Site[0])) {
                        dict[site.Site[0]].UpdateProtocol(site.Site[0], mod);
                    }
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

        public static ModificationContainer GetModificationContainer(List<string> selectedFixedModifications, List<string> selectedVariableModifications) {
            var fParser = new ModificationsXmlRefParser();
            fParser.Read();
            var fixedMods = fParser.Modifications;

            var vParser = new ModificationsXmlRefParser();
            vParser.Read();
            var variableMods = vParser.Modifications;

            return GetModificationContainer(fixedMods, variableMods, selectedFixedModifications, selectedVariableModifications);
        }

        public static ModificationContainer GetModificationContainer(List<Modification> fixedModifications, List<Modification> variableModifications,
            List<string> selectedFixedModifications, List<string> selectedVariableModifications) {
            
            var sModifications = new List<Modification>();
            foreach (var modString in selectedFixedModifications) {
                foreach (var modObj in fixedModifications) {
                    if (modString == modObj.Title) {
                        modObj.IsSelected = true;
                        modObj.IsVariable = false;
                        sModifications.Add(modObj);
                        break;
                    }
                }
            }

            foreach (var modString in selectedVariableModifications) {
                foreach (var modObj in variableModifications) {
                    if (modString == modObj.Title) {
                        modObj.IsSelected = true;
                        modObj.IsVariable = true;
                        sModifications.Add(modObj);
                        break;
                    }
                }
            }

            return new ModificationContainer(sModifications);
        }

        public static ModificationContainer GetModificationContainer(List<Modification> fixedModifications, List<Modification> variableModifications) {
            var modifications = new List<Modification>();
            var fixedModStrings = new List<string>();
            foreach (var mod in fixedModifications.Where(n => n.IsSelected)) {
                mod.IsVariable = false;
                modifications.Add(mod);
            }

            fixedModStrings = modifications.Select(n => n.Title).ToList();
            foreach (var mod in variableModifications.Where(n => n.IsSelected)) {
                if (!fixedModStrings.Contains(mod.Title)) {
                    mod.IsVariable = true;
                    modifications.Add(mod);
                }
            }

            return new ModificationContainer(modifications);
        }


        public static List<Peptide> GetModifiedPeptides(List<Peptide> peptides, ModificationContainer modContainer,
            int maxNumberOfModificationsPerPeptide = 2,
            double minPeptideMass = 300,
            double maxPeptideMass = 4600) {
            var mPeptides = new List<Peptide>();
            foreach (var peptide in peptides) {
                var results = PeptideCalc.Sequence2Peptides(peptide, modContainer, maxNumberOfModificationsPerPeptide, minPeptideMass, maxPeptideMass);
                foreach (var result in results.OrEmptyIfNull()) {
                    mPeptides.Add(result);
                }
            }
            return mPeptides;
        }

        public static List<Peptide> GetFastModifiedPeptides(List<Peptide> peptides, ModificationContainer modContainer,
            int maxNumberOfModificationsPerPeptide = 2,
            double minPeptideMass = 300,
            double maxPeptideMass = 4600) {
            var mPeptides = new List<Peptide>();
            foreach (var peptide in peptides) {
                if (peptide.ExactMass > maxPeptideMass) continue;
                var results = PeptideCalc.Sequence2FastPeptides(peptide, modContainer, maxNumberOfModificationsPerPeptide, minPeptideMass, maxPeptideMass);
                foreach (var result in results.OrEmptyIfNull()) {
                    mPeptides.Add(result);
                }
            }
            return mPeptides;
        }
    }

    [MessagePackObject]
    public class ModificationProtocol {
        [Key(0)]
        public AminoAcid OriginalAA { get; set; }
        public bool IsModified() { return !ModSequence.IsEmptyOrNull();  }
        [Key(1)]
        public string ModifiedAACode { get; set; } //Tp, K(Acethyl)
        [Key(2)]
        public AminoAcid ModifiedAA { get; set; }
        [Key(3)]
        public Formula ModifiedComposition { get; set; }
        [Key(4)]
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
        [Key(15)]
        public bool IsSelected { get; set; }
        [Key(16)]
        public bool IsVariable { get; set; }
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
