using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class DataBaseStorage
    {
        public DataBaseStorage(
            List<DataBaseItem<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> metabolomicsDataBases,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> proteomicsDataBases) {
            MetabolomicsDataBases = metabolomicsDataBases ?? throw new System.ArgumentNullException(nameof(metabolomicsDataBases));
            ProteomicsDataBases = proteomicsDataBases ?? throw new System.ArgumentNullException(nameof(proteomicsDataBases));
        }

        public DataBaseStorage()
            : this(
                new List<DataBaseItem<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(),
                new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>()) {
        }

        [Key(nameof(MetabolomicsDataBases))]
        public List<DataBaseItem<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> MetabolomicsDataBases { get; }

        [Key(nameof(ProteomicsDataBases))]
        public List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> ProteomicsDataBases { get; }

        public void AddMoleculeDataBase(
            MoleculeDataBase db,
            List<IAnnotatorParameterPair<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> annotators) {
            MetabolomicsDataBases.Add(new DataBaseItem<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>(db, annotators));
        }

        public void AddProteomicsDataBase(
            ShotgunProteomicsDB db,
            List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> annotators) {
            ProteomicsDataBases.Add(new DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>(db, annotators));
        }

        private static readonly string StoragePath = "Storage";
        private static readonly string MetabolomicsDataBasePath = "MetabolomicsDB";
        private static readonly string ProteomicsDataBasePath = "ProteomicsDB";
        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                foreach (var item in MetabolomicsDataBases) {
                    var dbEntry = archive.CreateEntry(Path.Combine(MetabolomicsDataBasePath, item.DataBaseID));
                    using (var dbStream = dbEntry.Open()) {
                        item.Save(dbStream);
                    }
                }

                foreach (var item in ProteomicsDataBases) {
                    var dbEntry = archive.CreateEntry(Path.Combine(ProteomicsDataBasePath, item.DataBaseID));
                    using (var dbStream = dbEntry.Open()) {
                        item.Save(dbStream);
                    }
                }

                var storageEntry = archive.CreateEntry(StoragePath);
                using (var storageStream = storageEntry.Open()) {
                    MessagePackDefaultHandler.SaveToStream(this, storageStream);
                }
            }
        }

        public static DataBaseStorage Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            DataBaseStorage result;
            try {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                    var storageEntry = archive.GetEntry(StoragePath);
                    using (var storageStream = storageEntry.Open()) {
                        result = MessagePackDefaultHandler.LoadFromStream<DataBaseStorage>(storageStream);
                    }

                    foreach (var item in result.MetabolomicsDataBases) {
                        var dbEntry = archive.GetEntry(Path.Combine(MetabolomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor);
                        }
                    }

                    foreach (var item in result.ProteomicsDataBases) {
                        var dbEntry = archive.GetEntry(Path.Combine(ProteomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor);
                        }
                    }
                }
            }
            catch (InvalidDataException) {
                result = new DataBaseStorage(
                    new List<DataBaseItem<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(),
                    new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>());
            }
            return result;
        }
    }

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

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true)) {
                var dbEntry = archive.GetEntry(DataBasePath);
                using (var dbStream = dbEntry.Open()) {
                    DataBase.Load(dbStream);
                }
                foreach (var container in Pairs) {
                    var annotatorEntry = archive.GetEntry(Path.Combine(AnnotatorsPath, container.AnnotatorID));
                    using (var annotatorStream = annotatorEntry.Open()) {
                        container.Load(annotatorStream, visitor, DataBase);
                    }
                }
            }
        }
    }

    [Union(0, typeof(MetabolomicsAnnotatorParameterPair))]
    [Union(1, typeof(ProteomicsAnnotatorParameterPair))]
    public interface IAnnotatorParameterPair<TQuery, TReference, TResult, TDataBase> where TDataBase : IReferenceDataBase
    {
        string AnnotatorID { get; }
        ISerializableAnnotator<TQuery, TReference, TResult, TDataBase> SerializableAnnotator { get; }
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor, TDataBase database);
        IAnnotatorContainer<TQuery, TReference, TResult> ConvertToAnnotatorContainer();
               
    }

    [MessagePackObject]
    public class MetabolomicsAnnotatorParameterPair : IAnnotatorParameterPair<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public MetabolomicsAnnotatorParameterPair(
            ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotator,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

        public MetabolomicsAnnotatorParameterPair(
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotatorKey,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotatorKey = serializableAnnotatorKey ?? throw new System.ArgumentNullException(nameof(serializableAnnotatorKey));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

        [IgnoreMember]
        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> SerializableAnnotator { get; private set; }

        [Key(nameof(SerializableAnnotatorKey))]
        public IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> SerializableAnnotatorKey { get; private set; }

        [Key(nameof(SearchParameter))]
        public MsRefSearchParameterBase SearchParameter { get; }

        [IgnoreMember]
        public string AnnotatorID => SerializableAnnotator.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = SerializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, MoleculeDataBase dataBase) {
            SerializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
        }

        public IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }

    [MessagePackObject]
    public class ProteomicsAnnotatorParameterPair : IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>
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
        public string AnnotatorID => SerializableAnnotator.Key;

        public void Save(Stream stream) {
            SerializableAnnotatorKey = SerializableAnnotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor, ShotgunProteomicsDB dataBase) {
            SerializableAnnotator = SerializableAnnotatorKey.Accept(visitor, dataBase);
        }

        public IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }
}
