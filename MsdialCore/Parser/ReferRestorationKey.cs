using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public DataBaseRestorationKey(string key, int priority) {
            Key = key;
            Priority = priority;
        }

        [MessagePack.Key(nameof(Key))]
        public string Key { get; }

        [MessagePack.Key(nameof(Priority))]
        public int Priority { get; }

        public abstract ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database);
        public abstract IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, MoleculeDataBase database);
    }

    [MessagePack.MessagePackObject]
    public class MspDbRestorationKey : DataBaseRestorationKey
    {
        public MspDbRestorationKey(string key, int priority) : base(key, priority) {

        }

        public override ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }

        public override IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, MoleculeDataBase database) {
            return factoryVisitor.Visit(this, annotatorVisitor.Visit(this, database));
        }
    }

    [MessagePack.MessagePackObject]
    public class TextDbRestorationKey : DataBaseRestorationKey
    {
        public TextDbRestorationKey(string key, int priority) : base(key, priority) {

        }

        public override ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }

        public override IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, MoleculeDataBase database) {
            return factoryVisitor.Visit(this, annotatorVisitor.Visit(this, database));
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

        public override ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }

        public override IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, MoleculeDataBase database) {
            return factoryVisitor.Visit(this, annotatorVisitor.Visit(this, database));
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
        public abstract IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, ShotgunProteomicsDB database);
    }

    [MessagePack.MessagePackObject]
    public class ShotgunProteomicsRestorationKey : FastaDbRestorationKey {
        public ShotgunProteomicsRestorationKey(
            string key, 
            int priority,
            MsRefSearchParameterBase msRefSearchParameter,
            ProteomicsParameter proteomicsParameter, 
            SourceType sourceType) : base(key, priority) {
            MsRefSearchParameter = msRefSearchParameter;
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

        public override IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, ShotgunProteomicsDB database) {
            return factoryVisitor.Visit(this, annotatorVisitor.Visit(this, database));
        }
    }

    [MessagePack.MessagePackObject]
    public class EadLipidDatabaseRestorationKey : IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>
    {
        public EadLipidDatabaseRestorationKey(string key, int priority, MsRefSearchParameterBase msRefSearchParameter, SourceType sourceType) {
            Key = key;
            Priority = priority;
            MsRefSearchParameter = msRefSearchParameter;
            SourceType = sourceType;
        }

        [MessagePack.Key(nameof(Key))]
        public string Key { get; }

        [MessagePack.Key(nameof(Priority))]
        public int Priority { get; }

        [MessagePack.Key(nameof(MsRefSearchParameter))]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }

        [MessagePack.Key(nameof(SourceType))]
        public SourceType SourceType { get; set; }

        public ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Accept(ILoadAnnotatorVisitor visitor, EadLipidDatabase database) {
            return visitor.Visit(this, database);
        }

        public IAnnotationQueryFactory<MsScanMatchResult> Accept(IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, EadLipidDatabase database) {
            return factoryVisitor.Visit(this, annotatorVisitor.Visit(this, database));
        }
    }
}
