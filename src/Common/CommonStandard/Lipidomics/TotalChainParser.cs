using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics
{
    public class TotalChainParser {
        private static readonly string CarbonPattern = @"(?<carbon>\d+)";
        private static readonly string DoubleBondPattern = @"(?<db>\d+)";
        private static readonly string OxidizedPattern = @";(?<ox>O(?<oxnum>\d+)?)";
        //private static readonly string OxidizedPattern = @";(?<ox>(?<oxnum>\d+)?O)"; //


        private static readonly string ChainsPattern = $"(?<Chain>{AlkylChainParser.Pattern}|{AcylChainParser.Pattern})";
        private static readonly string AcylChainsPattern = $"(?<Chain>{AcylChainParser.Pattern})";

        private static readonly AlkylChainParser AlkylParser = new AlkylChainParser();
        private static readonly AcylChainParser AcylParser = new AcylChainParser();
        private static readonly SphingoChainParser SphingoParser = new SphingoChainParser();

        private readonly bool HasSphingosine;

        public static TotalChainParser BuildParser(int chainCount) {
            return new TotalChainParser(chainCount, chainCount, false, false, false);
        }

        public static TotalChainParser BuildSpeciesLevelParser(int chainCount, int capacity) {
            return new TotalChainParser(chainCount, capacity, false, false, true);
        }

        public static TotalChainParser BuildEtherParser(int chainCount) {
            return new TotalChainParser(chainCount, chainCount, false, true, false);
        }
        public static TotalChainParser BuildLysoEtherParser(int chainCount, int capacity)
        {
            return new TotalChainParser(chainCount, capacity, false, true, true);
        }

        public static TotalChainParser BuildCeramideParser(int chainCount) {
            return new TotalChainParser(chainCount, chainCount, true, false, false);
        }

        private TotalChainParser(int chainCount, int capacity, bool hasSphingosine, bool hasEther, bool atLeastSpeciesLevel) {
            ChainCount = chainCount;
            Capacity = capacity;
            var submolecularLevelPattern = hasEther
                ? $"(?<TotalChain>(?<plasm>[de]?[OP]-)?{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)"
                : $"(?<TotalChain>{CarbonPattern}:{DoubleBondPattern}({OxidizedPattern})?)";
            var molecularSpeciesLevelPattern = hasSphingosine
                ? $"(?<MolecularSpeciesLevel>(?<Chain>{SphingoChainParser.Pattern})_({ChainsPattern}_?){{{ChainCount-1}}})"
                : hasEther
                ? $"(?<MolecularSpeciesLevel>({ChainsPattern}_?){{{ChainCount}}})"
                : $"(?<MolecularSpeciesLevel>({AcylChainsPattern}_?){{{ChainCount}}})";
            var positionLevelPattern = hasSphingosine
                ? $"(?<PositionLevel>(?<Chain>{SphingoChainParser.Pattern})/({ChainsPattern}/?){{{Capacity-1}}})"
                : hasEther
                ? $"(?<PositionLevel>({ChainsPattern}/?){{{Capacity}}})"
                : $"(?<PositionLevel>({AcylChainsPattern}/?){{{Capacity}}})";
            if (Capacity == 1) {
                var postionLevelExpression = new Regex(positionLevelPattern, RegexOptions.Compiled);
                Pattern = positionLevelPattern;
                Expression = postionLevelExpression;
            }
            else {
                var patterns = new[] { positionLevelPattern, molecularSpeciesLevelPattern, };
                var totalPattern = string.Join("|", atLeastSpeciesLevel ? patterns : patterns.Append(submolecularLevelPattern));
                var totalExpression = new Regex(totalPattern, RegexOptions.Compiled);
                Pattern = totalPattern;
                Expression = totalExpression;
            }
            HasSphingosine = hasSphingosine;
        }

        public int ChainCount { get; }
        public int Capacity { get; }
        public string Pattern { get; }

        private readonly Regex Expression;

        public ITotalChain Parse(string lipidStr) {
            var match = Expression.Match(lipidStr);
            if (match.Success) {
                var groups = match.Groups;
                if (groups["PositionLevel"].Success) {
                    var chains = ParsePositionLevelChains(groups);
                    if (chains.ChainCount == Capacity) {
                        return chains;
                    }
                }
                else if (groups["MolecularSpeciesLevel"].Success) {
                    var chains = ParseMolecularSpeciesLevelChains(groups);
                    if (chains.ChainCount == Capacity) {
                        return chains;
                    }
                    else if (chains.ChainCount < Capacity) {
                        return new MolecularSpeciesLevelChains(
                            chains.GetDeterminedChains()
                                .Concat(Enumerable.Range(0, Capacity - chains.ChainCount).Select(_ => new AcylChain(0, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition())))
                                .ToArray()
                        );
                    }
                }
                else if (ChainCount > 1 && groups["TotalChain"].Success) {
                    return ParseTotalChains(groups, ChainCount);
                }
            }
            return null;
        }

        private PositionLevelChains ParsePositionLevelChains(GroupCollection groups) {
            var matches = groups["Chain"].Captures.Cast<Capture>().ToArray();
            if (HasSphingosine) {
                if (SphingoParser.Parse(matches[0].Value) is IChain sphingo) {
                    return new PositionLevelChains(
                        matches.Skip(1)
                            .Select(c => AlkylParser.Parse(c.Value) ?? AcylParser.Parse(c.Value))
                            .Prepend(sphingo)
                            .ToArray()
                    );
                }
                return null;
            }
            return new PositionLevelChains(
                matches.Select(c => AlkylParser.Parse(c.Value) ?? AcylParser.Parse(c.Value)).ToArray()
            );

        }

        private MolecularSpeciesLevelChains ParseMolecularSpeciesLevelChains(GroupCollection groups) {
            return new MolecularSpeciesLevelChains(
                groups["Chain"].Captures.Cast<Capture>()
                    .Select(c => AlkylParser.Parse(c.Value) ?? AcylParser.Parse(c.Value))
                    .ToArray());
        }

        private TotalChain ParseTotalChains(GroupCollection groups, int chainCount) {
            var carbon = int.Parse(groups["carbon"].Value);
            var db = int.Parse(groups["db"].Value);
            var ox = !groups["ox"].Success ? 0 : !groups["oxnum"].Success ? 1 : int.Parse(groups["oxnum"].Value);
            
            switch (groups["plasm"].Value) {
                case "O-":
                    return new TotalChain(carbon, db, ox, chainCount - 1, 1, 0);
                case "dO-":
                    return new TotalChain(carbon, db, ox, chainCount - 2, 2, 0);
                case "eO-":
                    return new TotalChain(carbon, db, ox, chainCount - 4, 4, 0);
                case "P-":
                    return new TotalChain(carbon, db + 1, ox, chainCount - 1, 1, 0);
                case "dP-":
                    return new TotalChain(carbon, db + 2, ox, chainCount - 2, 2, 0);
                case "eP-":
                    return new TotalChain(carbon, db + 4, ox, chainCount - 4, 4, 0);
                case "":
                    if (HasSphingosine) {
                        return new TotalChain(carbon, db, ox, chainCount - 1, 0, 1);
                    }
                    else {
                        return new TotalChain(carbon, db, ox, chainCount, 0, 0);
                    }
            }
            return null;
        }
    }
}
