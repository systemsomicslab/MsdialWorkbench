using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    public interface IReferRestorationKey
    {
        IAnnotator<IMSIonProperty, IMSScanProperty> Accept(ILoadAnnotatorVisitor visitor, MoleculeDataBase database);
    }

    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey
    {
        public DataBaseRestorationKey(string key) {
            Key = key;
        }

        [MessagePack.Key(0)]
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
}
