using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class GM3LipidParser : ILipidParser
    {
        public string Target { get; } = "GM3";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^GM3\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 23,
            MassDiffDictionary.HydrogenMass * 37,
            MassDiffDictionary.OxygenMass * 18,
            MassDiffDictionary.NitrogenMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.GM3, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
