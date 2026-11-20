using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CLLipidParser : ILipidParser
    {
        public string Target { get; } = "CL";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(4);
        public static readonly string Pattern = $"^CL\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly TotalChainParser chainsParserSub = TotalChainParser.BuildParser(2);
        public static readonly string PatternSub = $"^CL\\s*(?<sn1sn2>{chainsParserSub.Pattern})/?(?<sn3sn4>{chainsParserSub.Pattern})*?$";
        private static readonly Regex patternSub = new Regex(PatternSub, RegexOptions.Compiled);
        private static readonly TotalChainParser chainsParserSub2 = TotalChainParser.BuildSpeciesLevelParser(1, 2);
        public static readonly string PatternSub2 = $"^CL\\s*(?<sn>{chainsParserSub.Pattern})*?$";
        private static readonly Regex patternSub2 = new Regex(PatternSub2, RegexOptions.Compiled);


        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.OxygenMass * 13,
            MassDiffDictionary.PhosphorusMass *2,
        }.Sum();

        private static readonly double SkeltonSub = new[]
        {
            MassDiffDictionary.CarbonMass * 9,
            MassDiffDictionary.HydrogenMass * 16,
            MassDiffDictionary.OxygenMass * 15,
            MassDiffDictionary.PhosphorusMass *2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.CL, Skelton + chains.Mass, chains);
            }
            else
            {
                var matchSub = patternSub.Match(lipidStr);
                if (matchSub.Success)
                {
                    var groupSub = matchSub.Groups;
                    if (groupSub["sn3sn4"].Value != "")
                    {
                        match = pattern.Match(lipidStr.Replace("_", "/"));
                        var group = match.Groups;
                        var chains = chainsParser.Parse(group["sn"].Value);
                        return new Lipid(LbmClass.CL, Skelton + chains.Mass, chains);
                    }
                    else if (groupSub["sn1sn2"].Value != "")
                    {
                        var matchSub2 = patternSub2.Match(lipidStr);
                        var groupSub2 = matchSub2.Groups;
                        var chains = chainsParserSub.Parse(groupSub2["sn"].Value);
                        return new Lipid(LbmClass.CL, SkeltonSub + chains.Mass, chains);
                    }
                }
            }
            return null;
        }
    }
}
