#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EngineHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.EvaluatorHarness;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.InlineRuleSet;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// One case of <c>engine/semantics.json</c> (design 7.2): an inline mini rule set, the options, the four inputs (plus
/// optional adduct evidence) and a hand-written expectation. The C# canonical result bytes are added when the fixture
/// is recorded.
/// </summary>
internal sealed class SemanticsCase
{
    public SemanticsCase(string name, string description, InlineRuleSet set) {
        Name = name;
        Description = description;
        Set = set;
    }

    public string Name { get; }
    public string Description { get; }
    public InlineRuleSet Set { get; }

    /// <summary>The option spec (see <see cref="EngineFixtures.Options"/>), single quotes for double quotes.</summary>
    public string Options { get; set; } = "{}";

    /// <summary>[m/z text, intensity text] in input order: decimal text written from integers, never from a double.</summary>
    public List<string[]> Peaks { get; set; } = new List<string[]>();

    /// <summary>Precursor m/z text; null is a missing precursor (NaN).</summary>
    public string? Precursor { get; set; }

    /// <summary>pos, neg or null (unsupported).</summary>
    public string? IonMode { get; set; } = "neg";

    public string? Instrument { get; set; } = "QTOF";

    public string? EvidenceAdduct { get; set; }

    public string? EvidenceTier { get; set; }

    /// <summary>The hand-written expectation (see <see cref="EngineFixtures.CheckExpectations"/>), single quotes for
    /// double quotes.</summary>
    public string Expect { get; set; } = "{}";
}

/// <summary>
/// The hand-derived cases of <c>engine/semantics.json</c>. Every m/z is written from an integer number of micro-Da:
/// a theoretical ion m/z from the test-side pDa oracle (<see cref="TestMass"/>), rounded to 1e-6 Da, plus a chosen
/// integer offset. Intensities are integers. Every rule set carries the synthetic tables of <see cref="InlineRuleSet"/>.
/// The expectations were written from the design (sections 4.3 to 4.11), not copied from the output.
/// </summary>
internal static class SemanticsFixtureCases
{
    private const long MicroPerDa = 1000000L;

    // Rule ids.
    private const string Neg = RuleId; // flavonoid.o_glycoside.neg: all(Y0, X), core C15H10O5 + Hex
    private const string Pos = "flavonoid.o_glycoside.pos";
    private const string Loss = "flavonoid.loss.neg";
    private const string SeriesRule = "flavonoid.series.neg";
    private const string Counted = "flavonoid.counted.neg";
    private const string KVariable = "flavonoid.kvar.neg";
    private const string Ordering = "flavonoid.ordering.neg";
    private const string Stronger = "flavonoid.stronger.neg";
    private const string Upgrade = "flavonoid.upgrade.neg";
    private const string Caffeoyl = "hydroxycinnamoyl.caffeoyl_hexose.neg";
    private const string Msdial = "flavonoid.msdial.neg";
    private const string Ppm = "flavonoid.ppm.neg";
    private const string KOffsetRule = "flavonoid.kof.neg";
    private const string Plain = "flavonoid.plain.neg";
    private const string Scaffold = "flavonoid.scaffold.neg";
    private const string Branch = "flavonoid.branch.neg";
    private const string Glycoside = "flavonoid.glycoside.neg", Flavonoid = "flavonoid.any.neg", Anthocyanin = "anthocyanin.any.neg";

    // Ions (negative unless noted) and their formulas.
    private const string AIon = "C7H3O4", BIon = "C8H5O4", CIon = "C9H7O4", VIon = "C5H3O3", UIon = "C10H9O4", Decarb = "C8H7O2";
    private const string Y0Pos = "C15H11O5", XPos = "C7H5O4";

    // ------------------------------------------------------------------------------------------------ m/z text

    /// <summary>pDa rounded half up to an integer number of micro-Da.</summary>
    public static long Micro(long pda) => (pda + 500000L) / 1000000L;

    /// <summary>The m/z of an ion of charge z in micro-Da.</summary>
    public static long IonMicro(string formula, int z) => Micro(TestMass.Of(formula) - z * TestMass.Electron);

    /// <summary>Decimal text of an integer number of micro-units: at most 6 decimals, at least one.</summary>
    public static string Text(long micro) {
        var negative = micro < 0;
        var a = negative ? -micro : micro;
        var frac = (a % MicroPerDa).ToString("D6", CultureInfo.InvariantCulture).TrimEnd('0');
        return (negative ? "-" : "") + (a / MicroPerDa).ToString(CultureInfo.InvariantCulture) + "." + (frac.Length == 0 ? "0" : frac);
    }

    private static string[] Peak(long mzMicro, long intensity) => new[] { Text(mzMicro), Text(intensity * MicroPerDa) };

    private static string[] NegIon(string formula, long intensity, long offsetMicro = 0) => Peak(IonMicro(formula, -1) + offsetMicro, intensity);

    private static string[] PosIon(string formula, long intensity, long offsetMicro = 0) => Peak(IonMicro(formula, 1) + offsetMicro, intensity);

    /// <summary>The base peak of every spectrum: m/z 300, intensity 1000, so a peak of intensity I has p2 rel I/1000.</summary>
    private static string[] Base => Peak(300 * MicroPerDa, 1000);

    /// <summary>[M-H]- of a neutral formula, in micro-Da.</summary>
    private static long Deprotonated(string neutral) => Micro(TestMass.Of(neutral) - TestMass.Of("H") + TestMass.Electron);

    private static long Protonated(string neutral) => Micro(TestMass.Of(neutral) + TestMass.Of("H") - TestMass.Electron);

    private static long Sodiated(string neutral) => Micro(TestMass.Of(neutral) + TestMass.Of("Na") - TestMass.Electron);

    /// <summary>[M-H]- of apigenin hexoside C21H20O10 (431.098371).</summary>
    private static long GlycosideNeg => Deprotonated("C21H20O10");

    private static List<string[]> Peaks(params string[][] peaks) => peaks.ToList();

