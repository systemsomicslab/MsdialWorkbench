using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Parser
{
    public interface IReferRestorationKey<in T, U, V>
    {
        ISerializableAnnotator<T, U, V> Accept(ILoadAnnotatorVisitor visitor);

        string Key { get; }
    }

    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    [MessagePack.Union(3, typeof(StandardRestorationKey))]
    [MessagePack.Union(4, typeof(ShotgunProteomicsRestorationKey))]
    public interface IReferRestorationKey<in T, U, V, in W>
    {
        ISerializableAnnotator<T, U, V, W> Accept(ILoadAnnotatorVisitor visitor, W database);

        string Key { get; }

        int Priority { get; }
    }

   
    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public DataBaseRestorationKey(string key, int priority) {
            Key = key;
            Priority = priority;
        }

        [MessagePack.Key(nameof(Key))]
        public string Key { get; }

        [MessagePack.Key(nameof(Priority))]
        public int Priority { get; }

        public abstract ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database);
    }

    [MessagePack.MessagePackObject]
    public class MspDbRestorationKey : DataBaseRestorationKey
    {
        public MspDbRestorationKey(string key, int priority) : base(key, priority) {

        }

        public override ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }

    [MessagePack.MessagePackObject]
    public class TextDbRestorationKey : DataBaseRestorationKey
    {
        public TextDbRestorationKey(string key, int priority) : base(key, priority) {

        }

        public override ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }

    [MessagePack.MessagePackObject]
    public class StandardRestorationKey : DataBaseRestorationKey
    {
        public StandardRestorationKey(string key, int priority, MsRefSearchParameterBase parameter, SourceType sourceType) : base(key, priority) {
            Parameter = parameter;
            SourceType = sourceType;
        }

        [MessagePack.Key(nameof(Parameter))]
        public MsRefSearchParameterBase Parameter { get; set; }

        [MessagePack.Key(nameof(SourceType))]
        public SourceType SourceType { get; set; }

        public override ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }

    [MessagePack.MessagePackObject]
    public abstract class FastaDbRestorationKey : IReferRestorationKey<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> {
        public FastaDbRestorationKey(string key, int priority) {
            Key = key;
            Priority = priority;
        }

        [MessagePack.Key(nameof(Key))]
        public string Key { get; }

        [MessagePack.Key(nameof(Priority))]
        public int Priority { get; }

        public abstract ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Accept(ILoadAnnotatorVisitor visitor, ShotgunProteomicsDB database);
    }

    [MessagePack.MessagePackObject]
    public class ShotgunProteomicsRestorationKey : FastaDbRestorationKey {
        public ShotgunProteomicsRestorationKey(string key, int priority, MsRefSearchParameterBase msrefSearchParameter, ProteomicsParameter proteomicsParameter, SourceType sourceType) : base(key, priority) {
            MsRefSearchParameter = msrefSearchParameter;
            ProteomicsParameter = proteomicsParameter;
            SourceType = sourceType;
        }

        [MessagePack.Key(nameof(MsRefSearchParameter))]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }

        [MessagePack.Key(nameof(ProteomicsParameter))]
        public ProteomicsParameter ProteomicsParameter { get; set; }

        [MessagePack.Key(nameof(SourceType))]
        public SourceType SourceType { get; set; }

        public override ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Accept(ILoadAnnotatorVisitor visitor, ShotgunProteomicsDB database) {
            return visitor.Visit(this, database);
        }
    }

}
