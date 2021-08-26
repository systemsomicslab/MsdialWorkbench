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

        public override ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> Build(ParameterBase parameter) {
            var db = LoadDataBase();
            return BuildCore(db);
        }

        private ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> BuildCore(ShotgunProteomicsDB database) {
            return new ShotgunProteomicsDBAnnotatorContainer(
                new LcmsFastaAnnotator(database, MsRefSearchParameter, ProteomicsParameter, AnnotatorID, AnnotationSource),
                database,
                ProteomicsParameter,
                MsRefSearchParameter);
        }

        private ShotgunProteomicsDB LoadDataBase() {
            return LoadShotgunProteomicsDB(DataBasePath, DataBaseID, ProteomicsParameter, MsRefSearchParameter);
        }
    }
}
