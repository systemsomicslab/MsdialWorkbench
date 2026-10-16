using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class ASMLipidParser : ILipidParser
    {
        public string Target { get; } = "SM";

        private static readonly ASMChainParser chainsParser = new ASMChainParser();
        public static readonly string AsmPattern = $"^SM\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex asmPattern = new Regex(AsmPattern, RegexOptions.Compiled);

        private static readonly double Skelton = new[]
{
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 11,
            MassDiffDictionary.OxygenMass * 3,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double SkeltonTotal = new[]
{
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 2,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double SkeltonMsl = new[]
{
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.NitrogenMass * 1,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        public ILipid Parse(string lipidStr)
        {
            var match = asmPattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains is TotalChain)
                {
                    return new Lipid(LbmClass.ASM, SkeltonTotal + chains.Mass, chains);
                }
                else if (chains is MolecularSpeciesLevelChains)
                {
                    return new Lipid(LbmClass.ASM, SkeltonMsl + chains.Mass, chains);
                }
                else if (chains is PositionLevelChains)
                {
                    return new Lipid(LbmClass.ASM, Skelton + chains.Mass, chains);
                }
            }
            return null;
        }
    }
    public class ASMChainParser : TotalChainParser
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


        public ASMChainParser()
        : base(chainCount: chainCount, capacity: capacity, hasSphingosine: true, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>({AcylChainsPattern}))";
            var molecularSpeciesLevelPattern = $"(?<MolecularSpeciesLevel>(?<sm>{SphingoChainParser.Pattern})\\(FA ({AcylChainsPattern})\\))";
            var positionLevelPattern = $"(?<PositionLevel>(?<Chain>{SphingoChainParser.Pattern})\\(FA ({AcylChainsPattern})\\)/({AcylChainsPattern}))";

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
                    var sm = groups["sm"].Captures.Cast<Capture>().ToArray();
                    var acyl = groups["Chain"].Captures.Cast<Capture>().ToArray();
                    var parsedChain = new List<IChain>();
                    parsedChain.Add(SphingoParser.Parse(sm[0].Value));
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

