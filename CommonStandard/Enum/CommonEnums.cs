using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Enum {
    public enum IonMode { Positive, Negative, Both }
    public enum MachineCategory { GCMS, LCMS, IMMS, LCIMMS, IFMS, IMS }
    public enum MassToleranceType { Da, Ppm }
    public enum CollisionType { CID, HCD }
    public enum SolventType { CH3COONH4, HCOONH4 }
    public enum SeparationType { Chromatography, IonMobility, Infusion }
    public enum DataType { Centroid, Profile }
    public enum MethodType { ddMSMS, diMSMS }
    public enum AnalysisFileType { Sample, Standard, QC, Blank }
    public enum RetentionType { RI, RT }
    public enum RiCompoundType { Alkanes, Fames }
    public enum AlignmentIndexType { RT, RI }
    public enum TargetOmics { Metablomics, Lipidomics }
    public enum Ionization { ESI, EI }
    public enum ExportSpectraFileFormat { mgf, msp, txt, mat }
    public enum ExportspectraType { profile, centroid, deconvoluted }
    public enum IonAbundanceUnit {
        Intensity, Height, Area, pmol, fmol, ng, pg,
        nmol_per_microL_plasma, pmol_per_microL_plasma, fmol_per_microL_plasma,
        nmol_per_mg_tissue, pmol_per_mg_tissue, fmol_per_mg_tissue,
        nmol_per_10E6_cells, pmol_per_10E6_cells, fmol_per_10E6_cells,
        NormalizedByInternalStandardPeakHeight, NormalizedByQcPeakHeight, NormalizedByTIC, NormalizedByMTIC
    }
    public enum PeakLinkFeatureEnum {
        SameFeature, Isotope, Adduct, ChromSimilar, FoundInUpperMsMs, CorrelSimilar
    }
}
