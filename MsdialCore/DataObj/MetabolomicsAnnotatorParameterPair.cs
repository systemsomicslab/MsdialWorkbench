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
    public class MetabolomicsAnnotatorParameterPair : IAnnotatorParameterPair<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public MetabolomicsAnnotatorParameterPair(
            ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotator,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

        [SerializationConstructor]
        public MetabolomicsAnnotatorParameterPair(
            IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotatorKey,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

        [IgnoreMember]
        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> SerializableAnnotator { get; private set; }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> SerializableAnnotatorKey { get; private set; }

        [Key(nameof(SearchParameter))]
        public MsRefSearchParameterBase SearchParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => SerializableAnnotator?.Key ?? SerializableAnnotatorKey.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = SerializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, MoleculeDataBase dataBase) {
            SerializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateQueryFactory(ICreateAnnotationQueryFactoryVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, MoleculeDataBase dataBase) {
            return SerializableAnnotatorKey.Accept(factoryVisitor, annotatorVisitor, dataBase);
        }

        public IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }
}
