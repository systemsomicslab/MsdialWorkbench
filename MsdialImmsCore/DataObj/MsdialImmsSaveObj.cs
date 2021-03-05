using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.DataObj
{
    [MessagePackObject]
    public class MsdialImmsSaveObj {
        [Key(0)]
        public List<AnalysisFileBean> AnalysisFiles { get; set; }
        [Key(1)]
        public List<AlignmentFileBean> AlignmentFiles { get; set; }
        [Key(2)]
        public List<MoleculeMsReference> MspDB { get; set; }
        [Key(3)]
        public List<MoleculeMsReference> TextDB { get; set; }
        [Key(4)]
        public List<MoleculeMsReference> IsotopeTextDB { get; set; }
        [Key(5)]
        public IupacDatabase IupacDatabase { get; set; }
        [Key(6)]
        public MsdialImmsParameter MsdialImmsParameter { get; set; }

        public MsdialImmsSaveObj() { }

        public MsdialImmsSaveObj(MsdialDataStorage container) {
            AnalysisFiles = container.AnalysisFiles;
            AlignmentFiles = container.AlignmentFiles;
            MspDB = container.MspDB;
            TextDB = container.TextDB;
            IsotopeTextDB = container.IsotopeTextDB;
            IupacDatabase = container.IupacDatabase;
            MsdialImmsParameter = (MsdialImmsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage(MsdialImmsSaveObj obj) {
            var storage = new MsdialDataStorage() {
                AnalysisFiles = obj.AnalysisFiles,
                AlignmentFiles = obj.AlignmentFiles,
                MspDB = obj.MspDB,
                TextDB = obj.TextDB,
                IsotopeTextDB = obj.IsotopeTextDB,
                IupacDatabase = obj.IupacDatabase,
                ParameterBase = obj.MsdialImmsParameter
            };
            return storage;
        }
    }
}
