using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class NAGly_FAHFALipidParser : ILipidParser
    {
        public string Target { get; } = "NAGly";

        private static readonly NA_FAHFAChainParser chainsParser = new NA_FAHFAChainParser();
        public static readonly string Pattern = $"^NAGly\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.OxygenMass * 2,
            MassDiffDictionary.NitrogenMass * 1,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.NAGly, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}