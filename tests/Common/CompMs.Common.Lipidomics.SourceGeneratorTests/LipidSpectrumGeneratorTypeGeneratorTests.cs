using CompMs.Common.Components;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class LipidSpectrumGeneratorTypeGeneratorTests
{
    [DataTestMethod]
    [DynamicData(nameof(GetTestData), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTest(MoleculeMsReference reference, ILipid lipid) {
        var generator = new PCCidLipidSpectrumGenerator();

        var spectrum = generator.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
        Assert.IsNotNull(spectrum);
        Assert.AreEqual(reference.Spectrum.Count, spectrum.Count);
        for (int i = 0; i < spectrum.Count; i++) {
            Assert.AreEqual(reference.Spectrum[i].Mass, spectrum[i].Mass, 1e-4);
            Assert.AreEqual(reference.Spectrum[i].Intensity, spectrum[i].Intensity);
        }
    }

    [DeploymentItem("LipidSpectrumGeneratorTest.msp")]
    public static IEnumerable<object[]> GetTestData() {
        var lib = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest.msp");
        foreach (var reference in lib) {
            var lipid = FacadeLipidParser.Default.Parse(reference.Name);
            yield return new object[] { reference, lipid };
        }
    }
}
