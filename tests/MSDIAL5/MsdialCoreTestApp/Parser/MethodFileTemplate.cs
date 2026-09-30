using CompMs.Common;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Globalization;
using System.Text;

namespace CompMs.App.MsdialConsole.Parser;

/// <summary>
/// The processing modes a method file can be written for, one per Console command.
/// </summary>
public enum MethodFileMode
{
    Gcms,
    Lcms,
    Dims,
    Imms,
    Lcimms,
}

/// <summary>
/// Writes a method file that the Console reads back without a single key reported as having no
/// effect.
/// </summary>
/// <remarks>
/// WHY THIS EXISTS. The only list of the keys a method file may contain is the set of case labels
/// in <see cref="ConfigParser"/>, and the only examples were files kept outside the repository. An
/// analyst started from somebody else's file, and the key report then told them which of its lines
/// had never worked. This hands them a file that works before they change anything.
///
/// THE VALUES ARE THE BUILT-IN DEFAULTS, taken from a freshly constructed parameter object for the
/// mode rather than typed here, so the template cannot claim a default the run does not use. Reading
/// the template back therefore changes nothing, and MethodFileTemplateTests hold it to that: a key
/// written here that the reader does not claim, or claims and assigns somewhere else, fails a test.
///
/// TWO VALUES ARE THE ONES A RUN USES RATHER THAN THE ONES THE OBJECT HOLDS. The ion mode is the one
/// asked for, and the searched adducts are the proton adduct of that ion mode. The parameter starts
/// with no adducts, and PeakCharacterEstimator falls back to [M+H]+ or [M-H]- when it finds none, so
/// writing the fallback out states what the run would do and gives the analyst a line to extend.
///
/// NOT EVERY KEY THE READER ACCEPTS IS WRITTEN. Aliases, lab-internal switches and keys the mode's
/// processing never consults are left out, so that each line in the template is one that matters.
/// So are the features that do not work from the Console yet - isotope tracking, CorrDec, the
/// isotope text DB - and the target-detection compound list, which is read and then used by no
/// process. Each library path sits in the section of the annotation that uses it.
/// Choices are listed only as the reader accepts them: several arms take a subset of their enum and
/// silently keep the default for the rest, so offering the whole enum would invite exactly that.
/// </remarks>
public static class MethodFileTemplate
{
    public static string Create(MethodFileMode mode, IonMode ionMode = IonMode.Positive) {
        var w = new Writer();
        w.Header(mode);
        switch (mode) {
            case MethodFileMode.Gcms: WriteGcms(w, WithIonMode(new MsdialGcmsParameter(), ionMode)); break;
            case MethodFileMode.Lcms: WriteLcms(w, WithIonMode(new MsdialLcmsParameter(), ionMode)); break;
            case MethodFileMode.Dims: WriteDims(w, WithIonMode(new MsdialDimsParameter(), ionMode)); break;
            case MethodFileMode.Imms: WriteImms(w, WithIonMode(new MsdialImmsParameter(), ionMode)); break;
            case MethodFileMode.Lcimms: WriteLcimms(w, WithIonMode(new MsdialLcImMsParameter(), ionMode)); break;
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        return w.ToString();
    }

    public static string CommandName(MethodFileMode mode) => mode.ToString().ToLowerInvariant();

    /// <summary>
    /// The adduct a run searches for when the method file names none.
    /// </summary>
    /// <remarks>
    /// Mirrors the fallback in PeakCharacterEstimator.SearchedAdductInitialize.
    /// </remarks>
    public static string DefaultAdduct(IonMode ionMode) => ionMode == IonMode.Positive ? "[M+H]+" : "[M-H]-";

    private static T WithIonMode<T>(T parameter, IonMode ionMode) where T : ParameterBase {
        parameter.IonMode = ionMode;
        return parameter;
    }

    private static void WriteGcms(Writer w, MsdialGcmsParameter p) {
        w.Section("Data type");
        DataType(w, p, ms2: false);
        w.Section("Data collection");
        w.Choice("Accuracy type", p.AccuracyType.ToString(), "IsNominal", "IsAccurate");
        w.Comment("IsNominal fixes Mass slice width and MS1 tolerance for centroid at 0.5 regardless of the values below.");
        RetentionRange(w, p);
        w.Value("MS1 mass range begin", F(p.MassRangeBegin));
        w.Value("MS1 mass range end", F(p.MassRangeEnd));
        w.Value("MS1 tolerance for centroid", F(p.CentroidMs1Tolerance));
        Threads(w, p);
        w.Section("Peak detection");
        PeakDetection(w, p);
        w.Section("Deconvolution");
        w.Value("Sigma window value", F(p.SigmaWindowValue));
        w.Value("Amplitude cut off", F(p.ChromDecBaseParam.AmplitudeCutoff));
        w.Section("Retention index");
        w.Blank("RI dictionary file path", "Tab-separated carbon number and retention time, used when Retention type is RI.");
        w.Choice("Retention type", p.RetentionType.ToString(), "RT", "RI");
        w.Choice("RI compound type", p.RiCompoundType.ToString(), "Alkanes", "Fames");
        w.Section("MSP-based annotation");
        w.Blank("MSP file path", "EI spectral library (.msp). Leave blank to skip MSP-based annotation.");
        w.Value("RT tolerance for MSP-based annotation", F(p.MspSearchParam.RtTolerance));
        w.Comment("On the Kovats scale (Alkanes) 20 is a fifth of a carbon; on the Fiehn scale (Fames) a comparable window is about 20000.");
        w.Value("RI tolerance for MSP-based annotation", F(p.MspSearchParam.RiTolerance));
        w.Value("Mass range begin for MSP-based annotation", F(p.MspSearchParam.MassRangeBegin));
        w.Value("Mass range end for MSP-based annotation", F(p.MspSearchParam.MassRangeEnd));
        w.Value("MS1 tolerance for MSP-based annotation", F(p.MspSearchParam.Ms1Tolerance));
        w.Value("Relative amplitude cutoff for MSP-based annotation", F(p.MspSearchParam.RelativeAmpCutoff));
        w.Value("Absolute amplitude cutoff for MSP-based annotation", F(p.MspSearchParam.AbsoluteAmpCutoff));
        SpectrumScores(w, p.MspSearchParam, "MSP");
        w.Value("Total score cutoff for MSP-based annotation", F(p.MspSearchParam.TotalScoreCutoff));
        w.Value("Use retention information for MSP-based annotation scoring", B(p.MspSearchParam.IsUseTimeForAnnotationScoring));
        w.Value("Use retention information for MSP-based annotation filtering", B(p.MspSearchParam.IsUseTimeForAnnotationFiltering));
        w.Value("Only report top hit for MSP-based annotation", B(p.OnlyReportTopHitInMspSearch));
        w.Section("Text-based annotation");
        w.Blank("Text DB file path", "Tab-separated compound list matched by m/z and retention. Leave blank to skip Text-based annotation.");
        w.Value("RT tolerance for Text-based annotation", F(p.TextDbSearchParam.RtTolerance));
        w.Value("RI tolerance for Text-based annotation", F(p.TextDbSearchParam.RiTolerance));
        w.Value("Accurate MS1 tolerance for Text-based annotation", F(p.TextDbSearchParam.Ms1Tolerance));
        w.Value("Total score cutoff for Text-based annotation", F(p.TextDbSearchParam.TotalScoreCutoff));
        w.Value("Use retention information for Text-based annotation scoring", B(p.TextDbSearchParam.IsUseTimeForAnnotationScoring));
        w.Value("Use retention information for Text-based annotation filtering", B(p.TextDbSearchParam.IsUseTimeForAnnotationFiltering));
        w.Value("Only report top hit for Text-based annotation", B(p.OnlyReportTopHitInTextDBSearch));
        w.Section("Alignment");
        w.Choice("Alignment index type", p.AlignmentIndexType.ToString(), "RT", "RI");
        w.Value("Alignment reference file ID", I(p.AlignmentReferenceFileID));
        w.Value("Retention time tolerance for alignment", F(p.RetentionTimeAlignmentTolerance));
        w.Value("Retention index tolerance for alignment", F(p.RetentionIndexAlignmentTolerance));
        w.Value("Retention time factor for alignment", F(p.RetentionTimeAlignmentFactor));
        w.Value("Spectrum similarity tolerance for alignment", F(p.SpectrumSimilarityAlignmentTolerance));
        w.Value("Spectrum similarity factor for alignment", F(p.SpectrumSimilarityAlignmentFactor));
        w.Value("MS1 tolerance for alignment", F(p.Ms1AlignmentTolerance));
        w.Value("Force insert peaks in gap filling", B(p.IsForceInsertForGapFilling));
        w.Value("Replace quant mass by user defined value", B(p.IsReplaceQuantmassByUserDefinedValue));
        w.Value("Is quant mass based on base peak mz", B(p.IsRepresentativeQuantMassBasedOnBasePeakMz));
        Filtering(w, p);
    }

    private static void WriteLcms(Writer w, MsdialLcmsParameter p) {
        w.Section("Data type");
        DataType(w, p, ms2: true);
        TargetAndAcquisition(w, p);
        w.Section("Data collection");
        RetentionRange(w, p);
        MassRanges(w, p);
        Threads(w, p);
        w.Section("Peak detection");
        PeakDetection(w, p);
        w.Value("Max charge number", I(p.MaxChargeNumber));
        Adducts(w, p);
        w.Section("Deconvolution");
        LiquidDeconvolution(w, p);
        w.Value("Target CE", D(p.TargetCE));
        LiquidAnnotation(w, p, retention: true, mobility: false, annotatorTables: true);
        w.Section("Alignment");
        LiquidAlignment(w, p, retention: true);
        Filtering(w, p);
        w.Section("Retention time correction");
        w.Blank("Compounds library file path for RT correction", "Reference compounds whose retention times anchor the correction.");
        w.Blank("RT correction peak selection file path", "Reviewed anchor-peak selection (TSV), the file rtcorrection takes as --selection.");
        w.Value("Execute RT correction", B(p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection));
        w.Value("RT correction with smoothing for RT diff", B(p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing));
        w.Value("User setting intercept", D(p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept));
        w.Choice("RT diff calc method", p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod.ToString(), "SampleMinusSampleAverage", "SampleMinusReference");
        w.Choice("Interpolation method", p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod.ToString(), "Linear");
        w.Choice("Extrapolation method (begin)", p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin.ToString(), "UserSetting", "FirstPoint", "LinearExtrapolation");
        w.Choice("Extrapolation method (end)", p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd.ToString(), "LastPoint", "LinearExtrapolation");
        w.Choice("RT correction peak selection mode", p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.PeakSelectionMode.ToString(), Enum.GetNames(typeof(RetentionTimeCorrectionPeakSelectionMode)));
        w.Comment("Between 0 and 1.");
        w.Value("RT correction peak selection RT weight", D(p.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.PeakSelectionRtWeight));
        w.Section("Console output");
        w.Value("Alignment light mode", "False");
        w.Value("Detailed alignment provenance", "False");
        w.Value("Export annotation candidates", "False");
    }

    private static void WriteDims(Writer w, MsdialDimsParameter p) {
        w.Section("Data type");
        DataType(w, p, ms2: true);
        TargetAndAcquisition(w, p);
        w.Section("Data collection");
        MassRanges(w, p);
        Threads(w, p);
        w.Section("Peak detection");
        PeakDetection(w, p);
        w.Value("Max charge number", I(p.MaxChargeNumber));
        Adducts(w, p);
        w.Section("Deconvolution");
        LiquidDeconvolution(w, p);
        LiquidAnnotation(w, p, retention: false, mobility: false);
        w.Section("Alignment");
        LiquidAlignment(w, p, retention: false);
        Filtering(w, p);
    }

    private static void WriteImms(Writer w, MsdialImmsParameter p) {
        w.Section("Data type");
        DataType(w, p, ms2: true);
        TargetAndAcquisition(w, p);
        w.Section("Data collection");
        MassRanges(w, p);
        Threads(w, p);
        w.Section("Ion mobility");
        IonMobility(w, p.IonMobilityType, p.DriftTimeBegin, p.DriftTimeEnd, p.DriftTimeAlignmentTolerance, p.DriftTimeAlignmentFactor);
        w.Section("Peak detection");
        PeakDetection(w, p);
        w.Value("Max charge number", I(p.MaxChargeNumber));
        Adducts(w, p);
        w.Section("Deconvolution");
        LiquidDeconvolution(w, p);
        LiquidAnnotation(w, p, retention: false, mobility: true);
        w.Section("Alignment");
        LiquidAlignment(w, p, retention: false);
        Filtering(w, p);
    }

    private static void WriteLcimms(Writer w, MsdialLcImMsParameter p) {
        w.Section("Data type");
        DataType(w, p, ms2: true);
        TargetAndAcquisition(w, p);
        w.Section("Data collection");
        RetentionRange(w, p);
        MassRanges(w, p);
        Threads(w, p);
        w.Section("Ion mobility");
        IonMobility(w, p.IonMobilityType, p.DriftTimeBegin, p.DriftTimeEnd, p.DriftTimeAlignmentTolerance, p.DriftTimeAlignmentFactor);
        w.Value("Accumulated RT range", F(p.AccumulatedRtRange));
        w.Value("Accumulate MS2 spectra", B(p.IsAccumulateMS2Spectra));
        w.Section("Peak detection");
        PeakDetection(w, p);
        w.Value("Max charge number", I(p.MaxChargeNumber));
        Adducts(w, p);
        w.Section("Deconvolution");
        LiquidDeconvolution(w, p);
        LiquidAnnotation(w, p, retention: true, mobility: true);
        w.Section("Alignment");
        LiquidAlignment(w, p, retention: true);
        Filtering(w, p);
    }

    private static void DataType(Writer w, ParameterBase p, bool ms2) {
        w.Choice("MS1 data type", p.MSDataType.ToString(), "Centroid", "Profile");
        if (ms2) {
            w.Choice("MS2 data type", p.MS2DataType.ToString(), "Centroid", "Profile");
        }
        w.Choice("Ion mode", p.IonMode.ToString(), "Positive", "Negative");
    }

    private static void TargetAndAcquisition(Writer w, ParameterBase p) {
        w.Choice("Target omics", p.TargetOmics.ToString(), "Metabolomics", "Lipidomics");
        // The parameter starts at None and CommonProcess turns None into DDA before anything reads
        // it, so DDA is the default a run actually uses, and writing it back changes nothing.
        w.Choice("Acquisition type", "DDA", "DDA", "SWATH", "AIF");
    }

    private static void RetentionRange(Writer w, ParameterBase p) {
        w.Value("Retention time begin", F(p.RetentionTimeBegin));
        w.Value("Retention time end", F(p.RetentionTimeEnd));
    }

    private static void MassRanges(Writer w, ParameterBase p) {
        w.Value("MS1 mass range begin", F(p.MassRangeBegin));
        w.Value("MS1 mass range end", F(p.MassRangeEnd));
        w.Value("MS2 mass range begin", F(p.Ms2MassRangeBegin));
        w.Value("MS2 mass range end", F(p.Ms2MassRangeEnd));
        w.Value("MS1 tolerance for centroid", F(p.CentroidMs1Tolerance));
        w.Value("MS2 tolerance for centroid", F(p.CentroidMs2Tolerance));
    }

    private static void Threads(Writer w, ParameterBase p) {
        w.Value("Number of threads", I(p.NumThreads));
    }

    private static void PeakDetection(Writer w, ParameterBase p) {
        w.Choice("Smoothing method", p.SmoothingMethod.ToString(), Enum.GetNames(typeof(SmoothingMethod)));
        w.Value("Smoothing level", I(p.SmoothingLevel));
        w.Value("Minimum peak width", D(p.MinimumDatapoints));
        w.Value("Minimum peak height", D(p.MinimumAmplitude));
        w.Value("Average peak width", F(p.AveragePeakWidth));
        w.Value("Mass slice width", F(p.MassSliceWidth));
    }

    private static void Adducts(Writer w, ParameterBase p) {
        w.Comment(p.IonMode == IonMode.Positive
            ? "Comma-separated and of the same polarity as Ion mode, e.g. [M+H]+,[M+Na]+,[M+NH4]+"
            : "Comma-separated and of the same polarity as Ion mode, e.g. [M-H]-,[M+Cl]-,[M+HCOO]-");
        w.Value("Searched adduct ions", DefaultAdduct(p.IonMode));
    }

    private static void LiquidDeconvolution(Writer w, ParameterBase p) {
        w.Value("Sigma window value", F(p.SigmaWindowValue));
        w.Value("Amplitude cut off", F(p.ChromDecBaseParam.AmplitudeCutoff));
        w.Value("Keep isotope range", F(p.KeptIsotopeRange));
        w.Value("Exclude after precursor", B(p.RemoveAfterPrecursor));
        w.Value("Keep original precursor isotopes", B(p.KeepOriginalPrecursorIsotopes));
    }

    private static void LiquidAnnotation(Writer w, ParameterBase p, bool retention, bool mobility, bool annotatorTables = false) {
        w.Section("MSP-based annotation");
        w.Blank("MSP file path", "MS/MS spectral library (.msp). Leave blank to skip MSP-based annotation.");
        if (annotatorTables) {
            w.Blank("MSP annotator settings file path", "Tab-separated table of MSP libraries, each with its own search settings and priority. When given, it is used instead of MSP file path and the settings below.");
        }
        SearchWindow(w, p.MspSearchParam, "MSP", retention, mobility);
        w.Value("Mass range begin for MSP-based annotation", F(p.MspSearchParam.MassRangeBegin));
        w.Value("Mass range end for MSP-based annotation", F(p.MspSearchParam.MassRangeEnd));
        w.Value("Relative amplitude cutoff for MSP-based annotation", F(p.MspSearchParam.RelativeAmpCutoff));
        w.Value("Absolute amplitude cutoff for MSP-based annotation", F(p.MspSearchParam.AbsoluteAmpCutoff));
        SpectrumScores(w, p.MspSearchParam, "MSP");
        w.Value("Total score cutoff for MSP-based annotation", F(p.MspSearchParam.TotalScoreCutoff));
        SearchSwitches(w, p.MspSearchParam, "MSP", retention, mobility);
        w.Value("Only report top hit for MSP-based annotation", B(p.OnlyReportTopHitInMspSearch));
        w.Value("Execute annotation process only for alignment file", B(p.IsIdentificationOnlyPerformedForAlignmentFile));

        w.Section("LBM-based annotation (Target omics: Lipidomics)");
        w.Blank("LBM file path", "Lipid library (.lbm). Leave blank to skip LBM-based annotation.");
        if (annotatorTables) {
            w.Comment("Breaks a tie between an LBM match and an MSP match of equal evidence: the larger priority is reported.");
            w.Value("LBM annotator priority", "1");
        }
        SearchWindow(w, p.LbmSearchParam, "LBM", retention, mobility);
        w.Value("Mass range begin for LBM-based annotation", F(p.LbmSearchParam.MassRangeBegin));
        w.Value("Mass range end for LBM-based annotation", F(p.LbmSearchParam.MassRangeEnd));
        w.Value("Relative amplitude cutoff for LBM-based annotation", F(p.LbmSearchParam.RelativeAmpCutoff));
        w.Value("Absolute amplitude cutoff for LBM-based annotation", F(p.LbmSearchParam.AbsoluteAmpCutoff));
        w.Value("Square root of weighted dot product cutoff for LBM-based annotation", F(p.LbmSearchParam.WeightedDotProductCutOff));
        w.Value("Square root of simple dot product cutoff for LBM-based annotation", F(p.LbmSearchParam.SimpleDotProductCutOff));
        w.Value("Square root of reverse dot product cutoff for LBM-based annotation", F(p.LbmSearchParam.ReverseDotProductCutOff));
        w.Value("Matched peaks percentage cutoff for LBM-based annotation", F(p.LbmSearchParam.MatchedPeaksPercentageCutOff));
        w.Value("Minimum spectrum match for LBM-based annotation", F(p.LbmSearchParam.MinimumSpectrumMatch));
        w.Value("Total score cutoff for LBM-based annotation", F(p.LbmSearchParam.TotalScoreCutoff));
        SearchSwitches(w, p.LbmSearchParam, "LBM", retention, mobility);
        w.Choice("Solvent type", p.LipidQueryContainer.SolventType.ToString(), "CH3COONH4", "HCOONH4", "NH4HCO3");

        w.Section("Text-based annotation");
        w.Blank("Text DB file path", "Tab-separated compound list matched by m/z. Leave blank to skip Text-based annotation.");
        if (annotatorTables) {
            w.Blank("Text annotator settings file path", "Tab-separated table of text libraries, each with its own search settings and priority. When given, it is used instead of Text DB file path and the settings below.");
        }
        if (retention) {
            w.Value("RT tolerance for Text-based annotation", F(p.TextDbSearchParam.RtTolerance));
        }
        if (mobility) {
            w.Value("CCS tolerance for Text-based annotation", F(p.TextDbSearchParam.CcsTolerance));
        }
        w.Value("Accurate MS1 tolerance for Text-based annotation", F(p.TextDbSearchParam.Ms1Tolerance));
        w.Value("Total score cutoff for Text-based annotation", F(p.TextDbSearchParam.TotalScoreCutoff));
        if (retention) {
            w.Value("Use retention information for Text-based annotation scoring", B(p.TextDbSearchParam.IsUseTimeForAnnotationScoring));
            w.Value("Use retention information for Text-based annotation filtering", B(p.TextDbSearchParam.IsUseTimeForAnnotationFiltering));
        }
        if (mobility) {
            w.Value("Use CCS for Text-based annotation scoring", B(p.TextDbSearchParam.IsUseCcsForAnnotationScoring));
            w.Value("Use CCS for Text-based annotation filtering", B(p.TextDbSearchParam.IsUseCcsForAnnotationFiltering));
        }
        w.Value("Only report top hit for Text-based annotation", B(p.OnlyReportTopHitInTextDBSearch));
    }

    private static void SearchWindow(Writer w, MsRefSearchParameterBase s, string kind, bool retention, bool mobility) {
        if (retention) {
            w.Value($"RT tolerance for {kind}-based annotation", F(s.RtTolerance));
        }
        if (mobility) {
            w.Value($"CCS tolerance for {kind}-based annotation", F(s.CcsTolerance));
        }
        w.Value($"MS1 tolerance for {kind}-based annotation", F(s.Ms1Tolerance));
        w.Value($"MS2 tolerance for {kind}-based annotation", F(s.Ms2Tolerance));
    }

    private static void SpectrumScores(Writer w, MsRefSearchParameterBase s, string kind) {
        w.Value($"Square root of weighted dot product cutoff for {kind}-based annotation", F(s.WeightedDotProductCutOff));
        w.Value($"Square root of simple dot product cutoff for {kind}-based annotation", F(s.SimpleDotProductCutOff));
        w.Value($"Square root of reverse dot product cutoff for {kind}-based annotation", F(s.ReverseDotProductCutOff));
        w.Value($"Matched peaks percentage cutoff for {kind}-based annotation", F(s.MatchedPeaksPercentageCutOff));
        w.Value($"Minimum spectrum match for {kind}-based annotation", F(s.MinimumSpectrumMatch));
    }

    private static void SearchSwitches(Writer w, MsRefSearchParameterBase s, string kind, bool retention, bool mobility) {
        if (retention) {
            w.Value($"Use retention information for {kind}-based annotation scoring", B(s.IsUseTimeForAnnotationScoring));
            w.Value($"Use retention information for {kind}-based annotation filtering", B(s.IsUseTimeForAnnotationFiltering));
        }
        if (mobility) {
            w.Value($"Use CCS for {kind}-based annotation scoring", B(s.IsUseCcsForAnnotationScoring));
            w.Value($"Use CCS for {kind}-based annotation filtering", B(s.IsUseCcsForAnnotationFiltering));
        }
    }

    private static void IonMobility(Writer w, IonMobilityType type, float begin, float end, float tolerance, float factor) {
        w.Choice("Ion mobility type", type.ToString(), "Tims", "Dtims", "Twims", "CCS");
        w.Value("Drift time begin", F(begin));
        w.Value("Drift time end", F(end));
        w.Value("Drift time alignment tolerance", F(tolerance));
        w.Value("Drift time alignment factor", F(factor));
    }

    private static void LiquidAlignment(Writer w, ParameterBase p, bool retention) {
        w.Value("Alignment reference file ID", I(p.AlignmentReferenceFileID));
        if (retention) {
            w.Value("Retention time tolerance for alignment", F(p.RetentionTimeAlignmentTolerance));
            w.Value("Retention time factor for alignment", F(p.RetentionTimeAlignmentFactor));
        }
        w.Value("MS1 tolerance for alignment", F(p.Ms1AlignmentTolerance));
        w.Value("MS1 factor for alignment", F(p.Ms1AlignmentFactor));
        w.Value("Force insert peaks in gap filling", B(p.IsForceInsertForGapFilling));
    }

    private static void Filtering(Writer w, ParameterBase p) {
        w.Section("Filtering");
        w.Value("Peak count filter", F(p.PeakCountFilter));
        w.Value("N percent detected in one group", F(p.NPercentDetectedInOneGroup));
        w.Value("Remove feature based on peak height fold-change", B(p.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange));
        w.Choice("Blank filtering", p.BlankFiltering.ToString(), "SampleMaxOverBlankAve");
        w.Value("Sample max / blank average", F(p.SampleMaxOverBlankAverage));
        w.Value("Sample average / blank average", F(p.SampleAverageOverBlankAverage));
        w.Value("Keep reference matched metabolites", B(p.IsKeepRefMatchedMetaboliteFeatures));
        w.Value("Keep suggested metabolites", B(p.IsKeepSuggestedMetaboliteFeatures));
        w.Value("Keep removable features and assigned tag for checking", B(p.IsKeepRemovableFeaturesAndAssignedTagForChecking));
        w.Value("Replace true zero values with 1/2 of minimum peak height over all samples", B(p.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples));
    }

    // Invariant culture throughout: the reader parses with it, and a template written on a machine
    // whose decimal separator is a comma must still read back as the same numbers.
    private static string F(float value) => value.ToString(CultureInfo.InvariantCulture);
    private static string D(double value) => value.ToString(CultureInfo.InvariantCulture);
    private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string B(bool value) => value ? "True" : "False";

    private sealed class Writer
    {
        private readonly StringBuilder _text = new StringBuilder();

        public void Header(MethodFileMode mode) {
            Comment($"MS-DIAL console method file for the '{CommandName(mode)}' command.");
            Comment($"Written by MS-DIAL {MsdialBuildIdentity.DisplayVersion}. Every value below is the built-in default.");
            Comment("One 'Key: value' per line. Keys are not case-sensitive, and lines starting with # are ignored.");
            Comment("A comment must be on its own line: text after a value is read as part of the value.");
            Comment("A key left blank, or deleted, keeps its built-in default.");
            Comment("Relative file paths are resolved against the folder of this method file.");
            Comment($"When the run starts, what happened to each key is written beside this file as <name>.keys.json.");
        }

        public void Section(string title) {
            _text.Append('\n');
            Comment(title);
        }

        public void Comment(string text) {
            _text.Append("# ").Append(text).Append('\n');
        }

        public void Value(string key, string value) {
            _text.Append(key).Append(": ").Append(value).Append('\n');
        }

        public void Choice(string key, string value, params string[] choices) {
            if (choices.Length > 1) {
                Comment(string.Join(" | ", choices));
            }
            Value(key, value);
        }

        public void Blank(string key, string? description = null) {
            if (description is not null) {
                Comment(description);
            }
            _text.Append(key).Append(":\n");
        }

        public override string ToString() => _text.ToString();
    }
}
