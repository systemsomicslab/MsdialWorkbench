using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring {
   
    public class MatchedPeak {
        public bool IsProductIonMatched { get; set; } = false;
        public bool IsNeutralLossMatched { get; set; } = false;
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public double MatchedIntensity { get; set; }
    }
    public sealed class MsScanMatching {
        private MsScanMatching() { }

        private static bool IsComparedAvailable<T>(IReadOnlyCollection<T> obj1, IReadOnlyCollection<T> obj2) {
            if (obj1 == null || obj2 == null || obj1.Count == 0 || obj2.Count == 0) return false;
            return true;
        }

        private static bool IsComparedAvailable(IMSScanProperty obj1, IMSScanProperty obj2) {
            if (obj1.Spectrum == null || obj2.Spectrum == null || obj1.Spectrum.Count == 0 || obj2.Spectrum.Count == 0) return false;
            return true;
        }

        public static double[] GetEieioBasedLipidomicsMatchedPeaksScores(IMSScanProperty scan, MoleculeMsReference reference, 
            float tolerance, float mzBegin, float mzEnd) {

            var returnedObj = GetEieioBasedLipidMoleculeAnnotationResult(scan, reference, tolerance, mzBegin, mzEnd);
            return returnedObj.Item2;
        }

        public static (ILipid, double[]) GetEieioBasedLipidMoleculeAnnotationResult(IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {
            var lipid = FacadeLipidParser.Default.Parse(reference.Name);
            switch (lipid.LipidClass) {
                case LbmClass.PC:
                    return PCEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PE:
                    return PEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PS:
                    return PSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PG:
                    return PGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PI:
                    return PIEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PA:
                    return PAEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DG:
                    return DGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.BMP:
                    return BMPEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPC:
                    return LPCEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPS:
                    return LPSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPE:
                    return LPEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPG:
                    return LPGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPI:
                    return LPIEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DGTA:
                    return DGTAEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DGTS:
                    return DGTSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LDGTA:
                    return LDGTAEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LDGTS:
                    return LDGTSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.SM:
                    return SMEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.Cer_NS:
                case LbmClass.Cer_NDS:
                case LbmClass.Cer_AS:
                case LbmClass.Cer_ADS:
                case LbmClass.Cer_BS:
                case LbmClass.Cer_BDS:
                case LbmClass.Cer_NP:
                case LbmClass.Cer_AP:
                case LbmClass.Cer_ABP:
                    return CeramideEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.HexCer_NS:
                    return HexCerEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.Hex2Cer:
                    return Hex2CerEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.HBMP:
                    return HBMPEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.TG:
                    return TGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherPC:
                    return EtherPCEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherPE:

                    return EtherPEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.SHexCer:
                    return SHexCerEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.GM3:
                    return GM3EadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.CE:
                    return CEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.MG:
                    return MGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.CAR:
                    return CAREadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.DMEDFAHFA:
                    return DMEDFAHFAEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DMEDFA:
                case LbmClass.DMEDOxFA:
                    return DMEDFAEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.PC_d5:
                    return PCEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PE_d5:
                    return PEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PS_d5:
                    return PSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PG_d5:
                    return PGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PI_d5:
                    return PIEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.Cer_NS_d7:
                    return CeramideEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.SM_d9:
                    return SMEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.TG_d5:
                    return TGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.CE_d7:
                    return CEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DG_d5:
                    return DGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPC_d5:
                    return LPCEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPS_d5:
                    return LPSEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPE_d5:
                    return LPEEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPG_d5:
                    return LPGEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPI_d5:
                    return LPIEadMsCharacterization.Characterize(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);


                default: return (null, new double[2] { 0.0, 0.0 });
            }
        }

        public static double[] GetEidBasedLipidomicsMatchedPeaksScores(IMSScanProperty scan, MoleculeMsReference reference,
           float tolerance, float mzBegin, float mzEnd) {

            var returnedObj = GetEidBasedLipidMoleculeAnnotationResult(scan, reference, tolerance, mzBegin, mzEnd);
            return returnedObj.Item2;
        }

        public static (ILipid, double[]) GetEidBasedLipidMoleculeAnnotationResult(IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {
            var lipid = FacadeLipidParser.Default.Parse(reference.Name);
            switch (lipid.LipidClass) {
                case LbmClass.PC:
                case LbmClass.PE:
                case LbmClass.PS:
                case LbmClass.PG:
                case LbmClass.PI:
                case LbmClass.PA:
                case LbmClass.DG:
                case LbmClass.BMP:
                case LbmClass.LPC:
                case LbmClass.LPS:
                case LbmClass.LPE:
                case LbmClass.LPG:
                case LbmClass.LPI:
                case LbmClass.DGTA:
                case LbmClass.DGTS:
                case LbmClass.LDGTA:
                case LbmClass.LDGTS:
                case LbmClass.DMEDFAHFA:
                case LbmClass.PC_d5:
                case LbmClass.PE_d5:
                case LbmClass.PS_d5:
                case LbmClass.PG_d5:
                case LbmClass.PI_d5:
                case LbmClass.LPC_d5:
                case LbmClass.LPE_d5:
                case LbmClass.LPS_d5:
                case LbmClass.LPG_d5:
                case LbmClass.LPI_d5:
                case LbmClass.DG_d5:
                    return EidDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.MG:
                    return EidDefaultCharacterization.Characterize4MonoacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.CAR:
                case LbmClass.DMEDFA:
                case LbmClass.DMEDOxFA:
                case LbmClass.CE:
                case LbmClass.CE_d7:
                    return EidDefaultCharacterization.Characterize4SingleAcylChain(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.SM:
                case LbmClass.Cer_NS:
                case LbmClass.Cer_NDS:
                case LbmClass.Cer_AS:
                case LbmClass.Cer_ADS:
                case LbmClass.Cer_BS:
                case LbmClass.Cer_BDS:
                case LbmClass.Cer_NP:
                case LbmClass.Cer_AP:
                case LbmClass.HexCer_NS:
                case LbmClass.SHexCer:
                case LbmClass.GM3:
                case LbmClass.SM_d9:
                case LbmClass.Cer_NS_d7:
                    return EidDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.HBMP:
                case LbmClass.TG:
                case LbmClass.TG_d5:
                    return EidDefaultCharacterization.Characterize4TriacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherPC:
                case LbmClass.EtherPE:
                    return EidDefaultCharacterization.Characterize4AlkylAcylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                default: return (null, new double[2] { 0.0, 0.0 });
            }
        }


        public static double[] GetOadBasedLipidomicsMatchedPeaksScores(IMSScanProperty scan, MoleculeMsReference reference,
           float tolerance, float mzBegin, float mzEnd) {

            var returnedObj = GetOadBasedLipidMoleculeAnnotationResult(scan, reference, tolerance, mzBegin, mzEnd);
            return returnedObj.Item2;
        }

        public static (ILipid, double[]) GetOadBasedLipidMoleculeAnnotationResult(IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {
            var lipid = FacadeLipidParser.Default.Parse(reference.Name);
            switch (lipid.LipidClass) {
                case LbmClass.PC:
                case LbmClass.PE:
                case LbmClass.PS:
                case LbmClass.PG:
                case LbmClass.PI:
                case LbmClass.PA:
                case LbmClass.DG:
                case LbmClass.BMP:
                case LbmClass.LPC:
                case LbmClass.LPS:
                case LbmClass.LPE:
                case LbmClass.LPG:
                case LbmClass.LPI:
                case LbmClass.DGTA:
                case LbmClass.DGTS:
                case LbmClass.LDGTA:
                case LbmClass.LDGTS:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.SM:
                case LbmClass.Cer_NS:
                case LbmClass.Cer_NDS:
                case LbmClass.Cer_AS:
                case LbmClass.Cer_ADS:
                case LbmClass.Cer_BS:
                case LbmClass.Cer_BDS:
                case LbmClass.Cer_NP:
                case LbmClass.Cer_AP:
                case LbmClass.HexCer_NS:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.HBMP:
                case LbmClass.TG:
                    return OadDefaultCharacterization.Characterize4TriacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherPC:
                case LbmClass.EtherPE:
                    return OadDefaultCharacterization.Characterize4AlkylAcylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherLPC:
                    return OadDefaultCharacterization.Characterize4AlkylAcylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.EtherLPE:
                    return OadDefaultCharacterization.Characterize4AlkylAcylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.SHexCer:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.GM3:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.MG:
                    return OadDefaultCharacterization.Characterize4SingleAcylChainLiipid(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.CAR:
                case LbmClass.DMEDFA:
                case LbmClass.DMEDOxFA:
                    return OadDefaultCharacterization.Characterize4SingleAcylChainLiipid(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DMEDFAHFA:
                    return OadDefaultCharacterization.Characterize4Fahfa(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                case LbmClass.PC_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PE_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PS_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PG_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.PI_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.Cer_NS_d7:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.SM_d9:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.TG_d5:
                    return OadDefaultCharacterization.Characterize4TriacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.CE_d7:
                    return OadDefaultCharacterization.Characterize4Ceramides(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.DG_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPC_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPS_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPE_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPG_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);
                case LbmClass.LPI_d5:
                    return OadDefaultCharacterization.Characterize4DiacylGlycerols(scan, (Lipid)lipid, reference, tolerance, mzBegin, mzEnd);

                default: return (null, new double[2] { 0.0, 0.0 });
            }
        }



        //public static MsScanMatchResult CompareIMMS2ScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, 
        //    MsRefSearchParameterBase param, double scanCCS, List<IsotopicPeak> scanIsotopes = null, List<IsotopicPeak> refIsotopes = null) {
        //    var result = CompareMS2ScanProperties(scanProp, refSpec, param, scanIsotopes, refIsotopes);
        //    var isCcsMatch = false;
        //    var ccsSimilarity = GetGaussianSimilarity(scanCCS, refSpec.CollisionCrossSection, param.CcsTolerance, out isCcsMatch);

        //    result.CcsSimilarity = (float)ccsSimilarity;
        //    result.IsCcsMatch = isCcsMatch;

        //    result.TotalScore = (float)GetTotalScore(result, param);

        //    return result;
        //}


        //public static MsScanMatchResult CompareIMMS2LipidomicsScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec,
        //    MsRefSearchParameterBase param, double scanCCS, List<IsotopicPeak> scanIsotopes = null, List<IsotopicPeak> refIsotopes = null) {
        //    var result = CompareMS2LipidomicsScanProperties(scanProp, refSpec, param, scanIsotopes, refIsotopes);
        //    var isCcsMatch = false;
        //    var ccsSimilarity = GetGaussianSimilarity(scanCCS, refSpec.CollisionCrossSection, param.CcsTolerance, out isCcsMatch);

        //    result.CcsSimilarity = (float)ccsSimilarity;
        //    result.IsCcsMatch = isCcsMatch;

        //    result.TotalScore = (float)GetTotalScore(result, param);

        //    return result;
        //}

        public static MsScanMatchResult CompareMS2ScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param,
            TargetOmics targetOmics = TargetOmics.Metabolomics, double scanCCS = -1.0,
            IReadOnlyList<IsotopicPeak> scanIsotopes = null, IReadOnlyList<IsotopicPeak> refIsotopes = null, 
            double andromedaDelta = 100, int andromedaMaxPeaks = 12) {

            MsScanMatchResult result = null;
            if (targetOmics == TargetOmics.Metabolomics) {
                result = CompareMSScanProperties(scanProp, refSpec, param, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            } 
            else if (targetOmics == TargetOmics.Lipidomics) {
                result = CompareMS2LipidomicsScanProperties(scanProp, refSpec, param);
            } 

            result.IsotopeSimilarity = (float)GetIsotopeRatioSimilarity(scanIsotopes, refIsotopes, scanProp.PrecursorMz, param.Ms1Tolerance);
            
            var isCcsMatch = false;
            var ccsSimilarity = GetGaussianSimilarity(scanCCS, refSpec.CollisionCrossSection, param.CcsTolerance, out isCcsMatch);

            result.CcsSimilarity = (float)ccsSimilarity;
            result.IsCcsMatch = isCcsMatch;
            result.TotalScore = (float)GetTotalScore(result, param);
            return result;
        }

        public static MsScanMatchResult CompareMS2ScanProperties(IMSScanProperty scanProp, int chargestate, PeptideMsReference refSpec, MsRefSearchParameterBase param,
            TargetOmics targetOmics = TargetOmics.Metabolomics, double scanCCS = -1.0,
            IReadOnlyList<IsotopicPeak> scanIsotopes = null, IReadOnlyList<IsotopicPeak> refIsotopes = null,
            double andromedaDelta = 100, int andromedaMaxPeaks = 12) {

            MsScanMatchResult result = null;
            if (targetOmics == TargetOmics.Proteomics) {
                result = CompareMS2ProteomicsScanProperties(scanProp, chargestate, refSpec, param, (float)andromedaDelta, andromedaMaxPeaks);
            }

            result.IsotopeSimilarity = (float)GetIsotopeRatioSimilarity(scanIsotopes, refIsotopes, scanProp.PrecursorMz, param.Ms1Tolerance);

            var isCcsMatch = false;
            var ccsSimilarity = GetGaussianSimilarity(scanCCS, refSpec.CollisionCrossSection, param.CcsTolerance, out isCcsMatch);

            result.CcsSimilarity = (float)ccsSimilarity;
            result.IsCcsMatch = isCcsMatch;
            result.TotalScore = (float)GetTotalScore(result, param);
            return result;
        }

        //public static MsScanMatchResult CompareMS2ScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param, 
        //    IReadOnlyList<IsotopicPeak> scanIsotopes = null, IReadOnlyList<IsotopicPeak> refIsotopes = null) {
        //    var result = CompareMSScanProperties(scanProp, refSpec, param, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);
        //    result.IsotopeSimilarity = (float)GetIsotopeRatioSimilarity(scanIsotopes, refIsotopes, scanProp.PrecursorMz, param.Ms1Tolerance);
        //    result.TotalScore = (float)GetTotalScore(result, param);
        //    return result;
        //}

        //public static MsScanMatchResult CompareMS2LipidomicsScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param,
        //   IReadOnlyList<IsotopicPeak> scanIsotopes = null, IReadOnlyList<IsotopicPeak> refIsotopes = null) {

        //    var result = CompareMS2LipidomicsScanProperties(scanProp, refSpec, param);
        //    var isotopeSimilarity = (float)GetIsotopeRatioSimilarity(scanIsotopes, refIsotopes, scanProp.PrecursorMz, param.Ms1Tolerance);
        //    result.IsotopeSimilarity = (float)GetIsotopeRatioSimilarity(scanIsotopes, refIsotopes, scanProp.PrecursorMz, param.Ms1Tolerance);
        //    result.TotalScore = (float)GetTotalScore(result, param);
        //    return result;
        //}

        public static MsScanMatchResult CompareMS2LipidomicsScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param) {

            var result = CompareBasicMSScanProperties(scanProp, refSpec, param, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            var matchedPeaksScores = GetLipidomicsMatchedPeaksScores(scanProp, refSpec, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            result.MatchedPeaksCount = (float)matchedPeaksScores[1];
            result.MatchedPeaksPercentage = (float)matchedPeaksScores[0];

            if (result.WeightedDotProduct >= param.WeightedDotProductCutOff &&
                result.SimpleDotProduct >= param.SimpleDotProductCutOff &&
                result.ReverseDotProduct >= param.ReverseDotProductCutOff &&
                result.MatchedPeaksPercentage >= param.MatchedPeaksPercentageCutOff &&
                result.MatchedPeaksCount >= param.MinimumSpectrumMatch) {
                result.IsSpectrumMatch = true;
            }

            var isLipidClassMatch = false;
            var isLipidChainsMatch = false;
            var isLipidPositionMatch = false;
            var isOtherLipidMatch = false;

            var name = GetRefinedLipidAnnotationLevel(scanProp, refSpec, param.Ms2Tolerance,
                out isLipidClassMatch, out isLipidChainsMatch, out isLipidPositionMatch, out isOtherLipidMatch);
            if (name == string.Empty)
                return null;

            result.Name = name;
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
            result.TotalScore = (float)GetTotalScore(result, param);

            return result;
        }

        public static MsScanMatchResult CompareMS2ProteomicsScanProperties(IMSScanProperty scanProp, int chargestate, PeptideMsReference refSpec, 
            MsRefSearchParameterBase param, float andromedaDelta, float andromedaMaxPeaks) {

            var result = CompareBasicMSScanProperties(scanProp, refSpec, param, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            var matchedPeaks = GetMachedSpectralPeaks(scanProp, chargestate, refSpec, param.Ms2Tolerance, param.MassRangeBegin, param.MassRangeEnd);

            result.Name = refSpec.Peptide.ModifiedSequence;
            result.AndromedaScore = (float)GetAndromedaScore(matchedPeaks, andromedaDelta, andromedaMaxPeaks);
            result.MatchedPeaksCount = matchedPeaks.Count(n => n.IsMatched);
            result.MatchedPeaksPercentage = (float)(result.MatchedPeaksCount / matchedPeaks.Count());

            if (result.WeightedDotProduct >= param.WeightedDotProductCutOff &&
                result.SimpleDotProduct >= param.SimpleDotProductCutOff &&
                result.ReverseDotProduct >= param.ReverseDotProductCutOff &&
                result.MatchedPeaksPercentage >= param.MatchedPeaksPercentageCutOff &&
                result.MatchedPeaksCount >= param.MinimumSpectrumMatch &&
                result.AndromedaScore >= param.AndromedaScoreCutOff) {
                result.IsSpectrumMatch = true;
            }
            result.TotalScore = (float)GetTotalScore(result, param);

            return result;
        }


        public static MsScanMatchResult CompareEIMSScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, 
            MsRefSearchParameterBase param, bool isUseRetentionIndex = false) {
            var result = CompareMSScanProperties(scanProp, refSpec, param, param.Ms1Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            var msMatchedScore = GetIntegratedSpectraSimilarity(result);
            if (isUseRetentionIndex) {
                result.TotalScore = (float)GetTotalSimilarity(result.RiSimilarity, msMatchedScore, param.IsUseTimeForAnnotationScoring);
            }
            else {
                result.TotalScore = (float)GetTotalSimilarity(result.RtSimilarity, msMatchedScore, param.IsUseTimeForAnnotationScoring);
            }
            result.IsReferenceMatched = result.IsSpectrumMatch
                && result.TotalScore > param.TotalScoreCutoff
                && (!param.IsUseTimeForAnnotationScoring || (isUseRetentionIndex && result.IsRiMatch) || (!isUseRetentionIndex && result.IsRtMatch));
            return result;
        }

        public static MsScanMatchResult CompareEIMSScanProperties(IMSScanProperty scan1, IMSScanProperty scan2,
            MsRefSearchParameterBase param, double eiFactor, double riFactor, bool isUseRetentionIndex = false) {
            var result = CompareMSScanProperties(scan1, scan2, param, param.Ms1Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            var msMatchedScore = GetIntegratedSpectraSimilarity(result);
            if (isUseRetentionIndex) {
                result.TotalScore = (float)GetTotalSimilarity(result.RiSimilarity, riFactor, msMatchedScore, eiFactor);
            }
            else {
                result.TotalScore = (float)GetTotalSimilarity(result.RtSimilarity, riFactor, msMatchedScore, eiFactor);
            }
            return result;
        }

        public static MsScanMatchResult CompareEIMSScanProperties(IMSScanProperty scan1, IMSScanProperty scan2,
            MsRefSearchParameterBase param, bool isUseRetentionIndex = false) {
            var result = CompareMSScanProperties(scan1, scan2, param, param.Ms1Tolerance, param.MassRangeBegin, param.MassRangeEnd);
            var msMatchedScore = GetIntegratedSpectraSimilarity(result);
            if (isUseRetentionIndex) {
                result.TotalScore = (float)GetTotalSimilarity(result.RiSimilarity, msMatchedScore);
            }
            else {
                result.TotalScore = (float)GetTotalSimilarity(result.RtSimilarity, msMatchedScore);
            }
            return result;
        }

        public static double GetIntegratedSpectraSimilarity(MsScanMatchResult result) {
            var dotproductFact = 3.0;
            var revDotproductFact = 2.0;
            var matchedRatioFact = 1.0;
            return (dotproductFact * result.WeightedDotProduct + revDotproductFact * result.ReverseDotProduct + matchedRatioFact * result.MatchedPeaksPercentage) /
                (dotproductFact + revDotproductFact + matchedRatioFact);
        }


        public static MsScanMatchResult CompareMSScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param, 
            float ms2Tol, float massRangeBegin, float massRangeEnd) {

            var result = CompareMSScanProperties(scanProp, (IMSScanProperty)refSpec, param, ms2Tol, massRangeBegin, massRangeEnd);
            result.Name = refSpec.Name;
            result.LibraryID = refSpec.ScanID;
            result.InChIKey = refSpec.InChIKey;
            return result;
        }

        public static MsScanMatchResult CompareMSScanProperties(IMSScanProperty scanProp, IMSScanProperty refSpec, MsRefSearchParameterBase param,
           float ms2Tol, float massRangeBegin, float massRangeEnd) {

            var result = CompareBasicMSScanProperties(scanProp, refSpec, param, ms2Tol, massRangeBegin, massRangeEnd);
            var matchedPeaksScores = GetMatchedPeaksScores(scanProp, refSpec, ms2Tol, massRangeBegin, massRangeEnd);

            result.MatchedPeaksCount = (float)matchedPeaksScores[1];
            result.MatchedPeaksPercentage = (float)matchedPeaksScores[0];
            if (result.WeightedDotProduct >= param.WeightedDotProductCutOff &&
                result.SimpleDotProduct >= param.SimpleDotProductCutOff &&
                result.ReverseDotProduct >= param.ReverseDotProductCutOff &&
                result.MatchedPeaksPercentage >= param.MatchedPeaksPercentageCutOff &&
                result.MatchedPeaksCount >= param.MinimumSpectrumMatch) {
                result.IsSpectrumMatch = true;
            }

            return result;
        }

        public static MsScanMatchResult CompareBasicMSScanProperties(IMSScanProperty scanProp, MoleculeMsReference refSpec, MsRefSearchParameterBase param,
           float ms2Tol, float massRangeBegin, float massRangeEnd) {
            var result = CompareMSScanProperties(scanProp, (IMSScanProperty)refSpec, param, ms2Tol, massRangeBegin, massRangeEnd);
            result.Name = refSpec.Name;
            result.LibraryID = refSpec.ScanID;
            result.InChIKey = refSpec.InChIKey;
            return result;
        }

        public static MsScanMatchResult CompareBasicMSScanProperties(IMSScanProperty scanProp, IMSScanProperty refSpec, MsRefSearchParameterBase param,
           float ms2Tol, float massRangeBegin, float massRangeEnd) {
            
            var isRtMatch = false;
            var isRiMatch = false;
            var isMs1Match = false;

            var weightedDotProduct = GetWeightedDotProduct(scanProp, refSpec, ms2Tol, massRangeBegin, massRangeEnd);
            var simpleDotProduct = GetSimpleDotProduct(scanProp, refSpec, ms2Tol, massRangeBegin, massRangeEnd);
            var reverseDotProduct = GetReverseDotProduct(scanProp, refSpec, ms2Tol, massRangeBegin, massRangeEnd);
            var rtSimilarity = GetGaussianSimilarity(scanProp.ChromXs.RT, refSpec.ChromXs.RT, param.RtTolerance, out isRtMatch);
            var riSimilarity = GetGaussianSimilarity(scanProp.ChromXs.RI, refSpec.ChromXs.RI, param.RiTolerance, out isRiMatch);

            var ms1Tol = param.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            if (scanProp.PrecursorMz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(scanProp.PrecursorMz, ppm);
            }
            var ms1Similarity = GetGaussianSimilarity(scanProp.PrecursorMz, refSpec.PrecursorMz, ms1Tol, out isMs1Match);

            var result = new MsScanMatchResult() {
                LibraryID = refSpec.ScanID, 
                WeightedDotProduct = (float)weightedDotProduct,
                SimpleDotProduct = (float)simpleDotProduct, ReverseDotProduct = (float)reverseDotProduct,
                AcurateMassSimilarity = (float)ms1Similarity,
                RtSimilarity = (float)rtSimilarity, RiSimilarity = (float)riSimilarity, IsPrecursorMzMatch = isMs1Match, IsRtMatch = isRtMatch, IsRiMatch = isRiMatch
            };

            return result;
        }





        /// <summary>
        /// This method returns the similarity score between theoretical isotopic ratios and experimental isotopic patterns in MS1 axis.
        /// This method will utilize up to [M+4] for their calculations.
        /// </summary>
        /// <param name="peaks1">
        /// Add the MS1 spectrum with respect to the focused data point.
        /// </param>
        /// <param name="peaks2">
        /// Add the theoretical isotopic abundances. The theoretical patterns are supposed to be calculated in MSP parcer.
        /// </param>
        /// <param name="targetedMz">
        /// Add the experimental precursor mass.
        /// </param>
        /// <param name="tolerance">
        /// Add the torelance to merge the spectrum of experimental MS1.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetIsotopeRatioSimilarity(IReadOnlyList<IsotopicPeak> peaks1, IReadOnlyList<IsotopicPeak> peaks2, double targetedMz, double tolerance) {
            if (!IsComparedAvailable(peaks1, peaks2)) return -1;

            double similarity = 0;
            double ratio1 = 0, ratio2 = 0;
            if (peaks1[0].RelativeAbundance <= 0 || peaks2[0].RelativeAbundance <= 0) return -1;

            var minimum = Math.Min(peaks1.Count, peaks2.Count);
            for (int i = 1; i < minimum; i++) {
                ratio1 = peaks1[i].RelativeAbundance / peaks1[0].RelativeAbundance;
                ratio2 = peaks2[i].RelativeAbundance / peaks2[0].RelativeAbundance;

                if (ratio1 <= 1 && ratio2 <= 1) similarity += Math.Abs(ratio1 - ratio2);
                else {
                    if (ratio1 > ratio2) {
                        similarity += 1 - ratio2 / ratio1;
                    }
                    else if (ratio2 > ratio1) {
                        similarity += 1 - ratio1 / ratio2;
                    }
                }
            }
            return 1 - similarity;
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="peaks1">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="peaks2">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// [0] The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be returned.
        /// [1] MatchedPeaksCount is also returned.
        /// </returns>
        public static double[] GetMatchedPeaksScores(IMSScanProperty prop1, IMSScanProperty prop2, double bin,
            double massBegin, double massEnd) {
            if (!IsComparedAvailable(prop1, prop2)) return new double[2] { -1, -1 };

            var peaks1 = prop1.Spectrum;
            var peaks2 = prop2.Spectrum;

            return GetMatchedPeaksScores(peaks1, peaks2, bin, massBegin, massEnd);
        }

        public static double[] GetMatchedPeaksScores(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin,
            double massBegin, double massEnd) {
            if (!IsComparedAvailable(peaks1, peaks2)) return new double[2] { -1, -1 };

            double sumM = 0, sumL = 0;
            double minMz = peaks2[0].Mass;
            double maxMz = peaks2[peaks2.Count - 1].Mass;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            double maxLibIntensity = peaks2.Max(n => n.Intensity);
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int libCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < peaks2.Count; i++) {
                    if (peaks2[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks2[i].Mass && peaks2[i].Mass < focusedMz + bin)
                        sumL += peaks2[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumL >= 0.01 * maxLibIntensity) {
                    libCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < peaks1.Count; i++) {
                    if (peaks1[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks1[i].Mass && peaks1[i].Mass < focusedMz + bin)
                        sumM += peaks1[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxLibIntensity) {
                    counter++;
                }

                if (focusedMz + bin > peaks2[peaks2.Count - 1].Mass) break;
                focusedMz = peaks2[remaindIndexL].Mass;
            }

            if (libCounter == 0) return new double[2] { 0, 0 };
            else
                return new double[2] { (double)counter / (double)libCounter, libCounter };
        }

        public static double GetSpetralEntropySimilarity(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin) {
            var combinedSpectrum = SpectrumHandler.GetCombinedSpectrum(peaks1, peaks2, bin);
            var entropy12 = GetSpectralEntropy(combinedSpectrum);
            var entropy1 = GetSpectralEntropy(peaks1);
            var entropy2 = GetSpectralEntropy(peaks2);

            return 1 - (2 * entropy12 - entropy1 - entropy2) * 0.5;
        }

        public static double GetSpectralEntropy(
            List<SpectrumPeak> peaks) {
            var sumIntensity = peaks.Sum(n => n.Intensity);
            return -1 * peaks.Sum(n => n.Intensity / sumIntensity * Math.Log(n.Intensity / sumIntensity, 2));
        }

        public static double[] GetModifiedDotProductScore(
            IMSScanProperty prop1,
            IMSScanProperty prop2,
            double massTolerance = 0.05,
            MassToleranceType massToleranceType = MassToleranceType.Da
            ) {
            var matchedPeaks = new List<MatchedPeak>();
            if (prop1.PrecursorMz < prop2.PrecursorMz) {
                SearchMatchedPeaks(prop1.Spectrum, prop1.PrecursorMz, prop2.Spectrum, prop2.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }
            else {
                SearchMatchedPeaks(prop2.Spectrum, prop2.PrecursorMz, prop1.Spectrum, prop1.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }

            if (matchedPeaks.Count == 0) {
                return new double[] { 0, 0 };
            }

            var product = matchedPeaks.Sum(n => n.Intensity * n.MatchedIntensity);
            var scaler1 = matchedPeaks.Sum(n => n.Intensity * n.Intensity);
            var scaler2 = matchedPeaks.Sum(n => n.MatchedIntensity * n.MatchedIntensity);
            return new double[] { product / (Math.Sqrt(scaler1) * Math.Sqrt(scaler2)), matchedPeaks.Count };
        }

        public static double[] GetBonanzaScore(
            IMSScanProperty prop1,
            IMSScanProperty prop2,
            double massTolerance = 0.05,
            MassToleranceType massToleranceType = MassToleranceType.Da) {
            var matchedPeaks = new List<MatchedPeak>();
            if (prop1.PrecursorMz < prop2.PrecursorMz) {
                SearchMatchedPeaks(prop1.Spectrum, prop1.PrecursorMz, prop2.Spectrum, prop2.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }
            else {
                SearchMatchedPeaks(prop2.Spectrum, prop2.PrecursorMz, prop1.Spectrum, prop1.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }

            if (matchedPeaks.Count == 0) {
                return new double[] { 0, 0 };
            }

            var product = matchedPeaks.Sum(n => n.Intensity * n.MatchedIntensity);
            var scaler1 = prop1.Spectrum.Where(n => n.IsMatched == false).Sum(n => Math.Pow(n.Intensity, 2));
            var scaler2 = prop2.Spectrum.Where(n => n.IsMatched == false).Sum(n => Math.Pow(n.Intensity, 2));
            return new double[] { product / (product + scaler1 + scaler2), matchedPeaks.Count };
        }

        public static double[] GetBonanzaModifiedDotCosineScores(
            IMSScanProperty prop1,
            IMSScanProperty prop2,
            double massTolerance = 0.05,
            MassToleranceType massToleranceType = MassToleranceType.Da) {
            var matchedPeaks = new List<MatchedPeak>();
            if (prop1.PrecursorMz < prop2.PrecursorMz) {
                SearchMatchedPeaks(prop1.Spectrum, prop1.PrecursorMz, prop2.Spectrum, prop2.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }
            else {
                SearchMatchedPeaks(prop2.Spectrum, prop2.PrecursorMz, prop1.Spectrum, prop1.PrecursorMz, massTolerance, massToleranceType, out matchedPeaks);
            }

            if (matchedPeaks.Count == 0) {
                return new double[] { 0, 0, 0, 0 };
            }

            // bonanza
            var product = matchedPeaks.Sum(n => n.Intensity * n.MatchedIntensity);
            var scaler1 = prop1.Spectrum.Where(n => n.IsMatched == false).Sum(n => Math.Pow(n.Intensity, 2));
            var scaler2 = prop2.Spectrum.Where(n => n.IsMatched == false).Sum(n => Math.Pow(n.Intensity, 2));
            var bonanza = product / (product + scaler1 + scaler2);

            // modified dot
            scaler1 = matchedPeaks.Sum(n => n.Intensity * n.Intensity);
            scaler2 = matchedPeaks.Sum(n => n.MatchedIntensity * n.MatchedIntensity);
            var modifieddot = scaler1 == 0 || scaler2 == 0 ? 0.0 : product / (Math.Sqrt(scaler1) * Math.Sqrt(scaler2));

            // cosine
            product = matchedPeaks.Where(n => n.IsProductIonMatched).Sum(n => n.Intensity * n.MatchedIntensity);
            scaler1 = matchedPeaks.Where(n => n.IsProductIonMatched).Sum(n => n.Intensity * n.Intensity);
            scaler2 = matchedPeaks.Where(n => n.IsProductIonMatched).Sum(n => n.MatchedIntensity * n.MatchedIntensity);
            var cosine = scaler1 == 0 || scaler2 == 0 ? 0.0 : product / (Math.Sqrt(scaler1) * Math.Sqrt(scaler2));

            return new double[] { bonanza, matchedPeaks.Count, modifieddot, cosine };
        }

        public static double[] GetCosineScore(
            IMSScanProperty prop1,
            IMSScanProperty prop2,
            double massTolerance = 0.05,
            MassToleranceType massToleranceType = MassToleranceType.Da) {

            var score = 0.0;
            var matched = 0.0;
            if (prop1.PrecursorMz < prop2.PrecursorMz) {
                score = GetSimpleDotProduct(prop2, prop1, massTolerance, 0, Math.Min(prop1.PrecursorMz, prop2.PrecursorMz));
                var matchedscores = GetMatchedPeaksScores(prop2, prop1, massTolerance, 0, Math.Min(prop1.PrecursorMz, prop2.PrecursorMz));
                matched = matchedscores[1];
            }
            else {
                score = GetSimpleDotProduct(prop1, prop2, massTolerance, 0, Math.Min(prop1.PrecursorMz, prop2.PrecursorMz));
                var matchedscores = GetMatchedPeaksScores(prop1, prop2, massTolerance, 0, Math.Min(prop1.PrecursorMz, prop2.PrecursorMz));
                matched = matchedscores[1];
            }

            if (matched == 0) {
                return new double[] { 0, 0 };
            }
            return new double[] { score, matched };
        }

        public static void SearchMatchedPeaks(
            List<SpectrumPeak> ePeaks,
            double ePrecursor, // small precursor
            List<SpectrumPeak> rPeaks,
            double rPrecursor, // large precursor
            double massTolerance,
            MassToleranceType massTolType,
            out List<MatchedPeak> matchedPeaks) {
            matchedPeaks = new List<MatchedPeak>();
            foreach (var e in ePeaks) e.IsMatched = false;
            foreach (var e in rPeaks) e.IsMatched = false;

            //match definition: if product ion or neutral loss are within the mass tolerance, it will be recognized as MATCH.
            //The smallest intensity difference will be recognized as highest match.
            var precursorDiff = rPrecursor - ePrecursor;
            for (int i = 0; i < rPeaks.Count; i++) {
                var rPeak = rPeaks[i];
                var massTol = massTolType == MassToleranceType.Da ? massTolerance : MolecularFormulaUtility.ConvertPpmToMassAccuracy(rPeak.Mass, massTolerance);
                var minPeakID = -1;
                var minIntensityDiff = double.MaxValue;
                var isProduct = false;
                for (int j = 0; j < ePeaks.Count; j++) {
                    var ePeak = ePeaks[j];
                    if (ePeak.IsMatched == true) continue;
                    if (Math.Abs(ePeak.Mass - rPeak.Mass) < massTol) {
                        var intensityDiff = Math.Abs(ePeak.Intensity - rPeak.Intensity);
                        if (intensityDiff < minIntensityDiff) {
                            minIntensityDiff = intensityDiff;
                            minPeakID = j;
                            isProduct = true;
                        }
                    }
                    else if (Math.Abs(precursorDiff + ePeak.Mass - rPeak.Mass) < massTol) {
                        var intensityDiff = Math.Abs(ePeak.Intensity - rPeak.Intensity);
                        if (intensityDiff < minIntensityDiff) {
                            minIntensityDiff = intensityDiff;
                            minPeakID = j;
                            isProduct = false;
                        }
                    }
                }

                if (minPeakID >= 0) {
                    rPeak.IsMatched = true;
                    ePeaks[minPeakID].IsMatched = true;
                    matchedPeaks.Add(
                        new MatchedPeak() {
                            Mass = rPeak.Mass,
                            Intensity = rPeak.Intensity,
                            MatchedIntensity = ePeaks[minPeakID].Intensity,
                            IsProductIonMatched = isProduct,
                            IsNeutralLossMatched = !isProduct,
                        });
                }
            }
        }

        public static List<SpectrumPeak> GetProcessedSpectrum(
            List<SpectrumPeak> peaks,
            double peakPrecursorMz,
            double minMz = 0.0,
            double maxMz = 10000,
            double relativeAbundanceCutOff = 0.1, // 0.1%
            double absoluteAbundanceCutOff = 50.0, 
            double massTolerance = 0.05,
            double massBinningValue = 1.0,
            double intensityScaleFactor = 0.5,
            double scaledMaxValue = 100,
            double massDelta = 1,
            int maxPeakNumInDelta = 12,
            MassToleranceType massToleranceType = MassToleranceType.Da,
            bool isBrClConsideredForIsotopes = false,
            bool isRemoveIsotopes = false,
            bool removeAfterPrecursor = true) {
            //Console.WriteLine("Original peaks");
            //foreach (var peak in peaks) {
            //    Console.WriteLine(peak.Mass + "\t" + peak.Intensity);
            //}

            peaks = SpectrumHandler.GetRefinedPeaklist(peaks, relativeAbundanceCutOff, absoluteAbundanceCutOff, minMz, maxMz, peakPrecursorMz, massTolerance, massToleranceType, 1, isBrClConsideredForIsotopes, isRemoveIsotopes, removeAfterPrecursor);
            //Console.WriteLine("Refined peaks");
            //foreach (var peak in peaks) {
            //    Console.WriteLine(peak.Mass + "\t" + peak.Intensity);
            //}


            peaks = SpectrumHandler.GetBinnedSpectrum(peaks, massBinningValue);
            //Console.WriteLine("Binned peaks");
            //foreach (var peak in peaks) {
            //    Console.WriteLine(peak.Mass + "\t" + peak.Intensity);
            //}

            if (massDelta > 1) { // meaning the peaks are selected by ordering the intensity values
                peaks = SpectrumHandler.GetBinnedSpectrum(peaks, massDelta, maxPeakNumInDelta);
            }
            peaks = SpectrumHandler.GetNormalizedPeaks(peaks, intensityScaleFactor, scaledMaxValue);
            //Console.WriteLine("Normalized peaks");
            //foreach (var peak in peaks) {
            //    Console.WriteLine(peak.Mass + "\t" + peak.Intensity);
            //}

            return peaks;
        }

        /// <summary>
        /// please set the 'mached spectral peaks' list obtained from the method of GetMachedSpectralPeaks where isMatched property is set to each spectrum peak obj
        /// </summary>
        /// <param name="peaks"></param>
        /// <returns></returns>
        public static double GetAndromedaScore(List<SpectrumPeak> peaks, double andromedaDelta, double andromedaMaxPeak) {
            var p = andromedaMaxPeak / andromedaDelta;
            var q = 1 - p;
            var n = peaks.Count;
            var k = peaks.Count(spec => spec.IsMatched == true);

            var sum = 0.0;
            for (int j = k; j <= n; j++) {
                var bc = Mathematics.Basic.BasicMathematics.BinomialCoefficient(n, j);
                var p_prob = Math.Pow(p, j);
                var q_prob = Math.Pow(q, n - j);
                sum += bc * p_prob * q_prob;
            }
            var andromeda = -10.0 * Math.Log10(sum);
            return andromeda < 0.000001 ? 0.000001 : andromeda;
        }

        /// </summary>
        /// <param name="peaks1">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="peaks2">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        public static List<SpectrumPeak> GetMachedSpectralPeaks(IMSScanProperty prop1, int chargeState, IMSScanProperty prop2, double bin,
           double massBegin, double massEnd) {
            if (!IsComparedAvailable(prop1, prop2)) return new List<SpectrumPeak>();

            var peaks1 = prop1.Spectrum;
            var peaks2 = prop2.Spectrum;

            var searchedPeaks = GetMachedSpectralPeaks(peaks1, peaks2, bin, massBegin, massEnd);

            // at this moment...
            var finalPeaks = new List<SpectrumPeak>();
            foreach (var group in searchedPeaks.GroupBy(n => n.PeakID)) {
                var isParentExist = false;
                foreach (var peak in group) {
                    if (peak.SpectrumComment.HasFlag(SpectrumComment.b) && peak.IsMatched) {
                        isParentExist = true;
                    }
                    if (peak.SpectrumComment.HasFlag(SpectrumComment.y) && peak.IsMatched) {
                        isParentExist = true;
                    }
                }
                foreach (var peak in group) {
                    if (peak.SpectrumComment.HasFlag(SpectrumComment.precursor)) continue; // exclude
                    if (chargeState <= 2 && (peak.SpectrumComment.HasFlag(SpectrumComment.b2) || peak.SpectrumComment.HasFlag(SpectrumComment.y2))) continue; // exclude
                    if (!isParentExist &&
                        (peak.SpectrumComment.HasFlag(SpectrumComment.b_h2o) ||
                         peak.SpectrumComment.HasFlag(SpectrumComment.b_nh3) ||
                         peak.SpectrumComment.HasFlag(SpectrumComment.y_h2o) ||
                         peak.SpectrumComment.HasFlag(SpectrumComment.y_nh3))) {
                        continue;
                    }
                    finalPeaks.Add(peak);
                }
            }

            return finalPeaks;
        }

        public static List<SpectrumPeak> GetMachedSpectralPeaks(List<SpectrumPeak> peaks1, List<SpectrumPeak> peaks2, double bin,
           double massBegin, double massEnd) {
            if (!IsComparedAvailable(peaks1, peaks2)) return new List<SpectrumPeak>();
            var minMz = Math.Max(peaks2[0].Mass, massBegin);
            var maxMz = Math.Min(peaks2[peaks2.Count - 1].Mass, massEnd);
            var focusedMz = minMz;

            int remaindIndexM = 0, remaindIndexL = 0;
            var searchedPeaks = new List<SpectrumPeak>();

            while (focusedMz <= maxMz) {
                var maxRefIntensity = double.MinValue;
                var maxRefID = -1;
                for (int i = remaindIndexL; i < peaks2.Count; i++) {
                    if (peaks2[i].Mass < focusedMz - bin) continue;
                    else if (Math.Abs(focusedMz - peaks2[i].Mass) < bin) {
                        if (maxRefIntensity < peaks2[i].Intensity) {
                            maxRefIntensity = peaks2[i].Intensity;
                            maxRefID = i;
                        }
                    }
                    else { remaindIndexL = i; break; }
                }

                SpectrumPeak spectrumPeak = maxRefID >= 0 ? peaks2[maxRefID].Clone() : null;
                if (spectrumPeak == null) {
                    focusedMz = peaks2[remaindIndexL].Mass;
                    if (remaindIndexL == peaks2.Count - 1) break;
                    continue;
                }
                var sumintensity = 0.0;
                var sumintensity_original = 0.0;
                for (int i = remaindIndexM; i < peaks1.Count; i++) {
                    if (peaks1[i].Mass < focusedMz - bin) continue;
                    else if (Math.Abs(focusedMz - peaks1[i].Mass) < bin) {
                        sumintensity += peaks1[i].Intensity;
                        sumintensity_original += peaks1[i].Resolution;
                        spectrumPeak.IsMatched = true;
                    }
                    else { remaindIndexM = i; break; }
                }

                spectrumPeak.Resolution = sumintensity;
                spectrumPeak.Charge = (int)sumintensity_original;
                searchedPeaks.Add(spectrumPeak);

                if (focusedMz + bin > peaks2[peaks2.Count - 1].Mass) break;
                focusedMz = peaks2[remaindIndexL].Mass;
            }

            return searchedPeaks;
        }

        

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="peaks1">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="refSpec">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// [0] The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// [1] MatchedPeaksCount is also returned.
        /// </returns>
        public static double[] GetLipidomicsMatchedPeaksScores(IMSScanProperty msScanProp, MoleculeMsReference molMsRef,
            double bin, double massBegin, double massEnd) {

            if (!IsComparedAvailable(msScanProp, molMsRef)) return new double[] { -1, -1 };
           
            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, BMP, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var resultArray = GetMatchedPeaksScores(msScanProp, molMsRef, bin, massBegin, massEnd); // [0] matched ratio [1] matched count
            var compClass = molMsRef.CompoundClass;
            var comment = molMsRef.Comment;
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(molMsRef);
                if (molecule == null || molecule.Adduct == null) return resultArray;
                if (molecule.LipidClass == LbmClass.EtherPE && molMsRef.Spectrum.Count == 3 && msScanProp.IonMode == IonMode.Positive) return resultArray;

                var result = GetLipidMoleculeAnnotationResult(msScanProp, molecule, bin);
                if (result != null) {
                    if (result.AnnotationLevel == 1) {
                        if (compClass == "SM" && (molecule.LipidName.Contains("3O") || molecule.LipidName.Contains("O3"))) {
                            resultArray[0] = 2.0;
                            return resultArray; // add bonus
                        }
                        else {
                            resultArray[0] = 1.0;
                            return resultArray; // add bonus
                        }
                    }
                    else if (result.AnnotationLevel == 2) {
                        resultArray[0] = 2.0;
                        return resultArray; // add bonus
                    }
                    else
                        return resultArray;
                }
                else {
                    return resultArray;
                }
            }
            else { // currently default value is retured for other lipids
                return resultArray;
            }
        }

        public static double[] GetLipidomicsMoleculerSpeciesLevelAnnotationPeaksScoresForEIEIO(IMSScanProperty msScanProp, MoleculeMsReference molMsRef,
            double bin, double massBegin, double massEnd) {

            if (!IsComparedAvailable(msScanProp, molMsRef)) return new double[] { -1, -1 };

            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, BMP, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var resultArray = GetMatchedPeaksScores(msScanProp, molMsRef, bin, massBegin, massEnd); // [0] matched ratio [1] matched count
            var compClass = molMsRef.CompoundClass;
            var comment = molMsRef.Comment;
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(molMsRef);
                if (molecule == null || molecule.Adduct == null) return resultArray;
                //if (molecule.LipidClass == LbmClass.EtherPE && molMsRef.Spectrum.Count == 3 && msScanProp.IonMode == IonMode.Positive) return resultArray;

                var result = GetLipidMoleculerSpeciesLevelAnnotationResultForEIEIO(msScanProp, molecule, bin);
                if (result != null) {
                    if (result.AnnotationLevel == 1) {
                        if (compClass == "SM" && (molecule.LipidName.Contains("3O") || molecule.LipidName.Contains("O3"))) {
                            resultArray[0] = 2.0;
                            return resultArray; // add bonus
                        }
                        else {
                            resultArray[0] = 1.0;
                            return resultArray; // add bonus
                        }
                    }
                    else if (result.AnnotationLevel == 2) {
                        resultArray[0] = 2.0;
                        return resultArray; // add bonus
                    }
                    else
                        return resultArray;
                }
                else {
                    return resultArray;
                }
            }
            else { // currently default value is retured for other lipids
                if (comment == "SPLASH" && compClass == "CE") return new double[] { -1, -1 };
                return resultArray;
            }
        }

        public static string GetLipidNameFromReference(MoleculeMsReference reference) {
            var compClass = reference.CompoundClass;
            var comment = reference.Comment;
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
                if (molecule == null || molecule.Adduct == null) {
                    return reference.Name;
                }
                var refinedName = string.Empty;
                if (molecule.SublevelLipidName == molecule.LipidName) {
                    return molecule.SublevelLipidName;
                }
                else {
                    return molecule.SublevelLipidName + "|" + molecule.LipidName;
                }
            }
            else {
                return reference.Name;
            }
        }

        public static string GetRefinedLipidAnnotationLevel(IMSScanProperty msScanProp, MoleculeMsReference molMsRef, double bin,
            out bool isLipidClassMatched, out bool isLipidChainMatched, out bool isLipidPositionMatched, out bool isOthers) {

            isLipidClassMatched = false;
            isLipidChainMatched = false;
            isLipidPositionMatched = false;
            isOthers = false;
            if (!IsComparedAvailable(msScanProp, molMsRef)) return string.Empty;

            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var compClass = molMsRef.CompoundClass;
            var comment = molMsRef.Comment;
           
            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {

                if (compClass == "Cholesterol" || compClass == "CholesterolSulfate" ||
                    compClass == "Undefined" || compClass == "BileAcid" ||
                    compClass == "Ac2PIM1" || compClass == "Ac2PIM2" || compClass == "Ac3PIM2" || compClass == "Ac4PIM2" ||
                    compClass == "LipidA") {
                    isOthers = true;
                    return molMsRef.Name; // currently default value is retured for these lipids
                }

                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(molMsRef);
                if (molecule == null || molecule.Adduct == null) {
                    isOthers = true;
                    return molMsRef.Name;
                }

                var result = GetLipidMoleculeAnnotationResult(msScanProp, molecule, bin);
                if (result != null) {
                    var refinedName = string.Empty;
                    if (result.AnnotationLevel == 1) {
                        refinedName = result.SublevelLipidName;
                        isLipidClassMatched = true;
                        isLipidChainMatched = false;
                        isLipidPositionMatched = false;
                    }
                    else if (result.AnnotationLevel == 2) {
                        isLipidClassMatched = true;
                        isLipidChainMatched = true;
                        isLipidPositionMatched = false;
                        if (result.SublevelLipidName == result.LipidName) {
                            refinedName = result.SublevelLipidName;
                        }
                        else {
                            refinedName = result.SublevelLipidName + "|" + result.LipidName;
                        }
                    }
                    else
                        return string.Empty;
                    
                    return refinedName;
                }
                else {
                    return string.Empty;
                }
            }
            else { // currently default value is retured for other lipids
                isOthers = true;
                return molMsRef.Name;
            }
        }

        public static string GetRefinedLipidAnnotationLevelForEIEIO(IMSScanProperty msScanProp, MoleculeMsReference molMsRef, double bin,
            out bool isLipidClassMatched, out bool isLipidChainMatched, out bool isLipidPositionMatched, out bool isOthers) {

            isLipidClassMatched = false;
            isLipidChainMatched = false;
            isLipidPositionMatched = false;
            isOthers = false;
            if (!IsComparedAvailable(msScanProp, molMsRef)) return string.Empty;

            // in lipidomics project, currently, the well-known lipid classes now including
            // PC, PE, PI, PS, PG, SM, TAG are now evaluated.
            // if the lipid class diagnostic fragment (like m/z 184 in PC and SM in ESI(+)) is observed, 
            // the bonus 0.5 is added to the normal presence score

            var compClass = molMsRef.CompoundClass;
            var comment = molMsRef.Comment;

            if (comment != "SPLASH" && compClass != "Unknown" && compClass != "Others") {

                if (compClass == "Cholesterol" || compClass == "CholesterolSulfate" ||
                    compClass == "Undefined" || compClass == "BileAcid" ||
                    compClass == "Ac2PIM1" || compClass == "Ac2PIM2" || compClass == "Ac3PIM2" || compClass == "Ac4PIM2" ||
                    compClass == "LipidA") {
                    isOthers = true;
                    return molMsRef.Name; // currently default value is retured for these lipids
                }

                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(molMsRef);
                if (molecule == null || molecule.Adduct == null) {
                    isOthers = true;
                    return molMsRef.Name;
                }

                var result = GetLipidMoleculerSpeciesLevelAnnotationResultForEIEIO(msScanProp, molecule, bin);
                if (result != null) {
                    var refinedName = string.Empty;
                    if (result.AnnotationLevel == 1) {
                        refinedName = result.SublevelLipidName;
                        isLipidClassMatched = true;
                        isLipidChainMatched = false;
                        isLipidPositionMatched = false;
                    }
                    else if (result.AnnotationLevel == 2) {
                        isLipidClassMatched = true;
                        isLipidChainMatched = true;
                        isLipidPositionMatched = false;
                        if (result.SublevelLipidName == result.LipidName) {
                            refinedName = result.SublevelLipidName;
                        }
                        else {
                            refinedName = result.SublevelLipidName + "|" + result.LipidName;
                        }
                    }
                    else
                        return string.Empty;

                    return refinedName;
                }
                else {
                    return string.Empty;
                }
            }
            else { // currently default value is retured for other lipids
                isOthers = true;
                return molMsRef.Name;
            }
        }


        public static LipidMolecule GetLipidMoleculerSpeciesLevelAnnotationResultForEIEIO(IMSScanProperty msScanProp,
            LipidMolecule molecule, double ms2tol) {
            var lipidclass = molecule.LipidClass;
            var refMz = molecule.Mz;
            var adduct = molecule.Adduct;

            var totalCarbon = molecule.TotalCarbonCount;
            var totalDbBond = molecule.TotalDoubleBondCount;
            var totalOxidized = molecule.TotalOxidizedCount;

            var sn1Carbon = molecule.Sn1CarbonCount;
            var sn1DbBond = molecule.Sn1DoubleBondCount;
            var sn1Oxidized = molecule.Sn1Oxidizedount;
            var sn2Oxidized = molecule.Sn2Oxidizedount;

            // Console.WriteLine(molecule.LipidName);
            var lipidheader = LipidomicsConverter.GetLipidHeaderString(molecule.LipidName);
            // Console.WriteLine(lipidheader + "\t" + lipidclass.ToString());

            switch (lipidclass) {
                case LbmClass.PC: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylcholine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PE: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylethanolamine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PS: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylserine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PG: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.BMP: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfBismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PI: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylinositol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.SM: //EIEIO
                    if (molecule.TotalChainString.Contains("O3")) {
                        return LipidMsmsCharacterization.JudgeIfSphingomyelinPhyto(msScanProp, ms2tol, refMz,
                       totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else {
                        return LipidEieioMsmsCharacterization.JudgeIfSphingomyelin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                    }
                case LbmClass.LNAPE:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylethanolamine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LNAPS:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylserine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.CE: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfCholesterylEster(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.CAR: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfAcylcarnitine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);

                case LbmClass.DG: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfDag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);

                case LbmClass.MG: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfMag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MGDG:
                    return LipidMsmsCharacterization.JudgeIfMgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGDG:
                    return LipidMsmsCharacterization.JudgeIfDgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PMeOH:
                    return LipidMsmsCharacterization.JudgeIfPmeoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PEtOH:
                    return LipidMsmsCharacterization.JudgeIfPetoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PBtOH:
                    return LipidMsmsCharacterization.JudgeIfPbtoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPC: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);

                case LbmClass.LPE: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysope(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);

                case LbmClass.PA: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfPhosphatidicacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPA:
                    return LipidMsmsCharacterization.JudgeIfLysopa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPG: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);

                case LbmClass.LPI: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);

                case LbmClass.LPS: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysops(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);

                case LbmClass.EtherPC: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfEtherpc(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);

                case LbmClass.EtherPE: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfEtherpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);

                case LbmClass.EtherLPC:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherLPE:
                    return LipidMsmsCharacterization.JudgeIfEtherlysope(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.OxPC:
                    return LipidMsmsCharacterization.JudgeIfOxpc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPE:
                    return LipidMsmsCharacterization.JudgeIfOxpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPG:
                    return LipidMsmsCharacterization.JudgeIfOxpg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPI:
                    return LipidMsmsCharacterization.JudgeIfOxpi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPS:
                    return LipidMsmsCharacterization.JudgeIfOxps(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.EtherMGDG:
                    return LipidMsmsCharacterization.JudgeIfEthermgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherDGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherdgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGTS: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfDgts(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LDGTS: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLdgts(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DGCC:
                    return LipidMsmsCharacterization.JudgeIfDgcc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LDGCC:
                    return LipidMsmsCharacterization.JudgeIfLdgcc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGGA:
                    return LipidMsmsCharacterization.JudgeIfGlcadg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.SQDG:
                    return LipidMsmsCharacterization.JudgeIfSqdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DLCL:
                    return LipidMsmsCharacterization.JudgeIfDilysocardiolipin(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.FA:
                    return LipidMsmsCharacterization.JudgeIfFattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.OxFA:
                    return LipidMsmsCharacterization.JudgeIfOxfattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.FAHFA:
                    return LipidMsmsCharacterization.JudgeIfFahfa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFAHFA: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfFahfaDMED(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFA: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfDmedFattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DMEDOxFA: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfDmedOxfattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.EtherOxPC:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.EtherOxPE:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.Cer_NS:
                    return LipidMsmsCharacterization.JudgeIfCeramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NDS:
                    return LipidMsmsCharacterization.JudgeIfCeramidends(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_NS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_NDS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidends(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Hex2Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Hex3Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexhexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_AP:
                    return LipidMsmsCharacterization.JudgeIfCeramideap(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_ABP:
                    return LipidMsmsCharacterization.JudgeIfCeramideabp(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_AP:
                    return LipidMsmsCharacterization.JudgeIfHexceramideap(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);


                case LbmClass.SHexCer: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfShexcer(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct, totalOxidized);

                case LbmClass.GM3:
                    return LipidMsmsCharacterization.JudgeIfGm3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DHSph:
                    return LipidMsmsCharacterization.JudgeIfSphinganine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.Sph:
                    return LipidEieioMsmsCharacterization.JudgeIfSphingosine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.PhytoSph:
                    return LipidMsmsCharacterization.JudgeIfPhytosphingosine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.TG: //EIEIO
                    var sn2Carbon = molecule.Sn2CarbonCount;
                    var sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ADGGA:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylglcadg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                case LbmClass.HBMP: //EIEIO
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHemiismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.EtherTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfEthertag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.MLCL:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfLysocardiolipin(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EODS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeods(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.HexCer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHexceramideeos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ASM:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidEieioMsmsCharacterization.JudgeIfAcylsm(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn2Carbon, sn1DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EBDS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylcerbds(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.AHexCer: 
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylhexcer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
 
                case LbmClass.ASHexCer:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAshexcer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.CL: //EIEIO
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    var sn3Carbon = molecule.Sn3CarbonCount;
                    var sn3DbBond = molecule.Sn3DoubleBondCount;
                    if (sn3Carbon < 1) {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                    }

                //add 10/04/19
                case LbmClass.EtherPI:
                    return LipidMsmsCharacterization.JudgeIfEtherpi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherPS:
                    return LipidMsmsCharacterization.JudgeIfEtherps(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherDG:
                    return LipidMsmsCharacterization.JudgeIfEtherDAG(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI_Cer:
                    return LipidEieioMsmsCharacterization.JudgeIfPicermide(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct, totalOxidized);

                case LbmClass.PE_Cer:
                    return LipidMsmsCharacterization.JudgeIfPecermide(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                //add 13/5/19
                case LbmClass.DCAE:
                    return LipidMsmsCharacterization.JudgeIfDcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.GDCAE:
                    return LipidMsmsCharacterization.JudgeIfGdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.GLCAE:
                    return LipidMsmsCharacterization.JudgeIfGlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.TDCAE:
                    return LipidMsmsCharacterization.JudgeIfTdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.TLCAE:
                    return LipidMsmsCharacterization.JudgeIfTlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.NAE:
                    return LipidMsmsCharacterization.JudgeIfAnandamide(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);

                case LbmClass.NAGly:
                    if (totalCarbon == sn1Carbon) {
                        return LipidEieioMsmsCharacterization.JudgeIfNAcylGlyOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamidegly(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.NAGlySer:
                    if (totalCarbon == sn1Carbon) {
                        return LipidMsmsCharacterization.JudgeIfNAcylGlySerOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideglyser(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.SL:
                    return LipidMsmsCharacterization.JudgeIfSulfonolipid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.EtherPG:
                    return LipidMsmsCharacterization.JudgeIfEtherpg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherLPG:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.CoQ:
                    return LipidMsmsCharacterization.JudgeIfCoenzymeq(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.Vitamin_E:
                    return LipidMsmsCharacterization.JudgeIfVitaminEmolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.Vitamin_D:
                    return LipidMsmsCharacterization.JudgeIfVitaminDmolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.VAE:
                    return LipidMsmsCharacterization.JudgeIfVitaminaestermolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.NAOrn:
                    if (totalCarbon == sn1Carbon) {
                        return LipidMsmsCharacterization.JudgeIfNAcylOrnOxFa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideorn(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.BRSE:
                    return LipidMsmsCharacterization.JudgeIfBrseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.CASE:
                    return LipidMsmsCharacterization.JudgeIfCaseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.SISE:
                    return LipidMsmsCharacterization.JudgeIfSiseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.STSE:
                    return LipidMsmsCharacterization.JudgeIfStseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);


                case LbmClass.AHexBRS:
                    return LipidMsmsCharacterization.JudgeIfAhexbrseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexCAS:
                    return LipidMsmsCharacterization.JudgeIfAhexcaseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexCS:
                    return LipidMsmsCharacterization.JudgeIfAhexceSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexSIS:
                    return LipidMsmsCharacterization.JudgeIfAhexsiseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexSTS:
                    return LipidMsmsCharacterization.JudgeIfAhexstseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);


                // add 27/05/19
                case LbmClass.Cer_AS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfCeramideas(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_ADS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfCeramideads(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfCeramidebs(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BDS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfCeramidebds(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NP: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfCeramidenp(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_OS:
                    return LipidMsmsCharacterization.JudgeIfCeramideos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //add 190528
                case LbmClass.Cer_HS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_HDS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NDOS:
                    return LipidMsmsCharacterization.JudgeIfCeramidedos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HDS: //EIEIO
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //190801
                case LbmClass.SHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BAHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.SSulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BASulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                // added 190811
                case LbmClass.CerP:
                    return LipidMsmsCharacterization.JudgeIfCeramidePhosphate(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                ///2019/11/25 add
                case LbmClass.SMGDG:
                    return LipidMsmsCharacterization.JudgeIfSmgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherSMGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherSmgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //add 20200218
                case LbmClass.LCAE:
                    return LipidMsmsCharacterization.JudgeIfLcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KLCAE:
                    return LipidMsmsCharacterization.JudgeIfKlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KDCAE:
                    return LipidMsmsCharacterization.JudgeIfKdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                //add 20200714
                case LbmClass.DMPE:
                    return LipidMsmsCharacterization.JudgeIfDiMethylPE(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MMPE:
                    return LipidMsmsCharacterization.JudgeIfMonoMethylPE(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MIPC:
                    return LipidMsmsCharacterization.JudgeIfMipc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //add 20200720
                case LbmClass.EGSE:
                    return LipidMsmsCharacterization.JudgeIfErgoSESpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DEGSE:
                    return LipidMsmsCharacterization.JudgeIfDehydroErgoSESpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add 20200812
                case LbmClass.OxTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfOxTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, totalOxidized, adduct);
                case LbmClass.TG_EST:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    sn3Carbon = molecule.Sn3CarbonCount;
                    sn3DbBond = molecule.Sn3DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfFahfaTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond,
                        sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                //add 20200923
                case LbmClass.DSMSE:
                    return LipidMsmsCharacterization.JudgeIfDesmosterolSpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add20210216
                case LbmClass.GPNAE:
                    return LipidMsmsCharacterization.JudgeIfGpnae(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.MGMG:
                    return LipidMsmsCharacterization.JudgeIfMgmg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DGMG:
                    return LipidMsmsCharacterization.JudgeIfDgmg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                //add 20210315-
                case LbmClass.GD1a:
                    return LipidMsmsCharacterization.JudgeIfGD1a(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD1b:
                    return LipidMsmsCharacterization.JudgeIfGD1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD2:
                    return LipidMsmsCharacterization.JudgeIfGD2(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD3:
                    return LipidMsmsCharacterization.JudgeIfGD3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GM1:
                    return LipidMsmsCharacterization.JudgeIfGM1(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GQ1b:
                    return LipidMsmsCharacterization.JudgeIfGQ1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GT1b:
                    return LipidMsmsCharacterization.JudgeIfGT1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.NGcGM3:
                    return LipidMsmsCharacterization.JudgeIfNGcGM3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.ST:
                    return LipidMsmsCharacterization.JudgeIfnoChainSterol(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSLPHex:
                case LbmClass.BRSLPHex:
                case LbmClass.CASLPHex:
                case LbmClass.SISLPHex:
                case LbmClass.STSLPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithLpa(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSPHex:
                case LbmClass.BRSPHex:
                case LbmClass.CASPHex:
                case LbmClass.SISPHex:
                case LbmClass.STSPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithPa(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20220201
                case LbmClass.SPE:
                    return LipidMsmsCharacterization.JudgeIfSpeSpecies(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                //20220322
                case LbmClass.NAPhe:
                    return LipidMsmsCharacterization.JudgeIfNAcylPheFa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NATau:
                    return LipidMsmsCharacterization.JudgeIfNAcylTauFa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                //20221019
                case LbmClass.PT:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylThreonine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //20230407
                case LbmClass.PC_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylcholineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PE_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylethanolamineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PS_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylserineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PG_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylglycerolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.PI_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfPhosphatidylinositolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.LPC_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopcD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);
                case LbmClass.LPE_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopeD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);
                case LbmClass.LPG_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopgD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);
                case LbmClass.LPI_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopiD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);
                case LbmClass.LPS_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfLysopsD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1DbBond, adduct);
                case LbmClass.TG_d5: //EIEIO
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidEieioMsmsCharacterization.JudgeIfTriacylglycerolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, 
                        sn1Carbon, sn2Carbon, totalCarbon - sn1Carbon - sn2Carbon, 
                        sn1DbBond, sn2DbBond, totalDbBond - sn1DbBond - sn2DbBond, adduct);
                case LbmClass.DG_d5: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfDagD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.SM_d9: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfSphingomyelinD9(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.CE_d7: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfCholesterylEsterD7(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.Cer_NS_d7: //EIEIO
                    return LipidEieioMsmsCharacterization.JudgeIfCeramidensD7(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                //20230424
                case LbmClass.bmPC:
                    return LipidMsmsCharacterization.JudgeIfBetaMethylPhosphatidylcholine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230612
                case LbmClass.NATryA:
                    return LipidMsmsCharacterization.JudgeIfNAcylTryA(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                case LbmClass.NA5HT:
                    return LipidMsmsCharacterization.JudgeIfNAcyl5HT(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.WE:
                    return LipidMsmsCharacterization.JudgeIfWaxEster(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, totalCarbon - sn1Carbon, sn1DbBond, totalDbBond - sn1DbBond, adduct);
                //20230626
                case LbmClass.NAAla:
                    return LipidMsmsCharacterization.JudgeIfNAcylAla(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NAGln:
                    return LipidMsmsCharacterization.JudgeIfNAcylGln(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NALeu:
                    return LipidMsmsCharacterization.JudgeIfNAcylLeu(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NAVal:
                    return LipidMsmsCharacterization.JudgeIfNAcylVal(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NASer:
                    return LipidMsmsCharacterization.JudgeIfNAcylSer(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.BisMeLPA:
                    return LipidMsmsCharacterization.JudgeIfBismelpa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                default:
                    return null;
            }
        }

        public static LipidMolecule GetLipidMoleculeAnnotationResult(IMSScanProperty msScanProp,
            LipidMolecule molecule, double ms2tol) {

            var lipidclass = molecule.LipidClass;
            var refMz = molecule.Mz;
            var adduct = molecule.Adduct;

            var totalCarbon = molecule.TotalCarbonCount;
            var totalDbBond = molecule.TotalDoubleBondCount;
            var totalOxidized = molecule.TotalOxidizedCount;

            var sn1Carbon = molecule.Sn1CarbonCount;
            var sn1DbBond = molecule.Sn1DoubleBondCount;
            var sn1Oxidized = molecule.Sn1Oxidizedount;
            var sn2Oxidized = molecule.Sn2Oxidizedount;

            // Console.WriteLine(molecule.LipidName);
            var lipidheader = LipidomicsConverter.GetLipidHeaderString(molecule.LipidName);
            // Console.WriteLine(lipidheader + "\t" + lipidclass.ToString());

            switch (lipidclass) {
                case LbmClass.PC:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylcholine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PE:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PS:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylserine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PG:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.BMP:
                    return LipidMsmsCharacterization.JudgeIfBismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylinositol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.SM:
                    if (molecule.TotalChainString.Contains("O3")) {
                        return LipidMsmsCharacterization.JudgeIfSphingomyelinPhyto(msScanProp, ms2tol, refMz,
                       totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfSphingomyelin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                case LbmClass.LNAPE:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylethanolamine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LNAPS:
                    return LipidMsmsCharacterization.JudgeIfNacylphosphatidylserine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.CE:
                    return LipidMsmsCharacterization.JudgeIfCholesterylEster(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.CAR:
                    return LipidMsmsCharacterization.JudgeIfAcylcarnitine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);

                case LbmClass.DG:
                    return LipidMsmsCharacterization.JudgeIfDag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MG:
                    return LipidMsmsCharacterization.JudgeIfMag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MGDG:
                    return LipidMsmsCharacterization.JudgeIfMgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGDG:
                    return LipidMsmsCharacterization.JudgeIfDgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PMeOH:
                    return LipidMsmsCharacterization.JudgeIfPmeoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PEtOH:
                    return LipidMsmsCharacterization.JudgeIfPetoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PBtOH:
                    return LipidMsmsCharacterization.JudgeIfPbtoh(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPC:
                    return LipidMsmsCharacterization.JudgeIfLysopc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPE:
                    return LipidMsmsCharacterization.JudgeIfLysope(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.PA:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidicacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPA:
                    return LipidMsmsCharacterization.JudgeIfLysopa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPG:
                    return LipidMsmsCharacterization.JudgeIfLysopg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPI:
                    return LipidMsmsCharacterization.JudgeIfLysopi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LPS:
                    return LipidMsmsCharacterization.JudgeIfLysops(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherPC:
                    return LipidMsmsCharacterization.JudgeIfEtherpc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherPE:
                    return LipidMsmsCharacterization.JudgeIfEtherpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherLPC:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherLPE:
                    return LipidMsmsCharacterization.JudgeIfEtherlysope(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.OxPC:
                    return LipidMsmsCharacterization.JudgeIfOxpc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPE:
                    return LipidMsmsCharacterization.JudgeIfOxpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPG:
                    return LipidMsmsCharacterization.JudgeIfOxpg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPI:
                    return LipidMsmsCharacterization.JudgeIfOxpi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.OxPS:
                    return LipidMsmsCharacterization.JudgeIfOxps(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.EtherMGDG:
                    return LipidMsmsCharacterization.JudgeIfEthermgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherDGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherdgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGTS:
                    return LipidMsmsCharacterization.JudgeIfDgts(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LDGTS:
                    return LipidMsmsCharacterization.JudgeIfLdgts(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DGCC:
                    return LipidMsmsCharacterization.JudgeIfDgcc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.LDGCC:
                    return LipidMsmsCharacterization.JudgeIfLdgcc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DGGA:
                    return LipidMsmsCharacterization.JudgeIfGlcadg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.SQDG:
                    return LipidMsmsCharacterization.JudgeIfSqdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DLCL:
                    return LipidMsmsCharacterization.JudgeIfDilysocardiolipin(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.FA:
                    return LipidMsmsCharacterization.JudgeIfFattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.OxFA:
                    return LipidMsmsCharacterization.JudgeIfOxfattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.FAHFA:
                    return LipidMsmsCharacterization.JudgeIfFahfa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFAHFA:
                    return LipidMsmsCharacterization.JudgeIfFahfaDMED(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DMEDFA:
                    return LipidMsmsCharacterization.JudgeIfDmedFattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.DMEDOxFA:
                    return LipidMsmsCharacterization.JudgeIfDmedOxfattyacid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.EtherOxPC:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.EtherOxPE:
                    return LipidMsmsCharacterization.JudgeIfEtheroxpe(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized, sn1Oxidized, sn2Oxidized);

                case LbmClass.Cer_NS:
                    return LipidMsmsCharacterization.JudgeIfCeramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NDS:
                    return LipidMsmsCharacterization.JudgeIfCeramidends(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_NS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_NDS:
                    return LipidMsmsCharacterization.JudgeIfHexceramidends(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Hex2Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Hex3Cer:
                    return LipidMsmsCharacterization.JudgeIfHexhexhexceramidens(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_AP:
                    return LipidMsmsCharacterization.JudgeIfCeramideap(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_ABP:
                    return LipidMsmsCharacterization.JudgeIfCeramideabp(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_AP:
                    return LipidMsmsCharacterization.JudgeIfHexceramideap(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);


                case LbmClass.SHexCer:
                    return LipidMsmsCharacterization.JudgeIfShexcer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.GM3:
                    return LipidMsmsCharacterization.JudgeIfGm3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.DHSph:
                    return LipidMsmsCharacterization.JudgeIfSphinganine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.Sph:
                    return LipidMsmsCharacterization.JudgeIfSphingosine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.PhytoSph:
                    return LipidMsmsCharacterization.JudgeIfPhytosphingosine(msScanProp, ms2tol, refMz,
                        molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, adduct);

                case LbmClass.TG:
                    var sn2Carbon = molecule.Sn2CarbonCount;
                    var sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ADGGA:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylglcadg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                case LbmClass.HBMP:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHemiismonoacylglycerophosphate(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.EtherTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfEthertag(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.MLCL:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfLysocardiolipin(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EODS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfCeramideeods(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.HexCer_EOS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfHexceramideeos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ASM:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylsm(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon,
                         sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.Cer_EBDS:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylcerbds(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.AHexCer:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAcylhexcer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.ASHexCer:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfAshexcer(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);

                case LbmClass.CL:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    var sn3Carbon = molecule.Sn3CarbonCount;
                    var sn3DbBond = molecule.Sn3DoubleBondCount;
                    if (sn3Carbon < 1) {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfCardiolipin(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                    }

                //add 10/04/19
                case LbmClass.EtherPI:
                    return LipidMsmsCharacterization.JudgeIfEtherpi(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherPS:
                    return LipidMsmsCharacterization.JudgeIfEtherps(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherDG:
                    return LipidMsmsCharacterization.JudgeIfEtherDAG(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI_Cer:
                    return LipidMsmsCharacterization.JudgeIfPicermide(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);
                case LbmClass.PE_Cer:
                    return LipidMsmsCharacterization.JudgeIfPecermide(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                //add 13/5/19
                case LbmClass.DCAE:
                    return LipidMsmsCharacterization.JudgeIfDcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.GDCAE:
                    return LipidMsmsCharacterization.JudgeIfGdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.GLCAE:
                    return LipidMsmsCharacterization.JudgeIfGlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.TDCAE:
                    return LipidMsmsCharacterization.JudgeIfTdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.TLCAE:
                    return LipidMsmsCharacterization.JudgeIfTlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.NAE:
                    return LipidMsmsCharacterization.JudgeIfAnandamide(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);

                case LbmClass.NAGly:
                    if (totalCarbon == sn1Carbon)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylGlyOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamidegly(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.NAGlySer:
                    if (totalCarbon == sn1Carbon)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylGlySerOxFa(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideglyser(msScanProp, ms2tol, refMz,
                             totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.SL:
                    return LipidMsmsCharacterization.JudgeIfSulfonolipid(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct, totalOxidized);

                case LbmClass.EtherPG:
                    return LipidMsmsCharacterization.JudgeIfEtherpg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.EtherLPG:
                    return LipidMsmsCharacterization.JudgeIfEtherlysopg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.CoQ:
                    return LipidMsmsCharacterization.JudgeIfCoenzymeq(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.Vitamin_E:
                    return LipidMsmsCharacterization.JudgeIfVitaminEmolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.Vitamin_D:
                    return LipidMsmsCharacterization.JudgeIfVitaminDmolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.VAE:
                    return LipidMsmsCharacterization.JudgeIfVitaminaestermolecules(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);


                case LbmClass.NAOrn:
                    if (totalCarbon == sn1Carbon)
                    {
                        return LipidMsmsCharacterization.JudgeIfNAcylOrnOxFa(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, totalOxidized, adduct);
                    }
                    else {
                        return LipidMsmsCharacterization.JudgeIfFahfamideorn(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                    }


                case LbmClass.BRSE:
                    return LipidMsmsCharacterization.JudgeIfBrseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.CASE:
                    return LipidMsmsCharacterization.JudgeIfCaseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.SISE:
                    return LipidMsmsCharacterization.JudgeIfSiseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.STSE:
                    return LipidMsmsCharacterization.JudgeIfStseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);


                case LbmClass.AHexBRS:
                    return LipidMsmsCharacterization.JudgeIfAhexbrseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexCAS:
                    return LipidMsmsCharacterization.JudgeIfAhexcaseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexCS:
                    return LipidMsmsCharacterization.JudgeIfAhexceSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexSIS:
                    return LipidMsmsCharacterization.JudgeIfAhexsiseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);

                case LbmClass.AHexSTS:
                    return LipidMsmsCharacterization.JudgeIfAhexstseSpecies(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, adduct);


                // add 27/05/19
                case LbmClass.Cer_AS:
                    return LipidMsmsCharacterization.JudgeIfCeramideas(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_ADS:
                    return LipidMsmsCharacterization.JudgeIfCeramideads(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BS:
                    return LipidMsmsCharacterization.JudgeIfCeramidebs(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_BDS:
                    return LipidMsmsCharacterization.JudgeIfCeramidebds(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NP:
                    return LipidMsmsCharacterization.JudgeIfCeramidenp(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_OS:
                    return LipidMsmsCharacterization.JudgeIfCeramideos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //add 190528
                case LbmClass.Cer_HS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_HDS:
                    return LipidMsmsCharacterization.JudgeIfCeramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.Cer_NDOS:
                    return LipidMsmsCharacterization.JudgeIfCeramidedos(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HS:
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.HexCer_HDS:
                    return LipidMsmsCharacterization.JudgeIfHexceramideo(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //190801
                case LbmClass.SHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BAHex:
                    return LipidMsmsCharacterization.JudgeIfSterolHexoside(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.SSulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.BASulfate:
                    return LipidMsmsCharacterization.JudgeIfSterolSulfate(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                // added 190811
                case LbmClass.CerP:
                    return LipidMsmsCharacterization.JudgeIfCeramidePhosphate(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                ///2019/11/25 add
                case LbmClass.SMGDG:
                    return LipidMsmsCharacterization.JudgeIfSmgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.EtherSMGDG:
                    return LipidMsmsCharacterization.JudgeIfEtherSmgdg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //add 20200218
                case LbmClass.LCAE:
                    return LipidMsmsCharacterization.JudgeIfLcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KLCAE:
                    return LipidMsmsCharacterization.JudgeIfKlcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                case LbmClass.KDCAE:
                    return LipidMsmsCharacterization.JudgeIfKdcae(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct, totalOxidized);

                //add 20200714
                case LbmClass.DMPE:
                    return LipidMsmsCharacterization.JudgeIfDiMethylPE(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MMPE:
                    return LipidMsmsCharacterization.JudgeIfMonoMethylPE(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.MIPC:
                    return LipidMsmsCharacterization.JudgeIfMipc(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                //add 20200720
                case LbmClass.EGSE:
                    return LipidMsmsCharacterization.JudgeIfErgoSESpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DEGSE:
                    return LipidMsmsCharacterization.JudgeIfDehydroErgoSESpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add 20200812
                case LbmClass.OxTG:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfOxTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, totalOxidized, adduct);
                case LbmClass.TG_EST:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    sn3Carbon = molecule.Sn3CarbonCount;
                    sn3DbBond = molecule.Sn3DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfFahfaTriacylglycerol(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond,
                        sn3Carbon, sn3Carbon, sn3DbBond, sn3DbBond, adduct);
                //add 20200923
                case LbmClass.DSMSE:
                    return LipidMsmsCharacterization.JudgeIfDesmosterolSpecies(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                //add20210216
                case LbmClass.GPNAE:
                    return LipidMsmsCharacterization.JudgeIfGpnae(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.MGMG:
                    return LipidMsmsCharacterization.JudgeIfMgmg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);
                case LbmClass.DGMG:
                    return LipidMsmsCharacterization.JudgeIfDgmg(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, adduct);

                //add 20210315-
                case LbmClass.GD1a:
                    return LipidMsmsCharacterization.JudgeIfGD1a(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD1b:
                    return LipidMsmsCharacterization.JudgeIfGD1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD2:
                    return LipidMsmsCharacterization.JudgeIfGD2(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GD3:
                    return LipidMsmsCharacterization.JudgeIfGD3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GM1:
                    return LipidMsmsCharacterization.JudgeIfGM1(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GQ1b:
                    return LipidMsmsCharacterization.JudgeIfGQ1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.GT1b:
                    return LipidMsmsCharacterization.JudgeIfGT1b(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.NGcGM3:
                    return LipidMsmsCharacterization.JudgeIfNGcGM3(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);

                case LbmClass.ST:
                    return LipidMsmsCharacterization.JudgeIfnoChainSterol(molecule.LipidName, molecule.LipidClass, 
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSLPHex:
                case LbmClass.BRSLPHex:
                case LbmClass.CASLPHex:
                case LbmClass.SISLPHex:
                case LbmClass.STSLPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithLpa(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);

                case LbmClass.CSPHex:
                case LbmClass.BRSPHex:
                case LbmClass.CASPHex:
                case LbmClass.SISPHex:
                case LbmClass.STSPHex:
                    return LipidMsmsCharacterization.JudgeIfSteroidWithPa(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20220201
                case LbmClass.SPE:
                    return LipidMsmsCharacterization.JudgeIfSpeSpecies(molecule.LipidName, molecule.LipidClass,
                        msScanProp, ms2tol, refMz, totalCarbon, totalDbBond, adduct);
                //20220322
                case LbmClass.NAPhe:
                    return LipidMsmsCharacterization.JudgeIfNAcylPheFa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NATau:
                    return LipidMsmsCharacterization.JudgeIfNAcylTauFa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                //20221019
                case LbmClass.PT:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylThreonine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230407
                case LbmClass.PC_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylcholineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PE_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylethanolamineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PS_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylserineD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PG_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylglycerolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.PI_d5:
                    return LipidMsmsCharacterization.JudgeIfPhosphatidylinositolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPC_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopcD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPE_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopeD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPG_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopgD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPI_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopiD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.LPS_d5:
                    return LipidMsmsCharacterization.JudgeIfLysopsD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.TG_d5:
                    sn2Carbon = molecule.Sn2CarbonCount;
                    sn2DbBond = molecule.Sn2DoubleBondCount;
                    return LipidMsmsCharacterization.JudgeIfTriacylglycerolD5(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                        sn2Carbon, sn2Carbon, sn2DbBond, sn2DbBond, adduct);
                case LbmClass.DG_d5:
                    return LipidMsmsCharacterization.JudgeIfDagD5(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.SM_d9:
                    return LipidMsmsCharacterization.JudgeIfSphingomyelinD9(msScanProp, ms2tol, refMz,
                    totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.CE_d7:
                    return LipidMsmsCharacterization.JudgeIfCholesterylEsterD7(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, adduct);
                case LbmClass.Cer_NS_d7:
                    return LipidMsmsCharacterization.JudgeIfCeramidensD7(msScanProp, ms2tol, refMz,
                         totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230424
                case LbmClass.bmPC:
                    return LipidMsmsCharacterization.JudgeIfBetaMethylPhosphatidylcholine(msScanProp, ms2tol, refMz,
                        totalCarbon, totalDbBond, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                //20230612
                case LbmClass.NATryA:
                    return LipidMsmsCharacterization.JudgeIfNAcylTryA(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond, adduct);
                case LbmClass.NA5HT:
                    return LipidMsmsCharacterization.JudgeIfNAcyl5HT(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.WE:
                    return LipidMsmsCharacterization.JudgeIfWaxEster(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, sn1Carbon, sn1Carbon, sn1DbBond, sn1DbBond,
                     adduct);
                //20230626
                case LbmClass.NAAla:
                    return LipidMsmsCharacterization.JudgeIfNAcylAla(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NAGln:
                    return LipidMsmsCharacterization.JudgeIfNAcylGln(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NALeu:
                    return LipidMsmsCharacterization.JudgeIfNAcylLeu(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NAVal:
                    return LipidMsmsCharacterization.JudgeIfNAcylVal(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.NASer:
                    return LipidMsmsCharacterization.JudgeIfNAcylSer(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);
                case LbmClass.BisMeLPA:
                    return LipidMsmsCharacterization.JudgeIfBismelpa(msScanProp, ms2tol, refMz,
                     totalCarbon, totalDbBond, totalOxidized, adduct);

                default:
                    return null;
            }
        }

        /// <summary>
        /// This program will return so called reverse dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS with respect to library spectrum will be calculated in this method.
        /// </summary>
        /// <param name="peaks1">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="peaks2">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetReverseDotProduct(IMSScanProperty prop1, IMSScanProperty prop2, double bin,
            double massBegin, double massEnd) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;
            if (!IsComparedAvailable(prop1, prop2)) return -1;

            var peaks1 = prop1.Spectrum;
            var peaks2 = prop2.Spectrum;

            double minMz = peaks2[0].Mass;
            double maxMz = peaks2[peaks2.Count - 1].Mass;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            SummedPeak[] measuredMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            SummedPeak[] referenceMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            int size = 0;

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < peaks2.Count; i++) {
                    if (peaks2[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks2[i].Mass && peaks2[i].Mass < focusedMz + bin)
                        sumL += peaks2[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < peaks1.Count; i++) {
                    if (peaks1[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks1[i].Mass && peaks1[i].Mass < focusedMz + bin)
                        sumM += peaks1[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0) {
                    measuredMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumM);
                    if (sumM > baseM) baseM = sumM;

                    referenceMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumL);
                    if (sumL > baseR) baseR = sumL;
                }
                else {
                    measuredMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumM);
                    if (sumM > baseM) baseM = sumM;

                    referenceMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumL);
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }
                size++;

                if (focusedMz + bin > peaks2[peaks2.Count - 1].Mass) break;
                focusedMz = peaks2[remaindIndexL].Mass;
            }

            if (baseM == 0 || baseR == 0) {
                ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
                ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
                return 0;
            }

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < size; i++) {
                measuredMassBuffer[i] = new SummedPeak(focusedMz: measuredMassBuffer[i].FocusedMz, intensity: measuredMassBuffer[i].Intensity / baseM);
                referenceMassBuffer[i] = new SummedPeak(focusedMz: referenceMassBuffer[i].FocusedMz, intensity: referenceMassBuffer[i].Intensity / baseR);
                sumMeasure += measuredMassBuffer[i].Intensity;
                sumReference += referenceMassBuffer[i].Intensity;

                if (measuredMassBuffer[i].Intensity > 0.1) eSpectrumCounter++;
                if (referenceMassBuffer[i].Intensity > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < size; i++) {
                if (referenceMassBuffer[i].Intensity < cutoff)
                    continue;

                scalarM += measuredMassBuffer[i].Intensity * measuredMassBuffer[i].FocusedMz;
                scalarR += referenceMassBuffer[i].Intensity * referenceMassBuffer[i].FocusedMz;
                covariance += Math.Sqrt(measuredMassBuffer[i].Intensity * referenceMassBuffer[i].Intensity) * measuredMassBuffer[i].FocusedMz;

                //scalarM += measuredMassList[i][1];
                //scalarR += referenceMassList[i][1];
                //covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
            }

            ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
            ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This program will return so called dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS will be calculated in this method.
        /// </summary>
        /// <param name="peaks1">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="peaks2">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetWeightedDotProduct(IMSScanProperty prop1, IMSScanProperty prop2, double bin,
            double massBegin, double massEnd) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (!IsComparedAvailable(prop1, prop2)) return -1;

            var peaks1 = prop1.Spectrum;
            var peaks2 = prop2.Spectrum;

            double minMz = Math.Min(peaks1[0].Mass, peaks2[0].Mass);
            double maxMz = Math.Max(peaks1[peaks1.Count - 1].Mass, peaks2[peaks2.Count - 1].Mass);

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;

            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            SummedPeak[] measuredMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            SummedPeak[] referenceMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            int size = 0;

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < peaks1.Count; i++) {
                    if (peaks1[i].Mass < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= peaks1[i].Mass && peaks1[i].Mass < focusedMz + bin) sumM += peaks1[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < peaks2.Count; i++) {
                    if (peaks2[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks2[i].Mass && peaks2[i].Mass < focusedMz + bin)
                        sumR += peaks2[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumM);
                    if (sumM > baseM) baseM = sumM;

                    referenceMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumR);
                    if (sumR > baseR) baseR = sumR;
                }
                else {
                    measuredMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumM);
                    if (sumM > baseM) baseM = sumM;

                    referenceMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumR);
                    if (sumR > baseR) baseR = sumR;
                }
                size++;

                if (focusedMz + bin > Math.Max(peaks1[peaks1.Count - 1].Mass, peaks2[peaks2.Count - 1].Mass)) break;
                if (focusedMz + bin > peaks2[remaindIndexL].Mass && focusedMz + bin <= peaks1[remaindIndexM].Mass)
                    focusedMz = peaks1[remaindIndexM].Mass;
                else if (focusedMz + bin <= peaks2[remaindIndexL].Mass && focusedMz + bin > peaks1[remaindIndexM].Mass)
                    focusedMz = peaks2[remaindIndexL].Mass;
                else
                    focusedMz = Math.Min(peaks1[remaindIndexM].Mass, peaks2[remaindIndexL].Mass);
            }

            if (baseM == 0 || baseR == 0) {
                ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
                ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
                return 0;
            }

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < size; i++) {
                measuredMassBuffer[i] = new SummedPeak (focusedMz: measuredMassBuffer[i].FocusedMz, intensity: measuredMassBuffer[i].Intensity / baseM);
                referenceMassBuffer[i] = new SummedPeak (focusedMz: referenceMassBuffer[i].FocusedMz, intensity: referenceMassBuffer[i].Intensity / baseR);
                sumMeasure += measuredMassBuffer[i].Intensity;
                sumReference += referenceMassBuffer[i].Intensity;

                if (measuredMassBuffer[i].Intensity > 0.1) eSpectrumCounter++;
                if (referenceMassBuffer[i].Intensity > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;
            for (int i = 0; i < size; i++) {
                if (measuredMassBuffer[i].Intensity < cutoff)
                    continue;

                scalarM += measuredMassBuffer[i].Intensity * measuredMassBuffer[i].FocusedMz;
                scalarR += referenceMassBuffer[i].Intensity * referenceMassBuffer[i].FocusedMz;
                covariance += Math.Sqrt(measuredMassBuffer[i].Intensity * referenceMassBuffer[i].Intensity) * measuredMassBuffer[i].FocusedMz;

                //scalarM += measuredMassList[i][1];
                //scalarR += referenceMassList[i][1];
                //covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
            }

            ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
            ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        public static double GetSimpleDotProduct(IMSScanProperty prop1, IMSScanProperty prop2, double bin, double massBegin, double massEnd) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (!IsComparedAvailable(prop1, prop2)) return -1;

            var peaks1 = prop1.Spectrum;
            var peaks2 = prop2.Spectrum;

            double minMz = Math.Min(peaks1[0].Mass, peaks2[0].Mass);
            double maxMz = Math.Max(peaks1[peaks1.Count - 1].Mass, peaks2[peaks2.Count - 1].Mass);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            if (massBegin > minMz) minMz = massBegin;
            if (maxMz > massEnd) maxMz = massEnd;


            SummedPeak[] measuredMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            SummedPeak[] referenceMassBuffer = ArrayPool<SummedPeak>.Shared.Rent(peaks1.Count + peaks2.Count);
            int size = 0;

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < peaks1.Count; i++) {
                    if (peaks1[i].Mass < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= peaks1[i].Mass && peaks1[i].Mass < focusedMz + bin) sumM += peaks1[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < peaks2.Count; i++) {
                    if (peaks2[i].Mass < focusedMz - bin) continue;
                    else if (focusedMz - bin <= peaks2[i].Mass && peaks2[i].Mass < focusedMz + bin)
                        sumR += peaks2[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                measuredMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumM);
                if (sumM > baseM) baseM = sumM;

                referenceMassBuffer[size] = new SummedPeak(focusedMz: focusedMz, intensity: sumR);
                if (sumR > baseR) baseR = sumR;
                size++;

                if (focusedMz + bin > Math.Max(peaks1[peaks1.Count - 1].Mass, peaks2[peaks2.Count - 1].Mass)) break;
                if (focusedMz + bin > peaks2[remaindIndexL].Mass && focusedMz + bin <= peaks1[remaindIndexM].Mass)
                    focusedMz = peaks1[remaindIndexM].Mass;
                else if (focusedMz + bin <= peaks2[remaindIndexL].Mass && focusedMz + bin > peaks1[remaindIndexM].Mass)
                    focusedMz = peaks2[remaindIndexL].Mass;
                else
                    focusedMz = Math.Min(peaks1[remaindIndexM].Mass, peaks2[remaindIndexL].Mass);
            }

            if (baseM == 0 || baseR == 0) {
                ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
                ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
                return 0;
            }

            for (int i = 0; i < size; i++) {
                measuredMassBuffer[i] = new SummedPeak(focusedMz: measuredMassBuffer[i].FocusedMz, intensity: measuredMassBuffer[i].Intensity / baseM * 999);
                referenceMassBuffer[i] = new SummedPeak(focusedMz: referenceMassBuffer[i].FocusedMz, intensity: referenceMassBuffer[i].Intensity / baseR * 999);
            }

            for (int i = 0; i < size; i++) {
                scalarM += measuredMassBuffer[i].Intensity;
                scalarR += referenceMassBuffer[i].Intensity;
                covariance += Math.Sqrt(measuredMassBuffer[i].Intensity * referenceMassBuffer[i].Intensity);
            }

            ArrayPool<SummedPeak>.Shared.Return(measuredMassBuffer);
            ArrayPool<SummedPeak>.Shared.Return(referenceMassBuffer);
            if (scalarM == 0 || scalarR == 0) { return 0; }
            else {
                return Math.Pow(covariance, 2) / scalarM / scalarR;
            }
        }

        public static double GetGaussianSimilarity(IChromX actual, IChromX reference, double tolerance, out bool isInTolerance) {
            isInTolerance = false;
            if (actual == null || reference == null) return -1;
            if (actual.Value <= 0 || reference.Value <= 0) return -1;
            if (Math.Abs(actual.Value - reference.Value) <= tolerance) isInTolerance = true;
            var similarity = GetGaussianSimilarity(actual.Value, reference.Value, tolerance);
            return similarity;
        }

        public static double GetGaussianSimilarity(double actual, double reference, double tolerance, out bool isInTolerance) {
            isInTolerance = false;
            if (actual <= 0 || reference <= 0) return -1;
            if (Math.Abs(actual - reference) <= tolerance) isInTolerance = true;
            var similarity = GetGaussianSimilarity(actual, reference, tolerance);
            return similarity;
        }

        /// <summary>
        /// This method is to calculate the similarity of retention time differences or precursor ion difference from the library information as described in the previous report.
        /// Tsugawa, H. et al. Anal.Chem. 85, 5191-5199, 2013.
        /// </summary>
        /// <param name="actual">
        /// Add the experimental m/z or retention time.
        /// </param>
        /// <param name="reference">
        /// Add the theoretical m/z or library's retention time.
        /// </param>
        /// <param name="tolrance">
        /// Add the user-defined search tolerance.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetGaussianSimilarity(double actual, double reference, double tolrance) {
            return Math.Exp(-0.5 * Math.Pow((actual - reference) / tolrance, 2));
        }

        /// <summary>
        /// MS-DIAL program utilizes the total similarity score to rank the compound candidates.
        /// This method is to calculate it from four scores including RT, isotopic ratios, m/z, and MS/MS similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <param name="spectraSimilarity"></param>
        /// <param name="reverseSearchSimilarity"></param>
        /// <param name="presenceSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity,
            double spectraSimilarity, double reverseSearchSimilarity, double presenceSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT) {
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;

            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;

            if (targetOmics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; msmsFactor = 1.5; rtFactor = 0.5;
            }

            var msmsSimilarity =
                (dotProductFactor * spectraSimilarity + revesrseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenceSimilarity) /
                (dotProductFactor + revesrseDotProdFactor + presensePercentageFactor);

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metabolomics) msmsSimilarity = msmsSimilarity * 0.5;

            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + rtFactor);
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
                }
            }
        }

        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double ccsSimilarity, double isotopeSimilarity,
            double spectraSimilarity, double reverseSearchSimilarity, double presenceSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT, bool isUseCcs) {
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;

            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;
            var ccsFactor = 2.0;

            if (targetOmics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; msmsFactor = 1.5; rtFactor = 0.5; ccsFactor = 1.0F;
            }

            var msmsSimilarity =
                (dotProductFactor * spectraSimilarity + revesrseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenceSimilarity) /
                (dotProductFactor + revesrseDotProdFactor + presensePercentageFactor);

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metabolomics) msmsSimilarity = msmsSimilarity * 0.5;

            var useRtScore = true;
            var useCcsScore = true;
            var useIsotopicScore = true;
            if (!isUseRT || rtSimilarity < 0) useRtScore = false;
            if (!isUseCcs || ccsSimilarity < 0) useCcsScore = false;
            if (isotopeSimilarity < 0) useIsotopicScore = false;

            if (useRtScore == true && useCcsScore == true && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == true && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + rtFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
                      / (msmsFactor + massFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == false) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
            }
            else if (useRtScore == false && useCcsScore == false && useIsotopicScore == true) {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
            }
            else {
                return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
            }
            //if (!isUseRT) {
            //    if (isotopeSimilarity < 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
            //            / (msmsFactor + massFactor);
            //    }
            //    else if (isotopeSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor);
            //    }
            //    else {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor + ccsFactor);
            //    }
            //}
            //else {
            //    if (rtSimilarity < 0 && isotopeSimilarity < 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
            //            / (msmsFactor + massFactor);
            //    }
            //    else if (rtSimilarity < 0 && isotopeSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
            //            / (msmsFactor + massFactor + rtFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity < 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor);
            //    }
            //    else if (rtSimilarity >= 0 && isotopeSimilarity >= 0 && ccsSimilarity < 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + rtFactor * rtSimilarity)
            //            / (msmsFactor + massFactor + isotopeFactor + rtFactor);
            //    }
            //    else if (isotopeSimilarity < 0 && rtSimilarity >= 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + rtFactor + ccsFactor);
            //    }
            //    else if (isotopeSimilarity >= 0 && rtSimilarity < 0 && ccsSimilarity >= 0) {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity + isotopeFactor * isotopeSimilarity)
            //            / (msmsFactor + massFactor + ccsFactor + isotopeFactor);
            //    }
            //    else {
            //        return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
            //            / (msmsFactor + massFactor + rtFactor + isotopeFactor + ccsFactor);
            //    }
            //}
        }

        /// <summary>
        /// MS-DIAL program also calculate the total similarity score without the MS/MS similarity scoring.
        /// It means that the total score will be calculated from RT, m/z, and isotopic similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity, bool isUseRT) {
            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return accurateMassSimilarity;
                }
                else {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 1.5;
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return accurateMassSimilarity * 0.5;
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 2.5;
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (accurateMassSimilarity + rtSimilarity) * 0.5;
                }
                else {
                    return (accurateMassSimilarity + rtSimilarity + 0.5 * isotopeSimilarity) * 0.4;
                }
            }
        }

        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double ccsSimilarity, double isotopeSimilarity, bool isUseRT, bool isUseCcs) {

            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;
            var ccsFactor = 2.0;

            var useRtScore = true;
            var useCcsScore = true;
            var useIsotopicScore = true;
            if (!isUseRT || rtSimilarity < 0) useRtScore = false;
            if (!isUseCcs || ccsSimilarity < 0) useCcsScore = false;
            if (isotopeSimilarity < 0) useIsotopicScore = false;

            if (useRtScore == true && useCcsScore == true && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + rtFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == true && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + rtFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (massFactor + rtFactor + isotopeFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity + ccsFactor * ccsSimilarity)
                        / (massFactor + isotopeFactor + ccsFactor);
            }
            else if (useRtScore == false && useCcsScore == true && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + ccsFactor * ccsSimilarity)
                      / (massFactor + ccsFactor);
            }
            else if (useRtScore == true && useCcsScore == false && useIsotopicScore == false) {
                return (massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (massFactor + rtFactor);
            }
            else if (useRtScore == false && useCcsScore == false && useIsotopicScore == true) {
                return (massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (massFactor + isotopeFactor);
            }
            else {
                return (massFactor * accurateMassSimilarity)
                        / massFactor;
            }
        }

        public static double GetTotalSimilarityUsingSimpleDotProduct(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity,
            double dotProductSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT) {
            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;

            var msmsSimilarity = dotProductSimilarity;

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metabolomics) msmsSimilarity = msmsSimilarity * 0.5;

            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor + rtFactor);
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
                }
            }
        }

        public static double GetTotalScore(MsScanMatchResult result, MsRefSearchParameterBase param) {
            var totalScore = 0.0;
            if (param.IsUseTimeForAnnotationScoring && result.RtSimilarity > 0) {
                totalScore += result.RtSimilarity;
            }
            if (param.IsUseCcsForAnnotationScoring && result.CcsSimilarity > 0) {
                totalScore += result.CcsSimilarity;
            }
            if (result.AcurateMassSimilarity > 0) {
                totalScore += result.AcurateMassSimilarity;
            }
            if (result.IsotopeSimilarity > 0) {
                totalScore += result.IsotopeSimilarity;
            }
            if (result.WeightedDotProduct > 0) {
                totalScore += (result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3.0;
            }
            if (result.MatchedPeaksPercentage > 0) {
                totalScore += result.MatchedPeaksPercentage;
            }
            if (result.AndromedaScore > 0) {
                totalScore += result.AndromedaScore;
            }
            return totalScore;
        }

        public static double GetTotalSimilarity(double rtSimilarity, double eiSimilarity, bool isUseRT = true) {
            if (rtSimilarity < 0 || !isUseRT) {
                return eiSimilarity;
            }
            else {
                return (0.6 * eiSimilarity + 0.4 * rtSimilarity);
            }
        }

        public static double GetTotalSimilarity(double rtSimilarity, double rtFactor, double eiSimilarity, double eiFactor, bool isUseRT = true) {
            if (rtSimilarity < 0 || !isUseRT) {
                return eiSimilarity;
            }
            else {
                return eiFactor * eiSimilarity + rtFactor * rtSimilarity;
            }
        }

        readonly struct SummedPeak {
            public SummedPeak(double focusedMz, double intensity) {
                FocusedMz = focusedMz;
                Intensity = intensity;
            }

            public readonly double FocusedMz;
            public readonly double Intensity;
        }
    }
}
