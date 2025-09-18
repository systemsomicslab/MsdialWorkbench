using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class NAGlySer_FAHFALipidParser : ILipidParser
    {
        public string Target { get; } = "NAGlySer";

        private static readonly NA_FAHFAChainParser chainsParser = new NA_FAHFAChainParser();
        public static readonly string Pattern = $"^NAGlySer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.NAGlySer, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
    public class NA_FAHFAChainParser : TotalChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";
        private static readonly string AcylChainsPattern = $"(?<HFA>{AcylChainParser.Pattern})?";
        private static readonly string OAcylPattern = $"\\(FA (?<OChain>{AcylChainParser.Pattern})\\)";

        private static readonly AcylChainParser AcylParser = new AcylChainParser();

        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        public bool HasSphingosine { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 2;
        private static readonly int capacity = 2;
        private readonly bool hasSphingosine;

        public NA_FAHFAChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: false, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern =
                $"(?<MolecularSpeciesLevel>(?<Chain>{AcylChainsPattern})(?<Chain>{OAcylPattern})?)";
            var positionLevelPattern =
                $"(?<PositionLevel>(?<Chain>{AcylChainsPattern})(?<Chain>{OAcylPattern})?)";

            var totalPattern = new[] { positionLevelPattern, molecularSpeciesLevelPattern, submolecularLevelPattern };
            Pattern = string.Join("|", totalPattern);
            Expression = new Regex(Pattern, RegexOptions.Compiled);
            HasSphingosine = hasSphingosine;
        }

        public new ITotalChain Parse(string lipidStr)
        {
            var match = Expression.Match(lipidStr);
            if (match.Success)
            {
                var groups = match.Groups;
                if (groups["TotalChain"].Success)
                {
                    return ParseTotalChains(groups, ChainCount);
                }
                else
                {
                    var chains = ParsePositionLevelChains(groups);
                    return chains;
                }
            }
            return null;
        }
    }
}