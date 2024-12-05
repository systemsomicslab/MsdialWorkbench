using CompMs.MsdialCore.DataObj;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public interface IAnalysisExporter<T> {
        void Export(Stream stream, AnalysisFileBean analysisFile, T data, ExportStyle exportStyle);
    }
}
