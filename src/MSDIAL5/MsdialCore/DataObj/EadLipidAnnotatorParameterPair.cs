using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class EadLipidAnnotatorParameterPair : IAnnotatorParameterPair<EadLipidDatabase>
    {
        public EadLipidAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<EadLipidDatabase> serializableAnnotatorKey, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory) {
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [SerializationConstructor]
        public EadLipidAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<EadLipidDatabase> serializableAnnotatorKey) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IAnnotationQueryFactoryGenerationKey<EadLipidDatabase> SerializableAnnotatorKey { get; }

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        public void Save() {

        }

        public void Load(ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, EadLipidDatabase dataBase) {
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }

        void IAnnotatorParameterPair<EadLipidDatabase>.Save(ZipArchive archive, string entryName) => Save();
        void IAnnotatorParameterPair<EadLipidDatabase>.Load(ZipArchive archive, string entryName, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, EadLipidDatabase database)
            => Load(visitor, factoryGenerationVisitor, database);
    }
}
