using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export;

public readonly struct AlignmentLightPeakRow {
    public AlignmentLightPeakRow(
        int fileID,
        string fileName,
        int masterPeakID,
        int peakID,
        double peakHeightTop,
        double peakAreaAboveZero,
        double normalizedPeakHeight,
        double normalizedPeakAreaAboveZero,
        double rt,
        double ri,
        double mobility,
        double collisionCrossSection,
        double mass,
        float signalToNoise,
        int ms1RawSpectrumIdTop,
        int ms2RawSpectrumID,
        int representativeLibraryID,
        bool isMsmsAssigned) {
        FileID = fileID;
        FileName = fileName;
        MasterPeakID = masterPeakID;
        PeakID = peakID;
        PeakHeightTop = peakHeightTop;
        PeakAreaAboveZero = peakAreaAboveZero;
        NormalizedPeakHeight = normalizedPeakHeight;
        NormalizedPeakAreaAboveZero = normalizedPeakAreaAboveZero;
        Rt = rt;
        Ri = ri;
        Mobility = mobility;
        CollisionCrossSection = collisionCrossSection;
        Mass = mass;
        SignalToNoise = signalToNoise;
        MS1RawSpectrumIdTop = ms1RawSpectrumIdTop;
        MS2RawSpectrumID = ms2RawSpectrumID;
        RepresentativeLibraryID = representativeLibraryID;
        IsMsmsAssigned = isMsmsAssigned;
    }

    public int FileID { get; }
    public string FileName { get; }
    public int MasterPeakID { get; }
    public int PeakID { get; }
    public double PeakHeightTop { get; }
    public double PeakAreaAboveZero { get; }
    public double NormalizedPeakHeight { get; }
    public double NormalizedPeakAreaAboveZero { get; }
    public double Rt { get; }
    public double Ri { get; }
    public double Mobility { get; }
    public double CollisionCrossSection { get; }
    public double Mass { get; }
    public float SignalToNoise { get; }
    public int MS1RawSpectrumIdTop { get; }
    public int MS2RawSpectrumID { get; }
    public int RepresentativeLibraryID { get; }
    public bool IsMsmsAssigned { get; }
}

public sealed class AlignmentLightPeakStore : IDisposable {
    private const int RecordSize = 102;

    private readonly string _filePath;
    private readonly FileStream _stream;
    private readonly BinaryWriter _writer;
    private IReadOnlyList<AnalysisFileBean> _files = Array.Empty<AnalysisFileBean>();
    private Dictionary<int, int> _fileIdToIndex = new Dictionary<int, int>();
    private Dictionary<int, int>? _logicalToPhysicalSpotId;
    private int _spotCount;
    private bool _disposed;
    private bool _initialized;

    private AlignmentLightPeakStore(string filePath) {
        _filePath = filePath;
        _stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        _writer = new BinaryWriter(_stream);
    }

    public static AlignmentLightPeakStore CreateTemp() {
        return new AlignmentLightPeakStore(Path.GetTempFileName());
    }

