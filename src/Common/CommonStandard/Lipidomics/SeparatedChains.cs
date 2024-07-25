using CompMs.Common.DataStructure;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class SeparatedChains : ITotalChain
    {
        private readonly ChainInformation[] _chains, _decided, _undecided;

        /// <summary>
        /// chains should contains at least 1 chain.
        /// </summary>
        /// <param name="chains"></param>
        /// <param name="description"></param>
        /// <exception cref="ArgumentException"></exception>
        public SeparatedChains(IChain[] chains, LipidDescription description) {
            if (chains.Length == 0) {
                throw new ArgumentException(nameof(chains));
            }
            _chains = chains.Select(c => new ChainInformation(c)).ToArray();
            _decided = Array.Empty<ChainInformation>();
            _undecided = _chains;
            if (chains.All(c => c.DoubleBond.UnDecidedCount == 0)) {
                description |= LipidDescription.DoubleBondPosition;
            }
            Description = description;
        }

        /// <summary>
        /// chains should contains at least 1 chain.
        /// </summary>
        /// <param name="chains"></param>
        /// <param name="description"></param>
        /// <exception cref="ArgumentException"></exception>
        public SeparatedChains((IChain, int)[] chains, LipidDescription description) {
            if (chains.Length == 0) {
                throw new ArgumentException(nameof(chains));
            }
            _chains = chains.Select(c => new ChainInformation(c.Item1, c.Item2)).ToArray();
            _decided = _chains.Where(c => c.Position >= 0).ToArray();
            _undecided = _chains.Where(c => c.Position < 0).ToArray();
            if (chains.All(c => c.Item1.DoubleBond.UnDecidedCount == 0)) {
                description |= LipidDescription.DoubleBondPosition;
            }
            Description = description;
        }

        IChain ITotalChain.GetChainByPosition(int position) {
            return _chains.FirstOrDefault(c => c.Position == position)?.Chain;
        }

        public IChain[] GetDeterminedChains() {
            return _chains.Select(c => c.Chain).ToArray();
        }

        public int ChainCount => _chains.Length;
        public int AcylChainCount => _chains.Count(c => c.Chain is AcylChain);
        public int AlkylChainCount => _chains.Count(c => c.Chain is AlkylChain);
        public int SphingoChainCount => _chains.Count(c => c.Chain is SphingoChain);
        public int CarbonCount => _chains.Sum(c => c.Chain.CarbonCount);
        public int DoubleBondCount => _chains.Sum(c => c.Chain.DoubleBondCount);
        public int OxidizedCount => _chains.Sum(c => c.Chain.OxidizedCount);
        public LipidDescription Description { get; }
        public double Mass => _chains.Sum(c => c.Chain.Mass);

        IEnumerable<ITotalChain> ITotalChain.GetCandidateSets(ITotalChainVariationGenerator totalChainGenerator) {
            var gc = new GenerateChain(_chains.Length, _decided);
            var indetermined = _undecided.Select(c => c.Chain).ToArray();
            if (indetermined.Length > 0) {
                return SearchCollection.Permutations(indetermined).Select(cs => new PositionLevelChains(gc.Apply(cs)));
            }
            ITotalChain pc = new PositionLevelChains(_decided.Select(c => c.Chain).ToArray());
            return pc.GetCandidateSets(totalChainGenerator);
        }

        public override string ToString() {
            var box = new ChainInformation[_chains.Length];
            foreach (var c in _decided) {
                box[c.Position - 1] = c;
            }
            var idx = 0;
            foreach (var c in _undecided) {
                while (idx < box.Length && box[idx] != null) {
                    ++idx;
                }
                if (idx < box.Length) {
                    box[idx++] = c;
                }
            }
            return string.Concat(box.Select(c => c.Chain.ToString() + (c.Position < 0 ? "_" : "/"))).TrimEnd('_', '/');
        }

        bool ITotalChain.Includes(ITotalChain chains) {
            if (chains.ChainCount != ChainCount) {
                return false;
            }
            if (!(chains is SeparatedChains sChains)) {
                return false;
            }
            if (_decided.Length > sChains._decided.Length) {
                return false;
            }
            var used = new HashSet<ChainInformation>();
            foreach (var d in _decided) {
                if (sChains._decided.FirstOrDefault(d2 => d2.Position == d.Position) is ChainInformation ci && d.Chain.Includes(ci.Chain)) {
                    used.Add(ci);
                }
                else {
                    return false;
                }
            }
            var canUse = new HashSet<ChainInformation>(sChains._chains.Except(used));
            foreach (var d in _undecided) {
                if (canUse.All(d2 => !d.Chain.Includes(d2.Chain))) {
                    return false;
                }
            }
            return true;
        }

        public virtual bool Equals(ITotalChain other) {
            if (other.ChainCount != ChainCount) {
                return false;
            }
            if (!(other is SeparatedChains sChains)) {
                return false;
            }
            if (_decided.Length > sChains._decided.Length) {
                return false;
            }
            var used = new HashSet<ChainInformation>();
            foreach (var d in _decided) {
                if (sChains._decided.FirstOrDefault(d2 => d2.Position == d.Position) is ChainInformation ci && d.Chain.Equals(ci.Chain)) {
                    used.Add(ci);
                }
                else {
                    return false;
                }
            }
            var canUse = new HashSet<ChainInformation>(sChains._chains.Except(used));
            foreach (var d in _undecided) {
                if (canUse.All(d2 => !d.Chain.Equals(d2.Chain))) {
                    return false;
                }
            }
            return true;
        }

        public virtual TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, SeparatedChains> decomposer_) {
                return decomposer_.Decompose(visitor, this);
            }
            return default;
        }

        class ChainInformation {
            public ChainInformation(IChain chain, int position) {
                Chain = chain;
                Position = position;
            }

            public ChainInformation(IChain chain) : this(chain, -1) {
                
            }

            public IChain Chain { get; }

            /// <summary>
            /// position 1-indexed.
            /// If position is indetermined, -1 assigned.
            /// </summary>
            public int Position { get; }
        }

        class GenerateChain {
            private readonly IChain[] _box;
            private readonly List<int> _remains;

            public GenerateChain(int length, IEnumerable<ChainInformation> determined) {
                _box = new IChain[length + 1];
                _remains = Enumerable.Range(1, length).ToList();
                foreach (var d in determined) {
                    _box[d.Position] = d.Chain;
                    _remains.Remove(d.Position);
                }
            }

            public IChain[] Apply(IEnumerable<IChain> chains) {
                var result = _box.ToArray();
                foreach (var (i, c) in _remains.Zip(chains, (i, c) => (i, c))) {
                    result[i] = c;
                }
                return result;
            }
        }
    }
}
