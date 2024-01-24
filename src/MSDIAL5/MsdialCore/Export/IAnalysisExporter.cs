using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public interface ILegacyAnalysisExporter
    {
        void Export(
            Stream stream,
            IReadOnlyList<ChromatogramPeakFeature> features,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            IAnalysisMetadataAccessor metaAccessor,
            AnalysisFileBean analysisFile);
    }

    public interface IAnalysisExporter<T> {
        void Export(Stream stream, AnalysisFileBean analysisFile, T data);
    }
}
