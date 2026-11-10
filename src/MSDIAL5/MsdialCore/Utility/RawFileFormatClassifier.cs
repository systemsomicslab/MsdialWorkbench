using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using System;
using System.IO;

namespace CompMs.MsdialCore.Utility
{
    /// <summary>
    /// Tells the format of a raw data file without reading it, so that it can be recorded on
    /// <see cref="AnalysisFileBean.RawFileFormat"/> while the file is present.
    /// </summary>
    /// <remarks>
    /// Most formats are told by the extension alone, in any letter case. Two extensions need the disk: a .raw is a
    /// Waters folder or a Thermo file, and a .d folder is Agilent's when it holds AcqData and Bruker's when it holds
    /// analysis.tdf, analysis.tsf or analysis.baf. A .d is checked in the order RawDataHandler reads it, AcqData
    /// first, so the format named is the one whose reader MS-DIAL used. Without the raw data on disk these two are
    /// <see cref="RawFileFormat.Unknown"/>. Nothing here throws: a path that cannot be checked is Unknown.
    /// </remarks>
    public static class RawFileFormatClassifier
    {
        /// <summary>
        /// The format of the raw data file at <paramref name="path"/>, or <see cref="RawFileFormat.Unknown"/> when it
        /// cannot be told.
        /// </summary>
        public static RawFileFormat Classify(string path) {
            if (string.IsNullOrEmpty(path)) {
                return RawFileFormat.Unknown;
            }
            try {
                switch (Path.GetExtension(path).ToLowerInvariant()) {
                    case ".raw": return ClassifyDotRaw(path);
                    case ".d": return ClassifyDotD(path);
                    case ".wiff": return RawFileFormat.SciexWiff;
                    case ".wiff2": return RawFileFormat.SciexWiff2;
                    case ".lcd": return RawFileFormat.ShimadzuLcd;
                    case ".qgd": return RawFileFormat.ShimadzuQgd;
                    case ".mzml": return RawFileFormat.MzML;
                    case ".imzml": return RawFileFormat.ImzML;
                    case ".cdf": return RawFileFormat.NetCdf;
                    case ".abf": return RawFileFormat.Abf;
                    case ".ibf": return RawFileFormat.Ibf;
                    case ".lrp": return RawFileFormat.KanomaxLrp;
                    case ".hmd": return RawFileFormat.HiveHmd;
                    case ".mzb": return RawFileFormat.HiveMzb;
                    default: return RawFileFormat.Unknown;
                }
            }
            catch (Exception ex) when (IsPathFailure(ex)) {
                return RawFileFormat.Unknown;
            }
        }

        /// <summary>
        /// Records the format of <paramref name="file"/> when none is recorded yet and its raw data tell it.
        /// A recorded format is never replaced. Returns whether a format was recorded.
        /// </summary>
        public static bool RecordIfUnknown(AnalysisFileBean file) {
            if (file is null || file.RawFileFormat != RawFileFormat.Unknown) {
                return false;
            }
            var format = Classify(file.AnalysisFilePath);
            if (format == RawFileFormat.Unknown) {
                return false;
            }
            file.RawFileFormat = format;
            return true;
        }

        /// <summary>
        /// Whether a file with <paramref name="extension"/> (with its leading dot, any letter case) can be of
        /// <paramref name="format"/>. <see cref="RawFileFormat.Unknown"/> is consistent with nothing.
        /// </summary>
        public static bool IsConsistentWith(RawFileFormat format, string extension) {
            var expected = ExtensionOf(format);
            return expected is not null && string.Equals(expected, extension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The format to describe <paramref name="file"/> by: what <see cref="Classify"/> tells from the disk now, and when
        /// the disk cannot tell (the raw data are deleted or moved), the recorded format when it fits the file's extension.
        /// </summary>
        /// <remarks>
        /// The disk comes first because a format is recorded once and never replaced: if another file of the same extension
        /// later takes the place of the recorded one (a Waters .raw folder where a Thermo .raw file was), the record no
        /// longer describes what is at the path. A file on disk is therefore described by what is there, and the record is
        /// used only for what the disk can no longer tell.
        /// </remarks>
        public static RawFileFormat Resolve(AnalysisFileBean file) {
            var onDisk = Classify(file.AnalysisFilePath);
            if (onDisk != RawFileFormat.Unknown) {
                return onDisk;
            }
            var recorded = file.RawFileFormat;
            return IsConsistentWith(recorded, ExtensionOrNull(file.AnalysisFilePath)) ? recorded : RawFileFormat.Unknown;
        }

        private static RawFileFormat ClassifyDotRaw(string path) {
            if (Directory.Exists(path)) {
                return RawFileFormat.WatersRaw;
            }
            if (File.Exists(path)) {
                return RawFileFormat.ThermoRaw;
            }
            return RawFileFormat.Unknown;
        }

        private static RawFileFormat ClassifyDotD(string path) {
            if (Directory.Exists(Path.Combine(path, "AcqData"))) {
                return RawFileFormat.AgilentMassHunterD;
            }
            if (File.Exists(Path.Combine(path, "analysis.tdf"))) {
                return RawFileFormat.BrukerTdf;
            }
            if (File.Exists(Path.Combine(path, "analysis.tsf"))) {
                return RawFileFormat.BrukerTsf;
            }
            if (File.Exists(Path.Combine(path, "analysis.baf"))) {
                return RawFileFormat.BrukerBaf;
            }
            return RawFileFormat.Unknown;
        }

        private static string? ExtensionOf(RawFileFormat format) {
            switch (format) {
                case RawFileFormat.ThermoRaw:
                case RawFileFormat.WatersRaw:
                    return ".raw";
                case RawFileFormat.AgilentMassHunterD:
                case RawFileFormat.BrukerTdf:
                case RawFileFormat.BrukerTsf:
                case RawFileFormat.BrukerBaf:
                    return ".d";
                case RawFileFormat.SciexWiff: return ".wiff";
                case RawFileFormat.SciexWiff2: return ".wiff2";
                case RawFileFormat.ShimadzuLcd: return ".lcd";
                case RawFileFormat.ShimadzuQgd: return ".qgd";
                case RawFileFormat.MzML: return ".mzml";
                case RawFileFormat.ImzML: return ".imzml";
                case RawFileFormat.NetCdf: return ".cdf";
                case RawFileFormat.Abf: return ".abf";
                case RawFileFormat.Ibf: return ".ibf";
                case RawFileFormat.KanomaxLrp: return ".lrp";
                case RawFileFormat.HiveHmd: return ".hmd";
                case RawFileFormat.HiveMzb: return ".mzb";
                default: return null;
            }
        }

        private static string? ExtensionOrNull(string path) {
            try {
                return Path.GetExtension(path);
            }
            catch (Exception ex) when (IsPathFailure(ex)) {
                return null;
            }
        }

        private static bool IsPathFailure(Exception ex) {
            return ex is ArgumentException || ex is IOException || ex is NotSupportedException
                || ex is UnauthorizedAccessException || ex is System.Security.SecurityException;
        }
    }
}
