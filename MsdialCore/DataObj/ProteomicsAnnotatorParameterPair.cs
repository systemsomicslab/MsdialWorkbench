using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class ProteomicsAnnotatorParameterPair : IAnnotatorParameterPair<ShotgunProteomicsDB>
    {
        private ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> _serializableAnnotator;

        public ProteomicsAnnotatorParameterPair(ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> serializableAnnotator, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory, ProteomicsParameter proteomicsParameter) {
            _serializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
        }

        [SerializationConstructor]
        public ProteomicsAnnotatorParameterPair(IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> serializableAnnotatorKey, ProteomicsParameter proteomicsParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> SerializableAnnotatorKey { get; private set; }

        [Key(nameof(ProteomicsParameter))]
        public ProteomicsParameter ProteomicsParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => _serializableAnnotator?.Key ?? SerializableAnnotatorKey.Key;

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        public void Save(Stream stream) {
            SerializableAnnotatorKey = _serializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, ShotgunProteomicsDB dataBase) {
            _serializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }
    }
}
