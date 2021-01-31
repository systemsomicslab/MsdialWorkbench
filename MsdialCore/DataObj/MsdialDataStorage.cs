using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Parameter;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class MsdialDataStorage {
        [Key(0)]
        public List<AnalysisFileBean> AnalysisFiles { get; set; } = new List<AnalysisFileBean>();
        [Key(1)]
        public List<AlignmentFileBean> AlignmentFiles { get; set; } = new List<AlignmentFileBean>();
        [Key(2)]
        public List<MoleculeMsReference> MspDB { get; set; } = new List<MoleculeMsReference>();
        [Key(3)]
        public List<MoleculeMsReference> TextDB { get; set; } = new List<MoleculeMsReference>();
        [Key(4)]
        public List<MoleculeMsReference> IsotopeTextDB { get; set; } = new List<MoleculeMsReference>();
        [Key(5)]
        public IupacDatabase IupacDatabase { get; set; }
        [Key(6)]
        public ParameterBase ParameterBase { get; set; }
    }
}
