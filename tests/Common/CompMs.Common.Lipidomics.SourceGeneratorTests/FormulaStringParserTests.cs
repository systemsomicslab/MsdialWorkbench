namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class FormulaStringParserTests
{
    [DataTestMethod]
    [DataRow(true, "C12H24O12")]
    [DataRow(false, "<Carbon>12</Carbon>")]
    [DataRow(false, "\n<Carbon>12</Carbon>\n")]
    [DataRow(false, "\n<Carbon>12</Carbon>\n <Hydrogen>24</Hydrogen>\n")]
    public void CanConvertToFormulaDictionaryTest(bool expected, string formulaString) {
        var result = FormulaStringParser.CanConvertToFormulaDictionary(formulaString);
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DataRow(false, "C12H24O12")]
    [DataRow(true, "<Carbon>12</Carbon>")]
    [DataRow(true, "\n<Carbon>12</Carbon>\n")]
    [DataRow(true, " <Carbon>12</Carbon> <Hydrogen>24</Hydrogen> ")]
    [DataRow(false, " <Carbon>12</Carbon> <Hydrogen>24</Hydrogen> aaaa")]
    [DataRow(false, " <Carbon>12</Hydrogen> ")]
    [DataRow(false, " <Carbon>12</Carbon><Hydrogen>24</Carbon>")]
    public void IsMarkupFormulaTest(bool expected, string markupFormula) {
        var actual = FormulaStringParser.IsMarkupFormula(markupFormula, ["Carbon", "Hydrogen", "Oxygen"]);
        Assert.AreEqual(expected, actual);
    }
}
