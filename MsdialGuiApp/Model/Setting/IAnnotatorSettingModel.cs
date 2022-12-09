using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAnnotatorSettingModel
    {
        SourceType AnnotationSource { get; }
        string AnnotatorID { get; set; }
        DataBaseSettingModel DataBaseSettingModel { get; }
    }

    public interface IMetabolomicsAnnotatorSettingModel : IAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }

        List<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> CreateAnnotator(MoleculeDataBase db, int priority, TargetOmics omics);
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer);
    }

    public interface IProteomicsAnnotatorSettingModel : IAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }

        List<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> CreateAnnotator(ShotgunProteomicsDB db, int priority, TargetOmics omics);
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, ShotgunProteomicsDB db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer);
    }

    public interface IEadLipidAnnotatorSettingModel : IAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }

        List<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> CreateAnnotator(EadLipidDatabase db, int priority);
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, EadLipidDatabase db, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer);
    }

    public interface IAnnotatorSettingModelFactory
    {
        IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter);
    }
}