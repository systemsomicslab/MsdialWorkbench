using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IAcylChainGenerator
    {
        IEnumerable<AcylChain[]> Separate(TotalAcylChain chain, int numChain);
        IEnumerable<SpecificAcylChain> Generate(AcylChain chain);
    }

    public class AcylChainGenerator : IAcylChainGenerator
    {
        public AcylChainGenerator(int minLength = 6, int begin = 3, int skip = 3) {
            MinLength = minLength;
            Begin = begin;
            Skip = skip;
        }

        public int MinLength { get; }
        public int Begin { get; } // if begin is 3, first double bond is 3-4 at the earliest counting from ketone carbon.
        public int Skip { get; } // if skip is 3 and 6-7 is double bond, next one is 9-10 at the earliest.

        public IEnumerable<AcylChain[]> Separate(TotalAcylChain chain, int numChain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount, chain.OxidizedCount, 1, -1, -1, numChain, new AcylChain[numChain]);
        }

        private IEnumerable<AcylChain[]> RecurseGenerate(int carbon, int db, int ox, int prevCarbon, int prevDb, int prevOx, int num, AcylChain[] set) {
            if (num == 1) {
                if (CarbonNumberValid(carbon)
                    && IsLexicographicOrder(prevCarbon, prevDb, prevOx, carbon, db, ox)
                    && DoubleChainIsValid(carbon, db)) {
                    set[set.Length - num] = CreateAcylChain(carbon, db, ox);
                    yield return set.ToArray();
                }
            }
            else {
                for (var c = prevCarbon; c < carbon; c++) {
                    if (!CarbonNumberValid(c)) {
                        continue;
                    }
                    for (var d = 0; d <= db && DoubleChainIsValid(c, d); d++) {
                        if (!IsLexicographicOrder(prevCarbon, prevDb, carbon, d)) {
                            continue;
                        }
                        for (var o = 0; o <= ox; o++) {
                            if (!IsLexicographicOrder(prevCarbon, prevDb, prevOx, c, d, o)) {
                                continue;
                            }
                            set[set.Length - num] = CreateAcylChain(c, d, o);
                            foreach (var res in RecurseGenerate(carbon - c, db - d, ox - o, c, d, o, num - 1, set)) {
                                yield return res;
                            }
                        }
                    }
                }
            }
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

        private Dictionary<(int, int, int), AcylChain> acylCache = new Dictionary<(int, int, int), AcylChain>();
        private AcylChain CreateAcylChain(int carbon, int db, int ox) {
            if (acylCache.TryGetValue((carbon, db, ox), out var chain)) {
                return chain;
            }
            return acylCache[(carbon, db, ox)] = new AcylChain(carbon, db, ox);
        }

        public IEnumerable<SpecificAcylChain> Generate(AcylChain chain) {
            return RecurseGenerate(chain.CarbonCount, chain.DoubleBondCount, Begin, new List<int>())
                .Select(set => new SpecificAcylChain(chain.CarbonCount, set, chain.OxidizedCount));   
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
