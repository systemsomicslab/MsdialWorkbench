using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public class FacadeLipidParser : ILipidParser
    {
        private readonly Dictionary<string, List<ILipidParser>> map = new Dictionary<string, List<ILipidParser>>();

        public string Target { get; } = string.Empty;

        public ILipid Parse(string lipidStr) {
            var key = lipidStr.Split()[0];
            if (map.TryGetValue(key, out var parsers)) {
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
    }
}