    public void Initialize(int spotCount, IReadOnlyList<AnalysisFileBean> files) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AlignmentLightPeakStore));
        }
        if (_initialized) {
            throw new InvalidOperationException("Alignment light peak matrix store was already initialized.");
        }
        if (spotCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(spotCount));
        }
        if (files is null || files.Count == 0) {
            throw new ArgumentException("At least one analysis file is required.", nameof(files));
        }

        _spotCount = spotCount;
        _files = files.OrderBy(file => file.AnalysisFileId).ToArray();
        _fileIdToIndex = _files
            .Select((file, index) => (file.AnalysisFileId, index))
            .ToDictionary(pair => pair.AnalysisFileId, pair => pair.index);
        _stream.SetLength((long)_spotCount * _files.Count * RecordSize);
        _initialized = true;
    }

    public void WriteSpotPeaks(int spotID, IReadOnlyList<AlignmentChromPeakFeature> peaks) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AlignmentLightPeakStore));
        }

        foreach (var peak in peaks) {
            WriteSpotPeak(spotID, peak);
        }
        _writer.Flush();
    }

    public void WriteSpotPeak(int spotID, AlignmentChromPeakFeature peak) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AlignmentLightPeakStore));
        }
        EnsureInitialized();

        var offset = GetOffset(spotID, peak.FileID);
        _stream.Seek(offset, SeekOrigin.Begin);
        WritePeak(peak);
    }

    public IReadOnlyList<AlignmentLightPeakRow> ReadSpotPeaks(int spotID) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AlignmentLightPeakStore));
        }
        EnsureInitialized();

        _writer.Flush();
        using var reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var physicalSpotId = GetPhysicalSpotId(spotID);
        var rows = new List<AlignmentLightPeakRow>(_files.Count);
        foreach (var file in _files) {
            var offset = GetOffset(physicalSpotId, file.AnalysisFileId);
            _stream.Seek(offset, SeekOrigin.Begin);
            rows.Add(ReadPeak(reader, file));
        }
        return rows;
    }

    public void RemapSpotIds(IReadOnlyDictionary<int, int> oldToNewSpotIDs) {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(AlignmentLightPeakStore));
        }
        EnsureInitialized();

        var remapped = new Dictionary<int, int>(oldToNewSpotIDs.Count);
        foreach (var pair in oldToNewSpotIDs) {
            remapped[pair.Value] = pair.Key;
        }
        _logicalToPhysicalSpotId = remapped;
    }

    private void WritePeak(AlignmentChromPeakFeature peak) {
        _writer.Write(true);
        _writer.Write(peak.FileID);
        _writer.Write(peak.MasterPeakID);
        _writer.Write(peak.PeakID);
        _writer.Write(peak.PeakHeightTop);
        _writer.Write(peak.PeakAreaAboveZero);
        _writer.Write(peak.NormalizedPeakHeight);
        _writer.Write(peak.NormalizedPeakAreaAboveZero);
        _writer.Write(peak.ChromXsTop?.RT.Value ?? 0d);
        _writer.Write(peak.ChromXsTop?.RI.Value ?? 0d);
        _writer.Write(peak.ChromXsTop?.Drift.Value ?? 0d);
        _writer.Write(peak.CollisionCrossSection);
        _writer.Write(peak.Mass);
        _writer.Write(peak.PeakShape?.SignalToNoise ?? 0f);
        _writer.Write(peak.MS1RawSpectrumIdTop);
        _writer.Write(peak.MS2RawSpectrumID);
        _writer.Write(peak.MatchResults?.Representative?.LibraryID ?? -1);
        _writer.Write(peak.IsMsmsAssigned);
    }

    private static AlignmentLightPeakRow ReadPeak(BinaryReader reader, AnalysisFileBean file) {
        var occupied = reader.ReadBoolean();
        if (!occupied) {
            return CreateMissingPeak(file);
        }

        return new AlignmentLightPeakRow(
            reader.ReadInt32(),
            file.AnalysisFileName,
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadDouble(),
            reader.ReadSingle(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadBoolean());
    }

    private static AlignmentLightPeakRow CreateMissingPeak(AnalysisFileBean file) {
        return new AlignmentLightPeakRow(
            file.AnalysisFileId,
            file.AnalysisFileName,
            -1,
            -1,
            0d,
            0d,
            0d,
            0d,
            0d,
            0d,
            0d,
            0d,
            0d,
            0f,
            -1,
            -1,
            -1,
            false);
    }

    private long GetOffset(int spotID, int fileID) {
        if (spotID < 0 || spotID >= _spotCount) {
            throw new ArgumentOutOfRangeException(nameof(spotID));
        }
        if (!_fileIdToIndex.TryGetValue(fileID, out var fileIndex)) {
            throw new ArgumentOutOfRangeException(nameof(fileID), $"Unknown analysis file ID: {fileID}");
        }
        return ((long)spotID * _files.Count + fileIndex) * RecordSize;
    }

    private int GetPhysicalSpotId(int spotID) {
        return _logicalToPhysicalSpotId is not null && _logicalToPhysicalSpotId.TryGetValue(spotID, out var physicalSpotId)
            ? physicalSpotId
            : spotID;
    }

    private void EnsureInitialized() {
        if (!_initialized) {
            throw new InvalidOperationException("Alignment light peak matrix store is not initialized.");
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }
        _disposed = true;
        _writer.Dispose();
        _stream.Dispose();
        if (File.Exists(_filePath)) {
            File.Delete(_filePath);
        }
    }
}

public sealed class AlignmentLightQuantValueAccessor : IQuantValueAccessor {
    private readonly string _exportType;
    private readonly ParameterBase _parameter;
    private readonly AlignmentLightPeakStore _store;

    public AlignmentLightQuantValueAccessor(string exportType, ParameterBase parameter, AlignmentLightPeakStore store) {
        _exportType = exportType;
        _parameter = parameter;
        _store = store;
    }

