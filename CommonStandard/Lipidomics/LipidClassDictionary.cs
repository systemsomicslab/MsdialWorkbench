using CompMs.Common.Enum;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

namespace CompMs.Common.Lipidomics
{
    public class LipidClassProperty
    {
        public LipidClassProperty(LbmClass @class, string displayName, int acylChain, int alkylChain, int sphingoChain) {
            Class = @class;
            DisplayName = displayName;
            AcylChain = acylChain;
            AlkylChain = alkylChain;
            SphingoChain = sphingoChain;
        }

        public LbmClass Class { get; }
        public string DisplayName { get; }

        public int TotalChain => AcylChain + AlkylChain + SphingoChain;
        public int AcylChain { get; }
        public int AlkylChain { get; }
        public int SphingoChain { get; }
        // public int ExtraAcylChain { get; }
    }

    public class LipidClassDictionary
    {
        private LipidClassDictionary() {
            lbmItems = new Dictionary<LbmClass, LipidClassProperty>();
            LbmItems = new ReadOnlyDictionary<LbmClass, LipidClassProperty>(lbmItems);
        }

        public ReadOnlyDictionary<LbmClass, LipidClassProperty> LbmItems { get; }
        
        private readonly Dictionary<LbmClass, LipidClassProperty> lbmItems;


        public static LipidClassDictionary Default {
            get {
                if (@default is null) {
                    @default = ParseDictinary();
                }
                return @default;
            }
        }
        private static LipidClassDictionary @default;

        private static LipidClassDictionary ParseDictinary() {
            var resourceName = "CompMs.Common.Resources.LipidClassProperties.csv";
            var assembly = Assembly.GetExecutingAssembly();
            var result = new LipidClassDictionary();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream)) {
                reader.ReadLine(); // skip header
                while (reader.Peek() >= 0) {
                    var cols = reader.ReadLine().Split(',');
                    var item = new LipidClassProperty(
                        (LbmClass)System.Enum.Parse(typeof(LbmClass), cols[0]),
                        cols[1],
                        int.TryParse(cols[2], out var acyl) ? acyl : 0,
                        int.TryParse(cols[3], out var alkyl) ? alkyl : 0,
                        int.TryParse(cols[4], out var sphingo) ? sphingo : 0);
                    result.lbmItems[item.Class] = item;
                }
            }
            return result;
        }
    }
}
