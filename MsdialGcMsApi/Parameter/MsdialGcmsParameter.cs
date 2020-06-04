using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialGcMsApi.Parameter {
    public class MsdialGcmsParameter : ParameterBase {
        public RiCompoundType RiCompoundType { get; set; } = RiCompoundType.Alkanes;
        public RetentionType RetentionType { get; set; } = RetentionType.RT;
    }
}
