using CompMs.Common.Components;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    abstract class MspAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public MspAnnotationSettingModel()
            : base() {

        }

        public MspAnnotationSettingModel(DataBaseAnnotationSettingModelBase model)
            : base(model) {

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
