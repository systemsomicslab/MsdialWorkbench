using CompMs.Common.DataObj;
using System;

namespace CompMs.Common.Enum
{
    public enum IonMode { Positive, Negative, Both }

    public static class IonModeExtension {
        public static ScanPolarity ToPolarity(this IonMode ionMode) {
            switch (ionMode) {
                case IonMode.Positive:
                    return ScanPolarity.Positive;
                case IonMode.Negative:
                    return ScanPolarity.Negative;
                case IonMode.Both:
                default:
                    return ScanPolarity.Undefined;
            }
        }
    }

    public enum MachineCategory { GCMS, LCMS, IMMS, LCIMMS, IFMS, IIMMS, IDIMS, }
    public enum MassToleranceType { Da, Ppm }
    public enum CollisionType { CID, HCD, EIEIO, ECD, HotECD, EID, OAD }
    public enum SolventType { CH3COONH4, HCOONH4 }
    [Flags]
    public enum SeparationType {
        None = 0x0,
        Infusion = 0x0,
        Chromatography = 0x1,
        IonMobility = 0x2,
        Imaging = 0x4,
    }
    public enum MSDataType { Centroid, Profile }
    public enum AcquisitionType { DDA, SWATH, AIF, None }
    public enum AnalysisFileType { Sample, Standard, QC, Blank }
    public enum RetentionType { RI, RT }
    public enum RiCompoundType { Alkanes, Fames }
    public enum AlignmentIndexType { RT, RI }
    public enum TargetOmics { Metabolomics, Lipidomics, Proteomics }
    public enum Ionization { ESI, EI }
    public enum ExportSpectraFileFormat { mgf, msp, txt, mat, ms }
    public enum ExportspectraType { profile, centroid, deconvoluted }
    public enum IonAbundanceUnit {
        Intensity, Height, Area, pmol, fmol, ng, pg,
        nmol_per_microL_plasma, pmol_per_microL_plasma, fmol_per_microL_plasma,
        nmol_per_mg_tissue, pmol_per_mg_tissue, fmol_per_mg_tissue,
        nmol_per_10E6_cells, pmol_per_10E6_cells, fmol_per_10E6_cells,
        NormalizedByInternalStandardPeakHeight, NormalizedByQcPeakHeight, NormalizedByMaxPeakOnTIC, NormalizedByMaxPeakOnNamedPeaks
    }

    public static class IonAbundanceUnitExtension {
        public static string ToLabel(this IonAbundanceUnit unit) {
            var unitEnum = unit;
            switch (unitEnum) {
                case IonAbundanceUnit.Intensity:
                    return "Original intensity";
                case IonAbundanceUnit.Height:
                    return "Height";
                case IonAbundanceUnit.Area:
                    return "Area";
                case IonAbundanceUnit.nmol_per_microL_plasma:
                    return "nmol/μL plasma";
                case IonAbundanceUnit.pmol_per_microL_plasma:
                    return "pmol/μL plasma";
                case IonAbundanceUnit.fmol_per_microL_plasma:
                    return "fmol/μL plasma";
                case IonAbundanceUnit.nmol_per_mg_tissue:
                    return "nmol/mg tissue";
                case IonAbundanceUnit.pmol_per_mg_tissue:
                    return "pmol/mg tissue";
                case IonAbundanceUnit.fmol_per_mg_tissue:
                    return "fmol/mg tissue";
                case IonAbundanceUnit.nmol_per_10E6_cells:
                    return "nmol/10^6 cells";
                case IonAbundanceUnit.pmol_per_10E6_cells:
                    return "pmol/10^6 cells";
                case IonAbundanceUnit.fmol_per_10E6_cells:
                    return "fmol/10^6 cells";
                case IonAbundanceUnit.NormalizedByInternalStandardPeakHeight:
                    return "Peak intensity/IS peak";
                case IonAbundanceUnit.NormalizedByQcPeakHeight:
                    return "Peak intensity/QC peak";
                case IonAbundanceUnit.NormalizedByMaxPeakOnTIC:
                    return "Peak intensity/TIC";
                case IonAbundanceUnit.NormalizedByMaxPeakOnNamedPeaks:
                    return "Peak intensity/MTIC";

                case IonAbundanceUnit.pmol:
                    return "pmol";
                case IonAbundanceUnit.fmol:
                    return "fmol";
                case IonAbundanceUnit.ng:
                    return "ng";
                case IonAbundanceUnit.pg:
                    return "pg";
                default:
                    return "pmol/μL plasma";

                // case IonAbundanceUnit.nmol_per_individual: return "nmol/individual";
                // case IonAbundanceUnit.pmol_per_individual: return "pmol/individual";
                // case IonAbundanceUnit.fmol_per_individual: return "fmol/individual";
                // case IonAbundanceUnit.nmol_per_microG_protein: return "nmol/μg protein";
                // case IonAbundanceUnit.pmol_per_microG_protein: return "pmol/μg protein";
                // case IonAbundanceUnit.fmol_per_microG_protein: return "fmol/μg protein";
            }
        }
    }

    public enum PeakLinkFeatureEnum {
        SameFeature, Isotope, Adduct, ChromSimilar, FoundInUpperMsMs, CorrelSimilar
    }
    [Flags]
    public enum ProcessOption {
        PeakSpotting = 1 << 0,
        Identification = 1 << 1,
        Alignment = 1 << 2,
        IdentificationPlusAlignment = Identification | Alignment,
        All = PeakSpotting | Identification | Alignment,
    };
    public enum BlankFiltering { SampleMaxOverBlankAve, SampleAveOverBlankAve }
    public enum MultivariateAnalysisOption { Pca, Plsda, Plsr, Oplsda, Oplsr, Hca }
    public enum IonMobilityType { Tims, Dtims, Twims, CCS }
    public enum AccuracyType { IsNominal, IsAccurate }

    public enum SmoothingMethod {
        SimpleMovingAverage,
        LinearWeightedMovingAverage,
        SavitzkyGolayFilter,
        BinomialFilter,
        LowessFilter,
        LoessFilter,
        TimeBasedLinearWeightedMovingAverage,
    }

    public enum ScaleMethod {
        None, MeanCenter, ParetoScale, AutoScale
    }

    public enum TransformMethod {
        None, Log10, QuadRoot
    }

    public enum DeconvolutionType { One, Both }

    public enum CoverRange { CommonRange, ExtendedRange, ExtremeRange }
    public enum PeakQuality { Ideal, Saturated, Leading, Tailing }

    public enum SearchType { ProductIon, NeutralLoss }
    public enum AndOr { AND, OR }
    public enum MsmsSimilarityCalc { ModDot, Bonanza, Cosine, All }
}
