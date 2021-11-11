using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class PCLipidParser {
        private static readonly AcylChainParser acylParser = new AcylChainParser();

        public static readonly string Pattern = $"PC\\s*(?<sn1>{AcylChainParser.Pattern})((?<sep>[_/])(?<sn2>{AcylChainParser.Pattern}))?";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 8,
            MassDiffDictionary.HydrogenMass * 18,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr) {
            var match = pattern.Match(lipidStr);
            if (match.Success) {
                var group = match.Groups;
                var acyl1 = acylParser.Parse(group["sn1"].Value);
                if (group["sep"].Success) {
                    var acyl2 = acylParser.Parse(group["sn2"].Value);
                    switch (group["sep"].Value) {
                        case "_":
                            return new Lipid(LbmClass.PC, Skelton + acyl1.Mass + acyl2.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                        case "/":
                            return new Lipid(LbmClass.PC, Skelton + acyl1.Mass + acyl2.Mass, new PositionLevelChains(acyl1, acyl2));
                    }
                }
                else {
                    var subAcyl = new TotalChains(acyl1.CarbonCount, acyl1.DoubleBondCount, acyl1.OxidizedCount, 2);
                    return new Lipid(LbmClass.PC, Skelton + subAcyl.Mass, subAcyl);
                }
            }
            return null;
        }
    }
}
