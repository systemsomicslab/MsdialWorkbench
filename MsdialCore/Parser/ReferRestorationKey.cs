using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    public interface IReferRestorationKey
    {
        IMatchResultRefer Restore(ParameterBase parameter);

    }

    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey
    {
        public DataBaseRestorationKey(string path) {
            DataBasePath = path;
        }

        [MessagePack.Key(0)]
        public string DataBasePath { get; set; }

        public abstract IMatchResultRefer Restore(ParameterBase parameter);
    }

    [MessagePack.MessagePackObject]
    public class MspDbRestorationKey : DataBaseRestorationKey
    {
        public MspDbRestorationKey(string path) : base(path) {

        }

        public override IMatchResultRefer Restore(ParameterBase parameter) {
            var db = new List<MoleculeMsReference>();
            if (!string.IsNullOrEmpty(DataBasePath)) {
                var ext = Path.GetExtension(DataBasePath);
                if (ext == ".msp" || ext == ".msp2") {
                    db = LibraryHandler.ReadMspLibrary(DataBasePath).OrderBy(msp => msp.PrecursorMz).ToList();
                }
                else if (ext == ".lbm" || ext == ".lbm2") {
                    db = LibraryHandler.ReadLipidMsLibrary(DataBasePath, parameter).OrderBy(msp => msp.PrecursorMz).ToList();
                }
            }
            return new DataBaseRefer(db);
        }
    }

    [MessagePack.MessagePackObject]
    public class TextDbRestorationKey : DataBaseRestorationKey
    {
        public TextDbRestorationKey(string path) : base(path) {

        }

        public override IMatchResultRefer Restore(ParameterBase parameter) {
            var db = new List<MoleculeMsReference>();
            if (!string.IsNullOrEmpty(DataBasePath)) {
                db = TextLibraryParser.TextLibraryReader(DataBasePath, out var _);
            }
            return new DataBaseRefer(db);
        }
    }
}
