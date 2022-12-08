using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class EadLipidAnnotatorParameterPair : 
        IAnnotatorParameterPair<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>
    {
        public EadLipidAnnotatorParameterPair(
            ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> serializableAnnotator,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }
        public EadLipidAnnotatorParameterPair(
            IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> serializableAnnotatorKey,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

        [IgnoreMember]
        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> SerializableAnnotator { get; private set;  }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> SerializableAnnotatorKey { get; private set; }

        [Key(nameof(SearchParameter))]
        public MsRefSearchParameterBase SearchParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => SerializableAnnotator?.Key ?? SerializableAnnotatorKey.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = SerializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, EadLipidDatabase dataBase) {
            SerializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateQueryFactory(ICreateAnnotationQueryFactoryVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, EadLipidDatabase dataBase) {
            return SerializableAnnotatorKey.Accept(factoryVisitor, annotatorVisitor, dataBase);
        }

        public IAnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }
}
