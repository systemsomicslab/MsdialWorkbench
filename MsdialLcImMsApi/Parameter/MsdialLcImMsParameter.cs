using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcImMsApi.Parameter {
    public class MsdialLcImMsParameter : ParameterBase {
        public float DriftTimeBegin { get; set; } = 0;
        public float DriftTimeEnd { get; set; } = 2000;
        public float AccumulatedRtRagne { get; set; } = 0.2F;
    }
}
