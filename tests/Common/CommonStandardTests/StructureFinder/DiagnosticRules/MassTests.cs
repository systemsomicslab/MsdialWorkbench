using CompMs.Common.StructureFinder.DiagnosticRules.Chemistry;
using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using CompMs.Common.StructureFinder.DiagnosticRules.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCDK.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static CompMs.Common.StructureFinder.DiagnosticRules.Tests.DiagnosticRuleFixtures;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Masses (design 4.1, 7.1): exact integer pico-daltons against the CPython vectors, the element table against
/// the RDKit 2026.09.2 literals, and every formula within 1e-5 Da of an independent oracle (NCDK's BODR major
/// isotopes).
/// </summary>
[TestClass]
[DeploymentItem(@"Resources\DiagnosticRuleFixtures", @"Resources\DiagnosticRuleFixtures")]
public class MassTests
{
    private static JsonObject MassVectors() => (JsonObject)Vectors()["masses"];

    private static ElementTable VectorElements() {
        var errors = new List<RuleLoadError>();
        var el = SchemaReader.LoadElements(new ErrorSink(errors, "tables/elements.json"), (JsonObject)MassVectors()["elements"]);
        Assert.AreEqual(0, errors.Count, ErrorList(errors));
        return el!;
    }

    private static double NcdkMass(long[] counts, ElementTable el) {
        var mass = 0.0;
        for (var i = 0; i < counts.Length; i++) {
            if (counts[i] != 0) {
                mass += counts[i] * BODRIsotopeFactory.Instance.GetMajorIsotope(el.Symbols[i]).ExactMass!.Value;
            }
        }
        return mass;
    }

    [TestMethod]
    public void TheElementTableIsTheRdkitLiterals() {
        // RDKit 2026.09.2 GetMostCommonIsotopeMass, read under D5 (design 3.4); the electron is the P3 contract.
        var expected = "{\"electron\":0.000549,\"elements\":{\"C\":12.0,\"Cl\":34.96885268,\"H\":1.007825032,\"K\":38.96370668,"
                     + "\"N\":14.003074,\"Na\":22.98976928,\"O\":15.99491462,\"P\":30.97376163,\"S\":31.972071},"
                     + "\"format\":\"msfinder-diagnostic-elements\",\"format_version\":1}";
        var set = DiagnosticRuleSet.LoadEmbedded();
        var embedded = DiagnosticRuleSetLoader.ReadEmbedded()["tables/elements.json"];
        Assert.AreEqual(expected, CanonicalJson.ToText(StrictJsonReader.Read(embedded)));
        Assert.AreEqual(expected, CanonicalJson.ToText(MassVectors()["elements"]));
        Assert.AreEqual(549000000, (long)set.Elements.ElectronPdaExact);
    }

