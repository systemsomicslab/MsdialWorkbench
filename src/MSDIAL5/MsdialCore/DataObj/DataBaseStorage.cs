using CompMs.Common.MessagePack;
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
            List<DataBaseItem<MoleculeDataBase>> metabolomicsDataBases,
            List<DataBaseItem<ShotgunProteomicsDB>> proteomicsDataBases,
            List<DataBaseItem<EadLipidDatabase>> eadLipidomicsDataBases) {
            MetabolomicsDataBases = metabolomicsDataBases ?? new List<DataBaseItem<MoleculeDataBase>>();
            ProteomicsDataBases = proteomicsDataBases ?? new List<DataBaseItem<ShotgunProteomicsDB>>();
            EadLipidomicsDatabases = eadLipidomicsDataBases ?? new List<DataBaseItem<EadLipidDatabase>>();
        }

        [Key(nameof(MetabolomicsDataBases))]
        public List<DataBaseItem<MoleculeDataBase>> MetabolomicsDataBases { get; }

        [Key(nameof(ProteomicsDataBases))]
        public List<DataBaseItem<ShotgunProteomicsDB>> ProteomicsDataBases { get; }

        [Key(nameof(EadLipidomicsDatabases))]
        public List<DataBaseItem<EadLipidDatabase>> EadLipidomicsDatabases { get; }

        public void AddMoleculeDataBase(
            MoleculeDataBase db,
            List<IAnnotatorParameterPair<MoleculeDataBase>> annotators) {
            MetabolomicsDataBases.Add(new DataBaseItem<MoleculeDataBase>(db, annotators));
        }

        public void AddProteomicsDataBase(
            ShotgunProteomicsDB db,
            List<IAnnotatorParameterPair<ShotgunProteomicsDB>> annotators) {
            ProteomicsDataBases.Add(new DataBaseItem<ShotgunProteomicsDB>(db, annotators));
        }

        public void AddEadLipidomicsDataBase(
            EadLipidDatabase db,
            List<IAnnotatorParameterPair<EadLipidDatabase>> annotators) {
            EadLipidomicsDatabases.Add(new DataBaseItem<EadLipidDatabase>(db, annotators));
        }

        private static readonly string StoragePath = "Storage";
        private static readonly string MetabolomicsDataBasePath = "MetabolomicsDB";
        private static readonly string ProteomicsDataBasePath = "ProteomicsDB";
        private static readonly string EadLipidomicsDataBasePath = "EadLipidomicsDB";
        public void Save(Stream stream) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                foreach (var item in MetabolomicsDataBases) {
                    item.Save(archive, MetabolomicsDataBasePath);
                }

                foreach (var item in ProteomicsDataBases) {
                    item.Save(archive, ProteomicsDataBasePath);
                }

                foreach (var item in EadLipidomicsDatabases) {
                    item.Save(archive, EadLipidomicsDataBasePath);
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
                        item.Load(archive, MetabolomicsDataBasePath, visitor, factoryGenerationVisitor, projectFolderPath);
                    }

                    foreach (var item in result.ProteomicsDataBases) {
                        item.Load(archive, ProteomicsDataBasePath, visitor, factoryGenerationVisitor, projectFolderPath);
                    }

                    foreach (var item in result.EadLipidomicsDatabases) {
                        item.Load(archive, EadLipidomicsDataBasePath, visitor, factoryGenerationVisitor, projectFolderPath);
                    }
                }
            }
            catch (InvalidDataException) {
                result = new DataBaseStorage(
                    new List<DataBaseItem<MoleculeDataBase>>(),
                    new List<DataBaseItem<ShotgunProteomicsDB>>(),
                    new List<DataBaseItem<EadLipidDatabase>>());
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
                new List<DataBaseItem<MoleculeDataBase>>(),
                new List<DataBaseItem<ShotgunProteomicsDB>>(),
                new List<DataBaseItem<EadLipidDatabase>>());
        }

        public string ParameterAsSimpleText() {
            var sb = new System.Text.StringBuilder();
            foreach (var dbPair in MetabolomicsDataBases) {
                SetDataBaseParameterAsSimpleText(sb, dbPair);
            }
            foreach (var dbPair in ProteomicsDataBases) {
                SetDataBaseParameterAsSimpleText(sb, dbPair);
            }  
            foreach (var dbPair in EadLipidomicsDatabases) {
                SetDataBaseParameterAsSimpleText(sb, dbPair);
            }  
            return sb.ToString();
        }

        private void SetDataBaseParameterAsSimpleText<T>(System.Text.StringBuilder sb, DataBaseItem<T> dbPair) where T: IReferenceDataBase {
            sb.AppendLine($"DataBaseID: {dbPair.DataBaseID}");
            foreach (var annotatorPair in dbPair.Pairs) {
                sb.AppendLine($"AnnotationMethod: {annotatorPair.AnnotatorID}");
                var factory = annotatorPair.AnnotationQueryFactory;
                var parameter = factory.PrepareParameter();
                sb.Append(parameter.ParameterAsString());
            }
            sb.AppendLine();
        }
    }
}
