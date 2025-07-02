using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using System.Reflection.Emit;

namespace CompMs.Common.Lipidomics.SourceGenerator.Tests;

[TestClass]
public class LipidSpectrumGeneratorTypeGeneratorTests
{
    [DataTestMethod]
    [DynamicData(nameof(GetTestDataGP), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTestGP(MoleculeMsReference reference, ILipid lipid)
    {
        var spectrum = new List<SpectrumPeak>();
        if (lipid is not null)
        {
            switch (lipid.LipidClass)
            {
                // GP
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
                case LbmClass.PC_d5:
                    var generatorPc_d5 = new PC_d5CidLipidSpectrumGenerator();
                    spectrum = generatorPc_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PE_d5:
                    var generatorPe_d5 = new PE_d5CidLipidSpectrumGenerator();
                    spectrum = generatorPe_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PG_d5:
                    var generatorPg_d5 = new PG_d5CidLipidSpectrumGenerator();
                    spectrum = generatorPg_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PI_d5:
                    var generatorPi_d5 = new PI_d5CidLipidSpectrumGenerator();
                    spectrum = generatorPi_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PS_d5:
                    var generatorPs_d5 = new PS_d5CidLipidSpectrumGenerator();
                    spectrum = generatorPs_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PA:
                    var generatorPa = new PACidLipidSpectrumGenerator();
                    spectrum = generatorPa.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PT:
                    var generatorPt = new PTCidLipidSpectrumGenerator();
                    spectrum = generatorPt.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.OxPC:
                    var generatorOxPc = new OxPCCidLipidSpectrumGenerator();
                    spectrum = generatorOxPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.OxPE:
                    var generatorOxPe = new OxPECidLipidSpectrumGenerator();
                    spectrum = generatorOxPe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.OxPG:
                    var generatorOxPg = new OxPGCidLipidSpectrumGenerator();
                    spectrum = generatorOxPg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.OxPI:
                    var generatorOxPi = new OxPICidLipidSpectrumGenerator();
                    spectrum = generatorOxPi.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.OxPS:
                    var generatorOxPs = new OxPSCidLipidSpectrumGenerator();
                    spectrum = generatorOxPs.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPC:
                    var generatorLpc = new LPCCidLipidSpectrumGenerator();
                    spectrum = generatorLpc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPE:
                    var generatorLpe = new LPECidLipidSpectrumGenerator();
                    spectrum = generatorLpe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPG:
                    var generatorLpg = new LPGCidLipidSpectrumGenerator();
                    spectrum = generatorLpg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPI:
                    var generatorLpi = new LPICidLipidSpectrumGenerator();
                    spectrum = generatorLpi.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPS:
                    var generatorLps = new LPSCidLipidSpectrumGenerator();
                    spectrum = generatorLps.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPC_d5:
                    var generatorLpc_d5 = new LPC_d5CidLipidSpectrumGenerator();
                    spectrum = generatorLpc_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPE_d5:
                    var generatorLpe_d5 = new LPE_d5CidLipidSpectrumGenerator();
                    spectrum = generatorLpe_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPG_d5:
                    var generatorLpg_d5 = new LPG_d5CidLipidSpectrumGenerator();
                    spectrum = generatorLpg_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPI_d5:
                    var generatorLpi_d5 = new LPI_d5CidLipidSpectrumGenerator();
                    spectrum = generatorLpi_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPS_d5:
                    var generatorLps_d5 = new LPS_d5CidLipidSpectrumGenerator();
                    spectrum = generatorLps_d5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LPA:
                    var generatorLpa = new LPACidLipidSpectrumGenerator();
                    spectrum = generatorLpa.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherPC:
                    var generatorEtherPc = new EtherPCCidLipidSpectrumGenerator();
                    spectrum = generatorEtherPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherPE:
                    if (lipid.Name.Contains("O-"))
                    {
                        var generatorEtherPeO = new EtherPE_OCidLipidSpectrumGenerator();
                        spectrum = generatorEtherPeO.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorEtherPeP = new EtherPE_PCidLipidSpectrumGenerator();
                        spectrum = generatorEtherPeP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.EtherLPC:
                    var generatorEtherLPc = new EtherLPCCidLipidSpectrumGenerator();
                    spectrum = generatorEtherLPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherLPE:
                    if (lipid.Name.Contains("O-"))
                    {
                        var generatorEtherLPeO = new EtherLPE_OCidLipidSpectrumGenerator();
                        spectrum = generatorEtherLPeO.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorEtherLPeP = new EtherLPE_PCidLipidSpectrumGenerator();
                        spectrum = generatorEtherLPeP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.EtherPG:
                    var generatorEtherPg = new EtherPGCidLipidSpectrumGenerator();
                    spectrum = generatorEtherPg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherPI:
                    var generatorEtherPi = new EtherPICidLipidSpectrumGenerator();
                    spectrum = generatorEtherPi.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherPS:
                    var generatorEtherPs = new EtherPSCidLipidSpectrumGenerator();
                    spectrum = generatorEtherPs.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherLPG:
                    var generatorEtherLPg = new EtherLPGCidLipidSpectrumGenerator();
                    spectrum = generatorEtherLPg.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherOxPC:
                    var generatorEtherOxPc = new EtherOxPCCidLipidSpectrumGenerator();
                    spectrum = generatorEtherOxPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.EtherOxPE:
                    var generatorEtherOxPe = new EtherOxPECidLipidSpectrumGenerator();
                    spectrum = generatorEtherOxPe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.bmPC:
                    var generatorbmPc = new bmPCCidLipidSpectrumGenerator();
                    spectrum = generatorbmPc.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.MMPE:
                    var generatorMmpe = new MMPECidLipidSpectrumGenerator();
                    spectrum = generatorMmpe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.DMPE:
                    var generatorDmpe = new DMPECidLipidSpectrumGenerator();
                    spectrum = generatorDmpe.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PMeOH:
                    var generatorPMeOH = new PMeOHCidLipidSpectrumGenerator();
                    spectrum = generatorPMeOH.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.PEtOH:
                    var generatorPEtOH = new PEtOHCidLipidSpectrumGenerator();
                    spectrum = generatorPEtOH.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.BisMeLPA:
                    var generatorBisMeLPA = new BisMeLPACidLipidSpectrumGenerator();
                    spectrum = generatorBisMeLPA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.GPNAE:
                    var generatorGPNAE = new GPNAECidLipidSpectrumGenerator();
                    spectrum = generatorGPNAE.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LNAPE:
                    var generatorLNAPE = new LNAPECidLipidSpectrumGenerator();
                    spectrum = generatorLNAPE.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.LNAPS:
                    var generatorLNAPS = new LNAPSCidLipidSpectrumGenerator();
                    spectrum = generatorLNAPS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                case LbmClass.BMP:
                    var generatorBmp = new BMPCidLipidSpectrumGenerator();
                    spectrum = generatorBmp.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
                //case LbmClass.HBMP:
                //    var generatorHBMP = new HBMPCidLipidSpectrumGenerator();
                //    spectrum = generatorHBMP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                //break;
                default:
                    //Assert.Inconclusive("Skip");
                    return;
            }
        }

        Console.WriteLine($"Lipid Name: {lipid?.Name.ToString() ?? "null"} {reference.AdductType.ToString() ?? "null"} {reference.CompoundClass.ToString() ?? "null"}");

        Assert.IsNotNull(spectrum);
        Assert.AreEqual(reference.Spectrum.Count, spectrum.Count);
        for (int i = 0; i < spectrum.Count; i++)
        {
            try
            {
                Assert.AreEqual(reference.Spectrum[i].Mass, spectrum[i].Mass, 1e-4);
                Assert.AreEqual(reference.Spectrum[i].Intensity, spectrum[i].Intensity);
            }
            catch (AssertFailedException ex)
            {
                Console.WriteLine($"  Reference Mass: {reference.Spectrum[i].Mass}, Spectrum Mass: {spectrum[i].Mass}");
                Console.WriteLine($"  Reference Intensity: {reference.Spectrum[i].Intensity}, Spectrum Intensity: {spectrum[i].Intensity}");
                Console.WriteLine($"  Spectrum Comment: {spectrum[i].Comment}");
                throw;
            }
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(GetTestDataGL), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTestGL(MoleculeMsReference reference, ILipid lipid)
    {
        var spectrum = new List<SpectrumPeak>();
        if (lipid is not null)
        {
            switch (lipid.LipidClass)
            {
                //// GL
                case LbmClass.MG: var generatorMG = new MGCidLipidSpectrumGenerator(); spectrum = generatorMG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.MGMG: var generatorMGMG = new MGMGCidLipidSpectrumGenerator(); spectrum = generatorMGMG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DGMG: var generatorDGMG = new DGMGCidLipidSpectrumGenerator(); spectrum = generatorDGMG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DG: var generatorDG = new DGCidLipidSpectrumGenerator(); spectrum = generatorDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DG_d5: var generatorDGd5 = new DG_d5CidLipidSpectrumGenerator(); spectrum = generatorDGd5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DGDG: var generatorDGDG = new DGDGCidLipidSpectrumGenerator(); spectrum = generatorDGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.MGDG: var generatorMGDG = new MGDGCidLipidSpectrumGenerator(); spectrum = generatorMGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.SMGDG: var generatorSMGDG = new SMGDGCidLipidSpectrumGenerator(); spectrum = generatorSMGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.SQDG: var generatorSQDG = new SQDGCidLipidSpectrumGenerator(); spectrum = generatorSQDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.EtherDG: var generatorEtherDG = new EtherDGCidLipidSpectrumGenerator(); spectrum = generatorEtherDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.EtherMGDG: var generatorEtherMGDG = new EtherMGDGCidLipidSpectrumGenerator(); spectrum = generatorEtherMGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.EtherDGDG: var generatorEtherDGDG = new EtherDGDGCidLipidSpectrumGenerator(); spectrum = generatorEtherDGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.EtherSMGDG: var generatorEtherSMGDG = new EtherSMGDGCidLipidSpectrumGenerator(); spectrum = generatorEtherSMGDG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DLCL: var generatorDLCL = new DLCLCidLipidSpectrumGenerator(); spectrum = generatorDLCL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DGCC: var generatorDGCC = new DGCCCidLipidSpectrumGenerator(); spectrum = generatorDGCC.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.LDGCC: var generatorLDGCC = new LDGCCCidLipidSpectrumGenerator(); spectrum = generatorLDGCC.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DGGA: var generatorDGGA = new DGGACidLipidSpectrumGenerator(); spectrum = generatorDGGA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DGTS: var generatorDGTS = new DGTSCidLipidSpectrumGenerator(); spectrum = generatorDGTS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.LDGTS: var generatorLDGTS = new LDGTSCidLipidSpectrumGenerator(); spectrum = generatorLDGTS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.TG: var generatorTG = new TGCidLipidSpectrumGenerator(); spectrum = generatorTG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.TG_d5: var generatorTGd5 = new TG_d5CidLipidSpectrumGenerator(); spectrum = generatorTGd5.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.OxTG: var generatorOxTG = new OxTGCidLipidSpectrumGenerator(); spectrum = generatorOxTG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.EtherTG: var generatorEtherTG = new EtherTGCidLipidSpectrumGenerator(); spectrum = generatorEtherTG.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.ADGGA: var generatorADGGA = new ADGGACidLipidSpectrumGenerator(); spectrum = generatorADGGA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                //case LbmClass.MLCL: var generatorMLCL = new MLCLCidLipidSpectrumGenerator(); spectrum = generatorMLCL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                //case LbmClass.TG_EST: var generatorTG_EST = new TG_ESTCidLipidSpectrumGenerator(); spectrum = generatorTG_EST.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                //case LbmClass.CL: var generatorCL = new CLCidLipidSpectrumGenerator(); spectrum = generatorCL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;

                default:
                    Assert.Inconclusive("Skip");
                    return;
            }
        }

        Console.WriteLine($"Lipid Name: {lipid?.Name.ToString() ?? "null"} {reference.AdductType.ToString() ?? "null"} {reference.CompoundClass.ToString() ?? "null"}");

        Assert.IsNotNull(spectrum);
        Assert.AreEqual(reference.Spectrum.Count, spectrum.Count);
        for (int i = 0; i < spectrum.Count; i++)
        {
            try
            {
                Assert.AreEqual(reference.Spectrum[i].Mass, spectrum[i].Mass, 1e-4);
                Assert.AreEqual(reference.Spectrum[i].Intensity, spectrum[i].Intensity);
            }
            catch (AssertFailedException ex)
            {
                Console.WriteLine($"  Reference Mass: {reference.Spectrum[i].Mass}, Spectrum Mass: {spectrum[i].Mass}");
                Console.WriteLine($"  Reference Intensity: {reference.Spectrum[i].Intensity}, Spectrum Intensity: {spectrum[i].Intensity}");
                Console.WriteLine($"  Spectrum Comment: {spectrum[i].Comment}");
                throw;
            }
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(GetTestDataCer), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTestCer(MoleculeMsReference reference, ILipid lipid)
    {
        var spectrum = new List<SpectrumPeak>();
        if (lipid is not null)
        {
            switch (lipid.LipidClass)
            {
                case LbmClass.Cer_NDS: var generatorCer_NDS = new Cer_NDSCidLipidSpectrumGenerator(); spectrum = generatorCer_NDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_NP: var generatorCer_NP = new Cer_NPCidLipidSpectrumGenerator(); spectrum = generatorCer_NP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_NS: var generatorCer_NS = new Cer_NSCidLipidSpectrumGenerator(); spectrum = generatorCer_NS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_NS_d7: var generatorCer_NS_d7 = new Cer_NS_d7CidLipidSpectrumGenerator(); spectrum = generatorCer_NS_d7.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.SM: var generatorSM = new SMCidLipidSpectrumGenerator(); spectrum = generatorSM.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_ABP: var generatorCer_ABP = new Cer_ABPCidLipidSpectrumGenerator(); spectrum = generatorCer_ABP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_ADS: var generatorCer_ADS = new Cer_ADSCidLipidSpectrumGenerator(); spectrum = generatorCer_ADS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_AP: var generatorCer_AP = new Cer_APCidLipidSpectrumGenerator(); spectrum = generatorCer_AP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_AS: var generatorCer_AS = new Cer_ASCidLipidSpectrumGenerator(); spectrum = generatorCer_AS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_BDS: var generatorCer_BDS = new Cer_BDSCidLipidSpectrumGenerator(); spectrum = generatorCer_BDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_BS: var generatorCer_BS = new Cer_BSCidLipidSpectrumGenerator(); spectrum = generatorCer_BS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_HDS: var generatorCer_HDS = new Cer_HDSCidLipidSpectrumGenerator(); spectrum = generatorCer_HDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_HS: var generatorCer_HS = new Cer_HSCidLipidSpectrumGenerator(); spectrum = generatorCer_HS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;

                default:
                    Assert.Inconclusive("Skip");
                    return;
            }
        }

        Console.WriteLine($"Lipid Name: {lipid?.Name.ToString() ?? "null"} {reference.AdductType.ToString() ?? "null"} {reference.CompoundClass.ToString() ?? "null"}");

        Assert.IsNotNull(spectrum);
        Assert.AreEqual(reference.Spectrum.Count, spectrum.Count);
        for (int i = 0; i < spectrum.Count; i++)
        {
            try
            {
                Assert.AreEqual(reference.Spectrum[i].Mass, spectrum[i].Mass, 1e-4);
                Assert.AreEqual(reference.Spectrum[i].Intensity, spectrum[i].Intensity);
            }
            catch (AssertFailedException ex)
            {
                Console.WriteLine($"  Reference Mass: {reference.Spectrum[i].Mass}, Spectrum Mass: {spectrum[i].Mass}");
                Console.WriteLine($"  Reference Intensity: {reference.Spectrum[i].Intensity}, Spectrum Intensity: {spectrum[i].Intensity}");
                Console.WriteLine($"  Spectrum Comment: {spectrum[i].Comment}");
                throw;
            }
        }
    }


    [DeploymentItem("LipidSpectrumGeneratorTest_GP.msp")]
    public static IEnumerable<object[]> GetTestDataGP() {
        var libGP = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_GP.msp");
        foreach (var reference in libGP) {
            var lipidGP = FacadeLipidParser.Default.Parse(reference.Name);
            yield return new object[] { reference, lipidGP };
        }
    }
    [DeploymentItem("LipidSpectrumGeneratorTest_GL.msp")]
    public static IEnumerable<object[]> GetTestDataGL()
    {
        var libGL = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_GL.msp");
        foreach (var reference in libGL)
        {
            var lipidGL = FacadeLipidParser.Default.Parse(reference.Name);
            yield return new object[] { reference, lipidGL };
        }
    }
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    public static IEnumerable<object[]> GetTestDataCer()
    {
        var libCer = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_Cer.msp");
        foreach (var reference in libCer)
        {
            var lipidCer = FacadeLipidParser.Default.Parse(reference.Name);
            yield return new object[] { reference, lipidCer };
        }
    }
}
