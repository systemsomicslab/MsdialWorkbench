using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Lipidomics;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    public interface ILoadAnnotatorVisitor :
        IVisitor<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>, (StandardRestorationKey key, MoleculeDataBase database)>,
        IVisitor<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>, (MspDbRestorationKey key, MoleculeDataBase database)>,
        IVisitor<ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>, (TextDbRestorationKey key, MoleculeDataBase database)>,
        IVisitor<ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>, (ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database)>,
        IVisitor<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, (EadLipidDatabaseRestorationKey key, EadLipidDatabase database)>,
        IVisitor<ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, (EadLipidDatabaseRestorationKey key, EadLipidDatabase database)> {

    }

    public interface IAnnotationQueryFactoryGenerationVisitor :
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (StandardRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder)>,
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (MspDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder)>,
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (TextDbRestorationKey key, IMatchResultFinder<AnnotationQuery, MsScanMatchResult> finder)>,
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (ShotgunProteomicsRestorationKey key, IMatchResultFinder<PepAnnotationQuery, MsScanMatchResult> finder)>,
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MsScanMatchResult> finder)>,
        IVisitor<IAnnotationQueryFactory<MsScanMatchResult>, (EadLipidDatabaseRestorationKey key, IMatchResultFinder<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, ILipid), MsScanMatchResult> finder)> {

    }
}
