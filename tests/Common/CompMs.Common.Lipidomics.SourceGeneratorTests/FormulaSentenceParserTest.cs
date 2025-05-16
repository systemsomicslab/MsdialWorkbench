namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class FormulaSentenceParserTest
{
    [DataTestMethod]
    [DataRow(1, "C12H24O12")]
    [DataRow(1, "2H")]
    [DataRow(2, "M+H")]
    [DataRow(2, "M-2H")]
    [DataRow(3, "M+H-H2O")]
    [DataRow(2, "M+22.98976928")]
    [DataRow(1, "<C>1</C><H>3</H>")]
    public void Parse_CanParseTest(int expected, string sentence) {
        var parser = new FormulaSentenceParser();
        var actual = parser.Parse(sentence);
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected, actual.Terms.Length);
    }

    [DataTestMethod]
    [DataRow(new[] { 1, }, "C12H24O12")]
    [DataRow(new[] { 2, }, "2H")]
    [DataRow(new[] { 1, 1 }, "M+H")]
    [DataRow(new[] { 1, -2 }, "M-2H")]
    [DataRow(new[] { 1, 1, -1, }, "M+H-H2O")]
    [DataRow(new[] { 1, 1, }, "M+22.98976928")]
    public void Parse_FactorIsCorrectTest(int[] expected, string sentence) {
        var parser = new FormulaSentenceParser();
        var actual = parser.Parse(sentence);
        Assert.IsNotNull(actual);
        CollectionAssert.AreEqual(expected, actual.Terms.Select(t => t.Item2).ToList());
    }
}
