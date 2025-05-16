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

    [DataTestMethod]
    [DataRow("<A>1</A><B>2</B><A>3</A>", "A:4,B:2")]
    [DataRow("<X>5</X><Y>10</Y><Z>15</Z>", "X:5,Y:10,Z:15")]
    [DataRow("<Item>7</Item>", "Item:7")]
    [DataRow("<A>0</A><A>0</A><A>0</A>", "A:0")]
    [DataRow("<Val>1</Val><Val>2</Val><Val>3</Val>", "Val:6")]
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
        string badValue = "<A>abc</A><B>3</B>";
        Assert.ThrowsException<FormatException>(() => {
            FormulaStringParser.ParseMarkupFormula(badValue);
        });
    }
}
