using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class DMEDFALipidParser : ILipidParser
    {
        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 11,
        }.Sum();
        public string Target { get; } = "DMEDFA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(1);
        public static readonly string Pattern = $"^DMEDFA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DMEDFA, Skelton + chains.Mass, chains);
            }
            return null;
        }

    }
}
