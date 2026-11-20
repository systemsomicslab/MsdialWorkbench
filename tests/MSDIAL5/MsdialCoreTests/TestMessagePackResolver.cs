using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialCore.DataObj.Tests;

namespace MsdialCoreTests;

[GeneratedMessagePackResolver]
internal partial class TestMessagePackResolver {
}

[TestClass]
public class TestMessagePackResolverTests {
    [TestMethod]
    public void SourceGeneratedResolverDiscoversTestFormatters() {
        var generated = TestMessagePackResolver.Instance.GetFormatter<FakeChromatogramPeakFeature>();

        Assert.IsNotNull(generated);
        Assert.AreSame(generated, SourceGeneratedFormatterResolver.Instance.GetFormatter<FakeChromatogramPeakFeature>());
    }
}
