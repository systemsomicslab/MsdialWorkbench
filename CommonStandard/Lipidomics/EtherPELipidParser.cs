using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class EtherPELipidParser
    {
        private static readonly AcylChainParser acylParser = new AcylChainParser();
        private static readonly AlkylChainParser alkylParser = new AlkylChainParser();

        public static readonly string Pattern = $"PE (?<sn1>{AlkylChainParser.Pattern})([_/](?<sn2>{AcylChainParser.Pattern}))?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var alkyl = alkylParser.Parse(group["sn1"].Value);
                if (group["sn2"].Success) {
                    var acyl = acylParser.Parse(group["sn2"].Value);
                    return new Lipid(LbmClass.EtherPE, Skelton + alkyl.Mass + acyl.Mass, new PositionLevelChains(alkyl, acyl));
                }
                else {
                    var subTotal = new TotalChains(alkyl.CarbonCount, alkyl.DoubleBondCount, alkyl.OxidizedCount, 2, 1);
                    return new Lipid(LbmClass.EtherPE, Skelton + subTotal.Mass, subTotal);
                }
            }

            return null;
        }
    }
}
