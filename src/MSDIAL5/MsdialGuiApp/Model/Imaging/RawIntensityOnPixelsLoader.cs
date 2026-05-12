using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.RawDataHandler.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imaging;

internal sealed class RawIntensityOnPixelsLoader
{
    private readonly List<Raw2DElement> _targets;
    private readonly AnalysisFileBeanModel _file;
    private readonly MaldiFrames _frames;
    private readonly RawDataAccess _rawDataAccess;

    public RawIntensityOnPixelsLoader(List<Raw2DElement> targets, AnalysisFileBeanModel file, MaldiFrames frames)
    {
        _targets = targets;
        _file = file;
        _frames = frames;
        _rawDataAccess = new RawDataAccess(_file.AnalysisFilePath, 0, getProfileData: true, isImagingMsData: true, isGuiProcess: true) { DriftToleranceForPixelData = .1d };
    }

    public async Task<RawSpectraOnPixels> LoadAsync(int index, CancellationToken token = default) {
        return await _rawDataAccess.GetRawPixelFeatureAsync(_targets, index, [.. _frames.Infos], isNewProcess: false, token: token).ConfigureAwait(false);
    }
}
