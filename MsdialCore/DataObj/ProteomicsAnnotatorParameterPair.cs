using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class ProteomicsAnnotatorParameterPair : IAnnotatorParameterPair<ShotgunProteomicsDB>
    {
        public ProteomicsAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<ShotgunProteomicsDB> serializableAnnotatorKey, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory, ProteomicsParameter proteomicsParameter) {
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [SerializationConstructor]
        public ProteomicsAnnotatorParameterPair(IAnnotationQueryFactoryGenerationKey<ShotgunProteomicsDB> serializableAnnotatorKey, ProteomicsParameter proteomicsParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IAnnotationQueryFactoryGenerationKey<ShotgunProteomicsDB> SerializableAnnotatorKey { get; }

        [Key(nameof(ProteomicsParameter))]
        public ProteomicsParameter ProteomicsParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        public void Save() {

        }

        public void Load(ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, ShotgunProteomicsDB dataBase) {
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }

        void IAnnotatorParameterPair<ShotgunProteomicsDB>.Save(ZipArchive archive, string entryName) => Save();
        void IAnnotatorParameterPair<ShotgunProteomicsDB>.Load(ZipArchive archive, string entryName, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, ShotgunProteomicsDB database)
            => Load(visitor, factoryGenerationVisitor, database);
    }
}
