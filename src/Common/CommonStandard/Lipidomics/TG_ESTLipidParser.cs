using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{

    public class TG_ESTLipidParser : ILipidParser
    {
        public string Target { get; } = "TG";

        private static readonly TG_ESTChainParser chainsParser = new TG_ESTChainParser();
        private static readonly string TgEstPattern = $"^TG\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex tgEstpattern = new Regex(TgEstPattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass * 3,
        }.Sum();

        private static readonly double SkeltonSub = new[]
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 6,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        public ILipid Parse(string lipidStr)
        {
            var match = tgEstpattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains is TotalChain)
                {
                    return new Lipid(LbmClass.TG_EST, SkeltonSub + chains.Mass, chains);
                }
                else
                {
                    return new Lipid(LbmClass.TG_EST, Skelton + chains.Mass, chains);
                }
            }
            return null;
        }
    }

    public class TG_ESTChainParser : TotalChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";
        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})?";
        private static readonly string FahfaAcylChainsPattern = $"(\\(FA\\s*{AcylChainsPattern}\\))?";

        private static readonly AcylChainParser AcylParser = new AcylChainParser();


        private static readonly string TgEstChainsPattern = $"({AcylChainsPattern})({FahfaAcylChainsPattern})?";
        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 4;
        private static readonly int capacity = 4;

        public TG_ESTChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: false, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern =
                $@"(?<MolecularSpeciesLevel>({TgEstChainsPattern})([_]{TgEstChainsPattern}){{{ChainCount - 2}}})";
            var positionLevelPattern =
                $@"(?<PositionLevel>({TgEstChainsPattern})([/]{TgEstChainsPattern}){{{ChainCount - 2}}})";

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
                    var chains = ParsePositionLevelChains(groups);
                    return chains;
                }
                else if (groups["MolecularSpeciesLevel"].Success)
                {
                    var chainStrs = match.Groups["MolecularSpeciesLevel"].Value.Split('_');
                    //var chains = ParseMolecularSpeciesLevelChains(groups);
                    var chains = ParseMslChainsFromStrings(chainStrs);
                    return chains;
                }
                else if (groups["TotalChain"].Success)
                {
                    return ParseTotalChains(groups, ChainCount);
                }
            }
            return null;
        }

        private ITotalChain ParseMslChainsFromStrings(string[] chainStrs)
        {
            var chains = new List<IChain>();
            var fahfaChains = new List<IChain>();
            foreach (var chainStr in chainStrs)
            {
                string baseChain = chainStr;
                string faChain = null;

                var faMatch = Regex.Match(chainStr, @"^(?<main>.+?)\(FA\s*(?<fa>.+?)\)$");
                if (faMatch.Success)
                {
                    baseChain = faMatch.Groups["main"].Value;
                    faChain = faMatch.Groups["fa"].Value;

                    var mainParsed = AcylParser.Parse(baseChain);
                    var faParsed = AcylParser.Parse(faChain);
                    fahfaChains.Add(mainParsed);
                    fahfaChains.Add(faParsed);
                }
                else
                {
                    var parsed = AcylParser.Parse(baseChain);
                    chains.Add(parsed);
                }
            }
            chains.Add((IChain)fahfaChains);
            return new PositionLevelChains(chains.ToArray()); // Need to fix the order of Chain. The last two are fahfa chains.
        }

    }
}

