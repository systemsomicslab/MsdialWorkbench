using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class ChainGenerator : IChainGenerator
    {
        public ChainGenerator(int begin = 3, int end = 3, int skip = 3) {
            Begin = begin;
            End = end;
            Skip = skip;
        }

        public int Begin { get; } // if begin is 3, first double bond is 3-4 at the earliest counting from ketone carbon.
        public int End { get; } // if end is 3 and number of carbon is 18, last double bond is 15-16 at latest.
        public int Skip { get; } // if skip is 3 and 6-7 is double bond, next one is 9-10 at the earliest.

        public IEnumerable<IChain> Generate(AcylChain chain) {
            var bs = EnumerateBonds(chain.CarbonCount, chain.DoubleBond).ToArray();
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, 2).ToArray();
            return bs.SelectMany(_ => os, (b, o) => new AcylChain(chain.CarbonCount, b, o));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            var bs = EnumerateBonds(chain.CarbonCount, chain.DoubleBond).ToArray();
            var os = EnumerateOxidized(chain.CarbonCount, chain.Oxidized, 2).ToArray();
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
                for (var j = i; j < carbon + 1; j++){
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

        public bool CarbonIsValid(int carbon) {
            return true;
        }

        public bool DoubleBondIsValid(int carbon, int db) {
            return db == 0 || carbon >= Begin + Skip * (db - 1) + End;
        }
    }
}
