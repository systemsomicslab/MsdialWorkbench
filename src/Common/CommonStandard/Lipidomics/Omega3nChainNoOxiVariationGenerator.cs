using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.Common.Lipidomics {
    public sealed class Omega3nChainNoOxiVariationGenerator : IChainGenerator {

        private readonly static ExtraChains _exChains;
        static Omega3nChainNoOxiVariationGenerator() {
            _exChains = new ExtraChains();
        }

        public bool CarbonIsValid(int carbon) {
            return true;
        }

        public bool DoubleBondIsValid(int carbon, int doubleBond) {
            return carbon >= doubleBond * 3 + 3 && doubleBond >= 0;
        }

        public IEnumerable<IChain> Generate(AcylChain chain) {
            var bs = EnumerateDoubleBond(chain.CarbonCount, chain.DoubleBond);
            return bs.Select(b => new AcylChain(chain.CarbonCount, b, chain.Oxidized));
        }

        public IEnumerable<IChain> Generate(AlkylChain chain) {
            IEnumerable<IDoubleBond> bs;
            if (chain.IsPlasmalogen) {
                bs = EnumerateDoubleBondInPlasmalogen(chain.CarbonCount, chain.DoubleBond);
            } else {
                bs = EnumerateDoubleBondInEther(chain.CarbonCount, chain.DoubleBond);
            }
            return bs.Select(b => new AlkylChain(chain.CarbonCount, b, chain.Oxidized));
        }

        public IEnumerable<IChain> Generate(SphingoChain chain) {
            var bs = EnumerateDoubleBondInSphingosine(chain.CarbonCount, chain.DoubleBond);
            return bs.Select(b => new SphingoChain(chain.CarbonCount, b, chain.Oxidized));
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBond(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            var result = InnerEnumerateDoubleBond(carbon, doubleBond);
            result = _exChains.Append(carbon, doubleBond).Concat(result);
            return result;
        }
        private static IEnumerable<IDoubleBond> InnerEnumerateDoubleBond(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            return GenerateDoubleBonds(set, SetToBitArray(carbon, doubleBond), carbon - doubleBond.UnDecidedCount * 3, carbon);
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBondInEther(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count - 1)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            return InnerEnumerateDoubleBondInEther(carbon, doubleBond);
        }

        private static IEnumerable<IDoubleBond> InnerEnumerateDoubleBondInEther(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            var sup = SetToBitArray(carbon, doubleBond);
            IEnumerable<IDoubleBond> result = GenerateDoubleBonds(set, sup, carbon - doubleBond.UnDecidedCount * 3, carbon).ToArray();
            if (doubleBond.UnDecidedCount >= 1) {
                sup.Add(1);
                set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
                set.Add(1);
                for (var i = 0; i < doubleBond.UnDecidedCount - 1; i++) {
                    set.Add(carbon - i * 3);
                }
                result = result.Concat(GenerateDoubleBonds(set, sup, carbon - (doubleBond.UnDecidedCount - 1) * 3, carbon));
            }
            return result;
        }

        private IEnumerable<IDoubleBond> EnumerateDoubleBondInPlasmalogen(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return IEnumerableExtension.Return(doubleBond);
            }
            if (!DoubleBondIsValid(carbon, doubleBond.Count - 1)) {
                return Enumerable.Empty<IDoubleBond>();
            }
            return InnerEnumerateDoubleBondInPlasmalogen(carbon, doubleBond);
        }

        private static IEnumerable<IDoubleBond> InnerEnumerateDoubleBondInPlasmalogen(int carbon, IDoubleBond doubleBond) {
            var set = new HashSet<int>(doubleBond.Bonds.Select(bond => bond.Position));
            for (var i = 0; i < doubleBond.UnDecidedCount; i++) {
                set.Add(carbon - i * 3);
            }
            return GenerateDoubleBonds(set, SetToBitArray(carbon, doubleBond), carbon - doubleBond.UnDecidedCount * 3, carbon);
        }

        private static IEnumerable<IDoubleBond> EnumerateDoubleBondInSphingosine(int carbon, IDoubleBond doubleBond) {
            if (doubleBond.UnDecidedCount == 0) {
                return new[] { doubleBond, };
            }
            return GenerateDoubleBondsForSphingosine(doubleBond, carbon);
        }

        private static IEnumerable<IDoubleBond> GenerateDoubleBonds(HashSet<int> set, HashSet<int> sup, int nextHead, int prevTail) {
            if (nextHead == prevTail) {
                if (set.IsSupersetOf(sup)) {
                    yield return BitArrayToBond(set);
                }
                yield break;
            }
            while (nextHead > 3) {
                if (!set.Contains(nextHead)) {
                    set.Add(nextHead);
                    set.Remove(prevTail);
                    if (set.IsSupersetOf(sup)) {
                        yield return BitArrayToBond(set);
                    }
                    prevTail -= 3;
                    if (sup.Contains(prevTail)) {
                        break;
                    }
                }
                nextHead -= 3;
            }
        }

        private static IEnumerable<IDoubleBond> GenerateDoubleBondsForSphingosine(IDoubleBond doubleBond, int length) {
            var sets = new HashSet<int>(doubleBond.Bonds.Select(b => b.Position));

            var blank = 3;
            if (length > 4 && sets.Contains(4)) --blank;
            if (length > 8 && sets.Contains(8)) --blank;
            if (length > 14 && sets.Contains(14)) --blank;

            IEnumerable<IDoubleBond> bs = new[] { doubleBond };
            var next = bs;
            var result = Enumerable.Empty<IDoubleBond>();
            if (length > 4 && !sets.Contains(4)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(4))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(4);
            }
            if (length > 8 && !sets.Contains(8)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(8))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(8);
            }
            if (length > 14 && !sets.Contains(14)) {
                var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(14))).ToArray();
                result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                sets.Add(14);
            }

            if (blank <= doubleBond.UnDecidedCount) {
                next = next.Where(b => b.UnDecidedCount > 0 && new[] { 4, 8, 14, }.All(i => b.Bonds.Select(bd => bd.Position).Contains(i)));
                for (int i = 6; i < length; i += 2) {
                    if (!sets.Contains(i)) {
                        sets.Add(i);
                        var tmp = next.Select(b => b.Decide(DoubleBondInfo.Create(i))).ToArray();
                        result = result.Concat(tmp.Where(b => b.UnDecidedCount == 0));
                        next = next.Concat(tmp.Where(b => b.UnDecidedCount > 0));
                    }
                }
            }
            return result;
        }

        private static HashSet<int> SetToBitArray(int length, IDoubleBond doubleBond) {
            if (doubleBond.DecidedCount == 0) {
                return new HashSet<int>();
            }
            var result = new HashSet<int>();
            foreach (var bond in doubleBond.Bonds) {
                result.Add(bond.Position);
            }
            return result;
        }

        private static IDoubleBond BitArrayToBond(HashSet<int> arr) {
            var bonds = new List<IDoubleBondInfo>();
            foreach (var v in arr.OrderBy(v => v)) {
                bonds.Add(DoubleBondInfo.Create(v));
            }
            return new DoubleBond(bonds.Count, bonds);
        }

        class ExtraChains {
            private readonly Dictionary<(int, int), DoubleBond[]> _map;

            public ExtraChains() {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream("CompMs.Common.Lipidomics.LipidmapsAcylChains.xml");
                var xml = XElement.Load(stream);
                var groups = xml.Elements("Chain").Select(elem => (carbon: int.Parse(elem.Attribute("carbon").Value), bond: Parse(elem))).GroupBy(pair => (pair.carbon, pair.bond.Count), pair => pair.bond);
                _map = groups.ToDictionary(group => group.Key, group => group.Where(db => db.Bonds.Any(b => (group.Key.carbon - b.Position) % 3 != 0)).ToArray());
            }

            private static DoubleBond Parse(XElement element) {
                var pos = element.Elements("DoubleBond").Select(elem => int.Parse(elem.Attribute("position").Value)).ToArray();
                return DoubleBond.CreateFromPosition(pos);

            }

            public IEnumerable<IDoubleBond> Append(int carbon, IDoubleBond baseBond) {
                if (_map.TryGetValue((carbon, baseBond.Count), out var bonds)) {
                    if (baseBond.DecidedCount >= 1) {
                        return bonds.Where(baseBond.Includes);
                    }
                    return bonds;
                }
                return Array.Empty<IDoubleBond>();
            }
        }
    }
}
