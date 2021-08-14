using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.IO;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotatorContainer
    {
        IAnnotator<IMSIonProperty, IMSScanProperty> Annotator { get; }
        string AnnotatorID { get; }

        MsRefSearchParameterBase Parameter { get; }
    }

    public sealed class AnnotatorContainer : IAnnotatorContainer
    {
        public AnnotatorContainer(
            IAnnotator<IMSIonProperty, IMSScanProperty> annotator,
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

        public IAnnotator<IMSIonProperty, IMSScanProperty> Annotator { get; }
        public string AnnotatorID { get; }

        public MsRefSearchParameterBase Parameter { get; set; }
    }

    [Union(0, typeof(DatabaseAnnotatorContainer))]
    [Union(1, typeof(SerializableAnnotatorContainer))]
    public interface ISerializableAnnotatorContainer : IAnnotatorContainer
    {
        void Save(Stream stream);
        void Load(Stream stream, ILoadAnnotatorVisitor visitor);
    }

    [MessagePackObject]
    public sealed class SerializableAnnotatorContainer : ISerializableAnnotatorContainer
    {
        public SerializableAnnotatorContainer(
            ISerializableAnnotator<IMSIonProperty, IMSScanProperty> annotator,
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
            IReferRestorationKey annotatorKey,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        IAnnotator<IMSIonProperty, IMSScanProperty> IAnnotatorContainer.Annotator => Annotator;

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

    [MessagePackObject]
    public sealed class DatabaseAnnotatorContainer : IDatabaseAnnotatorContainer
    {
        public DatabaseAnnotatorContainer(
            ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> annotator,
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
            IReferRestorationKey<MoleculeDataBase> annotatorKey,
            MoleculeDataBase database,
            MsRefSearchParameterBase parameter) {
            AnnotatorKey = annotatorKey;
            Parameter = parameter;
            Database = database;
            AnnotatorID = AnnotatorKey.Key;
        }

        [IgnoreMember]
        public ISerializableAnnotator<IMSIonProperty, IMSScanProperty, MoleculeDataBase> Annotator { get; private set; }
        [IgnoreMember]
        public string AnnotatorID { get; }

        [Key("AnnotatorKey")]
        public IReferRestorationKey<MoleculeDataBase> AnnotatorKey { get; set; }

        [Key("Parameter")]
        public MsRefSearchParameterBase Parameter { get; set; }

        [Key("Database")]
        public MoleculeDataBase Database { get; set; }
        [IgnoreMember]
        public string DatabaseID { get; }

        IAnnotator<IMSIonProperty, IMSScanProperty> IAnnotatorContainer.Annotator => Annotator;

        public void Save(Stream stream) {
            Database.Save(stream);
            AnnotatorKey = Annotator.Save();
        }

        public void Load(Stream stream, ILoadAnnotatorVisitor visitor) {
            Database.Load(stream);
            Annotator = AnnotatorKey.Accept(visitor, Database);
        }
    }
}
