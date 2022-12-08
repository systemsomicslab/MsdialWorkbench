using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseItem<TQuery, TReference, TResult, TDataBase> where TDataBase: IReferenceDataBase
    {
        public DataBaseItem(
            TDataBase dataBase,
            List<IAnnotatorParameterPair<TQuery, TReference, TResult, TDataBase>> pairs) {
            DataBase = dataBase;
            Pairs = pairs;
        }

        [IgnoreMember]
        public string DataBaseID => DataBase.Id;

        [Key(nameof(DataBase))]
        public TDataBase DataBase { get; }

        [Key(nameof(Pairs))]
        public List<IAnnotatorParameterPair<TQuery, TReference, TResult, TDataBase>> Pairs { get; }

        private static readonly string DataBasePath = "DataBase";
        private static readonly string AnnotatorsPath = "Annotators";

        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                var dbEntry = archive.CreateEntry(DataBasePath);
                using (var dbStream = dbEntry.Open()) {
                    DataBase.Save(dbStream);
                }
                foreach (var container in Pairs) {
                    var annotatorEntry = archive.CreateEntry(Path.Combine(AnnotatorsPath, container.AnnotatorID));
                    using (var annotatorStream = annotatorEntry.Open()) {
                        container.Save(annotatorStream);
                    }
                }
            }
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, string projectFolderPath) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                var dbEntry = archive.GetEntry(DataBasePath);
                using (var dbStream = dbEntry.Open()) {
                    DataBase.Load(dbStream, projectFolderPath);
                }
                foreach (var container in Pairs) {
                    var annotatorEntry = archive.GetEntry(Path.Combine(AnnotatorsPath, container.AnnotatorID));
                    using (var annotatorStream = annotatorEntry.Open()) {
                        container.Load(annotatorStream, visitor, DataBase);
                    }
                }
            }
        }

        public List<IAnnotationQueryFactory<MsScanMatchResult>> CreateQueryFactories(ICreateAnnotationQueryFactoryVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor) {
            var result = new List<IAnnotationQueryFactory<MsScanMatchResult>>();
            return Pairs.Select(pair => pair.CreateQueryFactory(factoryVisitor, annotatorVisitor, DataBase)).ToList();
        }
    }
}
