using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiModel : BindableBase {
        public AnalysisFileBeanModel File { get; }
        public List<MaldiFrameInfo> Pixels { get; }

        public RoiModel(AnalysisFileBeanModel file, List<MaldiFrameInfo> pixels) {
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
