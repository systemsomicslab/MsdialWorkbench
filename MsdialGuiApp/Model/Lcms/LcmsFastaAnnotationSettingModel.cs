using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.Common.DataObj.Result;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsFastaAnnotationSettingModel : FastaAnnotationSettingModel {
        public LcmsFastaAnnotationSettingModel(FastaAnnotationSettingModel other)
            : base(other) {
        }

        private ShotgunProteomicsDB db;
        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            if (db is null) {
                db = LoadDataBase();
            }
            return BuildCore(db);
        }

        private ISerializableAnnotatorContainer BuildCore(ShotgunProteomicsDB db) {
            throw new NotImplementedException();
        }

        private ShotgunProteomicsDB LoadDataBase() {
            return LoadShotgunProteomicsDB(DataBasePath, DataBaseID, ProteomicsParameter, MsRefSearchParameter);
        }
    }
}
