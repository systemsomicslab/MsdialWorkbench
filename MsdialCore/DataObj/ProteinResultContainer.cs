using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ProteinResultContainer {
        [Key(0)]
        public ParameterBase Parameter { get; }
        [Key(1)]
        public List<ProteinGroup> ProteinGroups { get; }
        public ProteinResultContainer() { }
        public ProteinResultContainer(ParameterBase parameter, List<ProteinGroup> proteinGroups) {
            this.Parameter = parameter;
            this.ProteinGroups = proteinGroups;
        }
    }
}
