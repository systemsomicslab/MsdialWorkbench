using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    public interface ILoadAnnotatorVisitor
    {
        ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database);
        ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database);
        ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database);
        ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database);
        ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Visit(EadLipidDatabaseRestorationKey key, EadLipidDatabase database);
    }

    public interface IAnnotationQueryFactoryGenerationVisitor {
        IAnnotationQueryFactory<MsScanMatchResult> Visit(StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder);
        IAnnotationQueryFactory<MsScanMatchResult> Visit(MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder);
        IAnnotationQueryFactory<MsScanMatchResult> Visit(TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder);
        IAnnotationQueryFactory<MsScanMatchResult> Visit(ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder);
        IAnnotationQueryFactory<MsScanMatchResult> Visit(EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder);
    }
}
