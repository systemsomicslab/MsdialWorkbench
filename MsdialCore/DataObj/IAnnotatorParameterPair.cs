using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [Union(0, typeof(MetabolomicsAnnotatorParameterPair))]
    [Union(1, typeof(ProteomicsAnnotatorParameterPair))]
    [Union(2, typeof(EadLipidAnnotatorParameterPair))]
    public interface IAnnotatorParameterPair<TDataBase> where TDataBase : IReferenceDataBase
    {
        string AnnotatorID { get; }
        IAnnotationQueryFactory<MsScanMatchResult> AnnotationQueryFactory { get; }

        void Save(ZipArchive archive, string entryName);
        void Load(ZipArchive archive, string entryName, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, TDataBase database);
    }
}
