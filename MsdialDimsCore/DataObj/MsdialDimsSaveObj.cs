using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialDimsCore.DataObj {
    class MsdialDimsSaveObj {
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
        public MsdialDimsParameter MsdialDimsParameter { get; set; }

        public MsdialDimsSaveObj() { }

        public MsdialDimsSaveObj(MsdialDataStorage container) {
            AnalysisFiles = container.AnalysisFiles;
            AlignmentFiles = container.AlignmentFiles;
            MspDB = container.MspDB;
            TextDB = container.TextDB;
            IsotopeTextDB = container.IsotopeTextDB;
            IupacDatabase = container.IupacDatabase;
            MsdialDimsParameter = (MsdialDimsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage(MsdialDimsSaveObj obj) {
            var storage = new MsdialDataStorage() {
                AnalysisFiles = obj.AnalysisFiles,
                AlignmentFiles = obj.AlignmentFiles,
                MspDB = obj.MspDB,
                TextDB = obj.TextDB,
                IsotopeTextDB = obj.IsotopeTextDB,
                IupacDatabase = obj.IupacDatabase,
                ParameterBase = obj.MsdialDimsParameter
            };
            return storage;
        }
    }
}
