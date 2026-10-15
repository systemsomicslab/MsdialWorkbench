using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>How far a lipid name resolves its chains.</summary>
    public enum LipidRtNameLevel : byte
    {
        None = 0,
        /// <summary>No fatty chain and no sphingoid base (a sterol core only, for instance).</summary>
        NonChain,
        /// <summary>A species-level total only, e.g. "PC 34:1".</summary>
        Species,
        /// <summary>Some chains resolved, the rest a total.</summary>
        Partial,
        /// <summary>A class with one fatty chain, or a name with one chain.</summary>
        SingleChain,
        /// <summary>Every chain resolved.</summary>
        Chain,
    }

    /// <summary>A lipid name read for the retention-time order check.</summary>
    public sealed class LipidRtParsedName
    {
        internal LipidRtParsedName() { }

        /// <summary>True when the name parsed and its total carbon count is positive.</summary>
        public bool IsParsed { get; internal set; }

        /// <summary>Why the name did not parse, or an empty string.</summary>
        public string Note { get; internal set; } = string.Empty;

        /// <summary>"Unsettled", "no MS2", "low score", "w/o MS2", or empty.</summary>
        public string Prefix { get; internal set; } = string.Empty;

        public bool IsUnsettled { get; internal set; }

        /// <summary>An isotope-labelled name such as "(d7)", "(13C)" or "(15N)": an internal standard.</summary>
        public bool IsLabelled { get; internal set; }

        /// <summary>The species-level half (left of "|"), labels and double-bond positions removed.</summary>
        public string Species { get; internal set; } = string.Empty;

        /// <summary>The chain-level half, or the name itself when a single name resolves several chains, or empty.</summary>
        public string Molecular { get; internal set; } = string.Empty;

        /// <summary>The class token of the name, e.g. "PC", "Cer", "SE".</summary>
        public string ClassToken { get; internal set; } = string.Empty;

        /// <summary>The class key: the Ontology, except "BMP" for a BMP-named row whose Ontology is PG.</summary>
        public string ClassKey { get; internal set; } = string.Empty;

        public int Carbon { get; internal set; }

        /// <summary>The double-bond count as written in the species total.</summary>
        public int DoubleBond { get; internal set; }

        /// <summary>The double-bond count with a plasmenyl (P-) linkage counted as O- plus one double bond.</summary>
        public int DoubleBondEquivalent { get; internal set; }

        /// <summary>Oxygens beyond the class's own, including sphingoid hydroxyls (";O2" counts 2).</summary>
        public int Oxygen { get; internal set; }

        /// <summary>"O", "P" or empty.</summary>
        public string Ether { get; internal set; } = string.Empty;

        /// <summary>The chains of the most resolved half, with their roles.</summary>
        public IReadOnlyList<LipidRtChain> Chains { get; internal set; } = Array.Empty<LipidRtChain>();

        public LipidRtNameLevel Level { get; internal set; }

        /// <summary>True when there is no chain at all, or the name did not parse.</summary>
        public bool IsNonChain => !IsParsed || Level == LipidRtNameLevel.NonChain || Level == LipidRtNameLevel.None;
    }

    /// <summary>
    /// Reads an MS-DIAL lipid name into the species-level composition the retention-time order check works on: class
    /// key, total carbons, total double bonds (P- counted as O- plus one) and total extra oxygens (SPEC section 2.2),
    /// and into chains with roles for the plausibility profile (SPEC section 2.4).
    /// </summary>
    /// <remarks>
    /// The species totals come from <see cref="LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(string, string)"/>
    /// applied to the species half of a "species|molecular" name, as the specification says. Three forms are read by
    /// the chain grammar below instead, because VS2 totals them differently from the species string MS-DIAL writes,
    /// or cannot total them:
    /// <list type="bullet">
    /// <item>sterol-type names (SE, SG, ASG, ST, BA): VS2 returns the acyl only and drops the ring core. The core is
    /// the same for every row of one Ontology, so the order is unaffected, but the species total includes it;</item>
    /// <item>a name VS2 cannot total (it throws, or returns no carbons), such as the LNAPE species form "LPE-N (FA)36:4";</item>
    /// <item>the ester scale: for a single chain-level name of FAHFA, NAGly, TG_EST, ASM, Cer_EOS and their kin, the
    /// species string MS-DIAL writes carries one more double bond (and oxygen) for the ester bond than the chains add
    /// up to, so that offset is added and one Ontology never mixes two scales.</item>
    /// </list>
    /// The grammar is a port of the reference parser of the specification (lipidparse.py), and the unit tests check the
    /// totals against the reference values for every eligible row of the evaluation tables.
    /// </remarks>
    public static class LipidRtNameParser
    {
        private static readonly Regex LabelRegex = new Regex(@"\((?:d|D)\d+\)|\(13C\d*\)|\(15N\d*\)", RegexOptions.CultureInvariant);
        private static readonly Regex PositionRegex = new Regex(@"\((?:\d+[EZ]?)(?:,\d+[EZ]?)*\)", RegexOptions.CultureInvariant);
        private static readonly Regex ChainTokenRegex = new Regex(
            @"^(?<eth>[OP]-)?(?<c>\d+):(?<db>\d+)(?<mods>(?:;[A-Za-z0-9]*|;?\((?:\d*OH,?)+\))*)$", RegexOptions.CultureInvariant);
        private static readonly Regex SpeciesRegex = new Regex(@"^(?<cls>[A-Za-z][A-Za-z0-9_\-]*?)\s+(?<body>.+)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex HydroxylGroupRegex = new Regex(@"\((?:\d*OH,?)+\)", RegexOptions.CultureInvariant);
        private static readonly Regex HydroxylGroupWithSeparatorRegex = new Regex(@";?\((?:\d*OH,?)+\)", RegexOptions.CultureInvariant);
        private static readonly Regex OxygenTokenRegex = new Regex(@"^(\d*)O(\d*)$", RegexOptions.CultureInvariant);
        private static readonly Regex DashBetweenDigitsRegex = new Regex(@"(?<=\d)-(?=\d)", RegexOptions.CultureInvariant);
        private static readonly Regex CoreSplitRegex = new Regex(@"[_/]", RegexOptions.CultureInvariant);
        private static readonly Regex SterolCoreRegex = new Regex(@"^(\d+:\d+(?:;[A-Za-z0-9]+)*)(.*)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex SterolAcylLeadRegex = new Regex(@"^(?:;|\s)*(?:FA|PA|LPA)\s+", RegexOptions.CultureInvariant);
        private static readonly Regex PrefixGroupRegex = new Regex(@"^\((O-|FA)?([^()]*)\)", RegexOptions.CultureInvariant);
        private static readonly Regex TailGroupRegex = new Regex(@"\(FA([^()]*)\)$", RegexOptions.CultureInvariant);
        private static readonly Regex CarbonDoubleBondRegex = new Regex(@"\d+:\d+", RegexOptions.CultureInvariant);
        private static readonly Regex Ms4OxygenRegex = new Regex(@"\+(\d*)O\b", RegexOptions.CultureInvariant);

        /// <summary>The isotope label pattern: an internal standard shares the endogenous Ontology in the lab LBM2.</summary>
        public static bool IsLabelledName(string name) {
            return !string.IsNullOrEmpty(name) && LabelRegex.IsMatch(name);
        }

        /// <summary>Sphingolipid Ontologies: the first core chain is the sphingoid base, the second the N-acyl.</summary>
        internal static readonly HashSet<string> Sphingo = new HashSet<string>(StringComparer.Ordinal) {
            "Sph", "DHSph", "PhytoSph", "SM", "ASM", "CerP", "Cer_ADS", "Cer_AS", "Cer_BDS", "Cer_BS", "Cer_OS",
            "Cer_EODS", "Cer_EOS", "Cer_NDS", "Cer_NS", "Cer_NP", "Cer_AP", "Cer_EBDS", "Cer_HS", "Cer_HDS",
            "Cer_NDOS", "Cer_ABP", "Cer_AH", "Cer_NH", "HexCer_NS", "HexCer_NDS", "HexCer_AP", "HexCer_HS",
            "HexCer_HDS", "HexCer_EOS", "Hex2Cer", "Hex3Cer", "SHexCer", "GM3", "GM1", "GD1a", "GD1b", "GD2",
            "GD3", "GT1b", "GQ1b", "NGcGM3", "AHexCer", "ASHexCer", "PE_Cer", "PI_Cer", "MIPC", "SL",
            "Cer_NS_d7", "SM_d9", "Cer_NH_d9", "Cer_AH_d9",
        };

        private static readonly HashSet<string> SphingoTokens = new HashSet<string>(StringComparer.Ordinal) {
            "SPB", "Cer", "SM", "HexCer", "Hex2Cer", "Hex3Cer", "SHexCer", "GM3", "GM1", "GD1a",
            "GD1b", "GD2", "GD3", "GT1b", "GQ1b", "NGcGM3", "AHexCer", "ASHexCer", "PE-Cer", "PI-Cer",
            "MIPC", "SL", "CerP",
        };

        /// <summary>Sphingoid bases without an N-acyl.</summary>
        internal static readonly HashSet<string> SphingoidBaseOnly = new HashSet<string>(StringComparer.Ordinal) { "Sph", "DHSph", "PhytoSph" };

        private static readonly HashSet<string> SterolTokens = new HashSet<string>(StringComparer.Ordinal) { "SE", "SG", "ASG", "ST", "BA" };

        private static readonly HashSet<string> SingleChain = new HashSet<string>(StringComparer.Ordinal) {
            "FA", "OxFA", "CAR", "NAE", "NAGln", "NALeu", "NAPhe", "NASer", "NAVal", "NAAla", "NAGABA", "NAAnt",
            "NA5HT", "NATau", "LPC", "LPE", "LPA", "LPS", "LPI", "LPG", "EtherLPC", "EtherLPE", "EtherLPG",
            "EtherLPS", "EtherLPI", "MG", "CE", "VAE", "LDGTS", "LDGCC", "LDGTA", "MGMG", "DMEDFA", "DMEDOxFA",
            "GPNAE", "BisMeLPA", "BRSE", "CASE", "SISE", "STSE", "EGSE", "DEGSE", "DSMSE", "DCAE", "GDCAE", "GLCAE",
            "TDCAE", "TLCAE", "LCAE", "KLCAE", "KDCAE", "AHexBRS", "AHexCAS", "AHexCS", "AHexSIS", "AHexSTS",
            "CSLPHex", "BRSLPHex", "CASLPHex", "SISLPHex", "STSLPHex", "CE_d7", "LPC_d5", "LPE_d5", "LPG_d5",
            "LPS_d5", "LPI_d5", "PC_d5",
        };

        /// <summary>The number of fatty chains (alkyl, N-acyl and ester acyl included; a sphingoid base counted apart).</summary>
        private static readonly Dictionary<string, int> FattyChainCount = BuildFattyChainCount();

        private static Dictionary<string, int> BuildFattyChainCount() {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal) {
                ["TG"] = 3, ["OxTG"] = 3, ["EtherTG"] = 3, ["TG_EST"] = 4, ["DG"] = 2, ["EtherDG"] = 2, ["PC"] = 2, ["PE"] = 2, ["PS"] = 2, ["PG"] = 2,
                ["PI"] = 2, ["PA"] = 2, ["PT"] = 2, ["PMeOH"] = 2, ["PEtOH"] = 2, ["PBtOH"] = 2, ["DMPE"] = 2, ["MMPE"] = 2, ["BMP"] = 2, ["HBMP"] = 3,
                ["EtherPC"] = 2, ["EtherPE"] = 2, ["EtherPS"] = 2, ["EtherPG"] = 2, ["EtherPI"] = 2, ["OxPC"] = 2, ["OxPE"] = 2, ["OxPS"] = 2,
                ["OxPG"] = 2, ["OxPI"] = 2, ["OxPA"] = 2, ["EtherOxPC"] = 2, ["EtherOxPE"] = 2, ["CL"] = 4, ["DLCL"] = 2, ["MLCL"] = 3,
                ["MGDG"] = 2, ["DGDG"] = 2, ["SQDG"] = 2, ["DGTS"] = 2, ["DGTA"] = 2, ["DGGA"] = 2, ["ADGGA"] = 3, ["DGCC"] = 2, ["SMGDG"] = 2,
                ["EtherMGDG"] = 2, ["EtherDGDG"] = 2, ["EtherSMGDG"] = 2, ["LNAPE"] = 2, ["LNAPS"] = 2, ["FAHFA"] = 2, ["DMEDFAHFA"] = 2,
                ["NAGly"] = 2, ["NAGlySer"] = 2, ["NAOrn"] = 2, ["NATryA"] = 2, ["WE"] = 2, ["DGMG"] = 1, ["Ac2PIM1"] = 2, ["Ac2PIM2"] = 2,
                ["Ac3PIM2"] = 3, ["Ac4PIM2"] = 4, ["SM"] = 1, ["Cer_NS"] = 1, ["Cer_NDS"] = 1, ["Cer_NP"] = 1, ["Cer_AS"] = 1, ["Cer_ADS"] = 1,
                ["Cer_AP"] = 1, ["Cer_HS"] = 1, ["Cer_HDS"] = 1, ["Cer_BS"] = 1, ["Cer_BDS"] = 1, ["Cer_EOS"] = 2, ["Cer_EODS"] = 2,
                ["Cer_EBDS"] = 2, ["HexCer_EOS"] = 2, ["ASM"] = 2, ["AHexCer"] = 2, ["ASHexCer"] = 2, ["SISPHex"] = 2, ["STSPHex"] = 2,
                ["BRSPHex"] = 2, ["CASPHex"] = 2, ["CSPHex"] = 2,
            };
            foreach (var name in SingleChain) {
                if (!counts.ContainsKey(name)) {
                    counts[name] = 1;
                }
            }
            foreach (var name in Sphingo) {
                if (!counts.ContainsKey(name)) {
                    counts[name] = SphingoidBaseOnly.Contains(name) ? 0 : 1;
                }
            }
            return counts;
        }

        /// <summary>
        /// The (double bond, oxygen) offset MS-DIAL's species string carries for the ester bond, relative to the sum of
        /// the chains (SPEC 2.2, verified on the dual names).
        /// </summary>
        internal static readonly Dictionary<string, (int DoubleBond, int Oxygen)> EsterScale = new Dictionary<string, (int, int)>(StringComparer.Ordinal) {
            ["FAHFA"] = (1, 0), ["DMEDFAHFA"] = (1, 0),
            ["NAGly"] = (1, 1), ["NAGlySer"] = (1, 1), ["NAOrn"] = (1, 1), ["NATryA"] = (1, 1),
            ["TG_EST"] = (1, 1), ["ASM"] = (1, 1),
            ["Cer_EOS"] = (1, 1), ["Cer_EODS"] = (1, 1), ["Cer_EBDS"] = (1, 1), ["HexCer_EOS"] = (1, 1),
        };

        private static readonly HashSet<string> NonLipidOntologies = new HashSet<string>(StringComparer.Ordinal) {
            "", "null", "NA", "Undefined", "Others", "Unknown", "SPLASH",
        };

        private static readonly string[] Prefixes = { "Unsettled:", "w/o MS2:", "no MS2:", "low score:" };

        /// <summary>
        /// True when the Ontology names a lipid class (an <see cref="LbmClass"/> member other than the placeholders) and the
        /// name is not a RIKEN VS1 record. The non-lipid MSP matches of a lipidomics run fail this.
        /// </summary>
        public static bool IsLipidOntology(string ontology, string name) {
            if (ontology is null || NonLipidOntologies.Contains(ontology)) {
                return false;
            }
            if (ontology.Length == 0 || !char.IsLetter(ontology[0])) {
                return false;
            }
            if (!System.Enum.TryParse(ontology, ignoreCase: false, out LbmClass lbmClass) || !System.Enum.IsDefined(typeof(LbmClass), lbmClass)) {
                return false;
            }
            if (lbmClass.ToString() != ontology) {
                return false;
            }
            return name is null || !name.TrimStart().StartsWith("RIKEN ", StringComparison.Ordinal);
        }

        /// <summary>
        /// Parses an MS-DIAL lipid name. <paramref name="ontology"/> is the feature's Ontology (LbmClass name); it decides
        /// the chain roles and the scale of the totals.
        /// </summary>
        public static LipidRtParsedName Parse(string name, string ontology) {
            ontology = ontology ?? string.Empty;
            var result = new LipidRtParsedName();
            if (name is null) {
                result.Note = "empty";
                return result;
            }
            var n = name.Trim().Trim('"').Trim();
            foreach (var prefix in Prefixes) {
                if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                    result.Prefix = prefix.TrimEnd(':');
                    if (prefix == "Unsettled:") {
                        result.IsUnsettled = true;
                    }
                    n = n.Substring(prefix.Length).Trim();
                }
            }
            if (n.Length == 0 || string.Equals(n, "unknown", StringComparison.OrdinalIgnoreCase) || string.Equals(n, "null", StringComparison.OrdinalIgnoreCase)) {
                result.Note = "unknown";
                return result;
            }
            result.IsLabelled = LabelRegex.IsMatch(n);
            var clean = LabelRegex.Replace(n, string.Empty);
            clean = PositionRegex.Replace(clean, string.Empty);
            clean = clean.Replace(";(", "(");
            clean = Ms4OxygenRegex.Replace(clean, m => ";O" + m.Groups[1].Value);
            var halves = clean.Split('|').Select(h => h.Trim()).ToArray();

            var species = ParseOne(halves[0], ontology, out var note);
            if (species is null) {
                result.Note = note;
                return result;
            }
            ParsedHalf molecular = null;
            if (halves.Length > 1 && halves[1].Length > 0) {
                molecular = ParseOne(halves[1], ontology, out var note2);
                if (molecular is null) {
                    result.Note = "molecular half: " + note2;
                }
            }
            result.ClassToken = species.ClassToken;
            var speciesChains = species.Chains;
            var speciesResolved = !speciesChains.Any(c => c.IsUnresolved)
                && speciesChains.Any(c => c.IsFatty || c.Role == LipidRtChainRole.SphingoidBase);
            IReadOnlyList<LipidRtChain> chains;
            if (molecular != null) {
                chains = molecular.Chains;
                result.Species = halves[0];
                result.Molecular = halves[1];
            }
            else {
                chains = speciesChains;
                result.Species = halves[0];
                result.Molecular = speciesResolved && speciesChains.Count > 1 ? halves[0] : string.Empty;
            }

            // Totals of the species half.
            var carbon = speciesChains.Sum(c => c.Carbon);
            var doubleBond = speciesChains.Sum(c => c.DoubleBond);
            var oxygen = speciesChains.Sum(c => c.Oxygen);
            var plasmenyl = speciesChains.Any(c => c.Ether == "P" && (c.Role == LipidRtChainRole.Alkyl || c.Role == LipidRtChainRole.SpeciesCore || c.Role == LipidRtChainRole.Acyl));
            if (!SterolTokens.Contains(species.ClassToken)
                && TryTotalsByLipidomicsConverter(DashBetweenDigitsRegex.Replace(halves[0], "_"), ontology, out var vs2Carbon, out var vs2DoubleBondEquivalent, out var vs2Oxygen)) {
                // VS2 counts a P- chain with one more double bond: that is already the equivalent.
                carbon = vs2Carbon;
                doubleBond = vs2DoubleBondEquivalent - (plasmenyl ? 1 : 0);
                oxygen = vs2Oxygen;
            }
            var applyEsterScale = molecular is null && speciesChains.Count > 1
                && EsterScale.TryGetValue(ontology, out _)
                && (speciesChains.Any(c => c.Role == LipidRtChainRole.EsterAcyl) || ontology == "FAHFA" || ontology == "DMEDFAHFA");
            if (applyEsterScale) {
                var offset = EsterScale[ontology];
                doubleBond += offset.DoubleBond;
                oxygen += offset.Oxygen;
            }

            var ether = string.Empty;
            foreach (var chain in speciesChains) {
                if (chain.Ether.Length > 0 && (chain.Role == LipidRtChainRole.Alkyl || chain.Role == LipidRtChainRole.SpeciesCore || chain.Role == LipidRtChainRole.Acyl)) {
                    ether = chain.Ether;
                    break;
                }
            }

            var fatty = chains.Count(c => c.IsFatty);
            var unresolved = chains.Count(c => c.IsUnresolved);
            var bases = chains.Count(c => c.Role == LipidRtChainRole.SphingoidBase);
            LipidRtNameLevel level;
            if (fatty == 0 && unresolved == 0 && bases == 0) {
                level = LipidRtNameLevel.NonChain;
            }
            else if (unresolved > 0 && (fatty > 0 || bases > 0)) {
                level = LipidRtNameLevel.Partial;
            }
            else if (unresolved > 0) {
                level = LipidRtNameLevel.Species;
            }
            else if (SingleChain.Contains(ontology) || SphingoidBaseOnly.Contains(ontology) || fatty + bases == 1) {
                level = LipidRtNameLevel.SingleChain;
            }
            else {
                level = LipidRtNameLevel.Chain;
            }

            result.IsParsed = carbon > 0;
            result.Carbon = carbon;
            result.DoubleBond = doubleBond;
            result.DoubleBondEquivalent = doubleBond + (ether == "P" ? 1 : 0);
            result.Oxygen = oxygen;
            result.Ether = ether;
            result.Chains = chains;
            result.Level = level;
            result.ClassKey = ontology == "PG" && species.ClassToken == "BMP" ? "BMP" : ontology;
            if (carbon == 0 && result.Note.Length == 0) {
                result.Note = "C=0";
            }
            return result;
        }

        /// <summary>
        /// The species totals by MS-DIAL's own lipid name converter. False when it cannot total the name.
        /// </summary>
        private static bool TryTotalsByLipidomicsConverter(string species, string ontology, out int carbon, out int doubleBondEquivalent, out int oxygen) {
            carbon = doubleBondEquivalent = oxygen = 0;
            try {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(species, ontology);
                if (molecule is null || molecule.TotalCarbonCount <= 0) {
                    return false;
                }
                carbon = molecule.TotalCarbonCount;
                doubleBondEquivalent = molecule.TotalDoubleBondCount;
                oxygen = molecule.TotalOxidizedCount;
                return true;
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentException || ex is FormatException || ex is NullReferenceException) {
                return false;
            }
        }

        private sealed class ParsedHalf
        {
            public string ClassToken;
            public List<LipidRtChain> Chains;
        }

        private sealed class RawChain
        {
            public int Carbon;
            public int DoubleBond;
            public int Oxygen;
            public string Ether;
            public string Role; // provisional: "core", or a final role token
        }

        private static ParsedHalf ParseOne(string text, string ontology, out string note) {
            note = string.Empty;
            var m = SpeciesRegex.Match(text.Trim());
            if (!m.Success) {
                note = "no 'CLASS C:DB' form";
                return null;
            }
            var classToken = m.Groups["cls"].Value;
            var body = m.Groups["body"].Value.Trim();
            if (!CarbonDoubleBondRegex.IsMatch(body)) {
                note = "no C:DB";
                return null;
            }
            var chains = ParseBody(classToken, body, out note);
            if (chains is null) {
                return null;
            }
            return new ParsedHalf { ClassToken = classToken, Chains = AssignRoles(chains, ontology, classToken), };
        }

        private static List<RawChain> ParseBody(string classToken, string body, out string note) {
            note = string.Empty;
            var chains = new List<RawChain>();
            if (SterolTokens.Contains(classToken)) {
                // "29:1;O;Hex;PA 7:0_15:3" / "29:2;O;Hex;FA 11:0" / "28:1/18:3" / "24:1;O3;T/20:1" / "27:1;O;PE"
                var m = SterolCoreRegex.Match(body.Trim());
                if (!m.Success) {
                    note = "sterol core not found";
                    return null;
                }
                var core = ParseChainToken(m.Groups[1].Value);
                if (core is null) {
                    note = "bad sterol core";
                    return null;
                }
                core.Role = "sterol_core";
                chains.Add(core);
                var rest = m.Groups[2].Value.Trim();
                rest = SterolAcylLeadRegex.Replace(rest, string.Empty, 1);
                rest = rest.TrimStart('/', ';').Trim();
                if (rest.Length > 0) {
                    foreach (var token in SplitCore(rest.Replace(" ", string.Empty))) {
                        var chain = ParseChainToken(token);
                        if (chain is null) {
                            note = $"bad sterol acyl '{token}'";
                            return null;
                        }
                        if (chain.Carbon == 0 && chain.DoubleBond == 0) {
                            continue;
                        }
                        chain.Role = "acyl";
                        chains.Add(chain);
                    }
                }
                return chains;
            }

            body = body.Replace(" ", string.Empty);
            // prefix groups "(O-12:0)" (O-acyl on a hexose) and "(FA 15:0)" / "(FA)" (N-acyl of LNAPE)
            while (body.StartsWith("(", StringComparison.Ordinal)) {
                var m = PrefixGroupRegex.Match(body);
                if (!m.Success) {
                    break;
                }
                var inner = m.Groups[2].Value;
                if (inner.Length > 0) {
                    var chain = ParseChainToken(inner);
                    if (chain is null) {
                        note = $"bad prefix group '{m.Value}'";
                        return null;
                    }
                    chain.Role = m.Groups[1].Value == "O-" ? "o_acyl" : "acyl";
                    chain.Ether = string.Empty;
                    chains.Add(chain);
                }
                body = body.Substring(m.Index + m.Length);
            }
            // tail groups "(FA 16:0)" (ester-linked acyl of EOS/ASM/TG_EST/NAGly)
            var tails = new List<RawChain>();
            while (true) {
                var m = TailGroupRegex.Match(body);
                if (!m.Success) {
                    break;
                }
                var chain = ParseChainToken(m.Groups[1].Value);
                if (chain is null) {
                    note = $"bad tail '{m.Value}'";
                    return null;
                }
                chain.Role = "ester_acyl";
                tails.Insert(0, chain);
                body = body.Substring(0, m.Index);
            }
            var tokens = SplitCore(body);
            if (tokens.Count == 0) {
                note = "no core chain";
                return null;
            }
            var cores = new List<RawChain>();
            foreach (var token in tokens) {
                var chain = ParseChainToken(token);
                if (chain is null) {
                    note = $"bad chain '{token}'";
                    return null;
                }
                if (chain.Carbon == 0 && chain.DoubleBond == 0 && chain.Oxygen == 0) {
                    continue; // "LPC 16:0/0:0"
                }
                chain.Role = "core";
                cores.Add(chain);
            }
            if (cores.Count == 0) {
                note = "only empty chains";
                return null;
            }
            chains.AddRange(cores);
            chains.AddRange(tails);
            return chains;
        }

        private static List<LipidRtChain> AssignRoles(List<RawChain> chains, string ontology, string classToken) {
            var core = chains.Where(c => c.Role == "core").ToList();
            var sphingo = Sphingo.Contains(ontology) || (NonLipidOntologies.Contains(ontology) && SphingoTokens.Contains(classToken));
            if (sphingo) {
                if (SphingoidBaseOnly.Contains(ontology) || classToken == "SPB") {
                    foreach (var c in core) {
                        c.Role = "sph_base";
                    }
                }
                else if (core.Count >= 2) {
                    core[0].Role = "sph_base";
                    for (int i = 1; i < core.Count; i++) {
                        core[i].Role = "n_acyl";
                    }
                }
                else {
                    foreach (var c in core) {
                        c.Role = "sph_core";
                    }
                }
            }
            else {
                var single = SingleChain.Contains(ontology);
                // "NAGly 8:0;O(FA 19:1)", "LPE-N (FA 15:0)18:2": the explicit prefix/tail acyls plus the one core chain
                // already account for every fatty chain of the class, so the core is resolved.
                var known = chains.Count(c => c.Role == "acyl" || c.Role == "ester_acyl" || c.Role == "o_acyl");
                var expected = FattyChainCount.TryGetValue(ontology, out var count) ? count : 99;
                var complete = core.Count == 1 && known >= 1 && known + 1 >= expected;
                if (core.Count == 1 && !single && !complete) {
                    core[0].Role = "species_core";
                }
                else {
                    foreach (var c in core) {
                        c.Role = c.Ether.Length > 0 ? "alkyl" : "acyl";
                    }
                }
            }
            return chains.Select(c => new LipidRtChain(LipidRtChain.ParseRole(c.Role), c.Carbon, c.DoubleBond, c.Oxygen, c.Ether)).ToList();
        }

        private static List<string> SplitCore(string core) {
            core = DashBetweenDigitsRegex.Replace(core, "_"); // MS-DIAL 4 "16:0-18:1"
            return CoreSplitRegex.Split(core).Where(t => t.Length > 0).ToList();
        }

        private static RawChain ParseChainToken(string token) {
            var m = ChainTokenRegex.Match(token.Trim());
            if (!m.Success) {
                return null;
            }
            var oxygen = CountOxygen(m.Groups["mods"].Value);
            return new RawChain {
                Carbon = int.Parse(m.Groups["c"].Value, CultureInfo.InvariantCulture),
                DoubleBond = int.Parse(m.Groups["db"].Value, CultureInfo.InvariantCulture),
                Oxygen = oxygen,
                Ether = m.Groups["eth"].Value.TrimEnd('-'),
            };
        }

        /// <summary>";O" is 1, ";O2" and ";2O" are 2, "(2OH)" is 1, "(1OH,3OH)" is 2; other ";X" tokens are ignored.</summary>
        private static int CountOxygen(string mods) {
            var n = 0;
            foreach (Match g in HydroxylGroupRegex.Matches(mods)) {
                n += CountOccurrences(g.Value, "OH");
            }
            mods = HydroxylGroupWithSeparatorRegex.Replace(mods, string.Empty);
            foreach (var token in mods.Split(';')) {
                if (token.Length == 0) {
                    continue;
                }
                var m = OxygenTokenRegex.Match(token);
                if (m.Success) {
                    var digits = m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
                    n += digits.Length > 0 ? int.Parse(digits, CultureInfo.InvariantCulture) : 1;
                }
            }
            return n;
        }

        private static int CountOccurrences(string text, string value) {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0) {
                count++;
                index += value.Length;
            }
            return count;
        }
    }
}
