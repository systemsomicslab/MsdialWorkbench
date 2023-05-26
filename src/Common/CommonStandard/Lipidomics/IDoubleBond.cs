using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IDoubleBond : IVisitableElement, IEquatable<IDoubleBond>
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }

        bool Includes(IDoubleBond bond);

        IDoubleBond Add(params IDoubleBondInfo[] infos);
        IDoubleBond Decide(params IDoubleBondInfo[] infos);
        IDoubleBond Indeterminate(DoubleBondIndeterminateState indeterminateState);
    }

    public sealed class DoubleBond : IDoubleBond
    {
        public DoubleBond(int count, IList<IDoubleBondInfo> bonds) {
            Count = count;
            Bonds = new ReadOnlyCollection<IDoubleBondInfo>(bonds);
        }

        public DoubleBond(int count, IEnumerable<IDoubleBondInfo> bonds) : this(count, (bonds as IList<IDoubleBondInfo>) ?? bonds.ToArray()) {

        }

        public DoubleBond(int count, params IDoubleBondInfo[] bonds) : this(count, (IList<IDoubleBondInfo>)bonds) {

        }

        public DoubleBond(params IDoubleBondInfo[] bonds) : this(bonds.Length, (IList<IDoubleBondInfo>)bonds) {

        }

        public int Count { get; }

        public int DecidedCount => Bonds.Count;

        public int UnDecidedCount => Count - DecidedCount;

        public ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }

        public IDoubleBond Add(params IDoubleBondInfo[] infos) {
            return new DoubleBond(Count + infos.Length, Bonds.Concat(infos).OrderBy(x => x.Position).ToArray());
        }

        public IDoubleBond Decide(params IDoubleBondInfo[] infos) {
            if (Bonds.Count + infos.Length > Count) {
                return null;
            }
            return new DoubleBond(Count, Bonds.Concat(infos).OrderBy(x => x.Position).ToArray());
        }

        public IDoubleBond Indeterminate(DoubleBondIndeterminateState indeterminateState) {
            return new DoubleBond(Count, indeterminateState.Indeterminate(Bonds));
        }

        public static DoubleBond CreateFromPosition(params int[] positions) {
            return new DoubleBond(positions.Length, positions.Select(p => DoubleBondInfo.Create(p)).ToArray());
        }

        public override string ToString() {
            if (DecidedCount >= 1) {
                return $"{Count}({string.Join(",", Bonds)})";
            }
            else {
                return Count.ToString();
            }
        }

        public TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer) {
            if (decomposer is IDecomposer<TResult, DoubleBond> concrete) {
                return concrete.Decompose(visitor, this);
            }
            return default;
        }

        public bool Includes(IDoubleBond bond) {
            return Count == bond.Count && DecidedCount <= bond.DecidedCount && Bonds.All(bd => bond.Bonds.Any(bd.Includes));
        }

        public bool Equals(IDoubleBond other) {
            return Count == other.Count && DecidedCount == other.DecidedCount
                && Bonds.All(bond => other.Bonds.Any(bond.Equals));
        }
    }
}
