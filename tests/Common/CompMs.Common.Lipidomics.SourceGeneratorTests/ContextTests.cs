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
    [DataRow("H * 1", "H")]
    [DataRow("C * 12 + H * 24 + O * 12", "C12H24O12")]
    [DataRow("22.98976928", "22.98976928")]
    public void ResolveTest(string expected, string raw) {
        var term = new Term() { Raw = raw, };
        var actual = _ctx.Resolve(term);
        Assert.AreEqual(expected, actual);
    }
}