using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class Annotation
    {

        public double InitialProgress { get; set; } = 60.0;
        public double ProgressMax { get; set; } = 30.0;

        public Annotation(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }

        public void MainProcess(
            List<RawSpectrum> spectrumList, 
            List<ChromatogramPeakFeature> chromPeakFeatures,
            List<MSDecResult> msdecResults,
            List<MoleculeMsReference> mspDB,
            List<MoleculeMsReference> textDB,
            MsdialImmsParameter paramter,
            Action<int> reportAction) {

        }
    }
}
