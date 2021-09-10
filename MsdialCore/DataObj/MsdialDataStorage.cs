using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class MsdialDataStorage
    {
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
        [Key(7)]
        public DataBaseMapper DataBaseMapper { get; set; }
        [Key(8)]
        public DataBaseStorage DataBases { get; set; }
    }
}
