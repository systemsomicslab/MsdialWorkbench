using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{

    public class LipidMolecule
    {

        public string LipidName { get; set; }
        public string SublevelLipidName { get; set; }
        public LbmClass LipidClass { get; set; }
        public int AnnotationLevel { get; set; } // 0: incorrect, 1: submolecular level, 2: acyl level, 3: acyl & positional level 
        public AdductIon Adduct { get; set; }
        public float Mz { get; set; }
        public float Rt { get; set; }
        public string Smiles { get; set; }
        public string Formula { get; set; }
        public string InChIKey { get; set; }
        public IonMode IonMode { get; set; }
        public string LipidSubclass { get; set; }
        public string LipidCategory { get; set; }
        public bool IsValidatedFormat { get; set; }

        public double Score { get; set; }

        public string TotalChainString { get; set; }

        // glycero lipids
        public string Sn1AcylChainString { get; set; }
        public string Sn2AcylChainString { get; set; }
        public string Sn3AcylChainString { get; set; }
        public string Sn4AcylChainString { get; set; }

        public int TotalCarbonCount { get; set; }
        public int TotalDoubleBondCount { get; set; }
        public int TotalOxidizedCount { get; set; }

        public int Sn1CarbonCount { get; set; } // it becomes sphingobase in ceramide species
        public int Sn1DoubleBondCount { get; set; } // it becomes sphingobase in ceramide species
        public int Sn1Oxidizedount { get; set; } // it becomes sphingobase in ceramide species
        public int Sn2CarbonCount { get; set; } // it becomes acylchain in ceramide species
        public int Sn2DoubleBondCount { get; set; } // it becomes acylchain in ceramide species
        public int Sn2Oxidizedount { get; set; } // it becomes acylchain in ceramide species
        public int Sn3CarbonCount { get; set; } // it becomes additional acyl chain in ceramide species like Cer-EOS
        public int Sn3DoubleBondCount { get; set; } // it becomes additional acyl chain in ceramide species like Cer-EOS
        public int Sn3Oxidizedount { get; set; } // it becomes additional acyl chain in ceramide species like Cer-EOS
        public int Sn4CarbonCount { get; set; }
        public int Sn4DoubleBondCount { get; set; }
        public int Sn4Oxidizedount { get; set; }
    }

    public sealed class LipidAnnotation
    {
        private LipidAnnotation() { }

        // test query spectrum
        public static RawData ReadTestSpectrum(string input)
        {
            return RawDataParcer.RawDataFileReader(input, new AnalysisParamOfMsfinder());
        }

        // ref molecules must be sorted by mz before using this program
        public static LipidMolecule Characterize(double queryMz, double queryRt,
            IMSScanProperty msScanProp, List<LipidMolecule> RefMolecules, IonMode ionMode,
            double ms1tol, double ms2tol)
        {

            var startID = GetDatabaseStartIndex(queryMz, ms1tol, RefMolecules);
            var molecules = new List<LipidMolecule>();
            for (int i = startID; i < RefMolecules.Count; i++)
            {
                var molecule = RefMolecules[i];
                var refMz = molecule.Mz;
                var refClass = molecule.LipidClass;
                var adduct = molecule.Adduct;
                var lipidclass = refClass;

                if (refMz < queryMz - ms1tol) continue;
                if (refMz > queryMz + ms1tol) break;
                //Console.WriteLine(molecule.LipidName + molecule.Adduct);
                LipidMolecule result = null;

                var totalCarbon = molecule.TotalCarbonCount;
                var totalDbBond = molecule.TotalDoubleBondCount;
                var totalOxidized = molecule.TotalOxidizedCount;

                //add MT
                if (molecule.LipidName.Contains("+O")) totalOxidized = 1;
                //

                var sn1MaxCarbon = 36;
                var sn1MaxDbBond = 12;
                var sn1MinCarbon = 2;
                var sn1MinDbBond = 0;
                var sn1MaxOxidized = 0;

                var sn2MaxCarbon = 36;
                var sn2MaxDbBond = 12;
                var sn2MinCarbon = 2;
                var sn2MinDbBond = 0;
                var sn2MaxOxidized = 6;

                var sn3MaxCarbon = 36;
                var sn3MaxDbBond = 12;
                var sn3MinCarbon = 2;
                var sn3MinDbBond = 0;
                //var sn3Oxidized = 6;

                switch (lipidclass)
                {
                    case LbmClass.PC:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylcholine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PE:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PS:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylserine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PG:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylglycerol(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylinositol(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.BMP:
                        result = LipidMsmsCharacterization.JudgeIfBismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LNAPE:
                        result = LipidMsmsCharacterization.JudgeIfNacylphosphatidylethanolamine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LNAPS:
                        result = LipidMsmsCharacterization.JudgeIfNacylphosphatidylserine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.SM:
                        result = LipidMsmsCharacterization.JudgeIfSphingomyelin(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.TG:
                        result = LipidMsmsCharacterization.JudgeIfTriacylglycerol(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.CAR:
                        result = LipidMsmsCharacterization.JudgeIfAcylcarnitine(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.CE:
                        result = LipidMsmsCharacterization.JudgeIfCholesterylEster(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;

                    // add MT, single or double chains pattern
                    case LbmClass.DG:
                        result = LipidMsmsCharacterization.JudgeIfDag(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.MG:
                        result = LipidMsmsCharacterization.JudgeIfMag(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.MGDG:
                        result = LipidMsmsCharacterization.JudgeIfMgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGDG:
                        result = LipidMsmsCharacterization.JudgeIfDgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PMeOH:
                        result = LipidMsmsCharacterization.JudgeIfPmeoh(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PEtOH:
                        result = LipidMsmsCharacterization.JudgeIfPetoh(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PBtOH:
                        result = LipidMsmsCharacterization.JudgeIfPbtoh(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.LPC:
                        result = LipidMsmsCharacterization.JudgeIfLysopc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPE:
                        result = LipidMsmsCharacterization.JudgeIfLysope(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PA:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidicacid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPA:
                        result = LipidMsmsCharacterization.JudgeIfLysopa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPG:
                        result = LipidMsmsCharacterization.JudgeIfLysopg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPI:
                        result = LipidMsmsCharacterization.JudgeIfLysopi(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPS:
                        result = LipidMsmsCharacterization.JudgeIfLysops(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPC:
                        result = LipidMsmsCharacterization.JudgeIfEtherpc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPE:
                        result = LipidMsmsCharacterization.JudgeIfEtherpe(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPC:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysopc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPE:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysope(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.OxPC:
                        result = LipidMsmsCharacterization.JudgeIfOxpc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPE:
                        result = LipidMsmsCharacterization.JudgeIfOxpe(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPG:
                        result = LipidMsmsCharacterization.JudgeIfOxpg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPI:
                        result = LipidMsmsCharacterization.JudgeIfOxpi(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPS:
                        result = LipidMsmsCharacterization.JudgeIfOxps(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.EtherMGDG:
                        result = LipidMsmsCharacterization.JudgeIfEthermgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherDGDG:
                        result = LipidMsmsCharacterization.JudgeIfEtherdgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGTS:
                        result = LipidMsmsCharacterization.JudgeIfDgts(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LDGTS:
                        result = LipidMsmsCharacterization.JudgeIfLdgts(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGCC:
                        result = LipidMsmsCharacterization.JudgeIfDgcc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LDGCC:
                        result = LipidMsmsCharacterization.JudgeIfLdgcc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGGA:
                        result = LipidMsmsCharacterization.JudgeIfGlcadg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SQDG:
                        result = LipidMsmsCharacterization.JudgeIfSqdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DLCL:
                        result = LipidMsmsCharacterization.JudgeIfDilysocardiolipin(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.FA:
                        result = LipidMsmsCharacterization.JudgeIfFattyacid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DMEDFA:
                        result = LipidMsmsCharacterization.JudgeIfDmedFattyacid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.FAHFA:
                        result = LipidMsmsCharacterization.JudgeIfFahfa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DMEDFAHFA:
                        result = LipidMsmsCharacterization.JudgeIfFahfaDMED(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.OxFA:
                        result = LipidMsmsCharacterization.JudgeIfOxfattyacid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.DMEDOxFA:
                        result = LipidMsmsCharacterization.JudgeIfDmedOxfattyacid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.EtherOxPC:
                        result = LipidMsmsCharacterization.JudgeIfEtheroxpc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.EtherOxPE:
                        result = LipidMsmsCharacterization.JudgeIfEtheroxpe(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.Cer_NS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidens(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_NDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidends(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_NS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramidens(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_NDS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramidends(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Hex2Cer:
                        result = LipidMsmsCharacterization.JudgeIfHexhexceramidens(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Hex3Cer:
                        result = LipidMsmsCharacterization.JudgeIfHexhexhexceramidens(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_AP:
                        result = LipidMsmsCharacterization.JudgeIfCeramideap(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_ABP:
                        result = LipidMsmsCharacterization.JudgeIfCeramideabp(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_AP:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideap(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_AS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideas(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_ADS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideads(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_BS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidebs(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_BDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidebds(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_NP:
                        result = LipidMsmsCharacterization.JudgeIfCeramidenp(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_OS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideos(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SHexCer:
                        result = LipidMsmsCharacterization.JudgeIfShexcer(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GM3:
                        result = LipidMsmsCharacterization.JudgeIfGm3(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DHSph:
                        result = LipidMsmsCharacterization.JudgeIfSphinganine(msScanProp, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;
                    case LbmClass.Sph:
                        result = LipidMsmsCharacterization.JudgeIfSphingosine(msScanProp, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;
                    case LbmClass.PhytoSph:
                        result = LipidMsmsCharacterization.JudgeIfPhytosphingosine(msScanProp, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;

                    // add mikiko takahashi, hetero type chains- and 3 or 4 chains pattern
                    case LbmClass.ADGGA:
                        result = LipidMsmsCharacterization.JudgeIfAcylglcadg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.HBMP:
                        result = LipidMsmsCharacterization.JudgeIfHemiismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherTG:
                        result = LipidMsmsCharacterization.JudgeIfEthertag(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.CL:
                        result = LipidMsmsCharacterization.JudgeIfCardiolipin(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                             sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, adduct);
                        break;
                    case LbmClass.MLCL:
                        result = LipidMsmsCharacterization.JudgeIfLysocardiolipin(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EOS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideeos(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EODS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideeods(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_EOS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideeos(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.ASM:
                        result = LipidMsmsCharacterization.JudgeIfAcylsm(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EBDS:
                        result = LipidMsmsCharacterization.JudgeIfAcylcerbds(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.AHexCer:
                        result = LipidMsmsCharacterization.JudgeIfAcylhexcer(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.ASHexCer:
                        result = LipidMsmsCharacterization.JudgeIfAshexcer(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 10/04/19
                    case LbmClass.EtherPI:
                        result = LipidMsmsCharacterization.JudgeIfEtherpi(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPS:
                        result = LipidMsmsCharacterization.JudgeIfEtherps(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI_Cer:
                        result = LipidMsmsCharacterization.JudgeIfPicermide(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.PE_Cer:
                        result = LipidMsmsCharacterization.JudgeIfPecermide(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;

                    //add 13/5/19 modified 20200218
                    case LbmClass.DCAE:
                        result = LipidMsmsCharacterization.JudgeIfDcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GDCAE:
                        result = LipidMsmsCharacterization.JudgeIfGdcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GLCAE:
                        result = LipidMsmsCharacterization.JudgeIfGlcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.TDCAE:
                        result = LipidMsmsCharacterization.JudgeIfTdcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.TLCAE:
                        result = LipidMsmsCharacterization.JudgeIfTlcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.NAE:
                        result = LipidMsmsCharacterization.JudgeIfAnandamide(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.NAGly:
                        if (totalCarbon == sn1MinCarbon && totalCarbon == sn1MaxCarbon)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylGlyOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamidegly(msScanProp, ms2tol, refMz,
                                 totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.NAGlySer:
                        if (totalCarbon == sn1MinCarbon && totalCarbon == sn1MaxCarbon)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylGlySerOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamideglyser(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.SL:
                        result = LipidMsmsCharacterization.JudgeIfSulfonolipid(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.EtherPG:
                        result = LipidMsmsCharacterization.JudgeIfEtherpg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPG:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysopg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.CoQ:
                        result = LipidMsmsCharacterization.JudgeIfCoenzymeq(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.Vitamin_E:
                        result = LipidMsmsCharacterization.JudgeIfVitaminEmolecules(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.Vitamin_D:
                        result = LipidMsmsCharacterization.JudgeIfVitaminDmolecules(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.VAE:
                        result = LipidMsmsCharacterization.JudgeIfVitaminaestermolecules(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.NAOrn:
                        if (totalCarbon == sn1MinCarbon && totalCarbon == sn1MaxCarbon)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylOrnOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamideorn(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.BRSE:
                        result = LipidMsmsCharacterization.JudgeIfBrseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.CASE:
                        result = LipidMsmsCharacterization.JudgeIfCaseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.SISE:
                        result = LipidMsmsCharacterization.JudgeIfSiseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.STSE:
                        result = LipidMsmsCharacterization.JudgeIfStseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.AHexBRS:
                        result = LipidMsmsCharacterization.JudgeIfAhexbrseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexCAS:
                        result = LipidMsmsCharacterization.JudgeIfAhexcaseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexCS:
                        result = LipidMsmsCharacterization.JudgeIfAhexceSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexSIS:
                        result = LipidMsmsCharacterization.JudgeIfAhexsiseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexSTS:
                        result = LipidMsmsCharacterization.JudgeIfAhexstseSpecies(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;

                    //add 190528
                    case LbmClass.Cer_HS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.Cer_HDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.Cer_NDOS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidedos(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.HexCer_HS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.HexCer_HDS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //190801
                    case LbmClass.SHex:
                        result = LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.BAHex:
                        result = LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.SSulfate:
                        result = LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.BASulfate:
                        result = LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    // added 190811
                    case LbmClass.CerP:
                        result = LipidMsmsCharacterization.JudgeIfCeramidePhosphate(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    ///2019/11/25 add
                    case LbmClass.SMGDG:
                        result = LipidMsmsCharacterization.JudgeIfSmgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherSMGDG:
                        result = LipidMsmsCharacterization.JudgeIfEtherSmgdg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 20200218
                    case LbmClass.LCAE:
                        result = LipidMsmsCharacterization.JudgeIfLcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.KLCAE:
                        result = LipidMsmsCharacterization.JudgeIfKlcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.KDCAE:
                        result = LipidMsmsCharacterization.JudgeIfKdcae(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    //add 20200714
                    case LbmClass.DMPE:
                        result = LipidMsmsCharacterization.JudgeIfDiMethylPE(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.MMPE:
                        result = LipidMsmsCharacterization.JudgeIfMonoMethylPE(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.MIPC:
                        result = LipidMsmsCharacterization.JudgeIfMipc(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 20200720
                    case LbmClass.EGSE:
                        result = LipidMsmsCharacterization.JudgeIfErgoSESpecies(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.DEGSE:
                        result = LipidMsmsCharacterization.JudgeIfDehydroErgoSESpecies(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    //add 20200812
                    case LbmClass.OxTG:
                        result = LipidMsmsCharacterization.JudgeIfOxTriacylglycerol(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.TG_EST:
                        result = LipidMsmsCharacterization.JudgeIfFahfaTriacylglycerol(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond,
                            sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, adduct);
                        break;
                    //add 20200923
                    case LbmClass.DSMSE:
                        result = LipidMsmsCharacterization.JudgeIfDesmosterolSpecies(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    //add20210216
                    case LbmClass.GPNAE:
                        result = LipidMsmsCharacterization.JudgeIfGpnae(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.MGMG:
                        result = LipidMsmsCharacterization.JudgeIfMgmg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.DGMG:
                        result = LipidMsmsCharacterization.JudgeIfDgmg(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);

                        //add 20210315
                        break;
                    case LbmClass.GD1a:
                        result = LipidMsmsCharacterization.JudgeIfGD1a(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD1b:
                        result = LipidMsmsCharacterization.JudgeIfGD1b(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD2:
                        result = LipidMsmsCharacterization.JudgeIfGD2(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD3:
                        result = LipidMsmsCharacterization.JudgeIfGD3(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GM1:
                        result = LipidMsmsCharacterization.JudgeIfGM1(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GQ1b:
                        result = LipidMsmsCharacterization.JudgeIfGQ1b(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GT1b:
                        result = LipidMsmsCharacterization.JudgeIfGT1b(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.NGcGM3:
                        result = LipidMsmsCharacterization.JudgeIfNGcGM3(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.ST:
                        result = LipidMsmsCharacterization.JudgeIfnoChainSterol(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.CSLPHex:
                    case LbmClass.BRSLPHex:
                    case LbmClass.CASLPHex:
                    case LbmClass.SISLPHex:
                    case LbmClass.STSLPHex:
                        result = LipidMsmsCharacterization.JudgeIfSteroidWithLpa(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;


                    case LbmClass.CSPHex:
                    case LbmClass.BRSPHex:
                    case LbmClass.CASPHex:
                    case LbmClass.SISPHex:
                    case LbmClass.STSPHex:
                        result = LipidMsmsCharacterization.JudgeIfSteroidWithPa(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20220201
                    case LbmClass.SPE:
                        result = LipidMsmsCharacterization.JudgeIfSpeSpecies(molecule.LipidName, molecule.LipidClass,
                            msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;
                    //20220322
                    case LbmClass.NAPhe:
                        result = LipidMsmsCharacterization.JudgeIfNAcylPheFa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NATau:
                        result = LipidMsmsCharacterization.JudgeIfNAcylTauFa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.PT:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylThreonine(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20230407
                    case LbmClass.PC_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylcholineD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PE_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamineD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PS_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylserineD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PG_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylglycerolD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylinositolD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPC_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopcD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPE_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopeD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPG_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopgD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPI_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopiD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPS_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopsD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.TG_d5:
                        result = LipidMsmsCharacterization.JudgeIfTriacylglycerolD5(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.DG_d5:
                        result = LipidMsmsCharacterization.JudgeIfDagD5(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SM_d9:
                        result = LipidMsmsCharacterization.JudgeIfSphingomyelinD9(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.CE_d7:
                        result = LipidMsmsCharacterization.JudgeIfCholesterylEsterD7(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.Cer_NS_d7:
                        result = LipidMsmsCharacterization.JudgeIfCeramidensD7(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20230424
                    case LbmClass.bmPC:
                        return LipidMsmsCharacterization.JudgeIfBetaMethylPhosphatidylcholine(msScanProp, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                    //20230612
                    case LbmClass.NATryA:
                        result = LipidMsmsCharacterization.JudgeIfNAcylTryA(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.NA5HT:
                        result = LipidMsmsCharacterization.JudgeIfNAcyl5HT(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.WE:
                        result = LipidMsmsCharacterization.JudgeIfWaxEster(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20230626
                    case LbmClass.NAAla:
                        result = LipidMsmsCharacterization.JudgeIfNAcylAla(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NAGln:
                        result = LipidMsmsCharacterization.JudgeIfNAcylGln(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NALeu:
                        result = LipidMsmsCharacterization.JudgeIfNAcylLeu(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NAVal:
                        result = LipidMsmsCharacterization.JudgeIfNAcylVal(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NASer:
                        result = LipidMsmsCharacterization.JudgeIfNAcylSer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.BisMeLPA:
                        result = LipidMsmsCharacterization.JudgeIfBismelpa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    default:
                        return null;
                }

                if (result != null)
                {
                    molecules.Add(result);
                }

                if (result != null && result.AnnotationLevel == 2)
                {
                    //Console.WriteLine("candidate {0}, suggested {1}, score {2}", molecule.LipidName, result.LipidName, result.Score);
                }
                else
                {
                    //Console.WriteLine("candidate {0}, suggested {1}, score {2}", molecule.LipidName, "NA", "-1");
                }
            }

            if (molecules.Count > 0)
            {
                return molecules.OrderByDescending(n => n.Score).ToList()[0];
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// peaks are converted to 
        /// 1. normalized spectrum where maximum intensity is normalized to 100
        /// 2. ordered as higher intensity -> lower intensity
        /// </summary>
        public static List<SpectrumPeak> ConvertToRequiredSpectrumFormat(List<SpectrumPeak> peaks)
        {
            var spectrum = new List<SpectrumPeak>();
            var maxintensity = peaks.Max(n => n.Intensity);
            foreach (var peak in peaks)
            {
                spectrum.Add(new SpectrumPeak { Mass = peak.Mass, Intensity = peak.Intensity / maxintensity * 100.0 });
            }
            return spectrum.OrderByDescending(n => n.Intensity).ToList();
        }

        public static int GetDatabaseStartIndex(double mz, double tolerance, List<LipidMolecule> molecules)
        {
            double targetMass = mz - tolerance;
            int startIndex = 0, endIndex = molecules.Count - 1;
            if (targetMass > molecules[endIndex].Mz) return endIndex;

            int counter = 0;
            while (counter < 10)
            {
                if (molecules[startIndex].Mz <= targetMass && targetMass < molecules[(startIndex + endIndex) / 2].Mz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (molecules[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < molecules[endIndex].Mz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
    }

    public sealed class LipidLibraryParser
    {
        private LipidLibraryParser() { }

        //[0] Name [1] m/z [2] adduct
        public static List<LipidMolecule> ReadLibrary(string file)
        {
            var molecules = new List<LipidMolecule>();
            using (var sr = new StreamReader(file, Encoding.ASCII))
            {
                sr.ReadLine(); // header pathed
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t'); // e.g. [0] PC 28:2+3O [1] 301.000 [2] [M+HCOO]-

                    var nameString = lineArray[0]; // PC 28:2+3O

                    // add MT
                    if (nameString.Contains(":") == !true) { nameString = nameString + " :"; }

                    var lipidClass = nameString.Split(' ')[0]; // PC
                    var lipidClassEnum = getLipidClassEnum(lipidClass);
                    if (lipidClassEnum == LbmClass.Undefined) continue;


                    var mzString = lineArray[1]; // 301.000
                    var mzValue = -1.0F;
                    if (!float.TryParse(mzString, out mzValue)) continue;

                    var adductString = lineArray[2]; // [M+HCOO]-
                    var adduct = AdductIon.GetAdductIon(adductString);
                    if (!adduct.FormatCheck) continue;

                    var chainString = nameString.Split(' ')[1]; // case 18:2, d18:2, t18:2, 28:2+3O
                    var totalCarbonString = chainString.Split(':')[0];
                    if (totalCarbonString.Contains("d"))
                    {
                        totalCarbonString = totalCarbonString.Replace("d", "");
                    }
                    if (totalCarbonString.Contains("t"))
                    {
                        totalCarbonString = totalCarbonString.Replace("t", "");
                    }
                    if (totalCarbonString.Contains("m"))
                    {
                        totalCarbonString = totalCarbonString.Replace("m", "");
                    }


                    //imagin 28:2+3O case
                    var bondString = nameString.Split(':')[1]; // 2+3O
                    var totalDoubleBondString = bondString.Split('+')[0]; // 2
                    var totalOxidizedString = "0";
                    if (bondString.Split('+').Length > 1)
                    {
                        totalOxidizedString = bondString.Split('+')[1]; //3O
                        totalOxidizedString = totalOxidizedString.Replace("O", "");//3
                    }

                    // Etheryzed case
                    if (totalDoubleBondString.Contains("e"))
                    {
                        totalDoubleBondString = totalDoubleBondString.Replace("e", "");
                    }


                    int totalCarbon = -1, totalDoubleBond = -1, totalOxidized;
                    int.TryParse(totalCarbonString, out totalCarbon);
                    int.TryParse(totalDoubleBondString, out totalDoubleBond);
                    int.TryParse(totalOxidizedString, out totalOxidized);

                    //if (totalCarbon <= 0 || totalDoubleBond < 0) continue;
                    var molecule = new LipidMolecule()
                    {
                        LipidName = lineArray[0],
                        SublevelLipidName = lineArray[0],
                        LipidClass = lipidClassEnum,
                        Adduct = adduct,
                        Mz = mzValue,
                        TotalChainString = chainString,
                        TotalCarbonCount = totalCarbon,
                        TotalDoubleBondCount = totalDoubleBond,
                        TotalOxidizedCount = totalOxidized
                    };
                    molecules.Add(molecule);
                }
            }
            return molecules.OrderBy(n => n.Mz).ToList();
        }

        private static LbmClass getLipidClassEnum(string lipidClass)
        {
            foreach (var lipid in System.Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>())
            {
                if (lipid.ToString() == lipidClass) { return lipid; }
            }
            return LbmClass.Undefined;
        }
    }
}
