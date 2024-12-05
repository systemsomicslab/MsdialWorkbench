using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public class FacadeLipidParser : ILipidParser
    {
        private readonly Dictionary<string, List<ILipidParser>> map = new Dictionary<string, List<ILipidParser>>();

        public string Target { get; } = string.Empty;

        /// <summary>
        /// Parses the given lipid string and returns the corresponding ILipid object.
        /// </summary>
        /// <param name="lipidStr">The lipid name to parse.</param>
        /// <returns>The parsed ILipid object, or null if parsing fails.</returns>
        public ILipid Parse(string lipidStr) {
            var key = lipidStr.Split()[0];
            if (map.TryGetValue(key, out var parsers)) {
                // Remove lipid synonym (e.g. PC 34:1|PC 16:0_18:1 -> PC 16:0_18:1) if it exists
                var parts = lipidStr.Split('|');
                lipidStr = parts.Length > 1 ? parts[1] : lipidStr;
                foreach (var parser in parsers) {
                    if (parser.Parse(lipidStr) is ILipid lipid) {
                        return lipid;
                    }
                }
            }
            return null;
        }

        public void Add(ILipidParser parser) {
            if (!map.ContainsKey(parser.Target)) {
                map.Add(parser.Target, new List<ILipidParser>());
            }
            map[parser.Target].Add(parser);
        }

        public void Remove(ILipidParser parser) {
            if (map.ContainsKey(parser.Target)) {
                map[parser.Target].Remove(parser);
            }
        }

        public static ILipidParser Default {
            get {
                if (@default is null) {
                    var parser = new FacadeLipidParser();
                    new List<ILipidParser>{
                        new BMPLipidParser(),
                        new CLLipidParser(),
                        new DGLipidParser(),
                        new HBMPLipidParser(),
                        new LPCLipidParser(),
                        new LPELipidParser(),
                        new LPGLipidParser(),
                        new LPILipidParser(),
                        new LPSLipidParser(),
                        new MGLipidParser(),
                        new PALipidParser(),
                        new PCLipidParser(),
                        new PELipidParser(),
                        new PGLipidParser(),
                        new PILipidParser(),
                        new PSLipidParser(),
                        new TGLipidParser(),
                        new DGLipidParser(),
                        new EtherPCLipidParser(),
                        new EtherPELipidParser(),
                        new EtherLPCLipidParser(),
                        new EtherLPELipidParser(),
                        new SMLipidParser(),
                        new CeramideLipidParser(),
                        new HexCerLipidParser(),
                        new Hex2CerLipidParser(),
                        new DGTALipidParser(),
                        new DGTSLipidParser(),
                        new LDGTALipidParser(),
                        new LDGTSLipidParser(),
                        new GM3LipidParser(),
                        new SHexCerLipidParser(),
                        new CARLipidParser(),
                        new CLLipidParser(),
                        new DMEDFAHFALipidParser(),
                        new DMEDFALipidParser(),
                        new CELipidParser(),
                        new PCd5LipidParser(),
                        new PEd5LipidParser(),
                        new PId5LipidParser(),
                        new PGd5LipidParser(),
                        new PSd5LipidParser(),
                        new LPCd5LipidParser(),
                        new LPEd5LipidParser(),
                        new LPId5LipidParser(),
                        new LPGd5LipidParser(),
                        new LPSd5LipidParser(),
                        new CeramideNsD7LipidParser(),
                        new SMd9LipidParser(),
                        new DGd5LipidParser(),
                        new TGd5LipidParser(),
                        new CEd7LipidParser(),

                    }.ForEach(parser.Add);
                    @default = parser;
                }
                return @default;
            }
        }
        private static ILipidParser @default;
    }
}
