using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class LNAPELipidParser : ILipidParser
    {
        public string Target { get; } = "LPE-N";

        private static readonly TotalChainParser chainsParser = TotalChainParser.BuildParser(2);
        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})";
        public static readonly string Pattern = $"^LPE-N\\s*(?<sn>(?<Nacyl>\\(FA )?{AcylChainsPattern}/?\\)?){AcylChainsPattern})";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);
        //public static readonly string PatternSub = $"^LPE-N\\s*(?<sn>{chainsParser.Pattern})$";
        //private static readonly Regex patternSub = new Regex(PatternSub, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 12,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LNAPE, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
}

//private TotalChainParser(int chainCount, int capacity, bool hasSphingosine, bool hasEther, bool atLeastSpeciesLevel)
//{
//    ChainCount = chainCount;
//    Capacity = capacity;
//    var submolecularLevelPattern = hasEther
//        ? $"(?<TotalChain>(?<plasm>[de]?[OP]-)?{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)"
//        : $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
//    var molecularSpeciesLevelPattern = hasSphingosine
//        ? $"(?<MolecularSpeciesLevel>(?<Chain>{SphingoChainParser.Pattern})_({ChainsPattern}_?){{{ChainCount - 1}}})"
//        : hasEther
//        ? $"(?<MolecularSpeciesLevel>({ChainsPattern}_?){{{ChainCount}}})"
//        : $"(?<MolecularSpeciesLevel>({AcylChainsPattern}_?){{{ChainCount}}})";
//    var positionLevelPattern = hasSphingosine
//        ? $"(?<PositionLevel>(?<Chain>{SphingoChainParser.Pattern})/({ChainsPattern}/?){{{Capacity - 1}}})"
//        : hasEther
//        ? $"(?<PositionLevel>({ChainsPattern}/?){{{Capacity}}})"
//        : $"(?<PositionLevel>((?<Nacyl>\\(FA )?{AcylChainsPattern}/?\\)?){{{Capacity}}})";
