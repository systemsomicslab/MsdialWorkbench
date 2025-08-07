using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class CerEosLipidParser : ILipidParser
    {
        public string Target { get; } = "Cer";

        private static readonly CerEosChainParser chainsParser = new CerEosChainParser();
        public static readonly string CerEosPattern = $"^Cer\\s*(?<sn>{chainsParser.Pattern})$";
        private static readonly Regex CerEospattern = new Regex(CerEosPattern, RegexOptions.Compiled);
        private static readonly SphingoChainParser SphingoParser = new SphingoChainParser();

        public ILipid Parse(string lipidStr)
        {
            var match = CerEospattern.Match(lipidStr);
            if (match.Success)
            {
                var group = match.Groups;
                var chains = chainsParser.Parse(group["sn"].Value);
                if (chains is TotalChain)
                {
                    var LipidClass = chains.DoubleBondCount == 0 ? LbmClass.Cer_EODS : LbmClass.Cer_EOS;
                    return new Lipid(LipidClass, chains.Mass - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass, chains);
                }
                else if (chains is MolecularSpeciesLevelChains)
                {
                    var Cer = SphingoParser.Parse(group["Cer"].Value);
                    var LipidClass = Cer.DoubleBondCount == 0 ? LbmClass.Cer_EODS : LbmClass.Cer_EOS;
                    return new Lipid(LipidClass, chains.Mass + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2, chains);
                }
                else if (chains is PositionLevelChains)
                {
                    var balance = -MassDiffDictionary.HydrogenMass;
                    if (group["OAcyl"].Value == null || group["OAcyl"].Value == "") { balance = 0.0; }
                    var Cer = SphingoParser.Parse(group["Cer"].Value);
                    var LipidClass = Cer.DoubleBondCount == 0 ? LbmClass.Cer_EODS : LbmClass.Cer_EOS;
                    return new Lipid(LipidClass, chains.Mass + balance, chains);
                }
            }
            return null;
        }
    }
    public class CerEosChainParser : TotalChainParser
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


        public CerEosChainParser()
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

