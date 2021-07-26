using CompMs.Common.Components;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    abstract class LbmAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public LbmAnnotationSettingModel()
            : base() {

        }

        public LbmAnnotationSettingModel(DataBaseAnnotationSettingModelBase model)
            : base(model) {

        }

        protected static List<MoleculeMsReference> LoadMspDataBase(string path, ParameterBase parameter) {
            var db = LibraryHandler.ReadLipidMsLibrary(path, parameter);
            for (int i = 0; i < db.Count; ++i) {
                db[i].ScanID = i;
            }
            return db;
        }
    }
}
