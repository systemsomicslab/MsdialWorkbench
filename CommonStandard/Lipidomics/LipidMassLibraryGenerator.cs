using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.Lipidomics {
    public sealed class LipidMassLibraryGenerator {
        private LipidMassLibraryGenerator() { }

        public static void Integrate(string inputfolder, string output) {
            var files = System.IO.Directory.GetFiles(inputfolder, "*.txt", SearchOption.TopDirectoryOnly);
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Name\tMZ\tAdduct");
                foreach (var file in files) {
                    using (var sr = new StreamReader(file, Encoding.ASCII)) {
                        sr.ReadLine();
                        while (sr.Peek() > -1) {
                            sw.WriteLine(sr.ReadLine());
                        }
                    }
                }
            }
        }

        public static void Run(string outputfolder, LbmClass lipidclass, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount,
            int minDoubleBond, int maxDoubleBond, int maxOxygen) {

            switch (lipidclass) {
                case LbmClass.PC:
                    generatePcSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PE:
                    generatePeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PS:
                    generatePsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PI:
                    generatePiSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PG:
                    generatePgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.TG:
                    generateTagSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.SM:
                    generateSmSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LNAPE:
                    generateLnapeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                //add
                case LbmClass.DG:
                    generateDagSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.MG:
                    generateMagSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.FA:
                    generateFaSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.FAHFA:
                    generateFahfaSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPC:
                    generateLpcSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPE:
                    generateLpeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPG:
                    generateLpgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPI:
                    generateLpiSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPS:
                    generateLpsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LPA:
                    generateLpaSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PA:
                    generatePaSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.MGDG:
                    generateMgdgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.SQDG:
                    generateSqdgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.DGDG:
                    generateDgdgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.DGTS:
                    generateDgtsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LDGTS:
                    generateLdgtsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.HBMP:
                    generateHbmpSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.BMP:
                    generateBmpSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.CAR:
                    generateAcarSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.DGGA:
                    generateGlcadgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.ADGGA:
                    generateAcylglcadgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.CL:
                    generateClSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.CE:
                    generateCeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherPE:
                    generateEtherpeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherPC:
                    generateEtherpcSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                // ceramide
                case LbmClass.Cer_AP:
                    generateCerapSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_ADS:
                    generateCeradsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_AS:
                    generateCerasSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_NP:
                    generateCernpSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_NDS:
                    generateCerndsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_NS:
                    generateCernsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_BDS:
                    generateCerbdsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_BS:
                    generateCerbsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.HexCer_AP:
                    generateHexcerapSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.HexCer_NDS:
                    generateHexcerndsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.HexCer_NS:
                    generateHexcernsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_EODS:
                    generateCereodsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_EOS:
                    generateCereosSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.HexCer_EOS:
                    generateHexcereosSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                // OxPls
                case LbmClass.OxFA:
                    generateOxfaSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.OxPC:
                    generateOxpcSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.OxPE:
                    generateOxpeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.OxPG:
                    generateOxpgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.OxPI:
                    generateOxpiSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.OxPS:
                    generateOxpsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.EtherOxPC:
                    generateOxpcEtherSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.EtherOxPE:
                    generateOxpeEtherSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.PEtOH:
                    generatePetohSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PMeOH:
                    generatePmeohSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PBtOH:
                    generatePbtohSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.GM3:
                    generateGm3Species(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.LNAPS:
                    generateLnapsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherLPE:
                    generateEtherlpeSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherLPC:
                    generateEtherlpcSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Hex2Cer:
                    generateHexhexcernsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Hex3Cer:
                    generateHexhexhexcernsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Cer_EBDS:
                    generateAcylcerbdsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.AHexCer:
                    generateAcylhexcerasSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.ASM:
                    generateAcylsmSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;

                case LbmClass.Cer_OS:
                    generateCerosSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.MLCL:
                    generateLclSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.DLCL:
                    generateDlclSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PhytoSph:
                    generatePhytosphingosineSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.Sph:
                    generateSphingosineSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.DHSph:
                    generateSphinganineSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherMGDG:
                    generateEthermgdgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherDGDG:
                    generateEtherdgdgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherTG:
                    generateEthertagSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                //add 10/04/19
                case LbmClass.EtherPI:
                    generateEtherpiSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherPS:
                    generateEtherpsSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PE_Cer:
                    generatePetceramideSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    generatePedceramideSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                // add 13/05/19
                case LbmClass.DCAE:
                    generateDcaesSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.GDCAE:
                    generateGdcaesSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.GLCAE:
                    generateGlcaesSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.TDCAE:
                    generateTdcaesSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.TLCAE:
                    generateTlcaesSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.NAE:
                    generateAnandamideSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.NAGly:
                    generateFAHFAmideGlySpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.NAGlySer:
                    generateFAHFAmideGlySerSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.SL:
                    generateSulfonolipidSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.EtherPG:
                    generateEtherpgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.EtherLPG:
                    generateEtherlpgSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.PI_Cer:
                    generatePiceramideDihydroxySpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    generatePiceramideTrihydroxySpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    generatePiceramideOxDihydroxySpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen);
                    break;
                case LbmClass.SHexCer:
                    generateShexcerSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    generateShexcerOxSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;

                case LbmClass.CoQ:
                    generateCoenzymeqSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount);
                    break;

                case LbmClass.Vitamin_E:
                    generateVitaminESpecies(outputfolder, adduct);
                    generateVitaminDSpecies(outputfolder, adduct);
                    break;

                case LbmClass.VAE:
                    generateVitaminASpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;

                case LbmClass.NAOrn:
                    generateFAHFAmideOrnSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;

                case LbmClass.BRSE:
                    generateBrseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.CASE:
                    generateCaseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.SISE:
                    generateSiseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.STSE:
                    generateStseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;

                case LbmClass.AHexBRS:
                    generateAhexbrseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.AHexCAS:
                    generateAhexcaseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.AHexCS:
                    generateAhexceSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.AHexSIS:
                    generateAhexsiseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;
                case LbmClass.AHexSTS:
                    generateAhexstseSpecies(outputfolder, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
                    break;


            }
        }

        
        private static void generatePsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12NO8P");
            commonGlycerolipidsGenerator(filepath, "PS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }

        private static void generatePeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H12NO6P");
            commonGlycerolipidsGenerator(filepath, "PE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }

        private static void generatePcSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H18NO6P");
            commonGlycerolipidsGenerator(filepath, "PC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }

        private static void generatePiSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PI" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H17O11P");
            commonGlycerolipidsGenerator(filepath, "PI", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }

        private static void generatePgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H13O8P");
            commonGlycerolipidsGenerator(filepath, "PG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }

        private static void generateTagSpecies(string outputfolder, AdductIon adduct, 
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "TAG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H5O3");
            commonGlycerolipidsGenerator(filepath, "TAG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 3);
        }

        
        private static void generateLnapeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond) {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LNAPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H12NO6P");
            commonGlycerolipidsGenerator(filepath, "LNAPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        //add
        private static void generateDagSpecies(string outputfolder, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "DAG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H6O3");
            commonGlycerolipidsGenerator(filepath, "DAG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateMagSpecies(string outputfolder, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "MAG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H7O3");
            commonGlycerolipidsGenerator(filepath, "MAG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateFaSpecies(string outputfolder, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "FA" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("HO");
            commonGlycerolipidsGenerator(filepath, "FA", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateFahfaSpecies(string outputfolder, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "FAHFA" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("HO");
            fahfaGenerator(filepath, "FAHFA", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
        }

        private static void generateLpcSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H19NO6P");
            commonGlycerolipidsGenerator(filepath, "LPC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateLpeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H13NO6P");
            commonGlycerolipidsGenerator(filepath, "LPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateLpgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H14O8P");
            commonGlycerolipidsGenerator(filepath, "LPG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateLpiSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPI" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H18O11P");
            commonGlycerolipidsGenerator(filepath, "LPI", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateLpsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H13NO8P");
            commonGlycerolipidsGenerator(filepath, "LPS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateLpaSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LPA" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H8O6P");
            commonGlycerolipidsGenerator(filepath, "LPA", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generatePaSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PA" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H7O6P");
            commonGlycerolipidsGenerator(filepath, "PA", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateMgdgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "MGDG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H16O8");
            commonGlycerolipidsGenerator(filepath, "MGDG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateSqdgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "SQDG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H16O10S");
            commonGlycerolipidsGenerator(filepath, "SQDG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateDgdgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "DGDG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C15H26O13");
            commonGlycerolipidsGenerator(filepath, "DGDG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateDgtsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "DGTS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C10H19NO5");
            commonGlycerolipidsGenerator(filepath, "DGTS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateLdgtsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LDGTS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C10H20NO5");
            commonGlycerolipidsGenerator(filepath, "LDGTS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateHbmpSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HBMP" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12O8P");
            commonGlycerolipidsGenerator(filepath, "HBMP", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 3);
        }
        private static void generateBmpSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "BMP" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H13O8P");
            commonGlycerolipidsGenerator(filepath, "BMP", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateAcarSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "ACar" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C7H15O3N");
            commonGlycerolipidsGenerator(filepath, "ACar", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateGlcadgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "GlcADG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H14O9");
            commonGlycerolipidsGenerator(filepath, "GlcADG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateAcylglcadgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AcylGlcADG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H13O9");
            commonGlycerolipidsGenerator(filepath, "AcylGlcADG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 3);
        }
        private static void generateClSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "CL" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H18O13P2");
            commonGlycerolipidsGenerator(filepath, "CL", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 4);
        }
        private static void generateCeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "CE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C27H45O");
            commonGlycerolipidsGenerator(filepath, "CE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateEtherpeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H12NO6P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2, 1);
        }

        private static void generateEtherpcSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherPC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H18NO6P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherPC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2, 1);
        }
        // ceramide
        private static void generateSmSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "SM" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H13NO3P");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "SM", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        //add MT
        private static void generateCerapSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_AP" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 3;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_AP", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCeradsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_ADS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_ADS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCerasSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_AS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_AS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCernpSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_NP" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 3;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "Cer_NP", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCerndsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_NDS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "Cer_NDS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCernsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_NS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "Cer_NS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCerbdsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_BDS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_BDS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCerbsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_BS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_BS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateHexcerapSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexCer_AP" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O5");
            var sphingoHydroxyCount = 3;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "HexCer_AP", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateHexcerndsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexCer_NDS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O5");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "HexCer_NDS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateHexcernsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexCer_NS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O5");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "HexCer_NS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCereodsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_EODS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideEsterGenerator(filepath, "Cer_EODS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCereosSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_EOS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideEsterGenerator(filepath, "Cer_EOS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateHexcereosSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexCer_EOS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O5");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideEsterGenerator(filepath, "HexCer_EOS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }

        //
        private static void generateOxfaSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxFA" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("HO");
            var acylCount = 1;
            commonGlycerolipidsOxGenerator(filepath, "OxFA", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpcSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxPC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H18NO6P");
            var acylCount = 2;
            commonGlycerolipidsOxGenerator(filepath, "OxPC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H12NO6P");
            var acylCount = 2;
            commonGlycerolipidsOxGenerator(filepath, "OxPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxPG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H13O8P");
            var acylCount = 2;
            commonGlycerolipidsOxGenerator(filepath, "OxPG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpiSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxPI" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H17O11P");
            var acylCount = 2;
            commonGlycerolipidsOxGenerator(filepath, "OxPI", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "OxPS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12NO8P");
            var acylCount = 2;
            commonGlycerolipidsOxGenerator(filepath, "OxPS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount);
        }
        private static void generateOxpcEtherSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherOxPC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H18NO6P");
            var acylCount = 2;
            var etherCount = 1;
            commonGlycerolipidsOxEtherGenerator(filepath, "EtherOxPC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount, etherCount);
        }
        private static void generateOxpeEtherSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherOxPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H12NO6P");
            var acylCount = 2;
            var etherCount = 1;
            commonGlycerolipidsOxEtherGenerator(filepath, "EtherOxPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, maxOxygen, acylCount, etherCount);
        }
        //others
        private static void generatePetohSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PEtOH" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H11O6P");
            commonGlycerolipidsGenerator(filepath, "PEtOH", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generatePmeohSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PMeOH" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C4H9O6P");
            commonGlycerolipidsGenerator(filepath, "PMeOH", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generatePbtohSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PBtOH" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C7H15O6P");
            commonGlycerolipidsGenerator(filepath, "PBtOH", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateGm3Species(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "GM3" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C23H38NO18");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "GM3", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateShexcerSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "SHexCer" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O8S");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "SHexCer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateLnapsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LNAPS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12NO8P");
            commonGlycerolipidsGenerator(filepath, "LNAPS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generateEtherlpeSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherLPE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H13NO6P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherLPE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1, 1);
        }

        private static void generateEtherlpcSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherLPC" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C8H19NO6P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherLPC", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1, 1);
        }
        private static void generateHexhexcernsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexHexCer_NS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C12H21O10");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "HexHexCer_NS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateHexhexhexcernsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "HexHexHexCer_NS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C18H31O15");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "HexHexHexCer_NS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateAcylcerbdsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AcylCer_BDS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideEsterGenerator(filepath, "AcylCer_BDS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateAcylhexcerasSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AcylHexCer" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O5");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideEsterGenerator(filepath, "AcylHexCer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateAcylsmSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AcylSM" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H13NO3P");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideEsterGenerator(filepath, "AcylSM", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCerosSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Cer_OS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideGenerator(filepath, "Cer_OS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateLclSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "LCL" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H19O13P2");
            commonGlycerolipidsGenerator(filepath, "LCL", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 3);
        }
        private static void generateDlclSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "DLCL" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H20O13P2");
            commonGlycerolipidsGenerator(filepath, "DLCL", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2);
        }
        private static void generatePhytosphingosineSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Phytosphingosine" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var hydroxyCount = 3;
            commonSphingosineGenerator(filepath, "Phytosphingosine", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, hydroxyCount);
        }
        private static void generateSphingosineSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Sphingosine" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var hydroxyCount = 2;
            commonSphingosineGenerator(filepath, "Sphingosine", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, hydroxyCount);
        }
        private static void generateSphinganineSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Sphinganine" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("H");
            var hydroxyCount = 2;
            commonSphingosineGenerator(filepath, "Sphinganine", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, hydroxyCount);
        }
        private static void generateEthermgdgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherMGDG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H16O8");
            commonGlycerolipidsEtherGenerator(filepath, "EtherMGDG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2, 1);
        }
        private static void generateEtherdgdgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherDGDG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C15H26O13");
            commonGlycerolipidsEtherGenerator(filepath, "EtherDGDG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2, 1);
        }

        private static void generateEthertagSpecies(string outputfolder, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherTAG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C3H5O3");
            commonGlycerolipidsEtherGenerator(filepath, "EtherTAG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 3,1);
        }

        // add 10/04/19
        private static void generateEtherpiSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherPI" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H17O11P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherPI", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2,1);
        }
        private static void generateEtherpsSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherPS" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12NO8P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherPS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2,1);
        }
        private static void generatePetceramideSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PE-Cer(t)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C2H7NO3P");
            var sphingoHydroxyCount = 3;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "PE_Cer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generatePedceramideSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PE-Cer(d)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C2H7NO3P");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "PE_Cer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }

        //// add 13/05/19
        private static void generateDcaesSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "DCAE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C24H39O4");
            commonGlycerolipidsGenerator(filepath, "DCAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateGdcaesSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "GDCAE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C26H42NO5");
            commonGlycerolipidsGenerator(filepath, "GDCAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateGlcaesSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "GLCAE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C26H42NO4");
            commonGlycerolipidsGenerator(filepath, "GLCAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateTdcaesSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "TDCAE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C26H44NO6S");
            commonGlycerolipidsGenerator(filepath, "TDCAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateTlcaesSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "TLCAE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C26H44NO5S");
            commonGlycerolipidsGenerator(filepath, "TLCAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateAnandamideSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Anandamide" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C2H6NO");
            commonGlycerolipidsGenerator(filepath, "NAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateFAHFAmideGlySpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "FAHFAmide(Gly)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C2H4NO2");
            fahfaGenerator(filepath, "NAAG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
        }

        private static void generateFAHFAmideGlySerSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "FAHFAmide(GlySer)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H9N2O4");
            fahfaGenerator(filepath, "NAAGS", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
        }

        private static void generateSulfonolipidSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond,int oxygenCount)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "Sulfonolipid" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("SO3H");
            commonSulfonolipidGenerator(filepath, "SL", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, oxygenCount);
        }

        private static void generateEtherpgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherPG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H13O8P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherPG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2,1);
        }

        private static void generateEtherlpgSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "EtherLPG" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H15O7P");
            commonGlycerolipidsEtherGenerator(filepath, "EtherLPG", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 2, 1);
        }

        private static void generatePiceramideDihydroxySpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxydized)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PI-Cer(d)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12O8P");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "PI_Cer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generatePiceramideTrihydroxySpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxydized)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PI-Cer(t)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12O8P");
            var sphingoHydroxyCount = 3;
            var acylHydroxyCount = 0;
            commonCeramideGenerator(filepath, "PI_Cer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generatePiceramideOxDihydroxySpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxydized)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "PI-Cer(d_O)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H12O8P");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideOxGenerator(filepath, "PI_Cer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateShexcerOxSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "SHexCerO" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C6H11O8S");
            var sphingoHydroxyCount = 2;
            var acylHydroxyCount = 1;
            commonCeramideOxGenerator(filepath, "SHexCer", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, sphingoHydroxyCount, acylHydroxyCount);
        }
        private static void generateCoenzymeqSpecies(string outputfolder, AdductIon adduct, int minRepeatCount, int maxRepeatCount)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "CoQ" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C9H10O4");
            var additionalFormula = FormulaStringParcer.OrganicElementsReader("C5H8");
            commonCoenzymeqlipidsGenerator(filepath, "CoQ", headerFormula, adduct, minRepeatCount, maxRepeatCount, additionalFormula);
        }

        private static void generateVitaminESpecies(string outputfolder, AdductIon adduct)
        {
            var adductString = adduct.AdductIonName;
            if (adductString.Substring(adductString.Length - 1, 1) == "-")
            {
                var filepath = outputfolder + "\\" + "VitaminE" + "_" + adductString + ".txt";
                var formula = FormulaStringParcer.OrganicElementsReader("C29H50O2");
                commonSinglemoleculeGenerator(filepath, "Vitamin", formula, adduct);
            }
        }
        private static void generateVitaminDSpecies(string outputfolder, AdductIon adduct)
        {
            var adductString = adduct.AdductIonName;
            if (adductString == "[M+H]+")
            {
                var filepath = outputfolder + "\\" + "VitaminD" + "_" + adductString + ".txt";
                var formula = FormulaStringParcer.OrganicElementsReader("C27H44O2");
                commonSinglemoleculeGenerator(filepath, "Vitamin", formula, adduct);
            }
        }
        private static void generateVitaminASpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            if (adductString.Substring(adductString.Length - 1, 1) == "+")
            {
                var filepath = outputfolder + "\\" + "VitaminA" + "_" + adductString + ".txt";
                var headerFormula = FormulaStringParcer.OrganicElementsReader("C20H29O");
                commonGlycerolipidsGenerator(filepath, "VAE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
            }
        }

        private static void generateFAHFAmideOrnSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "FAHFAmide(Orn)" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C5H11N2O2");
            fahfaGenerator(filepath, "NAAO", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond);
        }

        private static void generateBrseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "BRSE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C28H45O");
            commonGlycerolipidsGenerator(filepath, "BRSE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateCaseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "CASE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C28H47O");
            commonGlycerolipidsGenerator(filepath, "CASE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateSiseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "SISE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C29H49O");
            commonGlycerolipidsGenerator(filepath, "SISE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateStseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "STSE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C29H47O");
            commonGlycerolipidsGenerator(filepath, "STSE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateAhexbrseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AHexBRSE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C34H55O6");
            commonGlycerolipidsGenerator(filepath, "AHexBRSE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }

        private static void generateAhexcaseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AHexCASE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C34H57O6");
            commonGlycerolipidsGenerator(filepath, "AHexCASE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateAhexceSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AHexCE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C33H55O6");
            commonGlycerolipidsGenerator(filepath, "AHexCE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateAhexsiseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AHexSISE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C35H59O6");
            commonGlycerolipidsGenerator(filepath, "AHexSISE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }
        private static void generateAhexstseSpecies(string outputfolder, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            var adductString = adduct.AdductIonName;
            var filepath = outputfolder + "\\" + "AHexSTSE" + "_" + adductString + ".txt";
            var headerFormula = FormulaStringParcer.OrganicElementsReader("C35H57O6");
            commonGlycerolipidsGenerator(filepath, "AHexSTSE", headerFormula, adduct, minCarbonCount, maxCarbonCount, minDoubleBond, maxDoubleBond, 1);
        }



        private static void commonGlycerolipidsOxGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen, int acylCount)
        {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        for (int k = 1; k <= maxOxygen; k++)
                        {
                            
                            if (j + 1 < k)
                                continue;
                            //if (K + j > maxDoubleBond)        // extra Hydroxy + Doublebond < maxDoubleBond 
                            //    continue;

                            if (isPracticalDoubleBondSize(i, j))
                            {

                                var totalChainCarbon = i;
                                var totalChainDoubleBond = j;
                                var totalChainHydrogen = totalChainCarbon * 2 - acylCount - totalChainDoubleBond * 2;
                                var totalChainOxygen = acylCount + k ;

                                var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                    headerFormula.Nnum, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                                var mz = adduct.ConvertToMz(totalFormula.Mass);
                                var lipidname = classString + " " + totalChainCarbon + ":" + totalChainDoubleBond + "+" + k +"O";

                                sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                            }
                        }

                        
                    }
                }
            }
        }
        private static void commonGlycerolipidsOxEtherGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int maxOxygen, int acylCount, int etherCount)
        {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        for (int k = 1; k <= maxOxygen; k++)
                        {

                            if (j + 1 < k)
                                continue;
                            //if (K + j > maxDoubleBond)        // extra Hydroxy + Doublebond < maxDoubleBond 
                            //    continue;

                            if (isPracticalDoubleBondSize(i, j))
                            {

                                var totalChainCarbon = i;
                                var totalChainDoubleBond = j;
                                var totalChainHydrogen = totalChainCarbon * 2 - acylCount - totalChainDoubleBond * 2 + etherCount * 2;
                                var totalChainOxygen = acylCount + k -1 ;

                                var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                    headerFormula.Nnum, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                                var mz = adduct.ConvertToMz(totalFormula.Mass);
                                var lipidname = classString + " " + totalChainCarbon + ":" + totalChainDoubleBond + "e+" + k + "O";

                                sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                            }
                        }


                    }
                }
            }
        }


        private static void fahfaGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond)
        {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        if (isPracticalDoubleBondSize(i, j))
                        {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - 2 * totalChainDoubleBond - 3; // scafold FA
                            var totalChainOxygen = 3;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + totalChainCarbon + ":" + totalChainDoubleBond;
                            

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }


        private static void commonCeramideGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int sphingoHydroxyCount, int acylHydroxyCount) {

            var hydroHeader = "d";
            if (sphingoHydroxyCount == 3) hydroHeader = "t";
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++) {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++) {
                        if (isPracticalDoubleBondSize(i, j)) {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - totalChainDoubleBond * 2;
                            var totalChainOxygen = 1 + sphingoHydroxyCount + acylHydroxyCount;
                            var totalNitrogenCount = 1;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum + totalNitrogenCount, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond;

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }

        private static void commonGlycerolipidsGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int acylCount) {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++) {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++) {
                        if (isPracticalDoubleBondSize(i, j)) {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - acylCount - totalChainDoubleBond * 2;
                            var totalChainOxygen = acylCount;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + totalChainCarbon + ":" + totalChainDoubleBond;

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }
        //add MT
        private static void commonGlycerolipidsEtherGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int acylCount, int etherCount)
        {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        if (isPracticalDoubleBondSize(i, j))
                        {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - acylCount - totalChainDoubleBond * 2 + etherCount*2;
                            var totalChainOxygen = acylCount-1;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + totalChainCarbon + ":" + totalChainDoubleBond + "e";

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }
        private static void commonCeramideEsterGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct, int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int sphingoHydroxyCount, int acylHydroxyCount)
        {
            var hydroHeader = "d";
            if (sphingoHydroxyCount == 3) hydroHeader = "t";
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        if (isPracticalDoubleBondSize(i, j))
                        {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - totalChainDoubleBond * 2 - 2;
                            var totalChainOxygen = 2 + sphingoHydroxyCount + acylHydroxyCount;
                            var totalNitrogenCount = 1;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum + totalNitrogenCount, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond;

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }
        private static void commonSphingosineGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int HydroxyCount)
        {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        if (isPracticalDoubleBondSize(i, j))
                        {

                            var totalChainCarbon = i;
                            var totalChainDoubleBond = j;
                            var totalChainHydrogen = totalChainCarbon * 2 - totalChainDoubleBond * 2 + 2 ;
                            var totalChainOxygen = HydroxyCount;
                            var totalNitrogenCount = 1;

                            var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                headerFormula.Nnum + totalNitrogenCount, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString + " " + totalChainCarbon.ToString() + ":" + totalChainDoubleBond;

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                        }
                    }
                }
            }
        }

        private static void commonSulfonolipidGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
    int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int acylHydroxyCount)
        {

            var hydroHeader = "m";
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        for (int k = 0; k <= acylHydroxyCount; k++)
                        {
                            if (isPracticalDoubleBondSize(i, j))
                            {
                                var totalChainCarbon = i;
                                var totalChainDoubleBond = j;
                                var totalChainHydrogen = totalChainCarbon * 2 - totalChainDoubleBond * 2;
                                var totalChainOxygen = 1 + 1 + k;
                                var totalNitrogenCount = 1;

                                var totalString = string.Empty;

                                totalString = k == 0 ?
                                    hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond :
                                    hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond + "+" + k + "O";

                                var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                    headerFormula.Nnum + totalNitrogenCount, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                                var mz = adduct.ConvertToMz(totalFormula.Mass);
                                var lipidname = classString + " " + totalString;

                                sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                            }
                        }
                    }
                }
            }
        }

        private static void commonCeramideOxGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
            int minCarbonCount, int maxCarbonCount, int minDoubleBond, int maxDoubleBond, int sphingoHydroxyCount, int acylHydroxyCount)
        {

            var hydroHeader = "d";
            if (sphingoHydroxyCount == 3) hydroHeader = "t";
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minCarbonCount; i <= maxCarbonCount; i++)
                {
                    for (int j = minDoubleBond; j <= maxDoubleBond; j++)
                    {
                        for (int k = 1; k <= acylHydroxyCount; k++)
                        {

                            if (isPracticalDoubleBondSize(i, j))
                            {
                                var totalChainCarbon = i;
                                var totalChainDoubleBond = j;
                                var totalChainHydrogen = totalChainCarbon * 2 - totalChainDoubleBond * 2;
                                var totalChainOxygen = 1 + sphingoHydroxyCount + k;
                                var totalNitrogenCount = 1;

                                var totalString = string.Empty;

                                totalString = k == 1 ?
                                    hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond + "+" + "O" :
                                    hydroHeader + totalChainCarbon.ToString() + ":" + totalChainDoubleBond + "+" + k + "O";

                                var totalFormula = new Formula(headerFormula.Cnum + totalChainCarbon, headerFormula.Hnum + totalChainHydrogen,
                                    headerFormula.Nnum + totalNitrogenCount, headerFormula.Onum + totalChainOxygen, headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                                var mz = adduct.ConvertToMz(totalFormula.Mass);
                                var lipidname = classString + " " + totalString;

                                sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                            }
                        }
                    }
                }
            }
        }

        private static void commonCoenzymeqlipidsGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct,
    int minRepeatCount, int maxRepeatCount, Formula additionalFormula)
        {

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                for (int i = minRepeatCount; i <= maxRepeatCount; i++)
                {
                    var totalCarbon = headerFormula.Cnum + additionalFormula.Cnum * i;
                    var totalHydrogen = headerFormula.Hnum + additionalFormula.Hnum * i;
                    var totalOxygen = headerFormula.Onum + additionalFormula.Onum* i;
                    var totalNitrogen = headerFormula.Nnum + additionalFormula.Nnum * i;

                    var totalFormula = new Formula(totalCarbon, totalHydrogen, totalNitrogen, totalOxygen
                               , headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                            var mz = adduct.ConvertToMz(totalFormula.Mass);
                            var lipidname = classString;

                            sw.WriteLine(lipidname + "\t" + mz + "\t" + adduct.AdductIonName);
                   
                }
            }
        }

        private static void commonSinglemoleculeGenerator(string filepath, string classString, Formula headerFormula, AdductIon adduct)
        {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                writeHeader(sw);
                    var totalCarbon = headerFormula.Cnum;
                    var totalHydrogen = headerFormula.Hnum;
                    var totalOxygen = headerFormula.Onum;
                    var totalNitrogen = headerFormula.Nnum;

                    var totalFormula = new Formula(totalCarbon, totalHydrogen, totalNitrogen, totalOxygen
                               , headerFormula.Pnum, headerFormula.Snum, 0, 0, 0, 0, 0);
                    var mz = adduct.ConvertToMz(totalFormula.Mass);
                    var lipidname = classString;

                    sw.WriteLine(lipidname + " " + "\t" + mz + "\t" + adduct.AdductIonName);

            }
        }


        private static void writeHeader(StreamWriter sw) {
            sw.WriteLine("Name\tMz\tAdduct");
        }

        private static bool isPracticalDoubleBondSize(int carbon, int doublebond) {
            if (doublebond == 0) return true;
            else if ((double)carbon / (double)doublebond < 3.5) return false;
            else return true;
        }
    }
}
