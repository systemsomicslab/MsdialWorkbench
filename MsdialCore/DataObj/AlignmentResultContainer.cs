using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CompMs.Common.Enum;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentResultContainer
    {
        [Key(0)]
        public Ionization Ionization { get; set; }
        [Key(1)]
        public int AlignmentResultFileID { get; set; }
        [Key(2)]
        public int TotalAlignmentSpotCount { get; set; }
        [Key(3)]
        public ObservableCollection<AlignmentSpotProperty> AlignmentSpotProperties { get; set; }
        [Key(4)]
        public bool IsNormalized { get; set; }
        //public AnalysisParametersBean AnalysisParamForLC { get; set; }
        //public AnalysisParamOfMsdialGcms AnalysisParamForGC { get; set; }

    }
}
