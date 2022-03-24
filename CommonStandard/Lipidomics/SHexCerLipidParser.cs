using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class SHexCerLipidParser : ILipidParser
    {
        public string Target { get; } = "SHexCer";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^SHexCer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.SulfurMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.SHexCer, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
