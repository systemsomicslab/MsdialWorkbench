using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.RawDataHandler.Core;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

internal sealed class ImageGenerationProcess
{
    public async Task RunAsync(string inputFile, CancellationToken token = default) {
        var folder = Path.GetDirectoryName(inputFile);
        var title = Path.GetFileName(inputFile);
        var deserializer = new MsdialIntegrateSerializer();

        IMsdialDataStorage<ParameterBase> storage;
        using (IStreamManager manager = new DirectoryTreeStreamManager(folder!)) {
            storage = await deserializer.LoadAsync(manager, title, folder, string.Empty).ConfigureAwait(false);
            manager.Complete();
        }

        foreach (var analysis in storage.AnalysisFiles) {
            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysis.PeakAreaBeanInformationFilePath);

            using RawDataAccess rawDataAccess = new(analysis.AnalysisFilePath, 0, getProfileData: true, isImagingMsData: true, isGuiProcess: false) {
                DriftToleranceForPixelData = .1d
            };
            var frames = rawDataAccess.GetMaldiFrames();

            if (frames is not null) {
                var csvPath = Path.Combine(Path.GetDirectoryName(analysis.AnalysisFilePath) ?? folder!, Path.GetFileNameWithoutExtension(analysis.AnalysisFilePath) + "_frames.csv");
                var lines = new[] { "PixelIndex,XIndex,YIndex" }.Concat(frames.Select((f, i) => $"{i},{f.XIndexPos},{f.YIndexPos}"));
                File.WriteAllLines(csvPath, lines);
            }

            var raw2DElements = peaks.Select(peak => new Raw2DElement { Mz = peak.PrecursorMz, Drift = peak.PeakFeature.ChromXsTop.Drift.Value, }).ToList();
            await rawDataAccess.SaveRawPixelFeaturesAsync(raw2DElements, frames, token).ConfigureAwait(false);
        }
    }
}
