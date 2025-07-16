using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class MLCLLipidParser : ILipidParser
    {
        public string Target { get; } = "MLCL";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(3);
        public static readonly string Pattern = $"^MLCL\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly TotalChainParser chainsParserSub = TotalChainParser.BuildParser(2);
        public static readonly string PatternSub = $"^MLCL\\s*(?<sn1>{chainsParserSub.Pattern})/*?(?<sn2sn3>{chainsParserSub.Pattern})*?$";
        private static readonly Regex patternSub = new Regex(PatternSub, RegexOptions.Compiled);


        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 19,
            MassDiffDictionary.OxygenMass * 13,
            MassDiffDictionary.PhosphorusMass *2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.MLCL, Skelton + chains.Mass, chains);
            }
            else
            {
                var matchSub = patternSub.Match(lipidStr);
                if (matchSub.Success)
                {
                    match = pattern.Match(lipidStr.Replace("_", "/"));  //temporary change to PositionLevelChains
                    var group = match.Groups;
                    var groupSub = matchSub.Groups;
                    if (groupSub["sn2sn3"].Value != "")
                    {
                        var chains = chainsParser.Parse(group["sn"].Value);
                        return new Lipid(LbmClass.MLCL, Skelton + chains.Mass, chains);
                    }
                }
            }
            return null;
        }
    }
}
