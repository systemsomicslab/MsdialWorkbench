using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class DGTALipidParser : ILipidParser
    {
        public string Target { get; } = "DGTA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        public static readonly string Pattern = $"^DGTA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 10,
            MassDiffDictionary.HydrogenMass * 19,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.DGTA, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}
