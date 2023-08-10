using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics.Searcher;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.Lipidomics
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
        public static Rfx.Riken.OsakaUniv.RawData ReadTestSpectrum(string input)
        {
            return RawDataParcer.RawDataFileReader(input, new AnalysisParamOfMsfinder());
        }

        // ref molecules must be sorted by mz before using this program
        public static LipidMolecule Characterize(double queryMz, double queryRt,
            ObservableCollection<double[]> spectrum, List<LipidMolecule> RefMolecules, IonMode ionMode,
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
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylcholine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PE:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PS:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylserine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PG:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylglycerol(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylinositol(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.BMP:
                        result = LipidMsmsCharacterization.JudgeIfBismonoacylglycerophosphate(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LNAPE:
                        result = LipidMsmsCharacterization.JudgeIfNacylphosphatidylethanolamine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LNAPS:
                        result = LipidMsmsCharacterization.JudgeIfNacylphosphatidylserine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.SM:
                        result = LipidMsmsCharacterization.JudgeIfSphingomyelin(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.TG:
                        result = LipidMsmsCharacterization.JudgeIfTriacylglycerol(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.CAR:
                        result = LipidMsmsCharacterization.JudgeIfAcylcarnitine(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.CE:
                        result = LipidMsmsCharacterization.JudgeIfCholesterylEster(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;

                    // add MT, single or double chains pattern
                    case LbmClass.DG:
                        result = LipidMsmsCharacterization.JudgeIfDag(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.MG:
                        result = LipidMsmsCharacterization.JudgeIfMag(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.MGDG:
                        result = LipidMsmsCharacterization.JudgeIfMgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGDG:
                        result = LipidMsmsCharacterization.JudgeIfDgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PMeOH:
                        result = LipidMsmsCharacterization.JudgeIfPmeoh(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PEtOH:
                        result = LipidMsmsCharacterization.JudgeIfPetoh(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PBtOH:
                        result = LipidMsmsCharacterization.JudgeIfPbtoh(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.LPC:
                        result = LipidMsmsCharacterization.JudgeIfLysopc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPE:
                        result = LipidMsmsCharacterization.JudgeIfLysope(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PA:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidicacid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPA:
                        result = LipidMsmsCharacterization.JudgeIfLysopa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPG:
                        result = LipidMsmsCharacterization.JudgeIfLysopg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPI:
                        result = LipidMsmsCharacterization.JudgeIfLysopi(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPS:
                        result = LipidMsmsCharacterization.JudgeIfLysops(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPC:
                        result = LipidMsmsCharacterization.JudgeIfEtherpc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPE:
                        result = LipidMsmsCharacterization.JudgeIfEtherpe(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPC:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysopc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPE:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysope(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.OxPC:
                        result = LipidMsmsCharacterization.JudgeIfOxpc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPE:
                        result = LipidMsmsCharacterization.JudgeIfOxpe(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPG:
                        result = LipidMsmsCharacterization.JudgeIfOxpg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPI:
                        result = LipidMsmsCharacterization.JudgeIfOxpi(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.OxPS:
                        result = LipidMsmsCharacterization.JudgeIfOxps(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.EtherMGDG:
                        result = LipidMsmsCharacterization.JudgeIfEthermgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherDGDG:
                        result = LipidMsmsCharacterization.JudgeIfEtherdgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGTS:
                        result = LipidMsmsCharacterization.JudgeIfDgts(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LDGTS:
                        result = LipidMsmsCharacterization.JudgeIfLdgts(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGCC:
                        result = LipidMsmsCharacterization.JudgeIfDgcc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LDGCC:
                        result = LipidMsmsCharacterization.JudgeIfLdgcc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DGGA:
                        result = LipidMsmsCharacterization.JudgeIfGlcadg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SQDG:
                        result = LipidMsmsCharacterization.JudgeIfSqdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DLCL:
                        result = LipidMsmsCharacterization.JudgeIfDilysocardiolipin(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.FA:
                        result = LipidMsmsCharacterization.JudgeIfFattyacid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.OxFA:
                        result = LipidMsmsCharacterization.JudgeIfOxfattyacid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.FAHFA:
                        result = LipidMsmsCharacterization.JudgeIfFahfa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DMEDFAHFA:
                        result = LipidMsmsCharacterization.JudgeIfFahfaDMED(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DMEDFA:
                        result = LipidMsmsCharacterization.JudgeIfDmedFattyacid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DMEDOxFA:
                        result = LipidMsmsCharacterization.JudgeIfDmedOxfattyacid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.EtherOxPC:
                        result = LipidMsmsCharacterization.JudgeIfEtheroxpc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.EtherOxPE:
                        result = LipidMsmsCharacterization.JudgeIfEtheroxpe(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized, sn1MaxOxidized, sn2MaxOxidized);
                        break;
                    case LbmClass.Cer_NS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidens(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_NDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidends(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_NS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramidens(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_NDS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramidends(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Hex2Cer:
                        result = LipidMsmsCharacterization.JudgeIfHexhexceramidens(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Hex3Cer:
                        result = LipidMsmsCharacterization.JudgeIfHexhexhexceramidens(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_AP:
                        result = LipidMsmsCharacterization.JudgeIfCeramideap(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_AP:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideap(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_AS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideas(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_ADS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideads(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_BS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidebs(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_BDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidebds(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_NP:
                        result = LipidMsmsCharacterization.JudgeIfCeramidenp(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_OS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideos(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SHexCer:
                        result = LipidMsmsCharacterization.JudgeIfShexcer(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GM3:
                        result = LipidMsmsCharacterization.JudgeIfGm3(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.DHSph:
                        result = LipidMsmsCharacterization.JudgeIfSphinganine(spectrum, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;
                    case LbmClass.Sph:
                        result = LipidMsmsCharacterization.JudgeIfSphingosine(spectrum, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;
                    case LbmClass.PhytoSph:
                        result = LipidMsmsCharacterization.JudgeIfPhytosphingosine(spectrum, ms2tol, refMz,
                            molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);
                        break;

                    // add mikiko takahashi, hetero type chains- and 3 or 4 chains pattern
                    case LbmClass.ADGGA:
                        result = LipidMsmsCharacterization.JudgeIfAcylglcadg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.HBMP:
                        result = LipidMsmsCharacterization.JudgeIfHemiismonoacylglycerophosphate(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherTG:
                        result = LipidMsmsCharacterization.JudgeIfEthertag(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.CL:
                        result = LipidMsmsCharacterization.JudgeIfCardiolipin(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                             sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, adduct);
                        break;
                    case LbmClass.MLCL:
                        result = LipidMsmsCharacterization.JudgeIfLysocardiolipin(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EOS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideeos(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EODS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideeods(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.HexCer_EOS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideeos(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.ASM:
                        result = LipidMsmsCharacterization.JudgeIfAcylsm(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.Cer_EBDS:
                        result = LipidMsmsCharacterization.JudgeIfAcylcerbds(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.AHexCer:
                        result = LipidMsmsCharacterization.JudgeIfAcylhexceras(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.ASHexCer:
                        result = LipidMsmsCharacterization.JudgeIfAshexcer(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 10/04/19
                    case LbmClass.EtherPI:
                        result = LipidMsmsCharacterization.JudgeIfEtherpi(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherPS:
                        result = LipidMsmsCharacterization.JudgeIfEtherps(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI_Cer:
                        result = LipidMsmsCharacterization.JudgeIfPicermide(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.PE_Cer:
                        result = LipidMsmsCharacterization.JudgeIfPecermide(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;

                    //add 13/5/19 modified 20200218
                    case LbmClass.DCAE:
                        result = LipidMsmsCharacterization.JudgeIfDcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GDCAE:
                        result = LipidMsmsCharacterization.JudgeIfGdcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.GLCAE:
                        result = LipidMsmsCharacterization.JudgeIfGlcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.TDCAE:
                        result = LipidMsmsCharacterization.JudgeIfTdcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.TLCAE:
                        result = LipidMsmsCharacterization.JudgeIfTlcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.NAE:
                        result = LipidMsmsCharacterization.JudgeIfAnandamide(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.NAGly:
                        if (totalCarbon < 29)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylGlyOxFa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamidegly(spectrum, ms2tol, refMz,
                                 totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.NAGlySer:
                        if (totalCarbon < 29)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylGlySerOxFa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamideglyser(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.SL:
                        result = LipidMsmsCharacterization.JudgeIfSulfonolipid(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct, totalOxidized);
                        break;
                    case LbmClass.EtherPG:
                        result = LipidMsmsCharacterization.JudgeIfEtherpg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherLPG:
                        result = LipidMsmsCharacterization.JudgeIfEtherlysopg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.CoQ:
                        result = LipidMsmsCharacterization.JudgeIfCoenzymeq(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.Vitamin_E:
                        result = LipidMsmsCharacterization.JudgeIfVitaminEmolecules(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.Vitamin_D:
                        result = LipidMsmsCharacterization.JudgeIfVitaminDmolecules(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.VAE:
                        result = LipidMsmsCharacterization.JudgeIfVitaminaestermolecules(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.NAOrn:
                        if (totalCarbon < 29)
                        {
                            result = LipidMsmsCharacterization.JudgeIfNAcylOrnOxFa(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                        }
                        else
                        {
                            result = LipidMsmsCharacterization.JudgeIfFahfamideorn(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        }
                        break;

                    case LbmClass.BRSE:
                        result = LipidMsmsCharacterization.JudgeIfBrseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.CASE:
                        result = LipidMsmsCharacterization.JudgeIfCaseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.SISE:
                        result = LipidMsmsCharacterization.JudgeIfSiseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.STSE:
                        result = LipidMsmsCharacterization.JudgeIfStseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.AHexBRS:
                        result = LipidMsmsCharacterization.JudgeIfAhexbrseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexCAS:
                        result = LipidMsmsCharacterization.JudgeIfAhexcaseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexCS:
                        result = LipidMsmsCharacterization.JudgeIfAhexceSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexSIS:
                        result = LipidMsmsCharacterization.JudgeIfAhexsiseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.AHexSTS:
                        result = LipidMsmsCharacterization.JudgeIfAhexstseSpecies(spectrum, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                        break;

                    //add 190528
                    case LbmClass.Cer_HS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideo(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.Cer_HDS:
                        result = LipidMsmsCharacterization.JudgeIfCeramideo(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.Cer_NDOS:
                        result = LipidMsmsCharacterization.JudgeIfCeramidedos(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.HexCer_HS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideo(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.HexCer_HDS:
                        result = LipidMsmsCharacterization.JudgeIfHexceramideo(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //190801
                    case LbmClass.SHex:
                        result = LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.BAHex:
                        result = LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.SSulfate:
                        result = LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.BASulfate:
                        result = LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;

                    // added 190811
                    case LbmClass.CerP:
                        result = LipidMsmsCharacterization.JudgeIfCeramidePhosphate(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    ///2019/11/25 add
                    case LbmClass.SMGDG:
                        result = LipidMsmsCharacterization.JudgeIfSmgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.EtherSMGDG:
                        result = LipidMsmsCharacterization.JudgeIfEtherSmgdg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 20200218
                    case LbmClass.LCAE:
                        result = LipidMsmsCharacterization.JudgeIfLcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.KLCAE:
                        result = LipidMsmsCharacterization.JudgeIfKlcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    case LbmClass.KDCAE:
                        result = LipidMsmsCharacterization.JudgeIfKdcae(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct, totalOxidized);
                        break;

                    //add 20200714
                    case LbmClass.DMPE:
                        result = LipidMsmsCharacterization.JudgeIfDiMethylPE(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.MMPE:
                        result = LipidMsmsCharacterization.JudgeIfMonoMethylPE(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.MIPC:
                        result = LipidMsmsCharacterization.JudgeIfMipc(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    //add 20200720
                    case LbmClass.EGSE:
                        result = LipidMsmsCharacterization.JudgeIfErgoSESpecies(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.DEGSE:
                        result = LipidMsmsCharacterization.JudgeIfDehydroErgoSESpecies(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    //add 20200812
                    case LbmClass.OxTG:
                        result = LipidMsmsCharacterization.JudgeIfOxTriacylglycerol(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.TG_EST:
                        result = LipidMsmsCharacterization.JudgeIfFahfaTriacylglycerol(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond,
                            sn3MinCarbon, sn3MaxCarbon, sn3MinDbBond, sn3MaxDbBond, adduct);
                        break;
                    //add 20200923
                    case LbmClass.DSMSE:
                        result = LipidMsmsCharacterization.JudgeIfDesmosterolSpecies(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    //add20210216
                    case LbmClass.GPNAE:
                        result = LipidMsmsCharacterization.JudgeIfGpnae(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.MGMG:
                        result = LipidMsmsCharacterization.JudgeIfMgmg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.DGMG:
                        result = LipidMsmsCharacterization.JudgeIfDgmg(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, adduct);
                        break;
                    //add 20210315
                    case LbmClass.GD1a:
                        result = LipidMsmsCharacterization.JudgeIfGD1a(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD1b:
                        result = LipidMsmsCharacterization.JudgeIfGD1b(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD2:
                        result = LipidMsmsCharacterization.JudgeIfGD2(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GD3:
                        result = LipidMsmsCharacterization.JudgeIfGD3(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GM1:
                        result = LipidMsmsCharacterization.JudgeIfGM1(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GQ1b:
                        result = LipidMsmsCharacterization.JudgeIfGQ1b(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.GT1b:
                        result = LipidMsmsCharacterization.JudgeIfGT1b(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.NGcGM3:
                        result = LipidMsmsCharacterization.JudgeIfNGcGM3(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;

                    case LbmClass.ST:
                        result = LipidMsmsCharacterization.JudgeIfnoChainSterol(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;

                    case LbmClass.CSLPHex:
                    case LbmClass.BRSLPHex:
                    case LbmClass.CASLPHex:
                    case LbmClass.SISLPHex:
                    case LbmClass.STSLPHex:
                        result = LipidMsmsCharacterization.JudgeIfSteroidWithLpa(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;


                    case LbmClass.CSPHex:
                    case LbmClass.BRSPHex:
                    case LbmClass.CASPHex:
                    case LbmClass.SISPHex:
                    case LbmClass.STSPHex:
                        result = LipidMsmsCharacterization.JudgeIfSteroidWithPa(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz, totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20220201
                    case LbmClass.SPE:
                        result = LipidMsmsCharacterization.JudgeIfSpeSpecies(molecule.LipidName, molecule.LipidClass,
                            spectrum, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                        break;
                    //20220322
                    case LbmClass.NAPhe:
                        result = LipidMsmsCharacterization.JudgeIfNAcylPheFa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;
                    case LbmClass.NATau:
                        result = LipidMsmsCharacterization.JudgeIfNAcylTauFa(spectrum, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                        break;

                    //20221019
                    case LbmClass.PT:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylThreonine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20230407
                    case LbmClass.PC_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylcholineD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PE_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamineD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PS_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylserineD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PG_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylglycerolD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.PI_d5:
                        result = LipidMsmsCharacterization.JudgeIfPhosphatidylinositolD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPC_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopcD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPE_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopeD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPG_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopgD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPI_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopiD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.LPS_d5:
                        result = LipidMsmsCharacterization.JudgeIfLysopsD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.TG_d5:
                        result = LipidMsmsCharacterization.JudgeIfTriacylglycerolD5(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond,
                            sn2MinCarbon, sn2MaxCarbon, sn2MinDbBond, sn2MaxDbBond, adduct);
                        break;
                    case LbmClass.DG_d5:
                        result = LipidMsmsCharacterization.JudgeIfDagD5(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.SM_d9:
                        result = LipidMsmsCharacterization.JudgeIfSphingomyelinD9(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    case LbmClass.CE_d7:
                        result = LipidMsmsCharacterization.JudgeIfCholesterylEsterD7(spectrum, ms2tol, refMz,
                            totalCarbon, totalDbBond, adduct);
                        break;
                    case LbmClass.Cer_NS_d7:
                        result = LipidMsmsCharacterization.JudgeIfCeramidensD7(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
                        break;
                    //20230424
                    case LbmClass.bmPC:
                        result = LipidMsmsCharacterization.JudgeIfBetaMethylPhosphatidylcholine(spectrum, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1MinCarbon, sn1MaxCarbon, sn1MinDbBond, sn1MaxDbBond, adduct);
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
                    Console.WriteLine("candidate {0}, suggested {1}, score {2}", molecule.LipidName, result.LipidName, result.Score);
                }
                else
                {
                    Console.WriteLine("candidate {0}, suggested {1}, score {2}", molecule.LipidName, "NA", "-1");
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
        public static ObservableCollection<double[]> ConvertToRequiredSpectrumFormat(List<Peak> peaks)
        {
            var spectrum = new List<double[]>();
            var maxintensity = peaks.Max(n => n.Intensity);
            foreach (var peak in peaks)
            {
                spectrum.Add(new double[] { peak.Mz, peak.Intensity / maxintensity * 100.0 });
            }
            return new ObservableCollection<double[]>(spectrum.OrderByDescending(n => n[1]));
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
                    var adduct = AdductIonParcer.GetAdductIonBean(adductString);
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

                    // 42:1;2O case
                    totalDoubleBondString = bondString.Split(';')[0]; // 1
                    if (bondString.Split(';').Length > 1)
                    {
                        totalOxidizedString = bondString.Split(';')[1]; //2O
                        totalOxidizedString = totalOxidizedString.Replace("O", "");//2
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
            foreach (var lipid in Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>())
            {
                if (lipid.ToString() == lipidClass) { return lipid; }
            }
            return LbmClass.Undefined;
        }
    }
}