    /// <summary>The basic firing spectrum: base, Y0 (C15H9O5-) at 800 and X (C7H3O4-) at 500.</summary>
    private static List<string[]> Firing(long xOffsetMicro = 0) => Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 500, xOffsetMicro));

    // ------------------------------------------------------------------------------------------------ rule sets

    private static string Ion(string id, string role, string formula, string floor = "0.05") => Term(id, "product_ion", role, Formula(formula), floor);

    private static InlineRuleSet Claim(this InlineRuleSet set, string ruleId, params string[] ids) =>
        set.SetRule(ruleId, "claim/claim_term_ids", "[" + string.Join(",", ids.Select(i => "\"" + i + "\"")) + "]");

    /// <summary>all(Y0, Xp) in positive mode under [M+H]+, Xp = C7H5O4+.</summary>
    private static InlineRuleSet AddPos(this InlineRuleSet set) =>
        set.AddRule(Pos, Terms(EvaluatorHarness.Y0, Ion("Xp", "required", XPos)), AllOf(new[] { "Y0", "Xp" }), CoreHex, "[\"[M+H]+\"]", "Y0").Claim(Pos, "Y0", "Xp");

    /// <summary>all(loss_hex, X): the loss of C6H10O5 from the precursor and X.</summary>
    private static InlineRuleSet LossSet() =>
        new InlineRuleSet().AddRule(Loss, Terms(Term("loss_hex", "loss_from_precursor", "required", Formula("C6H10O5")), EvaluatorHarness.X),
                                    AllOf(new[] { "loss_hex", "X" }), CoreHex, "[\"[M-H]-\"]", "loss_hex").Claim(Loss, "loss_hex", "X");

    /// <summary>k_of(k, a, b, c), every term counted and a claim term.</summary>
    private static InlineRuleSet CountedSet(string ruleId, string k) =>
        new InlineRuleSet().AddRule(ruleId, Terms(Ion("a", "counted", AIon), Ion("b", "counted", BIon), Ion("c", "counted", CIon)),
                                    KOf(k, TermRef("a"), TermRef("b"), TermRef("c")), CoreHex, "[\"[M-H]-\"]", "a").Claim(ruleId, "a", "b", "c");

    /// <summary>Caffeoyl hexose (C9H8O4 + Hex, [M-H]- 341.087806): k_of(2, loss_hex, caffeate, decarb); the loss of
    /// C6H10O5 and caffeate C9H7O4- coincide exactly in pDa.</summary>
    private static InlineRuleSet CaffeoylSet(bool declared) {
        var set = new InlineRuleSet().AddRule(Caffeoyl, Terms(
                Term("loss_hex", "loss_from_precursor", "counted", Formula("C6H10O5")),
                Term("caffeate", "product_ion", "counted", Formula(CIon)),
                Term("decarb", "product_ion", "counted", Formula(Decarb))),
            KOf("2", TermRef("loss_hex"), TermRef("caffeate"), TermRef("decarb")),
            InlineRuleSet.Composition("C9H8O4", null, "{\"Hex\":[1,1]}", "{}"), "[\"[M-H]-\"]", "caffeate");
        return declared ? set.SetRule(Caffeoyl, "complementary", "[[\"loss_hex\",\"caffeate\"]]") : set;
    }

    private static long CaffeoylNeg => Deprotonated("C15H18O9");

    private static List<string[]> CaffeoylPeaks => Peaks(Base, NegIon(CIon, 600), NegIon(Decarb, 300));

    /// <summary>The basic rule and a copy in the msdial convention with 10 % floors.</summary>
    private static InlineRuleSet ConventionSet() {
        var set = Basic();
        set.AddRule(Msdial, Terms(Term("Y0", "aglycone_ion", "required", Aglycone("even", "proton"), "10.0"), Ion("X", "required", XIon, "10.0")),
                    AllOf(new[] { "Y0", "X" }), CoreHex, "[\"[M-H]-\"]", "Y0").Claim(Msdial, "Y0", "X");
        return set.SetRule(Msdial, "convention", "\"msdial\"");
    }

    /// <summary>Core C15H10O5 with three Hex ([M-H]- 755.204018): Y0 and the Y2 ion C27H29O15- (593.151194), above
    /// m/z 500 where p2 is 10 ppm (0.005932 Da).</summary>
    private static InlineRuleSet PpmSet() =>
        new InlineRuleSet().AddRule(Ppm, Terms(EvaluatorHarness.Y0, Ion("Y2", "required", "C27H29O15")), AllOf(new[] { "Y0", "Y2" }),
                                    InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[3,3]}", "{}"), "[\"[M-H]-\"]", "Y0").Claim(Ppm, "Y0", "Y2");

    private static List<string[]> PpmPeaks(long offsetMicro) => Peaks(Base, NegIon(Y0Ion, 800), NegIon("C27H29O15", 500, offsetMicro));

    private static InlineRuleSet Msfinder(this InlineRuleSet set, string ruleId) => set.SetRule(ruleId, "product_tolerance", "\"msfinder\"");

    private const string MsfinderReview = "{\"abs_da\":0.01,\"ppm\":0.0,\"source\":\"msfinder\"}";

    /// <summary>The measured k_of(2, c0, c1, c2) rule of the k-1 variant and the unmeasured basic rule.</summary>
    private static InlineRuleSet KOffsetSet() =>
        Basic().AddRule(KOffsetRule, Terms(Ion("c0", "counted", AIon), Ion("c1", "counted", BIon), Ion("c2", "counted", CIon)),
                        KOf("2", TermRef("c0"), TermRef("c1"), TermRef("c2")), CoreHex, "[\"[M-H]-\"]", "c0")
               .Claim(KOffsetRule, "c0", "c1", "c2").Measured(KOffsetRule);

    /// <summary>Three copies of the basic rule claiming flavonoid_o_glycoside (a child of flavonoid), flavonoid and,
    /// optionally, anthocyanin.</summary>
    private static InlineRuleSet MotifSet(bool anthocyanin) {
        var set = Basic(Glycoside).AddBasic(Flavonoid).SetRule(Flavonoid, "claim/motif_id", "\"flavonoid\"");
        return anthocyanin ? set.AddBasic(Anthocyanin).SetRule(Anthocyanin, "claim/motif_id", "\"anthocyanin\"") : set;
    }

    // ------------------------------------------------------------------------------------------------ the cases

    public static IReadOnlyList<SemanticsCase> All() {
        var cases = new List<SemanticsCase>();
        SemanticsCase Add(string name, string description, InlineRuleSet set) {
            var c = new SemanticsCase(name, description, set) { Options = "{'filter':'all'}", Precursor = Text(GlycosideNeg) };
            cases.Add(c);
            return c;
        }

        // ---- kinds, the required role, both modes of the basic rule
        var c01 = Add("basic_fires", "all(Y0, X): the aglycone ion and a fixed product ion, both required, fire on [M-H]-.", Basic());
        c01.Peaks = Firing();
        c01.Expect = "{'status':'evaluated','precondition_mode':'adduct_ambiguous','adduct_tier':'assumed','instrument_class':'QTOF-type',"
            + "'instrument_type':'QTOF','rules_evaluated':1,'not_measured':[],'n_peaks_in':3,'n_peaks_dropped':0,'base_intensity':'1000.000000',"
            + "'fired_motifs':['flavonoid_o_glycoside'],'narrowest_motifs':['flavonoid_o_glycoside'],'rule_conflict':false,'outcome_ids':['" + Neg + "'],"
            + "'outcomes':{'" + Neg + "':{'fired':true,'veto_blocked':false,'n_hypotheses':1,'n_hypotheses_fired':1,'n_distinct_formulas':1,"
            + "'chosen':'core|Hex1|[M-H]-','chosen_neutral_formula':'C21H20O10','chosen_adduct_source':'rule','chosen_adduct_tier':'assumed',"
            + "'chosen_score':'1.300000','chosen_failure':null,'matched':['Y0:A@1','X:C7H3O4@2'],'tied_hypothesis_ids':['core|Hex1|[M-H]-'],"
            + "'claim_eligible':false,'claim_ineligible':['not_reviewed'],'convention':'p2','convention_label':'exact','within_calibration':true,"
            + "'product_tolerance':{'abs_da':'0.005000','ppm':'10.000','source':'p2'},'precursor_tolerance_da':'0.010000','loss_reference':'hypothesis'}}}";

        var c02 = Add("required_absent", "X is absent: the hypothesis is counted and fails with logic_false; nothing is chosen.", Basic());
        c02.Options = "{'filter':'all','verbose':true}";
        c02.Instrument = null;
        c02.Peaks = Peaks(Base, NegIon(Y0Ion, 800));
        c02.Expect = "{'status':'evaluated','instrument_class':'unknown','instrument_type':null,'fired_motifs':[],"
            + "'outcomes':{'" + Neg + "':{'fired':false,'veto_blocked':false,'n_hypotheses':1,'n_hypotheses_fired':0,'chosen':null,"
            + "'tied_hypothesis_ids':[],'within_calibration':false,'hypotheses':{'core|Hex1|[M-H]-':'logic_false'}}}}";

        var c03 = Add("required_below_floor", "X at rel 0.04 is a hit below the 0.05 floor: not present, so not assigned.", Basic());
        c03.Options = "{'filter':'all','verbose':true}";
        c03.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 40));
        c03.Expect = "{'outcomes':{'" + Neg + "':{'fired':false,'chosen':null,'hypotheses':{'core|Hex1|[M-H]-':'logic_false'}}}}";

        var c04 = Add("positive_mode", "The positive rule all(Y0, Xp) fires on [M+H]+; Y0 is A+H.", new InlineRuleSet().AddPos());
        c04.IonMode = "pos";
        c04.Precursor = Text(Protonated("C21H20O10"));
        c04.Peaks = Peaks(Base, PosIon(Y0Pos, 800), PosIon(XPos, 500));
        c04.Expect = "{'status':'evaluated','outcomes':{'" + Pos + "':{'fired':true,'chosen':'core|Hex1|[M+H]+','chosen_adduct_source':'rule',"
            + "'matched':['Y0:A@1','Xp:C7H5O4@2'],'chosen_score':'1.300000'}}}";

        var c05 = Add("common_adduct_fallback", "adduct_ambiguous also tries the common adducts: [M+Na]+ matches, with adduct_source common.",
                      new InlineRuleSet().AddPos());
        c05.IonMode = "pos";
        c05.Precursor = Text(Sodiated("C21H20O10"));
        c05.Peaks = Peaks(Base, PosIon(Y0Pos, 800), PosIon(XPos, 500));
        c05.Expect = "{'outcomes':{'" + Pos + "':{'fired':true,'n_hypotheses':1,'chosen':'core|Hex1|[M+Na]+','chosen_adduct':'[M+Na]+',"
            + "'chosen_adduct_source':'common','matched':['Y0:A@1','Xp:C7H5O4@2']}}}";

        var c06 = Add("loss_from_precursor", "The loss of C6H10O5 from the hypothesis precursor (target 269.045547) and X.", LossSet());
        c06.Peaks = Peaks(Base, NegIon(Y0Ion, 700), NegIon(XIon, 500));
        c06.Expect = "{'outcomes':{'" + Loss + "':{'fired':true,'chosen':'core|Hex1|[M-H]-','matched':['loss_hex:C6H10O5@1','X:C7H3O4@2'],"
            + "'chosen_score':'1.200000'}}}";

        var c07 = Add("ion_series", "ion_series over sugars of C15H10O5 + Hex2: the one proper non-empty subset Hex1 (C21H19O10-) and Y0.",
                      new InlineRuleSet().AddRule(SeriesRule, Terms(EvaluatorHarness.Y0, Term("ys", "ion_series", "required", Series("\"sugar\""))),
                                                  AllOf(new[] { "Y0", "ys" }), InlineRuleSet.Composition("C15H10O5", null, "{\"Hex\":[2,2]}", "{}"),
                                                  "[\"[M-H]-\"]", "Y0").Claim(SeriesRule, "Y0", "ys"));
        c07.Precursor = Text(Deprotonated("C27H30O15"));
        c07.Peaks = Peaks(Base, NegIon(Y0Ion, 600), NegIon("C21H19O10", 700));
        c07.Expect = "{'outcomes':{'" + SeriesRule + "':{'fired':true,'chosen':'core|Hex2|[M-H]-','chosen_units':{'Hex':2},"
            + "'matched':['Y0:A@1','ys:Hex1@2'],'chosen_score':'1.300000'}}}";

        // ---- the veto roles and the window kind
        var c08 = Add("window_veto_present", "A peak 0.010 Da from the C9H7O4- window centre (half width 0.02) blocks the rule.", Basic(withVeto: true));
        c08.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 500), NegIon(VetoIon, 200, 10000));
        c08.Expect = "{'fired_motifs':[],'outcomes':{'" + Neg + "':{'fired':false,'veto_blocked':true,'n_hypotheses_fired':0,'tied_hypothesis_ids':[],"
            + "'chosen':'core|Hex1|[M-H]-','chosen_failure':'veto','chosen_fired':false,'chosen_fired_ignoring_vetoes':true,"
            + "'vetoes':['w:true'],'chosen_vetoes':[{'term_id':'w','fired':true,'i_a':'0.200000','i_b_max':'0.000000','peak_index':3}],"
            + "'matched':['Y0:A@1','X:C7H3O4@2']}}}";

        var c09 = Add("window_veto_outside", "A peak 0.030 Da from the window centre is outside it: no veto.", Basic(withVeto: true));
        c09.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 500), NegIon(VetoIon, 200, 30000));
        c09.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'veto_blocked':false,'vetoes':['w:false'],'chosen_vetoes':[{'term_id':'w','fired':false,"
            + "'i_a':'0.000000','peak_index':null,'peak_mz':null}]}}}";

        var vstTerms = Terms(Ion("a", "required", AIon), Ion("b", "required", BIon),
                             TermThan("v", "product_ion", "veto_stronger_than", Formula(VIon), new[] { "a" }, "0.5"));
        InlineRuleSet StrongerSet() =>
            new InlineRuleSet().AddRule(Stronger, vstTerms, AllNode(TermRef("a"), TermRef("b"), NotNode(TermRef("v"))), CoreHex, "[\"[M-H]-\"]", "a")
                               .Claim(Stronger, "a", "b");
        var c10 = Add("veto_stronger_than_above", "I_raw(v) 0.3 > 0.5 * I_raw(a) 0.4: vetoed.", StrongerSet());
        c10.Peaks = Peaks(Base, NegIon(AIon, 400), NegIon(BIon, 500), NegIon(VIon, 300));
        c10.Expect = "{'outcomes':{'" + Stronger + "':{'fired':false,'veto_blocked':true,'chosen_failure':'veto',"
            + "'chosen_vetoes':[{'term_id':'v','fired':true,'i_a':'0.300000','i_b_max':'0.400000','peak_index':3}]}}}";

        var c11 = Add("veto_stronger_than_equal", "I_raw(v) 0.2 equals 0.5 * 0.4 exactly (halving is exact): not stronger, so no veto.", StrongerSet());
        c11.Peaks = Peaks(Base, NegIon(AIon, 400), NegIon(BIon, 500), NegIon(VIon, 200));
        c11.Expect = "{'outcomes':{'" + Stronger + "':{'fired':true,'veto_blocked':false,'vetoes':['v:false'],'matched':['a:C7H3O4@1','b:C8H5O4@2']}}}";

        // ---- counted, k_of and variable k
        var c12 = Add("counted_k_minus_one", "k_of(2, a, b, c) with one unit: count 1 < 2.", CountedSet(Counted, "2"));
        c12.Peaks = Peaks(Base, NegIon(AIon, 500));
        c12.Expect = "{'outcomes':{'" + Counted + "':{'fired':false,'chosen':null}}}";
        var c12v = Add("counted_k_minus_one_verbose", "As counted_k_minus_one, verbose: the k_of count of the failing hypothesis is reported.", CountedSet(Counted, "2"));
        c12v.Options = "{'filter':'all','verbose':true}";
        c12v.Peaks = Peaks(Base, NegIon(AIon, 500));
        c12v.Expect = "{'outcomes':{'" + Counted + "':{'fired':false,'chosen':null,'all_k_of':{'core|Hex1|[M-H]-':['/logic 2 1 false']},"
            + "'hypotheses':{'core|Hex1|[M-H]-':'logic_false'}}}}";

        var c13 = Add("counted_k", "k_of(2, a, b, c) with a and c: count 2.", CountedSet(Counted, "2"));
        c13.Peaks = Peaks(Base, NegIon(AIon, 500), NegIon(CIon, 300));
        c13.Expect = "{'outcomes':{'" + Counted + "':{'fired':true,'k_of':['/logic 2 2 true'],'matched':['a:C7H3O4@1','c:C9H7O4@2'],'chosen_score':'0.800000'}}}";

        var c14 = Add("counted_k_plus_one", "k_of(2, a, b, c) with all three: count 3.", CountedSet(Counted, "2"));
        c14.Peaks = Peaks(Base, NegIon(AIon, 500), NegIon(BIon, 400), NegIon(CIon, 300));
        c14.Expect = "{'outcomes':{'" + Counted + "':{'fired':true,'k_of':['/logic 2 3 true'],'chosen_score':'1.200000'}}}";

        var c15 = Add("k_variable", "k = max(1, n_sugars + 1) = 2 for one Hex; a and b reach it.", CountedSet(KVariable, KVar("n_sugars", 1, 1)));
        c15.Peaks = Peaks(Base, NegIon(AIon, 500), NegIon(BIon, 400));
        c15.Expect = "{'outcomes':{'" + KVariable + "':{'fired':true,'k_of':['/logic 2 2 true']}}}";

        // ---- ordering and upgrade
        InlineRuleSet OrderingSet() =>
            new InlineRuleSet().AddRule(Ordering, Terms(TermThan("o", "product_ion", "ordering", Formula(AIon), new[] { "b" }, null), Ion("b", "required", BIon)),
                                        AllNode(TermRef("o"), TermRef("b")), CoreHex, "[\"[M-H]-\"]", "o").Claim(Ordering, "o", "b");
        var c16 = Add("ordering_above", "ordering o > b (ratio 1.0): 0.5 > 0.4 passes.", OrderingSet());
        c16.Peaks = Peaks(Base, NegIon(AIon, 500), NegIon(BIon, 400));
        c16.Expect = "{'outcomes':{'" + Ordering + "':{'fired':true,'matched':['o:C7H3O4@1','b:C8H5O4@2']}}}";

        var c17 = Add("ordering_equal", "ordering o > b at equality (0.4 = 0.4) fails; o is still assigned.", OrderingSet());
        c17.Options = "{'filter':'all','verbose':true}";
        c17.Peaks = Peaks(Base, NegIon(AIon, 400), NegIon(BIon, 400));
        c17.Expect = "{'outcomes':{'" + Ordering + "':{'fired':false,'chosen':null,'hypotheses':{'core|Hex1|[M-H]-':'logic_false'},"
            + "'all_matched':{'core|Hex1|[M-H]-':['o:C7H3O4@1','b:C8H5O4@2']}}}}";

        var c18 = Add("upgrade_reported", "An upgrade term is reported on the chosen hypothesis and adds nothing to the score.",
                      new InlineRuleSet().AddRule(Upgrade, Terms(Ion("a", "required", AIon), Ion("b", "required", BIon), Ion("u", "upgrade", UIon)),
                                                  AllNode(TermRef("a"), TermRef("b")), CoreHex, "[\"[M-H]-\"]", "a").Claim(Upgrade, "a", "b"));
        c18.Peaks = Peaks(Base, NegIon(AIon, 500), NegIon(BIon, 400), NegIon(UIon, 700));
        c18.Expect = "{'outcomes':{'" + Upgrade + "':{'fired':true,'matched':['a:C7H3O4@1','b:C8H5O4@2'],'upgrades':['u:C10H9O4@3'],"
            + "'chosen_score':'0.900000'}}}";

        // ---- one peak, one term; refusal; runtime overlap; assignment cap
        var c19 = Add("complementary_declared", "Declared loss_hex/caffeate pair: one peak satisfies both and counts once in k_of and the score.", CaffeoylSet(true));
        c19.Precursor = Text(CaffeoylNeg);
        c19.Peaks = CaffeoylPeaks;
        c19.Expect = "{'refusals':[],'outcomes':{'" + Caffeoyl + "':{'fired':true,'k_of':['/logic 2 2 true'],"
            + "'matched':['loss_hex:C6H10O5@1','caffeate:C9H7O4@1','decarb:C8H7O2@2'],'chosen_score':'0.900000'}}}";

        var c20 = Add("overlap_undeclared", "Without the declaration the coinciding loss and ion are refused (overlap_undeclared) and not evaluated.",
                      CaffeoylSet(false));
        c20.Precursor = Text(CaffeoylNeg);
        c20.Peaks = CaffeoylPeaks;
        c20.Expect = "{'status':'evaluated','rules_evaluated':0,'outcome_ids':[],'fired_motifs':[],"
            + "'refusals':['" + Caffeoyl + ":overlap_undeclared:core|Hex1|[M-H]-:loss_hex:C6H10O5,caffeate:C9H7O4']}";

        var c21 = Add("overlap_runtime", "OverlapMode.Runtime: the shared peak goes to the first term (loss_hex); caffeate is unassigned.", CaffeoylSet(false));
        c21.Options = "{'filter':'all','overlap':'runtime'}";
        c21.Precursor = Text(CaffeoylNeg);
        c21.Peaks = CaffeoylPeaks;
        c21.Expect = "{'refusals':[],'rules_evaluated':1,'outcomes':{'" + Caffeoyl + "':{'fired':true,'matched':['loss_hex:C6H10O5@1','decarb:C8H7O2@2'],"
            + "'k_of':['/logic 2 2 true'],'claim_ineligible':['not_reviewed','variant_active']}}}";

        var c22 = Add("assignment_overflow", "Three leaves above AssignmentCap 2: assignment_overflow, nothing fires.", CaffeoylSet(false));
        c22.Options = "{'filter':'all','overlap':'runtime','assignment_cap':2,'verbose':true}";
        c22.Precursor = Text(CaffeoylNeg);
        c22.Peaks = CaffeoylPeaks;
        c22.Expect = "{'outcomes':{'" + Caffeoyl + "':{'fired':false,'veto_blocked':false,'chosen':null,'hypotheses':{'core|Hex1|[M-H]-':'assignment_overflow'},"
            + "'all_matched':{'core|Hex1|[M-H]-':[]}}}}";

        // ---- conventions and the p2 base peak
        var c23 = Add("convention_p2_and_msdial", "A precursor peak of 10000: p2 rel uses the base outside 1 Da (X 0.5, fires); msdial uses maxI "
                      + "(X 5 %, below its 10 % floor).", ConventionSet());
        c23.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 500), Peak(GlycosideNeg, 10000));
        c23.Expect = "{'base_intensity':'1000.000000','fired_motifs':['flavonoid_o_glycoside'],'outcome_ids':['" + Msdial + "','" + Neg + "'],"
            + "'outcomes':{'" + Neg + "':{'fired':true,'convention':'p2'},'" + Msdial + "':{'fired':false,'convention':'msdial','convention_label':'approximate'}}}";

        var c24 = Add("no_base_peak", "Every peak lies within 1 Da of the precursor: p2 has no base (no_base_peak); msdial evaluates and finds nothing.",
                      ConventionSet());
        c24.Options = "{'filter':'all','verbose':true}";
        c24.Peaks = Peaks(Peak(GlycosideNeg, 1000), Peak(GlycosideNeg - 500000, 200));
        c24.Expect = "{'status':'evaluated','base_intensity':null,'outcomes':{'" + Neg + "':{'fired':false,'hypotheses':{'core|Hex1|[M-H]-':'no_base_peak'}},"
            + "'" + Msdial + "':{'fired':false,'hypotheses':{'core|Hex1|[M-H]-':'logic_false'}}}}";

        // ---- product tolerances
        var c25 = Add("p2_tolerance_da_in", "Below m/z 500 p2 is 0.005 Da: X 0.004990 Da high hits.", Basic());
        c25.Peaks = Firing(4990);
        c25.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'matched':['Y0:A@1','X:C7H3O4@2']}}}";

        var c26 = Add("p2_tolerance_da_out", "X 0.005010 Da high misses the 0.005 Da tolerance.", Basic());
        c26.Peaks = Firing(5010);
        c26.Expect = "{'outcomes':{'" + Neg + "':{'fired':false,'chosen':null}}}";

        var c27 = Add("p2_tolerance_ppm_in", "Above m/z 500 p2 is 10 ppm: Y2 (593.151194) 0.005500 Da high hits within 0.005932 Da.", PpmSet());
        c27.Precursor = Text(Deprotonated("C33H40O20"));
        c27.Peaks = PpmPeaks(5500);
        c27.Expect = "{'outcomes':{'" + Ppm + "':{'fired':true,'chosen':'core|Hex3|[M-H]-','matched':['Y0:A@1','Y2:C27H29O15@2']}}}";

        var c28 = Add("p2_tolerance_ppm_out", "Y2 0.006400 Da high misses 10 ppm (0.005932 Da).", PpmSet());
        c28.Precursor = Text(Deprotonated("C33H40O20"));
        c28.Peaks = PpmPeaks(6400);
        c28.Expect = "{'outcomes':{'" + Ppm + "':{'fired':false,'chosen':null}}}";

        var c29 = Add("msfinder_tolerance_default", "An msfinder rule uses MsfinderProductTolerance (0.01 Da by default): X 0.008 Da high hits.",
                      Basic().Msfinder(Neg));
        c29.Peaks = Firing(8000);
        c29.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'product_tolerance':{'abs_da':'0.010000','ppm':'0.000','source':'msfinder'}}}}";

        var c30 = Add("msfinder_tolerance_option", "MsfinderProductTolerance 0.005 Da: X 0.008 Da high misses; the option is not a variant.",
                      Basic().Msfinder(Neg));
        c30.Options = "{'filter':'all','msfinder_product_tolerance':{'abs_da':'0.005','ppm':'0.0'}}";
        c30.Peaks = Firing(8000);
        c30.Expect = "{'outcomes':{'" + Neg + "':{'fired':false,'claim_ineligible':['not_reviewed'],"
            + "'product_tolerance':{'abs_da':'0.005000','ppm':'0.000','source':'msfinder'}}}}";

        // ---- loss reference
        var c31 = Add("loss_reference_hypothesis", "P_obs 0.006 Da above the hypothesis: the loss peak at the hypothesis-referenced target hits.", LossSet());
        c31.Precursor = Text(GlycosideNeg + 6000);
        c31.Peaks = Peaks(Base, Peak(Micro(TestMass.Of("C21H20O10") - TestMass.Of("H") + TestMass.Electron - TestMass.Of("C6H10O5")), 700), NegIon(XIon, 500));
        c31.Expect = "{'outcomes':{'" + Loss + "':{'fired':true,'loss_reference':'hypothesis','matched':['loss_hex:C6H10O5@1','X:C7H3O4@2']}}}";

        var c32 = Add("loss_reference_override_observed", "LossReferenceOverride observed: the target moves with P_obs, 0.006 Da from the peak; no hit.", LossSet());
        c32.Options = "{'filter':'all','loss_reference_override':'observed'}";
        c32.Precursor = Text(GlycosideNeg + 6000);
        c32.Peaks = c31.Peaks;
        c32.Expect = "{'outcomes':{'" + Loss + "':{'fired':false,'loss_reference':'observed','claim_ineligible':['not_reviewed','variant_active']}}}";

        // ---- precursor tolerance
        var c33 = Add("precursor_out_of_tolerance", "P_obs 0.011 Da above the hypothesis: no hypothesis, and the compact result drops the outcome.", Basic());
        c33.Precursor = Text(GlycosideNeg + 11000);
        c33.Peaks = Firing();
        c33.Expect = "{'status':'evaluated','rules_evaluated':1,'outcome_ids':[],'fired_motifs':[]}";

        var c34 = Add("precursor_out_of_tolerance_verbose", "As precursor_out_of_tolerance, verbose: n_hypotheses 0 with no precondition failure.", Basic());
        c34.Options = "{'filter':'all','verbose':true}";
        c34.Precursor = Text(GlycosideNeg + 11000);
        c34.Peaks = Firing();
        c34.Expect = "{'outcome_ids':['" + Neg + "'],'outcomes':{'" + Neg + "':{'n_hypotheses':0,'precondition_failure':null,'fired':false,'chosen':null}}}";

        var c35 = Add("precursor_tolerance_option", "PrecursorToleranceDa 0.02 admits P_obs 0.011 Da above the hypothesis.", Basic());
        c35.Options = "{'filter':'all','precursor_tolerance_da':'0.02'}";
        c35.Precursor = Text(GlycosideNeg + 11000);
        c35.Peaks = Firing();
        c35.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'n_hypotheses':1,'precursor_tolerance_da':'0.020000',"
            + "'claim_ineligible':['not_reviewed','variant_active']}}}";

        // ---- the other variant options
        var c36 = Add("product_tolerance_override", "ProductToleranceOverride 0.02 Da: X 0.015 Da high hits; the source is override.", Basic());
        c36.Options = "{'filter':'all','product_tolerance_override':{'abs_da':'0.02','ppm':'0.0'}}";
        c36.Peaks = Firing(15000);
        c36.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'product_tolerance':{'abs_da':'0.020000','ppm':'0.000','source':'override'},"
            + "'claim_ineligible':['not_reviewed','variant_active']}}}";

        var c37 = Add("decoy_true_peaks", "DecoyShiftDa +5 moves every product target 5 Da up: the true peaks no longer hit.", Basic());
        c37.Options = "{'filter':'all','decoy_shift_da':5}";
        c37.Peaks = Firing();
        c37.Expect = "{'outcomes':{'" + Neg + "':{'fired':false,'n_hypotheses':1,'chosen':null}}}";

        var c38 = Add("decoy_shifted_peaks", "DecoyShiftDa +5 with peaks 5 Da above the true ions: they hit the shifted targets.", Basic());
        c38.Options = "{'filter':'all','decoy_shift_da':5}";
        c38.Peaks = Peaks(Base, NegIon(Y0Ion, 800, 5 * MicroPerDa), NegIon(XIon, 500, 5 * MicroPerDa));
        c38.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'matched':['Y0:A@1','X:C7H3O4@2'],'claim_ineligible':['not_reviewed','variant_active']}}}";

        var c39 = Add("floor_scale", "FloorScale 2: the 0.05 floor becomes 0.1, so X at rel 0.08 is not present.", Basic());
        c39.Options = "{'filter':'all','floor_scale':'2.0'}";
        c39.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(XIon, 80));
        c39.Expect = "{'outcomes':{'" + Neg + "':{'fired':false,'chosen':null,'claim_ineligible':['not_reviewed','variant_active']}}}";

        var c40 = Add("k_offset_variant_single_ion", "KOffset -1 under the default filter: the measured k_of(2) rule needs one unit, fails a "
                      + "two-ion pass (variant_single_ion) and is still evaluated; the unmeasured rule is not_measured.", KOffsetSet());
        c40.Options = "{'k_offset':-1}";
        c40.Peaks = Peaks(Base, NegIon(CIon, 500));
        c40.Expect = "{'rules_evaluated':1,'not_measured':['" + Neg + "'],'outcome_ids':['" + KOffsetRule + "'],"
            + "'outcomes':{'" + KOffsetRule + "':{'fired':true,'variant_single_ion':true,'k_of':['/logic 1 1 true'],'matched':['c2:C9H7O4@1'],"
            + "'claim_ineligible':['not_reviewed','variant_active']}}}";

        // ---- filters and claim eligibility
        var c41 = Add("filter_author_reviewed_eligible", "Filter AuthorReviewed: the reviewed rule is evaluated and claim-eligible; the other is not_measured.",
                      Basic().Reviewed(Neg).AddBasic(Plain));
        c41.Options = "{'filter':'author_reviewed'}";
        c41.Peaks = Firing();
        c41.Expect = "{'rules_evaluated':1,'not_measured':['" + Plain + "'],'eligible_rule_conflict':false,"
            + "'outcomes':{'" + Neg + "':{'fired':true,'author_reviewed':true,'claim_eligible':true,'claim_ineligible':[]}}}";

        var c42 = Add("claim_instrument_class", "A Q Exactive spectrum is Orbitrap/FT, which the review does not cover.", Basic().Reviewed(Neg));
        c42.Options = "{}";
        c42.Instrument = "Q Exactive";
        c42.Peaks = Firing();
        c42.Expect = "{'instrument_class':'Orbitrap/FT','outcomes':{'" + Neg + "':{'fired':true,'claim_eligible':false,"
            + "'claim_ineligible':['instrument_class'],'within_calibration':false}}}";

        var c43 = Add("claim_tolerance_differs", "An msfinder rule reviewed at 0.01 Da evaluated with MsfinderProductTolerance 0.05 Da.",
                      Basic().Msfinder(Neg).Reviewed(Neg, productTolerance: MsfinderReview));
        c43.Options = "{'msfinder_product_tolerance':{'abs_da':'0.05','ppm':'0.0'}}";
        c43.Peaks = Firing();
        c43.Expect = "{'outcomes':{'" + Neg + "':{'fired':true,'claim_ineligible':['tolerance_differs']}}}";

        // ---- mz_only and the tiers
        var c44 = Add("mz_only_cross_polarity", "Evidence [M-H]- cross_polarity: mz_only, which is not the rule's primary mode.", Basic());
        c44.EvidenceAdduct = "[M-H]-";
        c44.EvidenceTier = "cross_polarity";
        c44.Peaks = Firing();
        c44.Expect = "{'precondition_mode':'mz_only','adduct_tier':'cross_polarity','outcomes':{'" + Neg + "':{'fired':true,"
            + "'chosen_adduct_tier':'cross_polarity','claim_ineligible':['not_reviewed','precondition_mode']}}}";

        var c45 = Add("mz_only_within_polarity_pair", "A rule reviewed for mz_only is claim-eligible with within_polarity_pair evidence.",
                      Basic().SetRule(Neg, "primary_precondition_mode", "\"mz_only\"").Reviewed(Neg));
        c45.Options = "{}";
        c45.EvidenceAdduct = "[M-H]-";
        c45.EvidenceTier = "within_polarity_pair";
        c45.Peaks = Firing();
        c45.Expect = "{'precondition_mode':'mz_only','adduct_tier':'within_polarity_pair','outcomes':{'" + Neg + "':{'fired':true,"
            + "'claim_eligible':true,'claim_ineligible':[],'chosen_adduct_tier':'within_polarity_pair'}}}";

        var c46 = Add("mz_only_curated", "Evidence [M-H]- curated (benchmark only).", Basic());
        c46.EvidenceAdduct = "[M-H]-";
        c46.EvidenceTier = "curated";
        c46.Peaks = Firing();
        c46.Expect = "{'precondition_mode':'mz_only','adduct_tier':'curated','outcomes':{'" + Neg + "':{'fired':true,'chosen_adduct_tier':'curated'}}}";

        var c47 = Add("adduct_not_in_rule", "Evidence [M+Cl]-, which the rule does not list: n_hypotheses 0, adduct_not_in_rule, no fallback.", Basic());
        c47.Options = "{'filter':'all','verbose':true}";
        c47.EvidenceAdduct = "[M+Cl]-";
        c47.EvidenceTier = "curated";
        c47.Peaks = Firing();
        c47.Expect = "{'precondition_mode':'mz_only','outcomes':{'" + Neg + "':{'n_hypotheses':0,'precondition_failure':'adduct_not_in_rule','fired':false}}}";

        var c48 = Add("ion_mode_mismatch", "A negative spectrum: the positive rule is not tried (ion_mode); the negative rule fires.", Basic().AddPos());
        c48.Options = "{'filter':'all','verbose':true}";
        c48.Peaks = Firing();
        c48.Expect = "{'rules_evaluated':2,'outcomes':{'" + Neg + "':{'fired':true,'precondition_failure':null},"
            + "'" + Pos + "':{'n_hypotheses':0,'precondition_failure':'ion_mode','fired':false}}}";

        // ---- statuses
        var c49 = Add("status_no_precursor", "A missing precursor (NaN): no_precursor; nothing is evaluated.", Basic());
        c49.Precursor = null;
        c49.Peaks = Firing();
        c49.Expect = "{'status':'no_precursor','precursor_mz':null,'base_intensity':null,'rules_evaluated':1,'outcome_ids':[],'n_peaks_in':3}";

        var c50 = Add("status_zero_precursor", "Precursor m/z 0 (the MSDIAL4 parser's missing value): no_precursor.", Basic());
        c50.Precursor = "0.0";
        c50.Peaks = Firing();
        c50.Expect = "{'status':'no_precursor','precursor_mz':'0.000000','outcome_ids':[]}";

        var c51 = Add("status_unsupported_ion_mode", "No ion mode: unsupported_ion_mode.", Basic());
        c51.IonMode = null;
        c51.Peaks = Firing();
        c51.Expect = "{'status':'unsupported_ion_mode','outcome_ids':[],'base_intensity':null}";

        var c52 = Add("status_no_peaks", "Zero and negative intensities and m/z 0 are dropped and counted: no_peaks.", Basic());
        c52.Peaks = Peaks(Peak(300 * MicroPerDa, 0), Peak(IonMicro(XIon, -1), -5), Peak(0, 100));
        c52.Expect = "{'status':'no_peaks','n_peaks_in':3,'n_peaks_dropped':3,'outcome_ids':[]}";

        var c53 = Add("status_precedence", "No precursor, no ion mode and no peaks: no_precursor comes first.", Basic());
        c53.Precursor = null;
        c53.IonMode = null;
        c53.Peaks = Peaks();
        c53.Expect = "{'status':'no_precursor','n_peaks_in':0,'n_peaks_dropped':0}";

        var c54 = Add("dropped_peaks_evaluated", "A dropped peak is counted; the rest are evaluated and peak_index keeps the input index.", Basic());
        c54.Peaks = Peaks(Base, Peak(200 * MicroPerDa, 0), NegIon(Y0Ion, 800), NegIon(XIon, 500));
        c54.Expect = "{'status':'evaluated','n_peaks_in':4,'n_peaks_dropped':1,'outcomes':{'" + Neg + "':{'fired':true,'matched':['Y0:A@2','X:C7H3O4@3']}}}";

        // ---- selection, ties, motifs
        var c55 = Add("scaffold_tie", "Genistein and apigenin share C15H10O5: both hexosides fire with the same score; the lower id is chosen.",
                      new InlineRuleSet().AddRule(Scaffold, Terms(EvaluatorHarness.Y0, Term("rda", "product_ion", "required", "\"scaffold_ions\":\"rda\"")),
                                                  AllOf(new[] { "Y0", "rda" }), InlineRuleSet.Composition(null, "aglycones", "{\"Hex\":[1,1]}", "{}"),
                                                  "[\"[M-H]-\"]", "Y0").Claim(Scaffold, "Y0", "rda"));
        c55.Peaks = Firing();
        c55.Expect = "{'outcomes':{'" + Scaffold + "':{'fired':true,'n_hypotheses':2,'n_hypotheses_fired':2,'n_distinct_formulas':1,"
            + "'chosen':'apigenin|Hex1|[M-H]-','chosen_scaffold_id':'apigenin','tied_hypothesis_ids':['apigenin|Hex1|[M-H]-','genistein|Hex1|[M-H]-'],"
            + "'matched':['Y0:A@1','rda:C7H3O4@2']}}}";

        var c56 = Add("motifs_nested", "Glycoside and flavonoid rules fire; the glycoside motif is nested under flavonoid, so no conflict.", MotifSet(false));
        c56.Peaks = Firing();
        c56.Expect = "{'fired_motifs':['flavonoid','flavonoid_o_glycoside'],'narrowest_motifs':['flavonoid_o_glycoside'],'rule_conflict':false,"
            + "'eligible_rule_conflict':false,'outcome_ids':['" + Flavonoid + "','" + Glycoside + "']}";

        var c57 = Add("motifs_conflict", "An anthocyanin rule fires too: anthocyanin is not nested with the others, so rule_conflict.", MotifSet(true));
        c57.Peaks = Firing();
        c57.Expect = "{'fired_motifs':['anthocyanin','flavonoid','flavonoid_o_glycoside'],'narrowest_motifs':['anthocyanin','flavonoid_o_glycoside'],"
            + "'rule_conflict':true,'eligible_rule_conflict':false}";

        var c58 = Add("any_branches", "all(Y0, any(a, b)) with b only: the any node reports [false, true] and branch_fired agrees.",
                      new InlineRuleSet().AddRule(Branch, Terms(EvaluatorHarness.Y0, Ion("a", "required", AIon), Ion("b", "required", BIon)),
                                                  AllNode(TermRef("Y0"), AnyNode(TermRef("a"), TermRef("b"))), CoreHex, "[\"[M-H]-\"]", "Y0")
                                         .Claim(Branch, "Y0", "a", "b"));
        c58.Peaks = Peaks(Base, NegIon(Y0Ion, 800), NegIon(BIon, 400));
        c58.Expect = "{'outcomes':{'" + Branch + "':{'fired':true,'matched':['Y0:A@1','b:C8H5O4@2'],"
            + "'chosen_branches':[{'pointer':'/logic/all/1','children':[false,true]}],'branch_fired':{'/logic/all/1':[false,true]}}}}";

        return cases;
    }
}
