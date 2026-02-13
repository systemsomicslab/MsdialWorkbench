using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnalysisSdfExporter : IAnalysisExporter<ChromatogramPeakFeatureCollection>
    {
        private readonly Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> _loaderFactory;

        public AnalysisSdfExporter(Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> loaderFuctory) {
            _loaderFactory = loaderFuctory ?? throw new ArgumentNullException(nameof(loaderFuctory));
        }
        private readonly bool _exportNoMs2Molecule;
        private readonly bool _set2dCoordinates;
        public AnalysisSdfExporter(bool exportNoMs2Molecule, bool set2dCoordinates)
        {
            _exportNoMs2Molecule = exportNoMs2Molecule;
            _set2dCoordinates = set2dCoordinates;
        }
        public AnalysisSdfExporter() : this(exportNoMs2Molecule: true, set2dCoordinates: true) { }

        void IAnalysisExporter<ChromatogramPeakFeatureCollection>.Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection peakFeatureCollection, ExportStyle exportStyle) {
            var loader = _loaderFactory(analysisFile);
            foreach (var peak in peakFeatureCollection.Items) {
                SpectraExport.SaveSpectraTableAsSdfFormat(
                    stream, 
                    peak, 
                    loader.Load(peak).Spectrum,
                    _exportNoMs2Molecule,
                    _set2dCoordinates
            );
            }
        }
    }
}
