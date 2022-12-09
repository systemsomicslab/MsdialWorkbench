using Accord.Diagnostics;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.Common.Proteomics.DataObj;
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

        public static DataBaseStorage Load(Stream stream, ILoadAnnotatorVisitor visitor, IAnnotationQueryFactoryGenerationVisitor factoryGenerationVisitor, string projectFolderPath) {
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
                            item.Load(dbStream, visitor, factoryGenerationVisitor, projectFolderPath);
                        }
                    }

                    foreach (var item in result.ProteomicsDataBases) {
                        var dbEntry = archive.GetEntry(Path.Combine(ProteomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor, factoryGenerationVisitor, projectFolderPath);
                        }
                    }

                    foreach (var item in result.EadLipidomicsDatabases) {
                        var dbEntry = archive.GetEntry(Path.Combine(EadLipidomicsDataBasePath, item.DataBaseID));
                        using (var dbStream = dbEntry.Open()) {
                            item.Load(dbStream, visitor, factoryGenerationVisitor, projectFolderPath);
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

        public void SetDataBaseMapper(DataBaseMapper mapper) {
            foreach (var db in MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.AnnotatorID, db.DataBase);
                }
            }
            foreach (var db in ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.AnnotatorID, db.DataBase);
                }
            }
            foreach (var db in EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.AnnotatorID, db.DataBase);
                }
            }
        }

        public DataBaseMapper CreateDataBaseMapper() {
            var mapper = new DataBaseMapper();
            SetDataBaseMapper(mapper);
            return mapper;
        }

        public AnnotationQueryFactoryStorage CreateQueryFactories() {
            return new AnnotationQueryFactoryStorage(
                MetabolomicsDataBases.SelectMany(db => db.CreateQueryFactories()),
                    ProteomicsDataBases.SelectMany(db => db.CreateQueryFactories()),
                    EadLipidomicsDatabases.SelectMany(db => db.CreateQueryFactories()));
        }

        public static DataBaseStorage CreateEmpty() {
            return new DataBaseStorage(
                new List<DataBaseItem<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(),
                new List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>>(),
                new List<DataBaseItem<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>>());
        }
    }
}
