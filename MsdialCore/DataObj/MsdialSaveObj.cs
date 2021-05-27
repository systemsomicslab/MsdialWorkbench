using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public abstract class MsdialSaveObj
    {
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
        [Key(7)]
        public DataBaseMapper DataBaseMapper { get; set; }

        public MsdialSaveObj() { } // constructor for messagepack for c#

        public MsdialSaveObj(MsdialDataStorage container) {
            AnalysisFiles = container.AnalysisFiles;
            AlignmentFiles = container.AlignmentFiles;
            MspDB = container.MspDB;
            TextDB = container.TextDB;
            IsotopeTextDB = container.IsotopeTextDB;
            IupacDatabase = container.IupacDatabase;
            DataBaseMapper = container.DataBaseMapper;
        }

        protected virtual MsdialDataStorage ConvertToMsdialDataStorageCore() {
            return new MsdialDataStorage
            {
                AnalysisFiles = AnalysisFiles,
                AlignmentFiles = AlignmentFiles,
                MspDB = MspDB,
                TextDB = TextDB,
                IsotopeTextDB = IsotopeTextDB,
                IupacDatabase = IupacDatabase,
                DataBaseMapper = DataBaseMapper,
            };
        }
    }
}
