using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class StagedLipidAnnotatorParameterPair : IAnnotatorParameterPair<StagedLipidDatabase>
    {
        public StagedLipidAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<StagedLipidDatabase> serializableAnnotatorKey, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory) {
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [SerializationConstructor]
        public StagedLipidAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<StagedLipidDatabase> serializableAnnotatorKey) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IAnnotationQueryFactoryGenerationKey<StagedLipidDatabase> SerializableAnnotatorKey { get; }

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        public void Load(ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, StagedLipidDatabase dataBase) {
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }

        void IAnnotatorParameterPair<StagedLipidDatabase>.Save(ZipArchive archive, string entryName) { }
        void IAnnotatorParameterPair<StagedLipidDatabase>.Load(ZipArchive archive, string entryName, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, StagedLipidDatabase database)
            => Load(visitor, factoryGenerationVisitor, database);
    }
}
