using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.RawDataHandler.Core;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Imaging;

internal sealed class RawIntensityOnPixelsLoader
{
    private readonly List<Raw2DElement> _targets;
    private readonly AnalysisFileBeanModel _file;
    private readonly MaldiFrames _frames;

    public RawIntensityOnPixelsLoader(List<Raw2DElement> targets, AnalysisFileBeanModel file, MaldiFrames frames)
    {
        _targets = targets;
        _file = file;
        _frames = frames;
    }


    public RawSpectraOnPixels Load(int index) {
        using RawDataAccess rawDataAccess = new RawDataAccess(_file.AnalysisFilePath, 0, getProfileData: true, isImagingMsData: true, isGuiProcess: true) { DriftToleranceForPixelData = .1d };
        return rawDataAccess.GetRawPixelFeature(_targets, index, [.. _frames.Infos], isNewProcess: false);
    }
}
