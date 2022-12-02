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
        // MessagePack use this constructor
        [SerializationConstructor]
        public DataBaseStorage(
            List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> metabolomicsDataBases,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> proteomicsDataBases,
            List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> eadLipidomicsDataBases) {
            MetabolomicsDataBases = metabolomicsDataBases ?? new List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>();
            ProteomicsDataBases = proteomicsDataBases ?? new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>();
            EadLipidomicsDatabases = eadLipidomicsDataBases ?? new List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>>();
        }

        [Key(nameof(MetabolomicsDataBases))]
        public List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> MetabolomicsDataBases { get; }

        [Key(nameof(ProteomicsDataBases))]
        public List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> ProteomicsDataBases { get; }

        [Key(nameof(EadLipidomicsDatabases))]
        public List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> EadLipidomicsDatabases { get; }

        public void AddMoleculeDataBase(
            MoleculeDataBase db,
            List<IAnnotatorParameterPair<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>> annotators) {
            MetabolomicsDataBases.Add(new DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>(db, annotators));
        }

        public void AddProteomicsDataBase(
            ShotgunProteomicsDB db,
            List<IAnnotatorParameterPair<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> annotators) {
            ProteomicsDataBases.Add(new DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>(db, annotators));
        }

        public void AddEadLipidomicsDataBase(
            EadLipidDatabase db,
            List<IAnnotatorParameterPair<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>> annotators) {
            EadLipidomicsDatabases.Add(new DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>(db, annotators));
        }

        private static readonly string StoragePath = "Storage";
        private static readonly string MetabolomicsDataBasePath = "MetabolomicsDB";
        private static readonly string ProteomicsDataBasePath = "ProteomicsDB";
        private static readonly string EadLipidomicsDataBasePath = "EadLipidomicsDB";
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

                foreach (var item in EadLipidomicsDatabases) {
                    var dbEntry = archive.CreateEntry(Path.Combine(EadLipidomicsDataBasePath, item.DataBaseID));
                    using (var dbStream = dbEntry.Open()) {
                        item.Save(dbStream);
                    }
                    item.DataBase.SwitchTo(LipidDatabaseFormat.Dictionary);
                }

                var storageEntry = archive.CreateEntry(StoragePath);
                using (var storageStream = storageEntry.Open()) {
                    MessagePackDefaultHandler.SaveToStream(this, storageStream);
                }
            }
        }

        public static DataBaseStorage Load(Stream stream, ILoadAnnotatorVisitor visitor, string projectFolderPath) {
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
                            item.Load(dbStream, visitor, null);
                        }
                    }

                    foreach (var item in result.ProteomicsDataBases) {
                        var dbEntry = archive.GetEntry(Path.Combine(ProteomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor, projectFolderPath);
                        }
                    }

                    foreach (var item in result.EadLipidomicsDatabases) {
                        var dbEntry = archive.GetEntry(Path.Combine(EadLipidomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor, null);
                        }
                    }
                }
            }
            catch (InvalidDataException) {
                result = new DataBaseStorage(
                    new List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(),
                    new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>(),
                    new List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>>());
            }
            return result;
        }

        public DataBaseMapper CreateDataBaseMapper() {
            var mapper = new DataBaseMapper();
            foreach (var db in MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            foreach (var db in ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            foreach (var db in EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator);
                }
            }

            return mapper;
        }

        public static DataBaseStorage CreateEmpty() {
            return new DataBaseStorage(
                new List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(),
                new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>(),
                new List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>>());
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
    }

    [Union(0, typeof(MetabolomicsAnnotatorParameterPair))]
    [Union(1, typeof(ProteomicsAnnotatorParameterPair))]
    [Union(2, typeof(EadLipidAnnotatorParameterPair))]
    public interface IAnnotatorParameterPair<TQuery, TReference, TResult, TDataBase> where TDataBase : IReferenceDataBase
    {
        string AnnotatorID { get; }
        MsRefSearchParameterBase SearchParameter { get; }
        ISerializableAnnotator<TQuery, TReference, TResult, TDataBase> SerializableAnnotator { get; }
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor, TDataBase database);
        IAnnotatorContainer<TQuery, TReference, TResult> ConvertToAnnotatorContainer();
               
    }

    [MessagePackObject]
    public class MetabolomicsAnnotatorParameterPair : IAnnotatorParameterPair<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public MetabolomicsAnnotatorParameterPair(
            ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> serializableAnnotator,
            MsRefSearchParameterBase searchParameter) {
            SerializableAnnotator = serializableAnnotator ?? throw new System.ArgumentNullException(nameof(serializableAnnotator));
            SearchParameter = searchParameter ?? throw new System.ArgumentNullException(nameof(searchParameter));
        }

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

        public IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }

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

        public IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }

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

        public IAnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> ConvertToAnnotatorContainer() {
            return new AnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>(SerializableAnnotator, SearchParameter);
        }
    }
}
