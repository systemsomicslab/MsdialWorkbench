using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class HexCerEosLipidParser : ILipidParser
    {
        public string Target { get; } = "HexCer";
        private static readonly double Skelton = new[]
{
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 5,
        }.Sum();
        private static readonly HexCerEosChainParser chainsParser = new HexCerEosChainParser();
        public static readonly string HexCerEosPattern = $"^HexCer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex HexCerEospattern = new Regex(HexCerEosPattern, RegexOptions.Compiled);

        public ILipid Parse(string lipidStr)
        {
            var match = HexCerEospattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains is TotalChain)
                {
                    return new Lipid(LbmClass.HexCer_EOS, Skelton + chains.Mass - MassDiffDictionary.OxygenMass, chains);
                }
                else if (chains is MolecularSpeciesLevelChains)
                {
                    return new Lipid(LbmClass.HexCer_EOS, Skelton + chains.Mass + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass, chains);
                }
                else if (chains is PositionLevelChains)
                {
                    var balance = 0.0;
                    if (group["OAcyl"].Value == null || group["OAcyl"].Value == "") { balance = + MassDiffDictionary.HydrogenMass; }
                    return new Lipid(LbmClass.HexCer_EOS, Skelton + chains.Mass + balance, chains);
                }
            }
            return null;
        }
    }
    public class HexCerEosChainParser : TotalChainParser
    {
        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})?";
        private static readonly AcylChainParser AcylParser = new AcylChainParser();
        private static readonly SphingoChainParser SphingoParser = new SphingoChainParser();

        public int ChainCount { get; }
        public int Capacity { get; }
        public bool HasSphingosine { get; }
        public string Pattern { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 3;
        private static readonly int capacity = 3;
        private static readonly bool hasSphingosine = true;


        public HexCerEosChainParser()
        : base(chainCount: chainCount, capacity: capacity, hasSphingosine: true, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>({AcylChainsPattern}))";
            var molecularSpeciesLevelPattern = $"(?<MolecularSpeciesLevel>(?<Cer>{SphingoChainParser.Pattern})(?<OAcyl>\\(FA ({AcylChainsPattern})\\)))";
            var positionLevelPattern = $"(?<PositionLevel>(?<Cer>(?<Chain>{SphingoChainParser.Pattern})/({AcylChainsPattern}))(?<OAcyl>\\(FA ({AcylChainsPattern})\\))?)";

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
                if (groups["PositionLevel"].Success)
                {
                    var chains = ParsePositionLevelChains(groups);
                    return chains;
                }
                else if (groups["MolecularSpeciesLevel"].Success)
                {
                    var Cer = groups["Cer"].Captures.Cast<Capture>().ToArray();
                    var acyl = groups["Chain"].Captures.Cast<Capture>().ToArray();
                    var parsedChain = new List<IChain>();
                    parsedChain.Add(SphingoParser.Parse(Cer[0].Value));
                    parsedChain.Add(AcylParser.Parse(acyl[0].Value));
                    var chains = new MolecularSpeciesLevelChains(parsedChain.ToArray());
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