    [TestMethod]
    public void ElementAndLexemePicoDaltonsAreExact() {
        var el = VectorElements();
        Assert.AreEqual(Int(MassVectors(), "electron_pda"), (long)el.ElectronPdaExact);
        foreach (var m in ((JsonObject)MassVectors()["element_pda"]).Members) {
            Assert.IsTrue(el.TryGetIndex(m.Key, out var i), m.Key);
            Assert.AreEqual(((JsonInteger)m.Value).Value, (long)el.PdaExact(i), m.Key);
        }
        foreach (JsonObject c in ((JsonArray)MassVectors()["lexeme_pda"]).Cast<JsonObject>()) {
            var lexeme = Str(c, "lexeme");
            string got;
            try {
                var f = (JsonFloat)StrictJsonReader.Read(Encoding.ASCII.GetBytes("{\"a\":" + lexeme + "}"))["a"];
                got = "pda " + ((long)DecimalLexeme.ToPda(f)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (StrictJsonException e) {
                got = "code " + e.Code;
            }
            var expected = c.ContainsKey("pda") ? "pda " + Int(c, "pda").ToString(System.Globalization.CultureInfo.InvariantCulture) : "code " + Str(c, "code");
            Assert.AreEqual(expected, got, lexeme);
        }
    }

    [TestMethod]
    public void FormulaMassesAreExactAndWithin1e5OfTheOracle() {
        var el = VectorElements();
        foreach (JsonObject c in ((JsonArray)MassVectors()["formula_pda"]).Cast<JsonObject>()) {
            var formula = Str(c, "formula");
            var counts = Composition.Parse(formula, el);
            var pda = (long)el.MassPdaExact(counts);
            Assert.AreEqual(Int(c, "pda"), pda, formula);
            var mass = ElementTable.ToDa(pda);
            Assert.AreEqual(Str(c, "mass_ieee_hex"), IeeeBits.Of(mass), formula);
            Assert.AreEqual(Str(c, "mass_text"), ExactDecimal.Format(mass, 6), formula);
            Assert.AreEqual(NcdkMass(counts, el), mass, 1e-5, formula);
            Assert.AreEqual(formula, Composition.Format(counts, el), "Hill round trip");
        }
    }

    [TestMethod]
    public void RefusedFormulasGiveTheReferenceCodes() {
        var el = VectorElements();
        foreach (JsonObject c in ((JsonArray)MassVectors()["formula_errors"]).Cast<JsonObject>()) {
            var formula = Str(c, "formula");
            var e = Assert.ThrowsException<StrictJsonException>(() => Composition.Parse(formula, el), formula);
            Assert.AreEqual(Str(c, "code"), e.Code, formula);
        }
    }

    [TestMethod]
    public void IonMassesCarryTheElectronWithItsSign() {
        var el = VectorElements();
        foreach (JsonObject c in ((JsonArray)MassVectors()["ions"]).Cast<JsonObject>()) {
            var formula = Str(c, "formula");
            var charge = (int)Int(c, "charge");
            var counts = Composition.Parse(formula, el);
            var pda = (long)el.IonPdaExact(counts, charge);
            Assert.AreEqual(Int(c, "pda"), pda, formula);
            Assert.AreEqual(Str(c, "mz_ieee_hex"), IeeeBits.Of(ElementTable.ToDa(pda)), formula);
            Assert.AreEqual(Str(c, "mz_text"), ExactDecimal.Format(ElementTable.ToDa(pda), 6), formula);
            // A cation weighs one electron less than its neutral, an anion one more.
            Assert.AreEqual((long)el.MassPdaExact(counts) - charge * 549000000L, pda, formula);
            Assert.AreEqual(NcdkMass(counts, el) - charge * 0.000549, ElementTable.ToDa(pda), 1e-5, formula);
        }
    }

    [TestMethod]
    public void AdductPrecursorsAreExact() {
        var el = VectorElements();
        foreach (JsonObject c in ((JsonArray)MassVectors()["adduct_precursors"]).Cast<JsonObject>()) {
            var name = Str(c, "adduct");
            var row = "{\"adduct\":" + CanonicalJson.EscapeString(name) + ",\"aliases\":[],\"charge\":" + Int(c, "charge").ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"multiplier\":" + Int(c, "multiplier").ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"add\":" + CanonicalJson.ToText(c["add"])
                    + ",\"remove\":" + CanonicalJson.ToText(c["remove"])
                    + ",\"common\":true,\"origin\":\"as_published\",\"source\":[{\"reference\":\"synthetic\",\"locator\":\"line 1\"}]}";
            var errors = new List<RuleLoadError>();
            var adduct = SchemaReader.VAdductRow(new ErrorSink(errors, "tables/adducts.json"), StrictJsonReader.Read(Encoding.ASCII.GetBytes(row)), "", el);
            Assert.IsNotNull(adduct, name + " " + ErrorList(errors));
            var neutral = Composition.Parse(Str(c, "neutral"), el);
            var m = el.MassPdaExact(neutral);
            Assert.AreEqual(Int(c, "neutral_pda"), (long)m, name);
            var precursor = adduct!.Multiplier * m + el.MassPdaExact(adduct.Add) - el.MassPdaExact(adduct.Remove) - adduct.Charge * el.ElectronPdaExact;
            Assert.AreEqual(Int(c, "precursor_pda"), (long)precursor, name + " " + Str(c, "neutral"));
            Assert.AreEqual(Str(c, "mz_ieee_hex"), IeeeBits.Of(ElementTable.ToDa((long)precursor)), name);
            Assert.AreEqual(Str(c, "mz_text"), ExactDecimal.Format(ElementTable.ToDa((long)precursor), 6), name);
            var oracle = adduct.Multiplier * NcdkMass(neutral, el) + NcdkMass(adduct.Add, el) - NcdkMass(adduct.Remove, el) - adduct.Charge * 0.000549;
            Assert.AreEqual(oracle, ElementTable.ToDa((long)precursor), 1e-5, name);
        }
    }

    [TestMethod]
    public void TheFlavyliumCationIsItsCompositionLessOneElectron() {
        // Design 7.1: cyanidin 3-O-glucoside as composition C21H21O11 under [M]+ gives M - e = 449.107837.
        var set = DiagnosticRuleSet.LoadEmbedded();
        var m = set.Adducts.Single(a => a.Adduct == "[M]+");
        var counts = Composition.Parse("C21H21O11", set.Elements);
        var precursor = m.Multiplier * set.Elements.MassPdaExact(counts) + set.Elements.MassPdaExact(m.Add)
                      - set.Elements.MassPdaExact(m.Remove) - m.Charge * set.Elements.ElectronPdaExact;
        Assert.AreEqual("449.107837", ExactDecimal.Format(ElementTable.ToDa((long)precursor), 6));
    }

    [TestMethod]
    public void EveryEmbeddedTableFormulaIsWithin1e5OfTheOracle() {
        var set = DiagnosticRuleSet.LoadEmbedded();
        var el = set.Elements;
        for (var i = 0; i < el.Count; i++) {
            var oracle = BODRIsotopeFactory.Instance.GetMajorIsotope(el.Symbols[i]).ExactMass!.Value;
            Assert.AreEqual(oracle, el.Mass(i).Value, 1e-5, el.Symbols[i]);
        }
        foreach (var u in set.Units) {
            var counts = Composition.Parse(u.ResidueFormula, el);
            Assert.AreEqual(NcdkMass(counts, el), ElementTable.ToDa((long)el.MassPdaExact(counts)), 1e-5, u.Name);
        }
        foreach (var a in set.Adducts) {
            // [M+X]: the precursor of a 300 Da neutral (C15H10O5 here) under the adduct, against the oracle.
            var neutral = Composition.Parse("C15H10O5", el);
            var precursor = a.Multiplier * el.MassPdaExact(neutral) + el.MassPdaExact(a.Add) - el.MassPdaExact(a.Remove) - a.Charge * el.ElectronPdaExact;
            var oracle = a.Multiplier * NcdkMass(neutral, el) + NcdkMass(a.Add, el) - NcdkMass(a.Remove, el) - a.Charge * 0.000549;
            Assert.AreEqual(oracle, ElementTable.ToDa((long)precursor), 1e-5, a.Adduct);
        }
    }

    [TestMethod]
    public void HillOrderPutsCarbonAndHydrogenFirst() {
        var el = VectorElements();
        Assert.AreEqual("C6H10O5", Composition.Format(Composition.Parse("C6H10O5", el), el));
        Assert.AreEqual("ClH", Composition.Format(Composition.Parse("ClH", el), el));
        Assert.AreEqual("H4N", Composition.Format(Composition.Parse("H4N", el), el));
        Assert.AreEqual("CHO2", Composition.Format(Composition.Parse("CHO2", el), el));
        Assert.AreEqual("formula_not_hill", Assert.ThrowsException<StrictJsonException>(() => Composition.Parse("HCl", el)).Code);
        Assert.AreEqual("formula_not_hill", Assert.ThrowsException<StrictJsonException>(() => Composition.Parse("CH4C", el)).Code);
        Assert.AreEqual("bad_formula", Assert.ThrowsException<StrictJsonException>(() => Composition.Parse("C1H4", el)).Code);
    }

    [TestMethod]
    public void MassesAreOneDivisionOfExactPicoDaltons() {
        // pda / 1e12 is one correctly rounded IEEE division of exact operands (design 4.1).
        Assert.AreEqual(449.107837492, ElementTable.ToDa(449107837492000L));
        Assert.AreEqual(BitConverter.DoubleToInt64Bits(162.05282342), BitConverter.DoubleToInt64Bits(ElementTable.ToDa(162052823420000L)));
        Assert.AreEqual(new BigInteger(12000000000000L), DecimalLexeme.ToPda(JsonFloat.Parse("12.0")));
        Assert.AreEqual("mass_precision", Assert.ThrowsException<StrictJsonException>(() => DecimalLexeme.ToPda(JsonFloat.Parse("1.0000000000001"))).Code);
    }
}
