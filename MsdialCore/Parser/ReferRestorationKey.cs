using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    [MessagePack.Union(3, typeof(StandardRestorationKey))]
    public interface IReferRestorationKey
    {
        IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database);

        string Key { get; }
    }

    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey
    {
        public DataBaseRestorationKey(string key) {
            Key = key;
        }

        [MessagePack.Key(nameof(Key))]
        public string Key { get; set; }

        public abstract IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database);
    }

    [MessagePack.MessagePackObject]
    public class MspDbRestorationKey : DataBaseRestorationKey
    {
        public MspDbRestorationKey(string key) : base(key) {

        }

        public override IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }

    [MessagePack.MessagePackObject]
    public class TextDbRestorationKey : DataBaseRestorationKey
    {
        public TextDbRestorationKey(string key) : base(key) {

        }

        public override IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }

    [MessagePack.MessagePackObject]
    public class StandardRestorationKey : DataBaseRestorationKey
    {
        public StandardRestorationKey(string key, MsRefSearchParameterBase parameter, SourceType sourceType) : base(key) {
            Parameter = parameter;
            SourceType = sourceType;
        }

        public StandardRestorationKey(string key, MsRefSearchParameterBase parameter, ProteomicsParameter proteomicsparam, SourceType sourceType) : base(key) {
            Parameter = parameter;
            ProteomicsParameter = proteomicsparam;
            SourceType = sourceType;
        }

        [MessagePack.Key(nameof(Parameter))]
        public MsRefSearchParameterBase Parameter { get; set; }

        [MessagePack.Key(nameof(ProteomicsParameter))]
        public ProteomicsParameter ProteomicsParameter { get; set; }

        [MessagePack.Key(nameof(SourceType))]
        public SourceType SourceType { get; set; }

        public override IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database) {
            return visitor.Visit(this, database);
        }
    }
}
