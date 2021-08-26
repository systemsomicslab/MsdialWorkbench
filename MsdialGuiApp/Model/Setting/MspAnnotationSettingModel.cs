using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    public abstract class MspAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public MspAnnotationSettingModel()
            : base() {

        }

        public MspAnnotationSettingModel(DataBaseAnnotationSettingModelBase model)
            : base(model) {

        }

        public override ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> Build(ParameterBase parameter) {
            return BuildCore(parameter, LoadDataBase(DataBaseID, DataBasePath, DBSource));
        }

        protected abstract ISerializableAnnotatorContainer BuildCore(ParameterBase parameter, MoleculeDataBase molecules);

        protected static MoleculeDataBase LoadDataBase(string id, string path, DataBaseSource dbsource) {
            switch (dbsource) {
                case DataBaseSource.Msp:
                    return new MoleculeDataBase(LoadMspDataBase(path), id, DataBaseSource.Msp, SourceType.MspDB);
                default:
                    throw new NotSupportedException(dbsource.ToString());
            }
        }

        protected static List<MoleculeMsReference> LoadMspDataBase(string path) {
            var db = LibraryHandler.ReadMspLibrary(path);
            for (int i = 0; i < db.Count; ++i) {
                db[i].ScanID = i;
            }
            return db;
        }
    }
}
