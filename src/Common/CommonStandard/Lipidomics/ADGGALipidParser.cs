using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class ADGGALipidParser : ILipidParser
    {
        public string Target { get; } = "ADGGA";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(3);
        public static readonly string Pattern = $"^ADGGA\\s*(\\(O-)?(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);
                private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})";

        private static readonly TotalChainParser chainsParserSub = TotalChainParser.BuildParser(2);
        public static readonly string PatternSub = $"^ADGGA\\s*\\(O-(?<OAcyl>{AcylChainsPattern})\\)?(?<sn1sn2>{chainsParserSub.Pattern})*?$";
        private static readonly Regex patternSub = new Regex(PatternSub, RegexOptions.Compiled);


        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
        }.Sum();


        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr.Replace("_", "/")); //need consider
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.ADGGA, Skelton + chains.Mass, chains);
            }
            else
            {
                var matchSub = patternSub.Match(lipidStr);
                if (matchSub.Success)
                {
                    var groupSub = matchSub.Groups;
                    if (groupSub["sn1sn2"].Value != "")
                    {
                        var chains = chainsParser.Parse(groupSub["OAcyl"].Value + "/" + groupSub["sn1sn2"].Value);
                        return new Lipid(LbmClass.ADGGA, Skelton + chains.Mass, chains);
                    }
                }
            }
            return null;
        }
    }
}
