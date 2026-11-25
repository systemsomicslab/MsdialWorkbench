using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Strict-reader behaviour the vectors do not pin (design 3.1), each checked against CPython's json with the
/// reference hooks: float underflow, the document order of errors, and nesting far beyond 128 levels.
/// </summary>
[TestClass]
public class StrictJsonReaderTests
{
    private static string Outcome(byte[] input) {
        try {
            return "ok " + CanonicalJson.ToText(StrictJsonReader.Read(input));
        }
        catch (StrictJsonException e) {
            return "error " + e.Code;
        }
    }

    private static string Outcome(string ascii) => Outcome(Encoding.ASCII.GetBytes(ascii));

    private static string Zeros(int n) => new string('0', n);

    [TestMethod]
    public void AFloatThatUnderflowsIsZeroAsInPython() {
        // Python's float() rounds a value below 2^-1075 to 0.0, which the range test accepts; the reader agrees.
        Assert.AreEqual("ok {\"a\":0.0}", Outcome("{\"a\":0." + Zeros(323) + "1}"));
        Assert.AreEqual("ok {\"a\":0.0}", Outcome("{\"a\":0." + Zeros(323) + "247}"));
        Assert.AreEqual("error negative_zero", Outcome("{\"a\":-0." + Zeros(323) + "1}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0." + Zeros(323) + "2471}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0." + Zeros(323) + "3}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0.00001}"));
        // Very long lexemes, decided without an exact comparison over the whole lexeme (outcomes from the reference).
        Assert.AreEqual("ok {\"a\":0.0}", Outcome("{\"a\":0." + Zeros(1000000) + "1}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0." + Zeros(322) + "1}"));
        Assert.AreEqual("ok {\"a\":0.0}", Outcome("{\"a\":0." + Zeros(323) + "247" + Zeros(5000) + "}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0." + Zeros(323) + "2471" + Zeros(5000) + "}"));
        Assert.AreEqual("error number_range", Outcome("{\"a\":0.00001" + Zeros(100000) + "}"));
    }

    [TestMethod]
    public void ErrorsAreRaisedInDocumentOrderAsCPythonRaisesThem() {
        Assert.AreEqual("error negative_zero", Outcome("{\"a\":-0, }"), "a number is checked when its lexeme ends");
        Assert.AreEqual("error duplicate_key", Outcome("{\"x\":{\"a\":1,\"a\":2},\"y\":-0}"), "an inner object closes first");
        Assert.AreEqual("error number_form", Outcome("{\"a\":1,\"a\":1e5}"), "duplicates are found when the object closes");
        Assert.AreEqual("error duplicate_key", Outcome("{\"a\":\"\\u00e9\",\"a\":1}"), "printable ASCII is checked after the parse");
        Assert.AreEqual("error duplicate_key", Outcome("{\"a\":1,\"a\":2} x"), "before the extra data");
        Assert.AreEqual("error syntax", Outcome("{\"a\":1.5e}"), "an exponent without digits is backtracked");
        Assert.AreEqual("error syntax", Outcome("{\"a\":1."));
        Assert.AreEqual("error syntax", Outcome("{\"a\":-Inf}"));
    }

    [TestMethod]
    public void VeryDeepInputIsRefusedWithoutExhaustingTheStack() {
        var deep = "{\"a\":" + new string('[', 100000) + "1" + new string(']', 100000) + "}";
        Assert.AreEqual("error nesting_depth", Outcome(deep));
        var objects = string.Concat(Enumerable.Repeat("{\"a\":", 50000)) + "1" + new string('}', 50000);
        Assert.AreEqual("error nesting_depth", Outcome(objects));
    }

    [TestMethod]
    public void TheTopLevelMustBeAnObject() {
        Assert.AreEqual("error top_level_not_object", Outcome("[1]"));
        Assert.AreEqual("error top_level_not_object", Outcome("\"\\u00e9\""), "before printable ASCII");
        Assert.AreEqual("ok {}", Outcome(" \t\r\n{}\r\n"));
    }

    [TestMethod]
    public void FloatsKeepTheirLexemeAndIntegersStayIntegers() {
        var doc = StrictJsonReader.Read(Encoding.ASCII.GetBytes("{\"f\":5.00,\"i\":5,\"z\":0.000}"));
        Assert.AreEqual(JsonKind.Float, doc["f"].Kind);
        Assert.AreEqual(JsonKind.Integer, doc["i"].Kind);
        Assert.AreEqual("5.0", ((JsonFloat)doc["f"]).Canonical);
        Assert.AreEqual("0.0", ((JsonFloat)doc["z"]).Canonical);
        Assert.AreEqual("{\"f\":5.0,\"i\":5,\"z\":0.0}", CanonicalJson.ToText(doc));
        Assert.AreEqual("{\n \"f\": 5.0,\n \"i\": 5,\n \"z\": 0.0\n}\n", PrettyJson.ToText(doc));
    }
}
