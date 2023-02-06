using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class DMEDFAHFALipidParser : ILipidParser
    {
        private static readonly double C4N2H10 = new[]
        {
            MassDiffDictionary.CarbonMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.HydrogenMass * 10,
        }.Sum();
        public string Target { get; } = "DMEDFAHFA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^DMEDFAHFA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DMEDFAHFA, chains.Mass + C4N2H10, chains);
            }
            return null;
        }
    }
}
