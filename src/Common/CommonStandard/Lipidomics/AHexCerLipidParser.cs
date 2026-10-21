using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class AHexCerLipidParser : ILipidParser
    {
        public string Target { get; } = "AHexCer";

        private static readonly AHexCerChainParser chainsParser = new AHexCerChainParser();
        public static readonly string AHexCerPattern = $"^AHexCer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex AHexCerpattern = new Regex(AHexCerPattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();


        public ILipid Parse(string lipidStr)
        {
            var match = AHexCerpattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (group["MolecularSpeciesLevel"].Success)
                {
                    return new Lipid(LbmClass.AHexCer, Skelton + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass + chains.Mass, chains);
                }
                return new Lipid(LbmClass.AHexCer, Skelton + chains.Mass, chains);
            }
            return null;
        }
    }
    public class AHexCerChainParser : TotalChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";
        private static readonly string AcylChainsPattern = $"(?<CerAcyl>{AcylChainParser.Pattern})?";
        private static readonly string OAcylPattern = $"\\(O-(?<OChain>{CarbonPattern}:{DoubleBondPattern})\\)";

        private static readonly AcylChainParser AcylParser = new AcylChainParser();
        private static readonly SphingoChainParser SphingoParser = new SphingoChainParser();

        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        public bool HasSphingosine { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 3;
        private static readonly int capacity = 3;
        private readonly bool hasSphingosine;

        public AHexCerChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: true, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern =
                $"(?<MolecularSpeciesLevel>{OAcylPattern}(?<Sph>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)?)";
            var positionLevelPattern =
                $"(?<PositionLevel>{OAcylPattern}(?<Sph>{SphingoChainParser.Pattern})/({AcylChainsPattern}))";

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
        private new PositionLevelChains ParsePositionLevelChains(GroupCollection groups)
        {
            var OAcyl = groups["OChain"].Captures.Cast<Capture>();
            var Sph = SphingoParser.Parse(groups["Sph"].Value);
            var CerAcyl = AcylParser.Parse(groups["CerAcyl"].Value);
            var chains = OAcyl
                    .Select(c => AcylParser.Parse(c.Value))
                    .ToList();
            chains.Add(Sph);
            if (CerAcyl != null)
            {
                chains.Add(CerAcyl);
            }
            return new PositionLevelChains(chains.ToArray());
        }

    }
}

