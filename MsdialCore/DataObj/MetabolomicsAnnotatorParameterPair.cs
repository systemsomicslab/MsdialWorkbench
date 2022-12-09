using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class MetabolomicsAnnotatorParameterPair : IAnnotatorParameterPair<MoleculeDataBase>
    {
        private ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> _serializableAnnotator;

        public MetabolomicsAnnotatorParameterPair(ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotator, IAnnotationQueryFactory<MsScanMatchResult> annotationQueryFactory) {
            _serializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            AnnotationQueryFactory = annotationQueryFactory ?? throw new System.ArgumentNullException(nameof(annotationQueryFactory));
        }

        [SerializationConstructor]
        public MetabolomicsAnnotatorParameterPair(IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotatorKey) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
        }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> SerializableAnnotatorKey { get; private set; }

        [IgnoreMember]
        public string AnnotatorID => AnnotationQueryFactory?.AnnotatorId ?? SerializableAnnotatorKey.Key;

        [IgnoreMember]
        public IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; private set; }

        public void Save(Stream stream) {
            SerializableAnnotatorKey = _serializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, MoleculeDataBase dataBase) {
            _serializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
            AnnotationQueryFactory = SerializableAnnotatorKey.Accept(factoryGenerationVisitor, visitor, dataBase);
        }
    }
}
