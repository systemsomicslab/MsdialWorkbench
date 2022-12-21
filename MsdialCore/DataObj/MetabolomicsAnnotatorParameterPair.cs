using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class MetabolomicsAnnotatorParameterPair : IAnnotatorParameterPair<MoleculeDataBase>
    {
        public MetabolomicsAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<MoleculeDataBase> serializableAnnotatorKey, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
        }

        [SerializationConstructor]
        public MetabolomicsAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<MoleculeDataBase> serializableAnnotatorKey) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IAnnotationQueryFactoryGenerationKey<MoleculeDataBase> SerializableAnnotatorKey { get; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        public void Save() {

        }

        public void Load(ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, MoleculeDataBase dataBase) {
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }

        void IAnnotatorParameterPair<MoleculeDataBase>.Save(ZipArchive archive, string entryName) => Save();
        void IAnnotatorParameterPair<MoleculeDataBase>.Load(ZipArchive archive, string entryName, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, MoleculeDataBase database)
            => Load(visitor, factoryGenerationVisitor, database);
    }
}
