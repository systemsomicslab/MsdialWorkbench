using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcMsApi.DataObj {
    public class MsdialLcmsSaveObj {
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
        public MsdialLcmsParameter MsdialLcmsParameter { get; set; }

        public MsdialLcmsSaveObj() { }

        public MsdialLcmsSaveObj(MsdialDataStorage container) {
            AnalysisFiles = container.AnalysisFiles;
            AlignmentFiles = container.AlignmentFiles;
            MspDB = container.MspDB;
            TextDB = container.TextDB;
            IsotopeTextDB = container.IsotopeTextDB;
            IupacDatabase = container.IupacDatabase;
            MsdialLcmsParameter = (MsdialLcmsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage(MsdialLcmsSaveObj obj) {
            var saveObj = new MsdialDataStorage() {
                AnalysisFiles = obj.AnalysisFiles,
                AlignmentFiles = obj.AlignmentFiles,
                MspDB = obj.MspDB,
                TextDB = obj.TextDB,
                IsotopeTextDB = obj.IsotopeTextDB,
                IupacDatabase = obj.IupacDatabase,
                ParameterBase = obj.MsdialLcmsParameter
            };
            return saveObj;
        }
    }
}
