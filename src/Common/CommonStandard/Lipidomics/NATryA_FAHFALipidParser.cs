using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class NATryA_FAHFALipidParser : ILipidParser
    {
        public string Target { get; } = "NATryA";

        private static readonly NA_FAHFAChainParser chainsParser = new NA_FAHFAChainParser();
        public static readonly string Pattern = $"^NATryA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 10,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.NitrogenMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.NATryA, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}