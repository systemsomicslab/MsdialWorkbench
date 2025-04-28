namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class FormulaStringParserTests
{
    [DataTestMethod]
    [DataRow(true, "C12H24O12")]
    [DataRow(false, "<Carbon>12</Carbon>")]
    [DataRow(false, "\n<Carbon>12</Carbon>\n")]
    [DataRow(false, "\n<Carbon>12</Carbon>\n <Hydrogen>24</Carbon>\n")]
    public void CanConvertToFormulaDictionaryTest(bool expected, string formulaString) {
        var result = FormulaStringParser.CanConvertToFormulaDictionary(formulaString);
        Assert.AreEqual(expected, result);
    }
}
