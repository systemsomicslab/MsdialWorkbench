using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;
using System.Linq.Expressions;
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
            MassDiffDictionary.HydrogenMass * 5,
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
                var fahfaCain= group["fahfa"].Value;
                var chains = chainsParser.Parse(group["sn"].Value);
                if(chains is TotalChain) {
                    return new Lipid(LbmClass.TG_EST, Skelton + chains.Mass, chains);
                }
                else
                {
                    return new Lipid(LbmClass.TG_EST, Skelton + chains.Mass, chains);

                }


                //if (chains.ChainCount > 3)
                //{
                //    return new Lipid(LbmClass.TG_EST, SkeltonSub + chains.Mass, chains);
                //}
                //else
                //{
                //    return new Lipid(LbmClass.TG_EST, Skelton + chains.Mass, chains);
                //}
            }
            return null;
        }
    }

    public class TG_ESTChainParser : TotalChainParser
    {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";

        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})";
        private static readonly string TgEstChainsPattern = $"({AcylChainsPattern})(\\(FA\\s*(?<fahfa>{AcylChainsPattern})\\))?";

        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }
        private readonly Regex Expression;

        private static readonly int chainCount = 3;
        private static readonly int capacity = 4;

        public TG_ESTChainParser()
            : base(chainCount: chainCount, capacity: capacity, hasSphingosine: false, hasEther: false, atLeastSpeciesLevel: false)
        {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern = $"(?<MolecularSpeciesLevel>({TgEstChainsPattern}_?){{{ChainCount}}})";
            var positionLevelPattern = $"(?<PositionLevel>({TgEstChainsPattern}/?){{{ChainCount}}})";
            var totalPattern = new[] { positionLevelPattern, molecularSpeciesLevelPattern, submolecularLevelPattern };
            Pattern = string.Join("|", totalPattern);
            Expression = new Regex(Pattern, RegexOptions.Compiled);
        }
    }
}

