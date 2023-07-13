using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class HBMPLipidParser : ILipidParser {
        public string Target { get; } = "HBMP";

        //HBMP explain rule -> HBMP (1 chain(sn1))/(2 chain(sn2,sn3))
        //HBMP sn1_sn2_sn3 (follow the rules of alignment) -- MolecularSpeciesLevelChains
        //HBMP sn1/(sn2+sn3) (follow the rules of alignment) -- MolecularSpeciesLevelChains <- cannot parsing now. maybe don't need(?)
        //HBMP sn1/sn2_sn3 -- MolecularSpeciesLevelChains <- now same as sn1_sn2_sn3
        //HBMP sn1/sn4(or sn4/sn1)/sn2/sn3 (sn4= 0:0)  -- PositionLevelChains <- !?

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(3);
        public static readonly string Pattern = $"^HBMP\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 8,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.HBMP, Skelton + chains.Mass, chains);
            }
            else
            {
                var matchSub2 = pattern.Match(lipidStr.Replace("_", "/"));
                if (matchSub2.Success)
                {
                    var group = matchSub2.Groups;
                    var chains = chainsParser.Parse(group["sn"].Value);
                    return new Lipid(LbmClass.HBMP, Skelton + chains.Mass, chains);
                }
            }

            return null;
        }
    }
}
