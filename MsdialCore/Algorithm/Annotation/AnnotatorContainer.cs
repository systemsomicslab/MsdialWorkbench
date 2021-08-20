using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
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
    [Union(1, typeof(SerializableAnnotatorContainer))]
    public interface ISerializableAnnotatorContainer : IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
    {
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor);
    }

    [MessagePackObject]
    public sealed class SerializableAnnotatorContainer : ISerializableAnnotatorContainer
    {
        public SerializableAnnotatorContainer(
            ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator,
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
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotatorKey,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>.Annotator => Annotator;

        public void Save(Stream stream) {
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Annotator = AnnotatorKey.Accept(visitor);
        }
    }

    [Union(0, typeof(DatabaseAnnotatorContainer))]
    public interface IDatabaseAnnotatorContainer : ISerializableAnnotatorContainer
    {
        MoleculeDataBase Database { get; }
        string DatabaseID { get; }
    }

    [Union(0, typeof(ShotgunProteomicsDBAnnotatorContainer))]
    public interface IShotgunProteomicsDBAnnotatorContainer : ISerializableAnnotatorContainer {
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
            ISerializableAnnotator<IMSIonProperty, IMSScanProperty, ShotgunProteomicsDB> annotator,
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
        }

        public ShotgunProteomicsDBAnnotatorContainer(
            IReferRestorationKey<ShotgunProteomicsDB> annotatorKey,
            ShotgunProteomicsDB database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            MsRefSearchParameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, ShotgunProteomicsDB> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<ShotgunProteomicsDB> AnnotatorKey { get; set; }

        [Key("MsRefSearchParameter")]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }

        [Key("ProteomicsParameter")]
        public ProteomicsParameter ProteomicsParameter { get; set; }

        [Key("Database")]
        public ShotgunProteomicsDB Database { get; set; }
        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<IMSIonProperty, IMSScanProperty> IAnnotatorContainer.Annotator => Annotator;
        MsRefSearchParameterBase IAnnotatorContainer.Parameter => MsRefSearchParameter;

        public void Save(Stream stream) {
            Database.Save();
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load();
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }
}
