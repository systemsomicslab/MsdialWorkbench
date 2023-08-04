using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class Hex2CerLipidParser : ILipidParser
    {
        public string Target { get; } = "Hex2Cer";

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 12,
            MassDiffDictionary.HydrogenMass * 20,
            MassDiffDictionary.OxygenMass * 10,
        }.Sum();

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildCeramideParser(2);
        public static readonly string Pattern = $"^Hex2Cer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);
        //public static readonly string CeramideClassPattern = @"\d+:(?<d>\d+).*?\(?((?<sp>\d+)OH,?)+\)?/\d+:\d+.*?(;?(?<h>\(?((?<ab>\d+)OH,?)+\)?|((?<oxnum>\d+)?O)))?";
        //private static readonly Regex ceramideClassPattern = new Regex(CeramideClassPattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.Hex2Cer, chains.Mass + Skelton, chains);
            }
            return null;
        }
    }
}