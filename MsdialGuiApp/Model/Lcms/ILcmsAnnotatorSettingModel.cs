using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcms
{
    public interface ILcmsAnnotatorSettingModel : IAnnotatorSettingModel
    {

    }

    public interface ILcmsMetabolomicsAnnotatorSettingModel : ILcmsAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }

        List<ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics);
    }

    public interface ILcmsProteomicsAnnotatorSettingModel : ILcmsAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }
        ProteomicsParameter ProteomicsParameter { get; }

        List<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> CreateAnnotator(ShotgunProteomicsDB db, int priority, TargetOmics omics);
    }
}
