using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiModel : BindableBase {
        public AnalysisFileBean File { get; }
        public List<MaldiFrameInfo> Pixels { get; }

        public RoiModel(AnalysisFileBean file, List<MaldiFrameInfo> pixels) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
        }

        public RawSpectraOnPixels RetrieveRawSpectraOnPixels(List<Raw2DElement> targetElements) {
            using (RawDataAccess rawDataAccess = new RawDataAccess(File.AnalysisFilePath, 0, true, true, true)) {
                return rawDataAccess.GetRawPixelFeatures(targetElements, Pixels);
            }
        }
    }
}
