using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Enum {
    public enum IonMode { Positive, Negative, Both }
    public enum MachineCategory { GCMS, LCMS, IMMS, LCIMMS, IFMS, IMS }
    public enum MassToleranceType { Da, Ppm }
    public enum CollisionType { CID, HCD, EIEIO, ECD, HotECD }
    public enum SolventType { CH3COONH4, HCOONH4 }
    [Flags]
    public enum SeparationType {
        Infusion = 0x0,
        Chromatography = 0x1,
        IonMobility = 0x2,
    }
    public enum MSDataType { Centroid, Profile }
    public enum AcquisitionType { DDA, SWATH, AIF }
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
        LoessFilter
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
}
