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
        public static readonly string PatternSub = $"^CL\\s*(?<sn1sn2>{chainsParserSub.Pattern})[/_]*?(?<sn3sn4>{chainsParserSub.Pattern})*?$";
        private static readonly Regex patternSub = new Regex(PatternSub, RegexOptions.Compiled);
        public static readonly string PatternSub2 = $"^CL\\s*(?<sn>{chainsParserSub.Pattern})$";
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
            MassDiffDictionary.HydrogenMass * 14,
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
                        var chains = chainsParser.Parse(groupSub["sn1sn2"].Value + "_" + groupSub["sn3sn4"].Value);
                        return new Lipid(LbmClass.CL, Skelton + chains.Mass, chains);
                    }
                    //else
                    //{
                    //    var carbon = int.Parse(groupSub["carbon"].Captures[0].Value) + int.Parse(groupSub["carbon"].Captures[1].Value);
                    //    var db = int.Parse(groupSub["db"].Captures[0].Value) + int.Parse(groupSub["db"].Captures[1].Value);
                    //    var ox = !groupSub["ox"].Success ? 0 : !groupSub["oxnum"].Success ? 1 : int.Parse(groupSub["oxnum"].Value);
                    //    var chains = chainsParser.Parse("CL " + carbon + ":" + db + ";" + ox);
                    //    return new Lipid(LbmClass.CL, Skelton + chains.Mass, chains);

                    //}
                }
            }
            return null;
        }
    }
}
