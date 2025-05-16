namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass()]
public class ContextTests
{
    private Context _ctx = default!;

    [TestInitialize]
    public void Initialize() {
        _ctx = new Context([("H", "1"), ("C", "12"), ("O", "16")]);
    }

    [DataTestMethod()]
    [DataRow("M", "M")]
    [DataRow("H", "H")]
    [DataRow("C * 12 + H * 24 + O * 12", "C12H24O12")]
    [DataRow("22.98976928", "22.98976928")]
    [DataRow("C + H * 2 + O", "<C>1</C><H>2</H><O>1</O>")]
    public void ResolveTest(string expected, string raw) {
        var term = new Term() { Raw = raw, };
        var actual = _ctx.Resolve(term);
        Assert.AreEqual(expected, actual);
    }

    [DataTestMethod()]
    [DataRow("(M)", "M")]
    [DataRow("(M) * 2", "2M")]
    [DataRow("(H * 2 + O)", "H2O")]
    [DataRow("(H * 2 + O) * 2", "2H2O")]
    [DataRow("(M) + (H)", "M+H")]
    [DataRow("(M) + (H) * 2", "M+2H")]
    [DataRow("(M) * 2 + (H)", "2M+H")]
    public void Resolve_Sentence(string expected, string raw) {
        var parser = new FormulaSentenceParser();
        var sentence = parser.Parse(raw)!;
        var actual = _ctx.Resolve(sentence);
        Assert.AreEqual(expected, actual);
    }
}