using CompMs.Common.Extension;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class ChainGenerator : IChainGenerator
    {
        public ChainGenerator(int minLength = 6, int begin = 3, int end = 3, int skip = 3) {
            MinLength = minLength;
            Begin = begin;
            End = end;
            Skip = skip;
        }

        public int MinLength { get; }
        public int Begin { get; } // if begin is 3, first double bond is 3-4 at the earliest counting from ketone carbon.
        public int End { get; } // if end is 3 and number of carbon is 18, last double bond is 15-16 at latest.
        public int Skip { get; } // if skip is 3 and 6-7 is double bond, next one is 9-10 at the earliest.

        public IEnumerable<ITotalChain> Separate(TotalChain chain) {
            return InternalSeparate(chain).SelectMany(ListingCandidates);
        }

        class ChainCandidate
        {
            public ChainCandidate(int chainCount, int carbonCount, int doubleBondCount, int oxidizedCount, int minimumOxidizedCount) {
                ChainCount = chainCount;
                CarbonCount = carbonCount;
                DoubleBondCount = doubleBondCount;
                OxidizedCount = oxidizedCount;
            }

            public int ChainCount { get; }

            public int CarbonCount { get; }

            public int OxidizedCount { get; }

            public int MinimumOxidizedCount { get; }

            public int DoubleBondCount { get; }
        }

        class ChainSet
        {
            public ChainSet(ChainCandidate acylCandidate, ChainCandidate alkylCandidate, ChainCandidate sphingoCandidate) {
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
            if (candidates.SphingoCandidate.ChainCount > 0) {
                var sphingos = RecurseGenerate(candidates.SphingoCandidate, CreateSphingoChain).ToArray();
                var acyls = RecurseGenerate(candidates.AcylCandidate, CreateAcylChain).ToArray();
                var alkyls = RecurseGenerate(candidates.AlkylCandidate, CreateAlkylChain).ToArray();
                return from sphingo in sphingos
                       from acyl in acyls
                       from alkyl in alkyls
                       select new PositionLevelChains(sphingo.Concat<IChain>(acyl).Concat(alkyl).ToArray());
            }
            else {
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
            if (carbonRemain < 0) {
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
            var oxss = Distribute(chains.OxidizedCount, minAcylOx, maxAcylOx, minAlkylOx, maxAlkylOx, minSphingoOx, maxSphingoOx).ToArray();

            return from cs in css
                   from dbs in dbss
                   from oxs in oxss
                   select new ChainSet(
                       new ChainCandidate(chains.AcylChainCount, cs[0], dbs[0], oxs[0], 0),
                       new ChainCandidate(chains.AlkylChainCount, cs[1], dbs[1], oxs[1], 0),
                       new ChainCandidate(chains.SphingoChainCount, cs[2], dbs[2], oxs[2], 2));
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
            return SearchCollection.Permutations(chains.Chains).Select<IChain[], ITotalChain>(set => new PositionLevelChains(set));
        }

        public IEnumerable<ITotalChain> Product(PositionLevelChains chains) {
            if (chains.Chains.All(chain => chain.DoubleBond.UnDecidedCount == 0 && chain.Oxidized.UnDecidedCount == 0)) {
                return Enumerable.Empty<ITotalChain>();
            }
            return SearchCollection.CartesianProduct(chains.Chains.Select(c => c.GetCandidates(this).ToArray()).ToArray()).Select(set => new PositionLevelChains(set));
        }

        private bool CarbonNumberValid(int curCarbon) {
            return curCarbon >= MinLength;
        }

        private bool DoubleChainIsValid(int carbon, int db) {
            return db == 0 || carbon >= Begin + Skip * (db - 1) + End;
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
                }
                else {
                    return Enumerable.Empty<T[]>();
                }
            }

            var set = new T[candidate.ChainCount];
            IEnumerable<T[]> rec(int carbon_, int db_, int ox_, int minCarbon_, int minDb_, int minOx_, int chain_) {
                if (chain_ == 1) {
                    if (DoubleChainIsValid(carbon_, db_) && IsLexicographicOrder(minCarbon_, minDb_, minOx_, carbon_, db_, ox_)) {
                        set[candidate.ChainCount - 1] = create(carbon_, db_, ox_);
                        yield return set.ToArray();
                    }
                    yield break;
                }
                for (var c = minCarbon_; c <= carbon_ / chain_; c++) {
                    if (!CarbonNumberValid(c)) {
                        continue;
                    }
                    for (var d = 0; d <= db_ && DoubleChainIsValid(c, d); d++) {
                        if (!IsLexicographicOrder(minCarbon_, minDb_, c, d)) {
                            continue;
                        }
                        for (var o = candidate.MinimumOxidizedCount; o <= ox_; o++) {
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

        private Dictionary<(int, int, int), SphingoChain> sphingoCache = new Dictionary<(int, int, int), SphingoChain>();
        private SphingoChain CreateSphingoChain(int carbon, int db, int ox) {
            if (sphingoCache.TryGetValue((carbon, db, ox), out var chain)) {
                return chain;
            }
            return sphingoCache[(carbon, db, ox)] = new SphingoChain(carbon, new DoubleBond(db), new Oxidized(ox, 1, 3));
        }

        private Dictionary<(int, int, int), AcylChain> acylCache = new Dictionary<(int, int, int), AcylChain>();
        private AcylChain CreateAcylChain(int carbon, int db, int ox) {
            if (acylCache.TryGetValue((carbon, db, ox), out var chain)) {
                return chain;
            }
            return acylCache[(carbon, db, ox)] = new AcylChain(carbon, new DoubleBond(db), new Oxidized(ox));
        }

        private Dictionary<(int, int, int), AlkylChain> alkylCache = new Dictionary<(int, int, int), AlkylChain>();
        private AlkylChain CreateAlkylChain(int carbon, int db, int ox) {
            if (alkylCache.TryGetValue((carbon, db, ox), out var chain)) {
                return chain;
            }
            return alkylCache[(carbon, db, ox)] = new AlkylChain(carbon, new DoubleBond(db), new Oxidized(ox));
        }

        public IEnumerable<IChain> Generate(AcylChain chain) {
            var bs = EnumerateBonds(chain.CarbonCount, chain.DoubleBond).ToArray();
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, Begin).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new AcylChain(chain.CarbonCount, b, o));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            var bs = EnumerateBonds(chain.CarbonCount, chain.DoubleBond).ToArray();
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, Begin).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new AlkylChain(chain.CarbonCount, b, o));
        }

        public IEnumerable<IChain> Generate(SphingoChain chain) {
            var bs = EnumerateBonds(chain.CarbonCount, chain.DoubleBond).ToArray();
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, 4).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new SphingoChain(chain.CarbonCount, b, o));
        }

        private IEnumerable<IDoubleBond> EnumerateBonds(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            var used = new bool[carbon];
            for(int i = 1; i < Begin; i++) {
                used[i - 1] = true;
            }
            foreach (var bond in doubleBond.Bonds) {
                for (int i = Math.Max(1, bond.Position - Skip + 1); i < bond.Position + Skip; i++) {
                    used[i - 1] = true;
                }
            }

            IEnumerable<IDoubleBond> rec(int i, List<IDoubleBondInfo> infos) {
                if (infos.Count == doubleBond.UnDecidedCount) {
                    yield return new DoubleBond(doubleBond.Bonds.Concat(infos).OrderBy(b => b.Position).ToArray());
                    yield break;
                }
                for (var j = i; j <= carbon - End; j++){
                    if (used[j - 1]) {
                        continue;
                    }
                    infos.Add(DoubleBondInfo.Create(j));
                    foreach (var res in rec(j + Skip, infos)) {
                        yield return res;
                    }
                    infos.RemoveAt(infos.Count - 1);
                }
            }

            return rec(Begin, new List<IDoubleBondInfo>(doubleBond.UnDecidedCount));
        }

        private IEnumerable<IOxidized> EnumerateOxidized(int carbon, IOxidized oxidized, int begin) {
            if (oxidized.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(oxidized);
            }

            IEnumerable<IOxidized> rec(int i, List<int> infos) {
                if (infos.Count == oxidized.UnDecidedCount) {
                    yield return Oxidized.CreateFromPosition(oxidized.Oxidises.Concat(infos).OrderBy(p => p).ToArray());
                    yield break;
                }
                for (var j = i; j < carbon; j++){
                    if (oxidized.Oxidises.Contains(j)) {
                        continue;
                    }
                    infos.Add(j);
                    foreach (var res in rec(j + 1, infos)) {
                        yield return res;
                    }
                    infos.RemoveAt(infos.Count - 1);
                }
            }

            return rec(begin, new List<int>(oxidized.UnDecidedCount));
        }
    }
}
