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
        [Key(2)]
        public Dictionary<string, ModificationContainer> DB2ModificationContainer { get; }
        public ProteinResultContainer() { }
        [SerializationConstructor]
        public ProteinResultContainer(
            ParameterBase parameter, 
            List<ProteinGroup> proteinGroups, 
            Dictionary<string, ModificationContainer> dB2ModificationContainer) {
            this.Parameter = parameter;
            this.ProteinGroups = proteinGroups;
            this.DB2ModificationContainer = dB2ModificationContainer;
        }
    }
}
