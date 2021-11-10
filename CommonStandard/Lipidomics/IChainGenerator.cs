using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IChainGenerator
    {
        IEnumerable<IChain[]> Separate(TotalAcylChain chain, int numChain);
        IEnumerable<SpecificAcylChain> Generate(AcylChain chain);

        IEnumerable<SpecificAlkylChain> Generate(AlkylChain chain);

        IEnumerable<SpecificAlkylChain> Generate(PlasmalogenAlkylChain chain);
    }

    public class AcylChainGenerator : IChainGenerator
    {
        public AcylChainGenerator(int minLength = 6, int begin = 3, int skip = 3) {
            MinLength = minLength;
            Begin = begin;
            Skip = skip;
        }

        public int MinLength { get; }
        public int Begin { get; } // if begin is 3, first double bond is 3-4 at the earliest counting from ketone carbon.
        public int Skip { get; } // if skip is 3 and 6-7 is double bond, next one is 9-10 at the earliest.

        public IEnumerable<IChain[]> Separate(TotalAcylChain chain, int numChain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount, chain.OxidizedCount, numChain - chain.AlkylChainCount, chain.AlkylChainCount);
        }

        private bool CarbonNumberValid(int curCarbon) {
            return curCarbon >= MinLength;
        }

        private bool DoubleChainIsValid(int carbon, int db) {
            return db == 0 || carbon >= Begin + 1 + Skip * (db - 1);
        }

        private bool IsLexicographicOrder(int prevCarbon, int prevDb, int curCarbon, int curDb) {
            return (prevCarbon, prevDb).CompareTo((curCarbon, curDb)) <= 0;
        }

        private bool IsLexicographicOrder(int prevCarbon, int prevDb, int prevOx, int curCarbon, int curDb, int curOx) {
            return (prevCarbon, prevDb, prevOx).CompareTo((curCarbon, curDb, curOx)) <= 0;
        }

        private IEnumerable<IChain[]> RecurseGenerate(int carbon, int db, int ox, int acyl, int alkyl) {
            return Enumerable.Range(MinLength * acyl, carbon - MinLength * (alkyl + acyl) + 1)
                .SelectMany(c => Enumerable.Range(0, db + 1)
                    .SelectMany(d => Enumerable.Range(0, ox + 1)
                        .SelectMany(o => RecurseGenerate(c, d, o, acyl, CreateAcylChain)
                            .SelectMany(x => RecurseGenerate(carbon - c, db - d, ox - o,  alkyl, CreateAlkylChain),
                                (x, y) => x.Concat<IChain>(y).ToArray()))));
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
            return acylCache[(carbon, db, ox)] = new AcylChain(carbon, db, ox);
        }

        private Dictionary<(int, int, int), AlkylChain> alkylCache = new Dictionary<(int, int, int), AlkylChain>();
        private AlkylChain CreateAlkylChain(int carbon, int db, int ox) {
            if (alkylCache.TryGetValue((carbon, db, ox), out var chain)) {
                return chain;
            }
            return alkylCache[(carbon, db, ox)] = new AlkylChain(carbon, db, ox);
        }

        public IEnumerable<SpecificAcylChain> Generate(AcylChain chain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount, Begin, new List<int>())
                .Select(set => new SpecificAcylChain(chain.CarbonCount, set, chain.OxidizedCount));   
        }

        public IEnumerable<SpecificAlkylChain> Generate(AlkylChain chain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount, Begin, new List<int>())
                .Select(set => new SpecificAlkylChain(chain.CarbonCount, set, chain.OxidizedCount));   
        }

        public IEnumerable<SpecificAlkylChain> Generate(PlasmalogenAlkylChain chain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount - 1, 1 + Skip, new List<int> { 1 })
                .Select(set => new SpecificAlkylChain(chain.CarbonCount, set, chain.OxidizedCount));
        }

        private IEnumerable<List<int>> RecurseGenerate(int carbon, int db, int delta, List<int> set) {
            if (delta < carbon || db == 0) {
                if (db == 0) {
                    yield return set.ToList();
                }
                else {
                    set.Add(delta);
                    foreach (var res in RecurseGenerate(carbon, db - 1, delta + Skip, set)) {
                        yield return res;
                    }
                    set.RemoveAt(set.Count - 1);

                    foreach (var res in RecurseGenerate(carbon, db, delta + 1, set)) {
                        yield return res;
                    }
                }
            }
        }
    }

}
