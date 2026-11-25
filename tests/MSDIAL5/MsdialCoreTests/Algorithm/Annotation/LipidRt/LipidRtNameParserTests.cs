using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The species composition and chain roles of MS-DIAL lipid names (SPEC 2.2 and 13.2), and the Mammalian
    /// plausibility profile (SPEC 2.4, addendum A3).
    /// </summary>
    /// <remarks>
    /// Every expectation below is the output of the reference parser (lipidparse.py) and profile (plausibility.py) of
    /// the specification on the same name, so the C# reads names exactly as the frozen evaluation did. The names are
    /// synthetic examples of every form, not library content. <see cref="LipidRtDemoParityTests"/> repeats the check
    /// over every eligible row of the evaluation tables when they are available.
    /// </remarks>
    [TestClass]
    public class LipidRtNameParserTests
    {
        public sealed class ParserCase
        {
            public ParserCase(string name, string ontology, bool parsed, int c, int db, int dbe, int o, string ether, string level, string chains,
                string classKey, bool labelled, bool unsettled, string species, string molecular, string plausibility) {
                Name = name; Ontology = ontology; Parsed = parsed; C = c; Db = db; Dbe = dbe; O = o; Ether = ether; Level = level; Chains = chains;
                ClassKey = classKey; Labelled = labelled; Unsettled = unsettled; Species = species; Molecular = molecular; Plausibility = plausibility;
            }

            public string Name, Ontology, Ether, Level, Chains, ClassKey, Species, Molecular, Plausibility;
            public bool Parsed, Labelled, Unsettled;
            public int C, Db, Dbe, O;
        }

        private static readonly ParserCase[] Cases = {
            new ParserCase("PC 34:1|PC 16:0_18:1", "PC", true, 34, 1, 1, 0, "", "chain", "acyl/16/0/0/|acyl/18/1/0/", "PC", false, false, "PC 34:1", "PC 16:0_18:1", "plausible::"),
            new ParserCase("PE P-38:4|PE P-18:0_20:4", "EtherPE", true, 38, 4, 5, 0, "P", "chain", "alkyl/18/0/0/P|acyl/20/4/0/", "EtherPE", false, false, "PE P-38:4", "PE P-18:0_20:4", "plausible::"),
            new ParserCase("SM 41:1;O2|SM 18:1;O2/23:0", "SM", true, 41, 1, 1, 2, "", "chain", "sph_base/18/1/2/|n_acyl/23/0/0/", "SM", false, false, "SM 41:1;O2", "SM 18:1;O2/23:0", "plausible::"),
            new ParserCase("Cer 50:4;O4|Cer 32:2;O3(FA 18:1)", "Cer_EOS", true, 50, 4, 4, 4, "", "partial", "sph_core/32/2/3/|ester_acyl/18/1/0/", "Cer_EOS", false, false, "Cer 50:4;O4", "Cer 32:2;O3(FA 18:1)", "untestable::"),
            new ParserCase("Cer 42:3;O4|Cer 13:2;O2/13:0;O(FA 16:0)", "Cer_EOS", true, 42, 3, 3, 4, "", "chain", "sph_base/13/2/2/|n_acyl/13/0/1/|ester_acyl/16/0/0/", "Cer_EOS", false, false, "Cer 42:3;O4", "Cer 13:2;O2/13:0;O(FA 16:0)", "implausible:B3:soft"),
            new ParserCase("LPC 16:1/0:0", "LPC", true, 16, 1, 1, 0, "", "single_chain", "acyl/16/1/0/", "LPC", false, false, "LPC 16:1/0:0", "", "plausible::"),
            new ParserCase("SE 28:1/18:2", "CASE", true, 46, 3, 3, 0, "", "single_chain", "sterol_core/28/1/0/|acyl/18/2/0/", "CASE", false, false, "SE 28:1/18:2", "SE 28:1/18:2", "plausible::"),
            new ParserCase("ST 27:1;O;S", "SSulfate", true, 27, 1, 1, 1, "", "nonchain", "sterol_core/27/1/1/", "SSulfate", false, false, "ST 27:1;O;S", "", "untestable::"),
            new ParserCase("BA 24:1;O4;T", "BileAcid", true, 24, 1, 1, 4, "", "nonchain", "sterol_core/24/1/4/", "BileAcid", false, false, "BA 24:1;O4;T", "", "untestable::"),
            new ParserCase("PC 33:1(d7)|PC 15:0_18:1(d7)", "PC", true, 33, 1, 1, 0, "", "chain", "acyl/15/0/0/|acyl/18/1/0/", "PC", true, false, "PC 33:1", "PC 15:0_18:1", "plausible::"),
            new ParserCase("TG 18:1_20:3_22:6", "TG", true, 60, 10, 10, 0, "", "chain", "acyl/18/1/0/|acyl/20/3/0/|acyl/22/6/0/", "TG", false, false, "TG 18:1_20:3_22:6", "TG 18:1_20:3_22:6", "plausible::"),
            new ParserCase("Dioctyl phthalate", "Others", false, 0, 0, 0, 0, "", "none", "", "Others", false, false, "", "", "untestable::"),
            new ParserCase("Unsettled: PC 34:1", "PC", true, 34, 1, 1, 0, "", "species", "species_core/34/1/0/", "PC", false, true, "PC 34:1", "", "untestable::"),
            new ParserCase("PC 16:0-18:1", "PC", true, 34, 1, 1, 0, "", "chain", "acyl/16/0/0/|acyl/18/1/0/", "PC", false, false, "PC 16:0-18:1", "PC 16:0-18:1", "plausible::"),
            new ParserCase("AHexCer 37:3;O3|AHexCer (O-12:0)25:3;O3", "AHexCer", true, 37, 3, 3, 3, "", "partial", "o_acyl/12/0/0/|sph_core/25/3/3/", "AHexCer", false, false, "AHexCer 37:3;O3", "AHexCer (O-12:0)25:3;O3", "untestable::"),
            new ParserCase("ASHexCer 48:9;O3|ASHexCer (O-15:1)15:3;O2/18:5;O", "ASHexCer", true, 48, 9, 9, 3, "", "chain", "o_acyl/15/1/0/|sph_base/15/3/2/|n_acyl/18/5/1/", "ASHexCer", false, false, "ASHexCer 48:9;O3", "ASHexCer (O-15:1)15:3;O2/18:5;O", "implausible:B2,B3:soft"),
            new ParserCase("LPE-N (FA)33:2|LPE-N (FA 15:0)18:2", "LNAPE", true, 33, 2, 2, 0, "", "chain", "acyl/15/0/0/|acyl/18/2/0/", "LNAPE", false, false, "LPE-N (FA)33:2", "LPE-N (FA 15:0)18:2", "plausible::"),
            new ParserCase("SG 29:1;O;Hex;PA 22:3|SG 29:1;O;Hex;PA 7:0_15:3", "SISPHex", true, 51, 4, 4, 1, "", "chain", "sterol_core/29/1/1/|acyl/7/0/0/|acyl/15/3/0/", "SISPHex", false, false, "SG 29:1;O;Hex;PA 22:3", "SG 29:1;O;Hex;PA 7:0_15:3", "plausible::"),
            new ParserCase("ASG 29:2;O;Hex;FA 11:0", "AHexSTS", true, 40, 2, 2, 1, "", "single_chain", "sterol_core/29/2/1/|acyl/11/0/0/", "AHexSTS", false, false, "ASG 29:2;O;Hex;FA 11:0", "ASG 29:2;O;Hex;FA 11:0", "plausible::"),
            new ParserCase("SG 28:1;O;Hex", "SHex", true, 28, 1, 1, 1, "", "nonchain", "sterol_core/28/1/1/", "SHex", false, false, "SG 28:1;O;Hex", "", "untestable::"),
            new ParserCase("ST 27:1;O;PE", "SPE", true, 27, 1, 1, 1, "", "nonchain", "sterol_core/27/1/1/", "SPE", false, false, "ST 27:1;O;PE", "", "untestable::"),
            new ParserCase("BA 24:1;O3;T/20:1", "TDCAE", true, 44, 2, 2, 3, "", "single_chain", "sterol_core/24/1/3/|acyl/20/1/0/", "TDCAE", false, false, "BA 24:1;O3;T/20:1", "BA 24:1;O3;T/20:1", "plausible::"),
            new ParserCase("TG 55:2;O2|TG 18:0_16:1_9:0;O(FA 12:0)", "TG_EST", true, 55, 2, 2, 2, "", "chain", "acyl/18/0/0/|acyl/16/1/0/|acyl/9/0/1/|ester_acyl/12/0/0/", "TG_EST", false, false, "TG 55:2;O2", "TG 18:0_16:1_9:0;O(FA 12:0)", "plausible::"),
            new ParserCase("NAGly 27:2;O2|NAGly 8:0;O(FA 19:1)", "NAGly", true, 27, 2, 2, 2, "", "chain", "acyl/8/0/1/|ester_acyl/19/1/0/", "NAGly", false, false, "NAGly 27:2;O2", "NAGly 8:0;O(FA 19:1)", "plausible::"),
            new ParserCase("SM 52:3;O3|SM 34:1;O2(FA 18:1)", "ASM", true, 52, 3, 3, 3, "", "partial", "sph_core/34/1/2/|ester_acyl/18/1/0/", "ASM", false, false, "SM 52:3;O3", "SM 34:1;O2(FA 18:1)", "untestable::"),
            new ParserCase("Cer 52:1;O4|Cer 22:0;O2/14:0(3OH)(FA 16:0)", "Cer_EBDS", true, 52, 1, 1, 4, "", "chain", "sph_base/22/0/2/|n_acyl/14/0/1/|ester_acyl/16/0/0/", "Cer_EBDS", false, false, "Cer 52:1;O4", "Cer 22:0;O2/14:0(3OH)(FA 16:0)", "implausible:B3:soft"),
            new ParserCase("FAHFA 24:1;O|FAHFA 5:0/19:0;O", "FAHFA", true, 24, 1, 1, 1, "", "chain", "acyl/5/0/0/|acyl/19/0/1/", "FAHFA", false, false, "FAHFA 24:1;O", "FAHFA 5:0/19:0;O", "plausible::"),
            new ParserCase("FA 16:0(2OH)", "OxFA", true, 16, 0, 0, 1, "", "single_chain", "acyl/16/0/1/", "OxFA", false, false, "FA 16:0(2OH)", "", "plausible::"),
            new ParserCase("Cer 34:0;O4|Cer 18:0;O3/16:0(2OH)", "Cer_AP", true, 34, 0, 0, 4, "", "chain", "sph_base/18/0/3/|n_acyl/16/0/1/", "Cer_AP", false, false, "Cer 34:0;O4", "Cer 18:0;O3/16:0(2OH)", "plausible::"),
            new ParserCase("SPB 18:1;O2", "Sph", true, 18, 1, 1, 2, "", "single_chain", "sph_base/18/1/2/", "Sph", false, false, "SPB 18:1;O2", "", "plausible::"),
            new ParserCase("DMEDFA 38:9", "DMEDFA", true, 38, 9, 9, 0, "", "single_chain", "acyl/38/9/0/", "DMEDFA", false, false, "DMEDFA 38:9", "", "implausible:B1:hard"),
            new ParserCase("CoQ10", "CoQ", false, 0, 0, 0, 0, "", "none", "", "CoQ", false, false, "", "", "untestable::"),
            new ParserCase("PE-Cer 28:4;O3|PE-Cer 12:0;O2/16:4;O", "PE_Cer", true, 28, 4, 4, 3, "", "chain", "sph_base/12/0/2/|n_acyl/16/4/1/", "PE_Cer", false, false, "PE-Cer 28:4;O3", "PE-Cer 12:0;O2/16:4;O", "implausible:B2,B3:soft"),
            new ParserCase("TG O-34:2|TG O-8:0_10:0_16:2", "EtherTG", true, 34, 2, 2, 0, "O", "chain", "alkyl/8/0/0/O|acyl/10/0/0/|acyl/16/2/0/", "EtherTG", false, false, "TG O-34:2", "TG O-8:0_10:0_16:2", "plausible::"),
            new ParserCase("PC 18:0_20:4+O", "OxPC", true, 38, 4, 4, 1, "", "chain", "acyl/18/0/0/|acyl/20/4/1/", "OxPC", false, false, "PC 18:0_20:4;O", "PC 18:0_20:4;O", "plausible::"),
            new ParserCase("PE 18:0_20:4;(2OH)", "OxPE", true, 38, 4, 4, 1, "", "chain", "acyl/18/0/0/|acyl/20/4/1/", "OxPE", false, false, "PE 18:0_20:4(2OH)", "PE 18:0_20:4(2OH)", "plausible::"),
            new ParserCase("HexCer 48:12;O4", "HexCer_EOS", true, 48, 12, 12, 4, "", "species", "sph_core/48/12/4/", "HexCer_EOS", false, false, "HexCer 48:12;O4", "", "untestable::"),
            new ParserCase("WE 16:4/6:0", "WE", true, 22, 4, 4, 0, "", "chain", "acyl/16/4/0/|acyl/6/0/0/", "WE", false, false, "WE 16:4/6:0", "WE 16:4/6:0", "plausible::"),
            new ParserCase("SM 36:2;O2", "SM", true, 36, 2, 2, 2, "", "species", "sph_core/36/2/2/", "SM", false, false, "SM 36:2;O2", "", "untestable::"),
            new ParserCase("PC O-36:7;O2|PC O-22:6_14:1;O2", "EtherOxPC", true, 36, 7, 7, 2, "O", "chain", "alkyl/22/6/0/O|acyl/14/1/2/", "EtherOxPC", false, false, "PC O-36:7;O2", "PC O-22:6_14:1;O2", "plausible::"),
            new ParserCase("NAGly 8:0;O(FA 19:1)", "NAGly", true, 27, 2, 2, 2, "", "chain", "acyl/8/0/1/|ester_acyl/19/1/0/", "NAGly", false, false, "NAGly 8:0;O(FA 19:1)", "NAGly 8:0;O(FA 19:1)", "plausible::"),
            new ParserCase("FAHFA 5:0/19:0;O", "FAHFA", true, 24, 1, 1, 1, "", "chain", "acyl/5/0/0/|acyl/19/0/1/", "FAHFA", false, false, "FAHFA 5:0/19:0;O", "FAHFA 5:0/19:0;O", "plausible::"),
            new ParserCase("Cer 32:2;O3(FA 18:1)", "Cer_EOS", true, 50, 4, 4, 4, "", "partial", "sph_core/32/2/3/|ester_acyl/18/1/0/", "Cer_EOS", false, false, "Cer 32:2;O3(FA 18:1)", "", "untestable::"),
            new ParserCase("HexCer 18:3;O2/33:2;O2", "HexCer_EOS", true, 51, 5, 5, 4, "", "chain", "sph_base/18/3/2/|n_acyl/33/2/2/", "HexCer_EOS", false, false, "HexCer 18:3;O2/33:2;O2", "HexCer 18:3;O2/33:2;O2", "implausible:B3:soft"),
            new ParserCase("LPE-N (FA 16:0)20:4", "LNAPE", true, 36, 4, 4, 0, "", "chain", "acyl/16/0/0/|acyl/20/4/0/", "LNAPE", false, false, "LPE-N (FA 16:0)20:4", "LPE-N (FA 16:0)20:4", "plausible::"),
            new ParserCase("ADGGA (O-14:1)14:0_14:0", "ADGGA", true, 42, 1, 1, 0, "", "chain", "o_acyl/14/1/0/|acyl/14/0/0/|acyl/14/0/0/", "ADGGA", false, false, "ADGGA (O-14:1)14:0_14:0", "ADGGA (O-14:1)14:0_14:0", "plausible::"),
            new ParserCase("PE P-16:0_22:6", "EtherPE", true, 38, 6, 7, 0, "P", "chain", "alkyl/16/0/0/P|acyl/22/6/0/", "EtherPE", false, false, "PE P-16:0_22:6", "PE P-16:0_22:6", "plausible::"),
            new ParserCase("PC 38:7|PC 16:0_22:7", "PC", true, 38, 7, 7, 0, "", "chain", "acyl/16/0/0/|acyl/22/7/0/", "PC", false, false, "PC 38:7", "PC 16:0_22:7", "implausible:B1:hard"),
            new ParserCase("Cer 34:4;O2|Cer 18:1;O2/16:3", "Cer_NS", true, 34, 4, 4, 2, "", "chain", "sph_base/18/1/2/|n_acyl/16/3/0/", "Cer_NS", false, false, "Cer 34:4;O2", "Cer 18:1;O2/16:3", "implausible:B2:soft"),
            new ParserCase("Cer 30:1;O2|Cer 15:1;O2/15:0", "Cer_NS", true, 30, 1, 1, 2, "", "chain", "sph_base/15/1/2/|n_acyl/15/0/0/", "Cer_NS", false, false, "Cer 30:1;O2", "Cer 15:1;O2/15:0", "implausible:B3:soft"),
            new ParserCase("SPB 24:0;O2", "DHSph", true, 24, 0, 0, 2, "", "single_chain", "sph_base/24/0/2/", "DHSph", false, false, "SPB 24:0;O2", "", "implausible:B3:soft"),
            new ParserCase("Cer 43:7;O4|Cer 27:6;O3(FA 16:0)", "Cer_EOS", true, 43, 7, 7, 4, "", "partial", "sph_core/27/6/3/|ester_acyl/16/0/0/", "Cer_EOS", false, false, "Cer 43:7;O4", "Cer 27:6;O3(FA 16:0)", "untestable::"),
            new ParserCase("CL 24:0_24:0_26:0_18:1", "CL", true, 92, 1, 1, 0, "", "chain", "acyl/24/0/0/|acyl/24/0/0/|acyl/26/0/0/|acyl/18/1/0/", "CL", false, false, "CL 24:0_24:0_26:0_18:1", "CL 24:0_24:0_26:0_18:1", "plausible::"),
            new ParserCase("PG 34:2|PG 16:0_18:2", "PG", true, 34, 2, 2, 0, "", "chain", "acyl/16/0/0/|acyl/18/2/0/", "PG", false, false, "PG 34:2", "PG 16:0_18:2", "plausible::"),
            new ParserCase("BMP 40:8|BMP 18:2_22:6", "PG", true, 40, 8, 8, 0, "", "chain", "acyl/18/2/0/|acyl/22/6/0/", "BMP", false, false, "BMP 40:8", "BMP 18:2_22:6", "plausible::"),
            new ParserCase("low score: PC 34:1", "PC", true, 34, 1, 1, 0, "", "species", "species_core/34/1/0/", "PC", false, false, "PC 34:1", "", "untestable::"),
            new ParserCase("PC 34:1(9Z)", "PC", true, 34, 1, 1, 0, "", "species", "species_core/34/1/0/", "PC", false, false, "PC 34:1", "", "untestable::"),
        };

        public static IEnumerable<object[]> CaseIndices => Enumerable.Range(0, Cases.Length).Select(i => new object[] { i, Cases[i].Name });

        [DataTestMethod]
        [DynamicData(nameof(CaseIndices))]
        public void ReadsTheNameAsTheReferenceParserDoes(int index, string name) {
            var expected = Cases[index];
            var parsed = LipidRtNameParser.Parse(expected.Name, expected.Ontology);
            Assert.AreEqual(expected.Parsed, parsed.IsParsed, "parsed");
            Assert.AreEqual(expected.C, parsed.Carbon, "C");
            Assert.AreEqual(expected.Db, parsed.DoubleBond, "DB");
            Assert.AreEqual(expected.Dbe, parsed.DoubleBondEquivalent, "DBe");
            Assert.AreEqual(expected.O, parsed.Oxygen, "O");
            Assert.AreEqual(expected.Ether, parsed.Ether, "ether");
            Assert.AreEqual(expected.Level, LevelToken(parsed.Level), "level");
            Assert.AreEqual(expected.Chains, string.Join("|", parsed.Chains.Select(c => c.ToString())), "chains");
            Assert.AreEqual(expected.ClassKey, parsed.ClassKey.Length > 0 ? parsed.ClassKey : expected.Ontology, "class key");
            Assert.AreEqual(expected.Labelled, parsed.IsLabelled, "labelled");
            Assert.AreEqual(expected.Unsettled, parsed.IsUnsettled, "unsettled");
            Assert.AreEqual(expected.Species, parsed.Species, "species");
            Assert.AreEqual(expected.Molecular, parsed.Molecular, "molecular");
        }

        [DataTestMethod]
        [DynamicData(nameof(CaseIndices))]
        public void TheMammalianProfileJudgesTheChainsAsTheReferenceProfileDoes(int index, string name) {
            var expected = Cases[index];
            var parsed = LipidRtNameParser.Parse(expected.Name, expected.Ontology);
            var verdict = LipidRtChainRuleProfile.Mammalian.Evaluate(expected.Ontology, parsed.Chains);
            Assert.AreEqual(expected.Plausibility, VerdictToken(verdict));
        }

        [TestMethod]
        public void TheProfileNeverJudgesAClass() {
            // Addendum A3: no lipid-class bias. MGDG/DGDG/SQDG with ordinary chains are plausible like any glycerolipid.
            foreach (var (name, ontology) in new[] { ("MGDG 34:2|MGDG 16:0_18:2", "MGDG"), ("DGDG 36:4|DGDG 18:2_18:2", "DGDG"), ("SQDG 34:1|SQDG 16:0_18:1", "SQDG") }) {
                var parsed = LipidRtNameParser.Parse(name, ontology);
                Assert.AreEqual(LipidRtPlausibility.Plausible, LipidRtChainRuleProfile.Mammalian.Evaluate(ontology, parsed.Chains).Verdict, name);
            }
        }

        [TestMethod]
        public void LipidOntologiesAreLbmClassMembers() {
            Assert.IsTrue(LipidRtNameParser.IsLipidOntology("PC", "PC 34:1"));
            Assert.IsTrue(LipidRtNameParser.IsLipidOntology("Cer_NS", "Cer 34:1;O2"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("Others", "Dioctyl phthalate"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("SPLASH", "PC 33:1(d7)"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("PC", "RIKEN N-VS1 ID-2003"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("1", "PC 34:1"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("pc", "PC 34:1"));
            Assert.IsFalse(LipidRtNameParser.IsLipidOntology("Phenylpropanoids", "Caffeic acid"));
        }

        internal static string LevelToken(LipidRtNameLevel level) {
            switch (level) {
                case LipidRtNameLevel.NonChain: return "nonchain";
                case LipidRtNameLevel.Species: return "species";
                case LipidRtNameLevel.Partial: return "partial";
                case LipidRtNameLevel.SingleChain: return "single_chain";
                case LipidRtNameLevel.Chain: return "chain";
                default: return "none";
            }
        }

        internal static string VerdictToken(LipidRtPlausibilityVerdict verdict) {
            var v = verdict.Verdict == LipidRtPlausibility.Plausible ? "plausible" : verdict.Verdict == LipidRtPlausibility.Implausible ? "implausible" : "untestable";
            var s = verdict.Severity == LipidRtPlausibilitySeverity.Hard ? "hard" : verdict.Severity == LipidRtPlausibilitySeverity.Soft ? "soft" : string.Empty;
            return v + ":" + verdict.Rule + ":" + s;
        }
    }
}
