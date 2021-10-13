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

    [Obsolete]
    public sealed class AnnotatorContainer : IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
    {
        public AnnotatorContainer(
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator,
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

        public IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> Annotator { get; }
        public string AnnotatorID { get; }

        public MsRefSearchParameterBase Parameter { get; set; }
    }

    [Union(0, typeof(DatabaseAnnotatorContainer))]
    [Union(1, typeof(SerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>))]
    [Union(2, typeof(SerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>))]
    public interface ISerializableAnnotatorContainer<in T, U, V> : IAnnotatorContainer<T, U, V>
    {
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor);
    }

    [MessagePackObject]
    public sealed class SerializableAnnotatorContainer<TQuery, TReference, TResult> : ISerializableAnnotatorContainer<TQuery, TReference, TResult>
    {
        public SerializableAnnotatorContainer(
            ISerializableAnnotator<TQuery, TReference, TResult> annotator,
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

        public SerializableAnnotatorContainer(
            IReferRestorationKey<TQuery, TReference, TResult> annotatorKey,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<TQuery, TReference, TResult> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<TQuery, TReference, TResult> AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        IAnnotator<TQuery, TReference, TResult> IAnnotatorContainer<TQuery, TReference, TResult>.Annotator => Annotator;

        public void Save(Stream stream) {
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Annotator = AnnotatorKey.Accept(visitor);
        }
    }

    [Union(0, typeof(DatabaseAnnotatorContainer))]
    public interface IDatabaseAnnotatorContainer : ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
    {
        MoleculeDataBase Database { get; }
        string DatabaseID { get; }
    }

    [Union(0, typeof(ShotgunProteomicsDBAnnotatorContainer))]
    public interface IShotgunProteomicsDBAnnotatorContainer : ISerializableAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult> {
        ShotgunProteomicsDB Database { get; }
        string DatabaseID { get; }
    }

    [MessagePackObject]
    public sealed class DatabaseAnnotatorContainer : IDatabaseAnnotatorContainer
    {
        public DatabaseAnnotatorContainer(
            ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotator,
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
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> annotatorKey,
            MoleculeDataBase database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        [Key("Database")]
        public MoleculeDataBase Database { get; set; }
        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>.Annotator => Annotator;

        public void Save(Stream stream) {
            Database.Save(stream);
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load(stream);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }

    [MessagePackObject]
    public sealed class ShotgunProteomicsDBAnnotatorContainer : IShotgunProteomicsDBAnnotatorContainer {
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
            Database.Load(null);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }
}
