namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class FormulaStringParserTests
{
    [DataTestMethod]
    [DataRow(true, "C12H24O12")]
    [DataRow(false, "<mz><Carbon>12</Carbon></mz>")]
    [DataRow(false, "<mz>\n<Carbon>12</Carbon>\n</mz>")]
    [DataRow(false, "<mz>\n<Carbon>12</Carbon>\n <Hydrogen>24</Hydrogen>\n</mz>")]
    public void CanConvertToFormulaDictionaryTest(bool expected, string formulaString) {
        var result = FormulaStringParser.CanConvertToFormulaDictionary(formulaString);
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DataRow(false, "C12H24O12")]
    [DataRow(true, "<mz><Carbon>12</Carbon></mz>")]
    [DataRow(true, "<mz>\n<Carbon>12</Carbon>\n</mz>")]
    [DataRow(true, "<mz> <Carbon>12</Carbon> <Hydrogen>24</Hydrogen> </mz>")]
    [DataRow(false, " <mz> <Carbon>12</Carbon> <Hydrogen>24</Hydrogen> </mz>aaaa")]
    [DataRow(false, " <mz><Carbon>12</Hydrogen> </mz>")]
    [DataRow(false, " <mz><Carbon>12</Carbon><Hydrogen>24</Carbon></mz>")]
    public void IsMarkupFormulaTest(bool expected, string markupFormula) {
        var actual = FormulaStringParser.IsMarkupFormula(markupFormula, ["Carbon", "Hydrogen", "Oxygen"]);
        Assert.AreEqual(expected, actual);
    }

    [DataTestMethod]
    [DataRow("<mz><A>1</A><B>2</B><A>3</A></mz>", "A:4,B:2")]
    [DataRow("<mz><X>5</X><Y>10</Y><Z>15</Z></mz>", "X:5,Y:10,Z:15")]
    [DataRow("<mz><Item>7</Item></mz>", "Item:7")]
    [DataRow("<mz><A>0</A><A>0</A><A>0</A></mz>", "A:0")]
    [DataRow("<mz><Val>1</Val><Val>2</Val><Val>3</Val></mz>", "Val:6")]
    public void ParseMarkupFormula_ValidMarkup_ReturnsExpectedDictionary(string markupBody, string expectedResult) {
        var expected = new Dictionary<string, int>();
        if (!string.IsNullOrEmpty(expectedResult)) {
            foreach (var pair in expectedResult.Split(',')) {
                var parts = pair.Split(':');
                expected[parts[0]] = int.Parse(parts[1]);
            }
        }

        var resultDict = FormulaStringParser.ParseMarkupFormula(markupBody);
        CollectionAssert.AreEquivalent(expected, resultDict);
    }

    [TestMethod]
    public void ParseMarkupFormula_NoElements_ReturnsEmptyDictionary() {
        var actual = FormulaStringParser.ParseMarkupFormula(string.Empty);
        Assert.IsTrue(actual.Count == 0);
    }

    [TestMethod]
    public void ParseMarkupFormula_NonNumericValue_ThrowsException() {
        string badValue = "<mz><A>abc</A><B>3</B></mz>";
        Assert.ThrowsException<FormatException>(() => {
            FormulaStringParser.ParseMarkupFormula(badValue);
        });
    }
}
