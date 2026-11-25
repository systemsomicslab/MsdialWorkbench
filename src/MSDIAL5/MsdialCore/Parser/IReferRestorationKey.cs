using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    [MessagePack.Union(3, typeof(StandardRestorationKey))]
    [MessagePack.Union(4, typeof(ShotgunProteomicsRestorationKey))]
    [MessagePack.Union(5, typeof(EadLipidDatabaseRestorationKey))]
    public interface IReferRestorationKey
    {
        ISerializableAnnotator<TQuery, TReference, TResult, TDatabase> Accept<TQuery, TReference, TResult, TDatabase>(ILoadAnnotatorVisitor visitor, TDatabase database);
        IAnnotationQueryFactory<MsScanMatchResult> Accept<TDatabase>(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, TDatabase database);

        int Priority { get; }
        string Key { get; }
    }

    public interface IReferRestorationKey<in TQuery, TReference, TResult, in TDatabase> : IAnnotationQueryFactoryGenerationKey<TDatabase>
    {
        ISerializableAnnotator<TQuery, TReference, TResult, TDatabase> Accept(ILoadAnnotatorVisitor visitor, TDatabase database);
    }

    public interface IAnnotationQueryFactoryGenerationKey<in TDatabase> : IReferRestorationKey {
        IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, TDatabase database);
    }
}
