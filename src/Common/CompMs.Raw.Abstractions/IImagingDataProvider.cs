using CompMs.Common.DataObj;
using System.Collections.Generic;

namespace CompMs.Raw.Abstractions;

public interface IImagingDataProvider : IDataProvider
{
    MaldiFrameLaserInfo GetMaldiFrameLaserInfo();
    List<MaldiFrameInfo> GetMaldiFrames();
    RawSpectraOnPixels GetRawPixelFeatures(List<Raw2DElement> targetFeatures, List<MaldiFrameInfo> targetFrames, bool isNewProcess = false);
    RawSpectraOnPixels GetRawPixelFeature(List<Raw2DElement> targetFeatures, int featureIndex, List<MaldiFrameInfo> targetFrames, bool isNewProcess = false);
}
