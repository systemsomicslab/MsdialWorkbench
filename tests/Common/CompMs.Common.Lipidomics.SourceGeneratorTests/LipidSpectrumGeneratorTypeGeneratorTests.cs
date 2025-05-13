using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class LipidSpectrumGeneratorTypeGeneratorTests
{
    [TestMethod]
    [DeploymentItem("LipidSpectrumGeneratorTest.msp")]
    public void GenreratedGeneratorTest() {
        var lib = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest.msp");

        var generator = new PCCidLipidSpectrumGenerator();

        foreach (var reference in lib) {
            var lipid = FacadeLipidParser.Default.Parse(reference.Name);
            var spectrum = generator.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();

            Assert.IsNotNull(spectrum);
            Assert.AreEqual(reference.Spectrum.Count, spectrum.Count);
            for (int i = 0; i < spectrum.Count; i++) {
                Assert.AreEqual(reference.Spectrum[i].Mass, spectrum[i].Mass, 1e-4);
                Assert.AreEqual(reference.Spectrum[i].Intensity, spectrum[i].Intensity);
            }
        }
    }
}
