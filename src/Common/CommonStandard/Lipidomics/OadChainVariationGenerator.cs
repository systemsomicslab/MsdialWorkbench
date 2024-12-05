using CompMs.Common.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public class OadChainVariationGenerator : ITotalChainVariationGenerator {
        public OadChainVariationGenerator(IChainGenerator chainGenerator, int minLength) {
            MinLength = minLength;
            this.chainGenerator = chainGenerator;
        }

        public OadChainVariationGenerator(int minLength = 6, int begin = 3, int end = 3, int skip = 3)
            : this(new ChainGenerator(begin, end, skip), minLength) {

        }

        public int MinLength { get; }

        private readonly IChainGenerator chainGenerator;

        public IEnumerable<ITotalChain> Separate(TotalChain chain) {
            return InternalSeparate(chain).SelectMany(ListingCandidates);
        }

        class ChainCandidate {
            public ChainCandidate(int chainCount, int carbonCount, int doubleBondCount, int oxidizedCount, int minimumOxidizedCount) {
                ChainCount = chainCount;
                CarbonCount = carbonCount;
                DoubleBondCount = doubleBondCount;
                OxidizedCount = oxidizedCount;
                MinimumOxidizedCount = minimumOxidizedCount;
            }

            public int ChainCount { get; }

            public int CarbonCount { get; }

            public int DoubleBondCount { get; }

            public int OxidizedCount { get; }

            public int MinimumOxidizedCount { get; }
        }

        class ChainSet {
            public ChainSet(ChainCandidate acylCandidate, ChainCandidate alkylCandidate, ChainCandidate sphingoCandidate)
            {
                AcylCandidate = acylCandidate;
                AlkylCandidate = alkylCandidate;
                SphingoCandidate = sphingoCandidate;
            }

            public ChainCandidate AcylCandidate { get; }
            public ChainCandidate AlkylCandidate { get; }
            public ChainCandidate SphingoCandidate { get; }
        }

        // TODO: refactoring
        private IEnumerable<ITotalChain> ListingCandidates(ChainSet candidates) {
            if (candidates.SphingoCandidate.ChainCount > 0)
            {
                var sphingos = RecurseGenerate(candidates.SphingoCandidate, CreateSphingoChain).ToArray();
                var acyls = RecurseGenerate(candidates.AcylCandidate, CreateAcylChain).ToArray();
                var alkyls = RecurseGenerate(candidates.AlkylCandidate, CreateAlkylChain).ToArray();
                return from sphingo in sphingos
                       from acyl in acyls
                       from alkyl in alkyls
                       select new PositionLevelChains(sphingo.Concat<IChain>(acyl).Concat(alkyl).ToArray());
            }
            else
            {
                var acyls = RecurseGenerate(candidates.AcylCandidate, CreateAcylChain).ToArray();
                var alkyls = RecurseGenerate(candidates.AlkylCandidate, CreateAlkylChain).ToArray();
                return from acyl in acyls
                       from alkyl in alkyls
                       select new MolecularSpeciesLevelChains(alkyl.Concat<IChain>(acyl).ToArray());
            }
        }

        private IEnumerable<ChainSet> InternalSeparate(TotalChain chains) {
            var (minAcylCarbon, minAlkylCarbon, minSphingoCarbon) = (MinLength * chains.AcylChainCount, MinLength * chains.AlkylChainCount, MinLength * chains.SphingoChainCount);
            var carbonRemain = chains.CarbonCount - minAcylCarbon - minAlkylCarbon - minSphingoCarbon;
            if (carbonRemain < 0)
            {
                return Enumerable.Empty<ChainSet>();
            }
            var (maxAcylCarbon, maxAlkylCarbon, maxSphingoCarbon) = ((carbonRemain + minAcylCarbon) * Math.Sign(chains.AcylChainCount), (carbonRemain + minAlkylCarbon) * Math.Sign(chains.AlkylChainCount), (carbonRemain + minSphingoCarbon) * Math.Sign(chains.SphingoChainCount));
            var (minAcylDb, minAlkylDb, minSphingoDb) = (0, 0, 0);
            var dbRemain = chains.DoubleBondCount - minAcylDb - minAlkylDb - minSphingoDb;
            var (maxAcylDb, maxAlkylDb, maxSphingoDb) = ((dbRemain + minAcylDb) * Math.Sign(chains.AcylChainCount), (dbRemain + minAlkylDb) * Math.Sign(chains.AlkylChainCount), (dbRemain + minSphingoDb) * Math.Sign(chains.SphingoChainCount));
            var (minAcylOx, minAlkylOx, minSphingoOx) = (0, 0, chains.SphingoChainCount * 2);
            var oxRemain = chains.OxidizedCount - minAcylOx - minAlkylOx - minSphingoOx;
            var (maxAcylOx, maxAlkylOx, maxSphingoOx) = ((dbRemain + minAcylOx) * Math.Sign(chains.AcylChainCount), (dbRemain + minAlkylOx) * Math.Sign(chains.AlkylChainCount), (dbRemain + minSphingoOx) * Math.Sign(chains.SphingoChainCount));

            var css = Distribute(chains.CarbonCount, minAcylCarbon, maxAcylCarbon, minAlkylCarbon, maxAlkylCarbon, minSphingoCarbon, maxSphingoCarbon).ToArray();
            var dbss = Distribute(chains.DoubleBondCount, minAcylDb, maxAcylDb, minAlkylDb, maxAlkylDb, minSphingoDb, maxSphingoDb).ToArray();

            return from cs in css
                   from dbs in dbss
                   select new ChainSet(
                       new ChainCandidate(chains.AcylChainCount, cs[0], dbs[0], chains.OxidizedCount, 0),
                       new ChainCandidate(chains.AlkylChainCount, cs[1], dbs[1], chains.OxidizedCount, 0),
                       new ChainCandidate(chains.SphingoChainCount, cs[2], dbs[2], chains.OxidizedCount, 2));
        }

        private IEnumerable<int[]> Distribute(int count, int acylMin, int acylMax, int alkylMin, int alkylMax, int sphingoMin, int sphingoMax) {
            return from i in Enumerable.Range(acylMin, acylMax - acylMin + 1)
                   where count - i <= alkylMax + sphingoMax
                   from j in Enumerable.Range(alkylMin, alkylMax - alkylMin + 1)
                   let k = count - i - j
                   where sphingoMin <= k && k <= sphingoMax
                   select new[] { i, j, k };
        }

        public IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains) {
            if (chains.GetDeterminedChains().All(chain => chain.DoubleBond.UnDecidedCount == 0)) {
                return Enumerable.Empty<ITotalChain>();
            }
            return SearchCollection.CartesianProduct(chains.GetDeterminedChains().Select(c => c.GetCandidates(chainGenerator).ToArray()).ToArray())
                .Select(set => new MolecularSpeciesLevelChains(set))
                .Distinct(ChainsComparer);
        }

        public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
            if (chains.GetDeterminedChains().All(chain => chain.DoubleBond.UnDecidedCount == 0)) {
                return Enumerable.Empty<ITotalChain>();
            }
            return SearchCollection.CartesianProduct(chains.GetDeterminedChains().Select(c => c.GetCandidates(chainGenerator).ToArray()).ToArray())
                .Select(set => new PositionLevelChains(set))
                .Distinct(ChainsComparer);
        }
        private readonly ConcurrentDictionary<(int, int, int), SphingoChain> _sphingoCache = new ConcurrentDictionary<(int, int, int), SphingoChain>();
        private SphingoChain CreateSphingoChain(int carbon, int db, int ox)
        {
            return _sphingoCache.GetOrAdd((carbon, db, ox), triple => new SphingoChain(triple.Item1, new DoubleBond(triple.Item2), new Oxidized(triple.Item3, 1, 3)));
        }

        private bool CarbonNumberValid(int curCarbon) {
            return curCarbon >= MinLength && chainGenerator.CarbonIsValid(curCarbon);
        }

        private bool DoubleBondIsValid(int carbon, int db) {
            return chainGenerator.DoubleBondIsValid(carbon, db);
        }

        private bool IsLexicographicOrder(int prevCarbon, int prevDb, int curCarbon, int curDb) {
            return (prevCarbon, prevDb).CompareTo((curCarbon, curDb)) <= 0;
        }

        private bool IsLexicographicOrder(int prevCarbon, int prevDb, int prevOx, int curCarbon, int curDb, int curOx) {
            return (prevCarbon, prevDb, prevOx).CompareTo((curCarbon, curDb, curOx)) <= 0;
        }

        private IEnumerable<T[]> RecurseGenerate<T>(ChainCandidate candidate, Func<int, int, int, T> create) {
            if (candidate.ChainCount == 0) {
                if (candidate.CarbonCount == 0 && candidate.DoubleBondCount == 0 && candidate.OxidizedCount == 0) {
                    return new[] { new T[0] };
                } else {
                    return Enumerable.Empty<T[]>();
                }
            }

            var set = new T[candidate.ChainCount];
            IEnumerable<T[]> rec(int carbon_, int db_, int ox_, int minCarbon_, int minDb_, int minOx_, int chain_) {
                if (chain_ == 1) {
                    if (DoubleBondIsValid(carbon_, db_) && IsLexicographicOrder(minCarbon_, minDb_, minOx_, carbon_, db_, ox_)) {
                        set[candidate.ChainCount - 1] = create(carbon_, db_, ox_);
                        yield return set.ToArray();
                    }
                    yield break;
                }
                for (var c = minCarbon_; c <= carbon_ / chain_; c++) {
                    if (!CarbonNumberValid(c)) {
                        continue;
                    }
                    for (var d = 0; d <= db_ && DoubleBondIsValid(c, d); d++) {
                        if (!IsLexicographicOrder(minCarbon_, minDb_, c, d)) {
                            continue;
                        }
                        for (var o = candidate.OxidizedCount; o <= ox_; o++) {
                            if (!IsLexicographicOrder(minCarbon_, minDb_, minOx_, c, d, o)) {
                                continue;
                            }
                            set[candidate.ChainCount - chain_] = create(c, d, o);
                            foreach (var res in rec(carbon_ - c, db_ - d, ox_ - o, c, d, o, chain_ - 1)) {
                                yield return res;
                            }
                        }
                    }
                }
            }
            return rec(candidate.CarbonCount, candidate.DoubleBondCount, candidate.OxidizedCount, MinLength, -1, -1, candidate.ChainCount);
        }
                
        private readonly ConcurrentDictionary<(int, int, int), AcylChain> _acylCache = new ConcurrentDictionary<(int, int, int), AcylChain>();
        private AcylChain CreateAcylChain(int carbon, int db, int ox) {
            return _acylCache.GetOrAdd((carbon, db, ox), triple => new AcylChain(triple.Item1, new DoubleBond(triple.Item2), new Oxidized(triple.Item3)));
        }

        private readonly ConcurrentDictionary<(int, int, int), AlkylChain> _alkylCache = new ConcurrentDictionary<(int, int, int), AlkylChain>();
        private AlkylChain CreateAlkylChain(int carbon, int db, int ox) {
            return _alkylCache.GetOrAdd((carbon, db, ox), triple => new AlkylChain(triple.Item1, new DoubleBond(triple.Item2), new Oxidized(triple.Item3)));
        }

        private static readonly PositionLevelChainEqualityCompaerer ChainsComparer = new PositionLevelChainEqualityCompaerer();

        class PositionLevelChainEqualityCompaerer : IEqualityComparer<ITotalChain> {
            public bool Equals(ITotalChain x, ITotalChain y) {
                return x.ToString() == y.ToString();
            }

            public int GetHashCode(ITotalChain obj) {
                return obj.ToString().GetHashCode();
            }
        }
    }
}
