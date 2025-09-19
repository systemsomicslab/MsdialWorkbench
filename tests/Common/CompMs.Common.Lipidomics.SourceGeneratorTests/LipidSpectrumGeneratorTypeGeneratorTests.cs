using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;

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
                case LbmClass.HBMP:
                    var generatorHBMP = new HBMPCidLipidSpectrumGenerator();
                    spectrum = generatorHBMP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    break;
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
                case LbmClass.MLCL: var generatorMLCL = new MLCLCidLipidSpectrumGenerator(); spectrum = generatorMLCL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.TG_EST: var generatorTG_EST = new TG_ESTCidLipidSpectrumGenerator(); spectrum = generatorTG_EST.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.CL: var generatorCL = new CLCidLipidSpectrumGenerator(); spectrum = generatorCL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;

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
                case LbmClass.SM:
                    if (lipid.Chains.OxidizedCount == 3)
                    {
                        var generatorSM = new SM_OCidLipidSpectrumGenerator();
                        spectrum = generatorSM.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorSM = new SMCidLipidSpectrumGenerator();
                        spectrum = generatorSM.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.Cer_ABP: var generatorCer_ABP = new Cer_ABPCidLipidSpectrumGenerator(); spectrum = generatorCer_ABP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_ADS: var generatorCer_ADS = new Cer_ADSCidLipidSpectrumGenerator(); spectrum = generatorCer_ADS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_AP: var generatorCer_AP = new Cer_APCidLipidSpectrumGenerator(); spectrum = generatorCer_AP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_AS: var generatorCer_AS = new Cer_ASCidLipidSpectrumGenerator(); spectrum = generatorCer_AS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_BDS: var generatorCer_BDS = new Cer_BDSCidLipidSpectrumGenerator(); spectrum = generatorCer_BDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_BS: var generatorCer_BS = new Cer_BSCidLipidSpectrumGenerator(); spectrum = generatorCer_BS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_HDS: var generatorCer_HDS = new Cer_HDSCidLipidSpectrumGenerator(); spectrum = generatorCer_HDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_HS: var generatorCer_HS = new Cer_HSCidLipidSpectrumGenerator(); spectrum = generatorCer_HS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.CerP: var generatorCerP = new CerPCidLipidSpectrumGenerator(); spectrum = generatorCerP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_NDS: var generatorHexCer_NDS = new HexCer_NDSCidLipidSpectrumGenerator(); spectrum = generatorHexCer_NDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_NS: var generatorHexCer_NS = new HexCer_NSCidLipidSpectrumGenerator(); spectrum = generatorHexCer_NS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_HDS: var generatorHexCer_HDS = new HexCer_HDSCidLipidSpectrumGenerator(); spectrum = generatorHexCer_HDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_HS: var generatorHexCer_HS = new HexCer_HSCidLipidSpectrumGenerator(); spectrum = generatorHexCer_HS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Hex2Cer: var generatorHex2Cer = new Hex2CerCidLipidSpectrumGenerator(); spectrum = generatorHex2Cer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Hex3Cer: var generatorHex3Cer = new Hex3CerCidLipidSpectrumGenerator(); spectrum = generatorHex3Cer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_AP: var generatorHexCer_AP = new HexCer_APCidLipidSpectrumGenerator(); spectrum = generatorHexCer_AP.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.PE_Cer: var generatorPE_Cer = new PE_CerCidLipidSpectrumGenerator(); spectrum = generatorPE_Cer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.PI_Cer: var generatorPI_Cer = new PI_CerCidLipidSpectrumGenerator(); spectrum = generatorPI_Cer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.SHexCer:
                    if (lipid.Chains.OxidizedCount == 3)
                    {
                        var generatorSHexCer = new SHexCer_OCidLipidSpectrumGenerator();
                        spectrum = generatorSHexCer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorSHexCer = new SHexCerCidLipidSpectrumGenerator();
                        spectrum = generatorSHexCer.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.SL:
                    if (lipid.Chains.OxidizedCount == 2)
                    {
                        var generatorSL = new SL_OCidLipidSpectrumGenerator();
                        spectrum = generatorSL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorSL = new SLCidLipidSpectrumGenerator();
                        spectrum = generatorSL.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.GD1a: var generatorGD1a = new GD1a_CerCidLipidSpectrumGenerator(); spectrum = generatorGD1a.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GD1b: var generatorGD1b = new GD1b_CerCidLipidSpectrumGenerator(); spectrum = generatorGD1b.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GD2: var generatorGD2 = new GD2_CerCidLipidSpectrumGenerator(); spectrum = generatorGD2.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GD3: var generatorGD3 = new GD3_CerCidLipidSpectrumGenerator(); spectrum = generatorGD3.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GM1: var generatorGM1 = new GM1_CerCidLipidSpectrumGenerator(); spectrum = generatorGM1.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GM3: var generatorGM3 = new GM3_CerCidLipidSpectrumGenerator(); spectrum = generatorGM3.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GQ1b: var generatorGQ1b = new GQ1b_CerCidLipidSpectrumGenerator(); spectrum = generatorGQ1b.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.GT1b: var generatorGT1b = new GT1b_CerCidLipidSpectrumGenerator(); spectrum = generatorGT1b.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.MIPC: var generatorMIPC = new MIPC_CerCidLipidSpectrumGenerator(); spectrum = generatorMIPC.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.NGcGM3: var generatorNGcGM3 = new NGcGM3_CerCidLipidSpectrumGenerator(); spectrum = generatorNGcGM3.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.AHexCer:
                    if (lipid.Chains.OxidizedCount == 2)
                    {
                        var generatorAHexCer_NS = new AHexCer_NSCidLipidSpectrumGenerator();
                        spectrum = generatorAHexCer_NS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorAHexCer_AS = new AHexCer_ASCidLipidSpectrumGenerator();
                        spectrum = generatorAHexCer_AS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.ASHexCer:
                    if (lipid.Chains.OxidizedCount == 2)
                    {
                        var generatorASHexCer_NS = new ASHexCer_NSCidLipidSpectrumGenerator();
                        spectrum = generatorASHexCer_NS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorASHexCer_AS = new ASHexCer_ASCidLipidSpectrumGenerator();
                        spectrum = generatorASHexCer_AS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.ASM: var generatorASM = new ASMCidLipidSpectrumGenerator(); spectrum = generatorASM.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_EBDS:
                    var generatorCer_EBDS = new Cer_EBDSCidLipidSpectrumGenerator(); spectrum = generatorCer_EBDS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_EODS:
                    var generatorCer_EODS = new Cer_EODSCidLipidSpectrumGenerator(); spectrum = generatorCer_EODS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.Cer_EOS:
                    var generatorCer_EOS = new Cer_EOSCidLipidSpectrumGenerator(); spectrum = generatorCer_EOS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.HexCer_EOS: var generatorHexCer_EOS = new HexCer_EOSCidLipidSpectrumGenerator(); spectrum = generatorHexCer_EOS.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;

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
    [DynamicData(nameof(GetTestDataOthers), DynamicDataSourceType.Method)]
    public void GenreratedGeneratorTestOther(MoleculeMsReference reference, ILipid lipid)
    {
        var spectrum = new List<SpectrumPeak>();
        if (lipid is not null)
        {
            switch (lipid.LipidClass)
            {
                case LbmClass.FA: var generatorFA = new FACidLipidSpectrumGenerator(); spectrum = generatorFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.OxFA:
                    if (lipid.Name.Contains("(2OH)"))
                    {
                        var generatoralphaOxFA = new alphaOxFACidLipidSpectrumGenerator(); spectrum = generatoralphaOxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorOxFA = new OxFACidLipidSpectrumGenerator(); spectrum = generatorOxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.FAHFA:
                    if (lipid.Name.StartsWith("AAHFA") || reference.Spectrum.Count == 5)
                    {
                        var generatorAAHFA = new AAHFACidLipidSpectrumGenerator(); spectrum = generatorAAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    else
                    {
                        var generatorFAHFA = new FAHFACidLipidSpectrumGenerator(); spectrum = generatorFAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.DMEDFAHFA: var generatorDMEDFAHFA = new DMEDFAHFACidLipidSpectrumGenerator(); spectrum = generatorDMEDFAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DMEDFA: var generatorDMEDFA = new DMEDFACidLipidSpectrumGenerator(); spectrum = generatorDMEDFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.DMEDOxFA: var generatorDMEDOxFA = new DMEDOxFACidLipidSpectrumGenerator(); spectrum = generatorDMEDOxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.WE: var generatorWE = new WECidLipidSpectrumGenerator(); spectrum = generatorWE.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                case LbmClass.NAOrn:
                    if (lipid.Chains.ChainCount == 2)
                    {
                        var generatorNAOrn_FAHFA = new NAOrn_FAHFACidLipidSpectrumGenerator(); spectrum = generatorNAOrn_FAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAOrn_OxFA = new NAOrn_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAOrn_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNAOrn_FA = new NAOrn_FACidLipidSpectrumGenerator(); spectrum = generatorNAOrn_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.NAGly:
                    if (lipid.Chains.ChainCount == 2)
                    {
                        var generatorNAGly_FAHFA = new NAGly_FAHFACidLipidSpectrumGenerator(); spectrum = generatorNAGly_FAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAGly_OxFA = new NAGly_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAGly_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNAGly_FA = new NAGly_FACidLipidSpectrumGenerator(); spectrum = generatorNAGly_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.NAGlySer:
                    if (lipid.Chains.ChainCount == 2)
                    {
                        var generatorNAGlySer_FAHFA = new NAGlySer_FAHFACidLipidSpectrumGenerator(); spectrum = generatorNAGlySer_FAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAGlySer_OxFA = new NAGlySer_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAGlySer_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.NATryA:
                    if (lipid.Chains.ChainCount == 2)
                    {
                        var generatorNATryA_FAHFA = new NATryA_FAHFACidLipidSpectrumGenerator(); spectrum = generatorNATryA_FAHFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNATryA_OxFA = new NATryA_OxFACidLipidSpectrumGenerator(); spectrum = generatorNATryA_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNATryA_FA = new NATryA_FACidLipidSpectrumGenerator(); spectrum = generatorNATryA_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NATau:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNATau_OxFA = new NATau_OxFACidLipidSpectrumGenerator(); spectrum = generatorNATau_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNATau_FA = new NATau_FACidLipidSpectrumGenerator(); spectrum = generatorNATau_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NAPhe:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAPhe_OxFA = new NAPhe_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAPhe_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNAPhe_FA = new NAPhe_FACidLipidSpectrumGenerator(); spectrum = generatorNAPhe_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NA5HT:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNA5HT_OxFA = new NA5HT_OxFACidLipidSpectrumGenerator(); spectrum = generatorNA5HT_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                    }
                    else
                    {
                        var generatorNA5HT_FA = new NA5HT_FACidLipidSpectrumGenerator(); spectrum = generatorNA5HT_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                //case LbmClass.NAAnt:
                //    if (lipid.Chains.OxidizedCount > 0)
                //    {
                //        var generatorNAAnt_OxFA = new NAAnt_OxFACidLipidSpectrumGenerator(); spectrum = generatorNA5HT_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                //    }
                //    else
                //    {
                //        var generatorNAAnt_FA = new NAAnt_FACidLipidSpectrumGenerator(); spectrum = generatorNA5HT_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); break;
                //    }                
                case LbmClass.NASer:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNASer_OxFA = new NASer_OxFACidLipidSpectrumGenerator(); spectrum = generatorNASer_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    else
                    {
                        var generatorNASer_FA = new NASer_FACidLipidSpectrumGenerator(); spectrum = generatorNASer_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList();
                    }
                    break;
                case LbmClass.NAAla:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAAla_OxFA = new NAAla_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAAla_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    else
                    {
                        var generatorNAAla_FA = new NAAla_FACidLipidSpectrumGenerator(); spectrum = generatorNAAla_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NAGln:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAGln_OxFA = new NAGln_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAGln_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    else
                    {
                        var generatorNAGln_FA = new NAGln_FACidLipidSpectrumGenerator(); spectrum = generatorNAGln_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NALeu:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNALeu_OxFA = new NALeu_OxFACidLipidSpectrumGenerator(); spectrum = generatorNALeu_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    else
                    {
                        var generatorNALeu_FA = new NALeu_FACidLipidSpectrumGenerator(); spectrum = generatorNALeu_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
                case LbmClass.NAVal:
                    if (lipid.Chains.OxidizedCount > 0)
                    {
                        var generatorNAVal_OxFA = new NAVal_OxFACidLipidSpectrumGenerator(); spectrum = generatorNAVal_OxFA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    else
                    {
                        var generatorNAVal_FA = new NAVal_FACidLipidSpectrumGenerator(); spectrum = generatorNAVal_FA.Generate((Lipid)lipid, reference.AdductType)?.OrderBy(s => s.Mass).ToList(); 
                    }
                    break;
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
    public static IEnumerable<object[]> GetTestDataGP()
    {
        var libGP = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_GP.msp");
        foreach (var reference in libGP)
        {
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
    [DeploymentItem("LipidSpectrumGeneratorTest_Others.msp")]
    public static IEnumerable<object[]> GetTestDataOthers()
    {
        var libOther = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_Others.msp");
        foreach (var reference in libOther)
        {
            var lipidOther = FacadeLipidParser.Default.Parse(reference.Name);
            yield return new object[] { reference, lipidOther };
        }
    }
    [DeploymentItem("LipidSpectrumGeneratorTest_Cer.msp")]
    public static IEnumerable<object[]> GetTestDataCer()
    {
        var libCer = MspFileParser.MspFileReader("LipidSpectrumGeneratorTest_Cer.msp");
        foreach (var reference in libCer)
        {
            var lipidCer = FacadeLipidParser.Default.Parse(reference.Name);


            if (lipidCer.LipidClass.ToString() != reference.CompoundClass)
            {
                var lipidClass = (LbmClass)System.Enum.Parse(typeof(LbmClass), reference.CompoundClass);

                var lipidCer2 = new Lipid(
                    lipidClass,
                    lipidCer.Mass,
                    lipidCer.Chains
                );
                yield return new object[] { reference, lipidCer2 };
            }
            else
            {
                yield return new object[] { reference, lipidCer };
            }
        }
    }
}
