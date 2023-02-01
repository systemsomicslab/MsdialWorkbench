using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.IO;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotatorContainer<in T, U, V>
    {
        IAnnotator<T, U, V> Annotator { get; }
        string AnnotatorID { get; }

        MsRefSearchParameterBase Parameter { get; }
    }

    public sealed class AnnotatorContainer<TQuery, TReference, TResult> : IAnnotatorContainer<TQuery, TReference, TResult>
    {
        public AnnotatorContainer(
            IAnnotator<TQuery, TReference, TResult> annotator,
            MsRefSearchParameterBase parameter) {
            if (annotator is null) {
                throw new ArgumentNullException(nameof(annotator));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Annotator = annotator;
            AnnotatorID = Annotator.Key;
            Parameter = parameter;
        }

        public IAnnotator<TQuery, TReference, TResult> Annotator { get; }
        public string AnnotatorID { get; }

        public MsRefSearchParameterBase Parameter { get; set; }
    }

    public interface ISerializableAnnotatorContainer<in T, U, V> : IAnnotatorContainer<T, U, V>
    {
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor);
    }

    [MessagePackObject]
    public sealed class DatabaseAnnotatorContainer : ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>
    {
        public DatabaseAnnotatorContainer(
            ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotator,
            MoleculeDataBase database,
            MsRefSearchParameterBase parameter) {
            if (annotator is null) {
                throw new ArgumentNullException(nameof(annotator));
            }

            if (database is null) {
                throw new ArgumentNullException(nameof(database));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Annotator = annotator;
            AnnotatorID = Annotator.Key;
            Database = database;
            DatabaseID = Database.Id;
            Parameter = parameter;
            AnnotatorKey = Annotator.Save();
        }

        public DatabaseAnnotatorContainer(
            IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotatorKey,
            MoleculeDataBase database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        [Key("Database")]
        public MoleculeDataBase Database { get; set; }
        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> IAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult>.Annotator => Annotator;

        public void Save(Stream stream) {
            Database.Save(stream);
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load(stream, null);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }

    [MessagePackObject]
    public sealed class ShotgunProteomicsDBAnnotatorContainer : ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> {
        public ShotgunProteomicsDBAnnotatorContainer(
            ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> annotator,
            ShotgunProteomicsDB database, ProteomicsParameter proteomicsParameter,
            MsRefSearchParameterBase msRefSearchParameter) {
            if (annotator is null) {
                throw new ArgumentNullException(nameof(annotator));
            }

            if (database is null) {
                throw new ArgumentNullException(nameof(database));
            }

            if (msRefSearchParameter is null) {
                throw new ArgumentNullException(nameof(msRefSearchParameter));
            }

            if (proteomicsParameter is null) {
                throw new ArgumentNullException(nameof(proteomicsParameter));
            }
            Annotator = annotator;
            AnnotatorID = Annotator.Key;
            Database = database;
            DatabaseID = Database.Id;
            MsRefSearchParameter = msRefSearchParameter;
            AnnotatorKey = Annotator.Save();
        }

        public ShotgunProteomicsDBAnnotatorContainer(
            IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> annotatorKey,
            ShotgunProteomicsDB database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            MsRefSearchParameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> AnnotatorKey { get; set; }

        [Key("MsRefSearchParameter")]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }

        [Key("ProteomicsParameter")]
        public ProteomicsParameter ProteomicsParameter { get; set; }

        [Key("Database")]
        public ShotgunProteomicsDB Database { get; set; }
        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>.Annotator => Annotator;
        MsRefSearchParameterBase IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>.Parameter => MsRefSearchParameter;

        public void Save(Stream stream) {
            Database.Save(null);
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load(null, null);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }

    [MessagePackObject]
    public sealed class EadLipidDatabaseAnnotatorContainer : ISerializableAnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>
    {
        public EadLipidDatabaseAnnotatorContainer(
            ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> annotator,
            EadLipidDatabase database,
            MsRefSearchParameterBase parameter) {
            if (annotator is null) {
                throw new ArgumentNullException(nameof(annotator));
            }

            if (database is null) {
                throw new ArgumentNullException(nameof(database));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Annotator = annotator;
            AnnotatorID = Annotator.Key;
            Database = database;
            DatabaseID = Database.Id;
            Parameter = parameter;
            AnnotatorKey = Annotator.Save();
        }

        public EadLipidDatabaseAnnotatorContainer(
            IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> annotatorKey,
            EadLipidDatabase database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key(nameof(AnnotatorKey))]
        public IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> AnnotatorKey { get; set; }

        [Key(nameof(Parameter))]
        public MsRefSearchParameterBase Parameter { get; set; }

        [Key(nameof(Database))]
        public EadLipidDatabase Database { get; set; }

        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> IAnnotatorContainer<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>.Annotator => Annotator;

        public void Save(Stream stream) {
            Database.Save(stream);
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load(stream, null);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }
}
