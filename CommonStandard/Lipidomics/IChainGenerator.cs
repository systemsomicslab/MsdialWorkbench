using CompMs.Common.Extension;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IChainGenerator
    {
        IEnumerable<ITotalChain> Separate(TotalChains chain);

        IEnumerable<ITotalChain> Permutate(MolecularSpeciesLevelChains chains);

        IEnumerable<ITotalChain> Product(PositionLevelChains chains);

        IEnumerable<IChain> Generate(AcylChain chain);

        IEnumerable<IChain> Generate(AlkylChain chain);
    }

    public class AcylChainGenerator : IChainGenerator
    {
        public AcylChainGenerator(int minLength = 6, int begin = 3, int end = 3, int skip = 3) {
            MinLength = minLength;
            Begin = begin;
            End = end;
            Skip = skip;
        }

        public int MinLength { get; }
        public int Begin { get; } // if begin is 3, first double bond is 3-4 at the earliest counting from ketone carbon.
        public int End { get; } // if end is 3 and number of carbon is 18, last double bond is 15-16 at latest.
        public int Skip { get; } // if skip is 3 and 6-7 is double bond, next one is 9-10 at the earliest.

        public IEnumerable<ITotalChain> Separate(TotalChains chain) {
            var acyl = chain.ChainCount - chain.AlkylChainCount;
            var carbon = chain.CarbonCount;
            var alkyl = chain.AlkylChainCount;
            var db = chain.DoubleBondCount;
            var ox = chain.OxidizedCount;
            return Enumerable.Range(MinLength * acyl, carbon - MinLength * (alkyl + acyl) + 1)
                .SelectMany(c => Enumerable.Range(0, db + 1)
                    .SelectMany(d => Enumerable.Range(0, ox + 1)
                        .SelectMany(o => RecurseGenerate(c, d, o, acyl, CreateAcylChain)
                            .SelectMany(x => RecurseGenerate(carbon - c, db - d, ox - o, alkyl, CreateAlkylChain),
                                (x, y) => new MolecularSpeciesLevelChains(x.Concat<IChain>(y).ToArray())))));
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

        private IEnumerable<T[]> RecurseGenerate<T>(int carbon, int db, int ox, int chain, Func<int, int, int, T> create) {
            if (chain == 0) {
                if (carbon == 0 && db == 0 && ox == 0) {
                    return new[] { new T[0] };
                }
                else {
                    return Enumerable.Empty<T[]>();
                }
            }

            var set = new T[chain];
            IEnumerable<T[]> rec(int carbon_, int db_, int ox_, int minCarbon_, int minDb_, int minOx_, int chain_) {
                if (chain_ == 1) {
                    if (DoubleChainIsValid(carbon_, db_) && IsLexicographicOrder(minCarbon_, minDb_, minOx_, carbon_, db_, ox_)) {
                        set[chain - 1] = create(carbon_, db_, ox_);
                        yield return set.ToArray();
                    }
                }
                else {
                    for (var c = minCarbon_; c <= carbon_ / chain_; c++) {
                        if (!CarbonNumberValid(c)) {
                            continue;
                        }
                        for (var d = 0; d <= db_ && DoubleChainIsValid(c, d); d++) {
                            if (!IsLexicographicOrder(minCarbon_, minDb_, c, d)) {
                                continue;
                            }
                            for (var o = 0; o <= ox_; o++) {
                                if (!IsLexicographicOrder(minCarbon_, minDb_, minOx_, c, d, o)) {
                                    continue;
                                }
                                set[chain - chain_] = create(c, d, o);
                                foreach (var res in rec(carbon_ - c, db_ - d, ox_ - o, c, d, o, chain_ - 1)) {
                                    yield return res;
                                }
                            }
                        }
                    }
                }
            }

            return rec(carbon, db, ox, MinLength, -1, -1, chain);
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
            return Products(chain.CarbonCount, chain.DoubleBond, chain.Oxidized, (c, b, o) => new AcylChain(c, b, o));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            return Products(chain.CarbonCount, chain.DoubleBond, chain.Oxidized, (c, b, o) => new AlkylChain(c, b, o));
        }

        private IEnumerable<IChain> Products(int carbon, IDoubleBond doubleBond, IOxidized oxidized, Func<int, IDoubleBond, IOxidized, IChain> create) {
            return EnumerateBonds(carbon, doubleBond).SelectMany(_ => EnumerateOxidized(carbon, oxidized), (b, o) => create(carbon, b, o));
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
                }
                else {
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
            }

            return rec(Begin, new List<IDoubleBondInfo>(doubleBond.UnDecidedCount));
        }

        private IEnumerable<IOxidized> EnumerateOxidized(int carbon, IOxidized oxidized) {
            if (oxidized.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(oxidized);
            }

            IEnumerable<IOxidized> rec(int i, List<int> infos) {
                if (infos.Count == oxidized.UnDecidedCount) {
                    yield return Oxidized.CreateFromPosition(oxidized.Oxidises.Concat(infos).OrderBy(p => p).ToArray());
                }
                else {
                    for (var j = i; j < carbon; j++){
                        if (oxidized.Oxidises.Contains(j)) {
                            continue;
                        }
                        infos.Add(j);
                        foreach (var res in rec(j + Skip, infos)) {
                            yield return res;
                        }
                        infos.RemoveAt(infos.Count - 1);
                    }
                }
            }

            return rec(Begin, new List<int>(oxidized.UnDecidedCount));
        }
    }
}
