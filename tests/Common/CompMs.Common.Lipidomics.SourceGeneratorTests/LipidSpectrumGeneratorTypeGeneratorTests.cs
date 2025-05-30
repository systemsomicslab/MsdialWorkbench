using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class LipidSpectrumGeneratorTypeGeneratorTests
{
    [DataTestMethod]
    [DynamicData(nameof(GetTestData), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTest(MoleculeMsReference reference, ILipid lipid)
    {
        var spectrum =new List<SpectrumPeak>();
        if(lipid is not null)
        {        
            switch (lipid.LipidClass)
            {
                case LbmClass.PC:
                    var generatorPc = new PCCidLipidSpectrumGenerator();
                    spectrum = generatorPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PE:
                    var generatorPe = new PECidLipidSpectrumGenerator();
                    spectrum = generatorPe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PG:
                    var generatorPg = new PGCidLipidSpectrumGenerator();
                    spectrum = generatorPg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PI:
                    var generatorPi = new PICidLipidSpectrumGenerator();
                    spectrum = generatorPi.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PS:
                    var generatorPs = new PSCidLipidSpectrumGenerator();
                    spectrum = generatorPs.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                //case LbmClass.LPC:
                //    var generatorLpc = new LPCCidLipidSpectrumGenerator();
                //    spectrum = generatorLpc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //    break;
                //case LbmClass.LPE:
                //    var generatorLpe = new LPECidLipidSpectrumGenerator();
                //    spectrum = generatorLpe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //    break;
                //case LbmClass.LPG:
                //    var generatorLpg = new LPGCidLipidSpectrumGenerator();
                //    spectrum = generatorLpg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //    break;
                //case LbmClass.LPI:
                //    var generatorLpi = new LPICidLipidSpectrumGenerator();
                //    spectrum = generatorLpi.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //    break;
                //case LbmClass.LPS:
                //    var generatorLps = new LPSCidLipidSpectrumGenerator();
                //    spectrum = generatorLps.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //    break;
                default:
                    Assert.Inconclusive("Skip");
                    return;
            }
        }

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
