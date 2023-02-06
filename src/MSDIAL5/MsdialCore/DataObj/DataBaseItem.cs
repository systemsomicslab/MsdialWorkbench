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
    public sealed class DataBaseItem<TDataBase> where TDataBase: IReferenceDataBase
    {
        public DataBaseItem(
            TDataBase dataBase,
            List<IAnnotatorParameterPair<TDataBase>> pairs) {
            DataBase = dataBase;
            Pairs = pairs;
        }

        [IgnoreMember]
        public string DataBaseID => DataBase.Id;

        [Key(nameof(DataBase))]
        public TDataBase DataBase { get; }

        [Key(nameof(Pairs))]
        public List<IAnnotatorParameterPair<TDataBase>> Pairs { get; }

        private static readonly string DataBasePath = "DataBase";
        private static readonly string AnnotatorsPath = "Annotators";

        public void Save(ZipArchive archive, string entryPath) {
            var entry = archive.CreateEntry(Path.Combine(entryPath, DataBaseID, DataBasePath));
            using (var dbStream = entry.Open()) {
                DataBase.Save(dbStream);
            }
            foreach (var container in Pairs) {
                container.Save(archive, Path.Combine(entryPath, DataBaseID, AnnotatorsPath, container.AnnotatorID));
            }
        }

        public void Load(ZipArchive archive, string entryPath, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, string projectFolderPath) {
            if (TryCurrentLoad(archive, entryPath, visitor, factoryGenerationVisitor, projectFolderPath)) {
                return;
            }
            if (TryOldLoad(archive, entryPath, visitor, factoryGenerationVisitor, projectFolderPath)) {
                return;
            }
        }

        private bool TryOldLoad(ZipArchive archive_, string entryPath, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, string projectFolderPath) {
            string dataBaseItemPath = Path.Combine(entryPath, DataBaseID);
            var dataBaseItemEntry = archive_.GetEntry(dataBaseItemPath);
            if (dataBaseItemEntry is null) {
                return false;
            }
            using (var dataBaseItemStream = dataBaseItemEntry.Open())
            using (var archive = new ZipArchive(dataBaseItemStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                ZipArchiveEntry dataBaseEntry = archive.GetEntry(DataBasePath);
                if (dataBaseEntry is null) {
                    return false;
                }
                using (var stream = dataBaseEntry.Open()) {
                    DataBase.Load(stream, projectFolderPath);
                }
                foreach (var container in Pairs) {
                    var annotatorEntryPath = Path.Combine(AnnotatorsPath, container.AnnotatorID);
                    container.Load(archive, annotatorEntryPath, visitor, factoryGenerationVisitor, DataBase);
                }
            }
            return true;
        }

        // 2023/01/04 added.
        private bool TryCurrentLoad(ZipArchive archive, string entryPath, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, string projectFolderPath) {
            string itemPath = Path.Combine(entryPath, DataBaseID);
            string dataBaseEntryPath = Path.Combine(itemPath, DataBasePath);
            ZipArchiveEntry dataBaseEntry = archive.GetEntry(dataBaseEntryPath);
            if (dataBaseEntry is null) {
                return false;
            }
            using (var stream = dataBaseEntry.Open()) {
                DataBase.Load(stream, projectFolderPath);
            }
            string annotatorsEntryPath = Path.Combine(itemPath, AnnotatorsPath);
            foreach (var container in Pairs) {
                var annotatorEntryPath = Path.Combine(annotatorsEntryPath, container.AnnotatorID);
                container.Load(archive, annotatorEntryPath, visitor, factoryGenerationVisitor, DataBase);
            }
            return true;
        }

        public List<IAnnotationQueryFactory<MsScanMatchResult>> CreateQueryFactories() {
            return Pairs.Select(pair => pair.AnnotationQueryFactory).ToList();
        }
    }
}
