using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [Union(0, typeof(MetabolomicsAnnotatorParameterPair))]
    [Union(1, typeof(ProteomicsAnnotatorParameterPair))]
    [Union(2, typeof(EadLipidAnnotatorParameterPair))]
    public interface IAnnotatorParameterPair<TQuery, TReference, TResult, TDataBase> where TDataBase : IReferenceDataBase
    {
        string AnnotatorID { get; }
        IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; }

        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, TDataBase database);
    }
}
