using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class ProteomicsAnnotatorParameterPair : 
        IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>
    {
        public ProteomicsAnnotatorParameterPair(
            ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> serializableAnnotator,
            MsRefSearchParameterBase searchParameter,
            ProteomicsParameter proteomicsParameter) {
            SerializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
        }
        public ProteomicsAnnotatorParameterPair(
            IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> serializableAnnotatorKey,
            MsRefSearchParameterBase searchParameter,
            ProteomicsParameter proteomicsParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
            ProteomicsParameter = proteomicsParameter ?? throw new System.ArgumentNullException(nameof(proteomicsParameter));
        }

        [IgnoreMember]
        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> SerializableAnnotator { get; private set;  }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> SerializableAnnotatorKey { get; private set; }

        [Key(nameof(SearchParameter))]
        public MsRefSearchParameterBase SearchParameter { get; }

        [Key(nameof(ProteomicsParameter))]
        public ProteomicsParameter ProteomicsParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => SerializableAnnotator?.Key ?? SerializableAnnotatorKey.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = SerializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, ShotgunProteomicsDB dataBase) {
            SerializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> CreateQueryFactory(ICreateAnnotationQueryFactoryVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, ShotgunProteomicsDB dataBase) {
            return SerializableAnnotatorKey.Accept(factoryVisitor, annotatorVisitor, dataBase);
        }

        public IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }
}
