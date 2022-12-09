using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class EadLipidAnnotatorParameterPair : IAnnotatorParameterPair<EadLipidDatabase>
    {
        private ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> _serializableAnnotator;

        public EadLipidAnnotatorParameterPair(ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> serializableAnnotator, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory) {
            _serializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
        }

        [SerializationConstructor]
        public EadLipidAnnotatorParameterPair(IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> serializableAnnotatorKey) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> SerializableAnnotatorKey { get; private set; }

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = _serializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, EadLipidDatabase dataBase) {
            _serializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }
    }
}
