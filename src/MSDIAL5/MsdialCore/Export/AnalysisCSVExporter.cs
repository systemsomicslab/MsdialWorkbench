using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export;

public sealed class AnalysisCSVExporter : ILegacyAnalysisExporter
{
    public AnalysisCSVExporter(string separator) {
        Separator = separator;
    }

    public string Separator { get; }

    public void Export(Stream stream, IReadOnlyList<ChromatogramPeakFeature> features, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
        using (var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)) {
            // Header
            var headers = metaAccessor.GetHeaders();
            WriteHeader(sw, headers);

            // Content
            foreach (var feature in features) {
                WriteContent(sw, feature, msdecResults[feature.MasterPeakID], provider, headers, metaAccessor, analysisFile);
            }
        }
    }

    private void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers) {
        sw.WriteLine(string.Join(Separator, headers));
    }

    private void WriteContent(StreamWriter sw, ChromatogramPeakFeature features, MSDecResult msdec, IDataProvider provider, IReadOnlyList<string> headers, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
        var metadata = metaAccessor.GetContent(features, msdec, provider, analysisFile);
        sw.WriteLine(string.Join(Separator, headers.Select(header => WrapField(metadata[header]))));
    }

    private string WrapField(string field) {
        if (field.Contains(Separator)) {
            return $"\"{field}\"";
        }
        return field;
    }
}

public sealed class AnalysisCSVExporterFactory {
    private readonly string _separator;

    public AnalysisCSVExporterFactory(string separator) {
        _separator = separator;
    }

    public IAnalysisExporter<ChromatogramPeakFeatureCollection> CreateExporter(IDataProviderFactory<AnalysisFileBean> providerFactory, IAnalysisMetadataAccessor metaAccessor) {
        return new InternalAnalysisCSVExporter(providerFactory, metaAccessor)
        {
            Separator = _separator
        };
    }

    public IAnalysisExporter<IReadOnlyList<T>> CreateExporter<T>(IAnalysisMetadataAccessor<T> metaAccessor) {
        return new InternalAnalysisCSVExporter<T>(metaAccessor)
        {
            Separator = _separator
        };
    }
} 

internal sealed class InternalAnalysisCSVExporter : IAnalysisExporter<ChromatogramPeakFeatureCollection>
{
    private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
    private readonly IAnalysisMetadataAccessor _metaAccessor;

    public InternalAnalysisCSVExporter(IDataProviderFactory<AnalysisFileBean> providerFactory, IAnalysisMetadataAccessor metaAccessor) {
        _providerFactory = providerFactory;
        _metaAccessor = metaAccessor;
    }

    public string Separator { get; set; }

    public void Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection data) {
        using var loader = new MSDecLoader(analysisFile.DeconvolutionFilePath);
        var msdecResults = loader.LoadMSDecResults();
        var provider = _providerFactory.Create(analysisFile);

        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);

        // Header
        var headers = _metaAccessor.GetHeaders();
        WriteHeader(sw, headers);

        // Content
        foreach (var feature in data.Items) {
            WriteContent(sw, feature, msdecResults[feature.GetMSDecResultID()], provider, headers, _metaAccessor, analysisFile);
        }
    }

    private void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers) {
        sw.WriteLine(string.Join(Separator, headers));
    }

    private void WriteContent(StreamWriter sw, ChromatogramPeakFeature features, MSDecResult msdec, IDataProvider provider, IReadOnlyList<string> headers, IAnalysisMetadataAccessor metaAccessor, AnalysisFileBean analysisFile) {
        var metadata = metaAccessor.GetContent(features, msdec, provider, analysisFile);
        sw.WriteLine(string.Join(Separator, headers.Select(header => WrapField(metadata[header]))));
    }

    private string WrapField(string field) {
        if (field.Contains(Separator)) {
            return $"\"{field}\"";
        }
        return field;
    }
}

internal sealed class InternalAnalysisCSVExporter<T> : IAnalysisExporter<IReadOnlyList<T>>
{
    private readonly IAnalysisMetadataAccessor<T> _metaAccessor;

    public InternalAnalysisCSVExporter(IAnalysisMetadataAccessor<T> metaAccessor) {
        _metaAccessor = metaAccessor;
    }

    public string Separator { get; set; }

    public void Export(Stream stream, AnalysisFileBean analysisFile, IReadOnlyList<T> data) {
        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);

        // Header
        var headers = _metaAccessor.GetHeaders();
        WriteHeader(sw, headers);

        // Content
        foreach (var feature in data) {
            WriteContent(sw, feature, headers, _metaAccessor);
        }
    }

    private void WriteHeader(StreamWriter sw, IReadOnlyList<string> headers) {
        sw.WriteLine(string.Join(Separator, headers));
    }

    private void WriteContent(StreamWriter sw, T features, IReadOnlyList<string> headers, IAnalysisMetadataAccessor<T> metaAccessor) {
        var metadata = metaAccessor.GetContent(features);
        sw.WriteLine(string.Join(Separator, headers.Select(header => WrapField(metadata[header]))));
    }

    private string WrapField(string field) {
        if (field.Contains(Separator)) {
            return $"\"{field}\"";
        }
        return field;
    }
}
