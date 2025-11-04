using Accord.Math;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class LipidALipidParser : ILipidParser
    {
        public string Target { get; } = "LipidA";

        private static readonly LipidAChainParser chainsParser = new LipidAChainParser();
        public static readonly string Pattern = $"^LipidA\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex pattern = new Regex(Pattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 12,
            MassDiffDictionary.HydrogenMass * 20,
            MassDiffDictionary.OxygenMass * 15,
            MassDiffDictionary.NitrogenMass * 2,
            MassDiffDictionary.PhosphorusMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = pattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                return new Lipid(LbmClass.LipidA, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
    public class LipidAChainParser : TotalChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";
        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})?";

        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 6;
        private static readonly int capacity = 6;

        public LipidAChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: false, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern =
                $@"(?<MolecularSpeciesLevel>({AcylChainsPattern})-O-({AcylChainsPattern})_N-({AcylChainsPattern})-O-({AcylChainsPattern})_({AcylChainsPattern})_N-({AcylChainsPattern}))";
            var positionLevelPattern =
                $@"(?<PositionLevel>({AcylChainsPattern})-O-({AcylChainsPattern})/N-({AcylChainsPattern})-O-({AcylChainsPattern})/({AcylChainsPattern})/N-({AcylChainsPattern}))";

            var totalPattern = new[] { positionLevelPattern, molecularSpeciesLevelPattern, submolecularLevelPattern };
            Pattern = string.Join("|", totalPattern);
            Expression = new Regex(Pattern, RegexOptions.Compiled);
        }

        public new ITotalChain Parse(string lipidStr)
        {
            var match = Expression.Match(lipidStr);
            if (match.Success)
            {
                var groups = match.Groups;
                if (groups["PositionLevel"].Success)
                {
                    var chainStrs = match.Groups["PositionLevel"].Value.Replace("-O-", '_').Replace("N-", "").Split('_');
                    var chains = ParsePositionLevelChains(groups);
                    return chains;
                }
                else if (groups["MolecularSpeciesLevel"].Success)
                {
                    var chainStrs = match.Groups["MolecularSpeciesLevel"].Value.Replace("-O-", '_').Replace("N-", "").Split('_');
                    var chains = ParseMolecularSpeciesLevelChains(groups);
                    return chains;
                }
                else if (groups["TotalChain"].Success)
                {
                    return ParseTotalChains(groups, ChainCount);
                }
            }
            return null;
        }
    }
}