    public List<string> GetQuantHeaders(IReadOnlyList<AnalysisFileBean> files) {
        return files.OrderBy(file => file.AnalysisFileId).Select(file => file.AnalysisFileName).ToList();
    }

    public List<string> GetStatHeaders() {
        return _parameter.ClassnameToOrder.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
    }

    public Dictionary<string, string> GetQuantValues(AlignmentSpotProperty spot) {
        var quantValues = new Dictionary<string, string>();
        var peaks = _store.ReadSpotPeaks(spot.MasterAlignmentID);
        var nonZeroMin = GetInterpolatedValueForMissingValue(peaks);
        foreach (var peak in peaks) {
            var spotValue = GetSpotValueAsString(peak);
            if (nonZeroMin >= 0) {
                double.TryParse(spotValue, out var doubleValue);
                if (doubleValue == 0) {
                    doubleValue = nonZeroMin * 0.1;
                }
                spotValue = doubleValue.ToString();
            }
            quantValues.Add(peak.FileName, spotValue);
        }
        return quantValues;
    }

    public Dictionary<string, string> GetStatsValues(AlignmentSpotProperty spot, StatsValue stat) {
        var groups = _store.ReadSpotPeaks(spot.MasterAlignmentID).GroupBy(
            peak => _parameter.FileID_ClassName[peak.FileID],
            peak => GetSpotValue(peak));
        switch (stat) {
            case StatsValue.Average:
                return groups.ToDictionary(
                    group => group.Key,
                    group => group.Average().ToString()
                );
            case StatsValue.Stdev:
                return groups.ToDictionary(
                    group => group.Key,
                    group => ReplaceNaN(BasicMathematics.Stdev(group.ToArray())).ToString()
                );
        }
        return new Dictionary<string, string>();
    }

    private double GetInterpolatedValueForMissingValue(IReadOnlyList<AlignmentLightPeakRow> peaks) {
        if (_exportType != "Height" && _exportType != "Area" && _exportType != "Normalized height" && _exportType != "Normalized area") {
            return -1;
        }
        if (!_parameter.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples) {
            return -1;
        }

        var nonZeroMin = double.MaxValue;
        foreach (var peak in peaks) {
            var variable = GetSpotValue(peak);
            if (variable > 0 && nonZeroMin > variable) {
                nonZeroMin = variable;
            }
        }
        return nonZeroMin == double.MaxValue ? 1 : nonZeroMin;
    }

    private string GetSpotValueAsString(AlignmentLightPeakRow peak) {
        switch (_exportType) {
            case "Height": return Math.Round(peak.PeakHeightTop, 0).ToString();
            case "Normalized height": return peak.NormalizedPeakHeight.ToString();
            case "Normalized area": return peak.NormalizedPeakAreaAboveZero.ToString();
            case "Area": return Math.Round(peak.PeakAreaAboveZero, 0).ToString();
            case "ID": return peak.MasterPeakID.ToString();
            case "RT": return Math.Round(peak.Rt, 3).ToString();
            case "RI": return Math.Round(peak.Ri, 2).ToString();
            case "Mobility": return Math.Round(peak.Mobility, 5).ToString();
            case "CCS": return Math.Round(peak.CollisionCrossSection, 3).ToString();
            case "MZ": return Math.Round(peak.Mass, 5).ToString();
            case "SN": return Math.Round(peak.SignalToNoise, 1).ToString();
            case "MSMS": return peak.MS2RawSpectrumID >= 0 ? "TRUE" : "FALSE";
            default: return string.Empty;
        }
    }

    private double GetSpotValue(AlignmentLightPeakRow peak) {
        switch (_exportType) {
            case "Height": return peak.PeakHeightTop;
            case "Normalized height": return peak.NormalizedPeakHeight;
            case "Normalized area": return peak.NormalizedPeakAreaAboveZero;
            case "Area": return peak.PeakAreaAboveZero;
            case "ID": return peak.MasterPeakID;
            case "RT": return peak.Rt;
            case "RI": return peak.Ri;
            case "Mobility": return peak.Mobility;
            case "CCS": return peak.CollisionCrossSection;
            case "MZ": return peak.Mass;
            case "SN": return peak.SignalToNoise;
            case "MSMS": return peak.MS2RawSpectrumID;
            default: return -1;
        }
    }

    private static double ReplaceNaN(double val) => double.IsNaN(val) ? 0d : val;

}
