using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

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
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, MoleculeDataBase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer);
        IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> CreateRestorationKey(int priority);
    }

    public interface IProteomicsAnnotatorSettingModel : IAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, ShotgunProteomicsDB db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer);
        IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> CreateRestorationKey(int priority);
    }

    public interface IEadLipidAnnotatorSettingModel : IAnnotatorSettingModel
    {
        MsRefSearchParameterBase SearchParameter { get; }
        IAnnotationQueryFactory<MsScanMatchResult> CreateAnnotationQueryFactory(int priority, EadLipidDatabase db, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer);
        IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> CreateRestorationKey(int priority);
    }

    public interface IAnnotatorSettingModelFactory
    {
        IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase? searchParameter);
    }
}