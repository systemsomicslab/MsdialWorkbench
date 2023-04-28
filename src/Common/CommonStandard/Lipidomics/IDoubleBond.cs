using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public sealed class DoubleBondIndeterminateState {
        public static DoubleBondIndeterminateState AllPositions { get; } = new DoubleBondIndeterminateState(State.AllPositions, new int[0]);
        public static DoubleBondIndeterminateState AllCisTransIsomers { get; } = new DoubleBondIndeterminateState(State.AllCisTransIsomers, new int[0]);

        private readonly State _state;
        private readonly int[] _positions;

        private DoubleBondIndeterminateState(State state, int[] positions) {
            _state = state;
            _positions = positions;
        }

        public IReadOnlyCollection<IDoubleBondInfo> Indeterminate(IReadOnlyCollection<IDoubleBondInfo> infos) {
            if (infos is null) {
                throw new ArgumentNullException(nameof(infos));
            }

            switch (_state) {
                case State.AllPositions:
                    return new IDoubleBondInfo[0];
                case State.AllCisTransIsomers:
                    if (infos.Any(info => info.Determined)) {
                        return infos.Select(info => info.Determined ? DoubleBondInfo.Create(info.Position) : info).ToArray();
                    }
                    return infos;
                case State.Position:
                    if (_positions is null) {
                        return infos;
                    }
                    return infos.Where(info => !_positions.Contains(info.Position)).ToList();
                case State.CisTransIsomer:
                    if (_positions is null || infos.All(info => !info.Determined)) {
                        return infos;
                    }
                    return infos.Select(info => _positions.Contains(info.Position) && info.Determined ? DoubleBondInfo.Create(info.Position) : info).ToArray();
            }
            return infos;
        }

        public static DoubleBondIndeterminateState Positions(params int[] positions) {
            return new DoubleBondIndeterminateState(State.Position, positions);
        }

        public static DoubleBondIndeterminateState CisTransIsomer(params int[] positions) {
            return new DoubleBondIndeterminateState(State.CisTransIsomer, positions);
        }

        enum State {
            None,
            Position,
            CisTransIsomer,
            AllPositions,
            AllCisTransIsomers,
        }
    }

    public interface IDoubleBond : IVisitableElement<IDoubleBond>
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }

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

        public TResult Accept<TResult, TDecomposed>(IAcyclicVisitor visitor, IDecomposer<TResult, IDoubleBond, TDecomposed> decomposer) {
            return decomposer.Decompose(visitor, this);
        }

        public TResult Accept<TResult, TDecomposed>(IVisitor<TResult, TDecomposed> visitor, IDecomposer<TResult, IDoubleBond, TDecomposed> decomposer) {
            return decomposer.Decompose(visitor, this);
        }
    }

    public enum DoubleBondState
    {
        Unknown, E, Z,
    }

    public interface IDoubleBondInfo
    {
        int Position { get; }
        DoubleBondState State { get; }
        bool Determined { get; }
    }

    public class DoubleBondInfo : IDoubleBondInfo
    {
        private static readonly Dictionary<(int, DoubleBondState), DoubleBondInfo> CACHE = new Dictionary<(int, DoubleBondState), DoubleBondInfo>();
        private static readonly object LOCKOBJ = new object();

        public static DoubleBondInfo Create(int position, DoubleBondState state = DoubleBondState.Unknown) {
            DoubleBondInfo info;
            lock (LOCKOBJ) {
                if (!CACHE.TryGetValue((position, state), out info)) {
                    return CACHE[(position, state)] = new DoubleBondInfo(position, state);
                }
            }
            return info;
        }

        private DoubleBondInfo(int position, DoubleBondState state) {
            Position = position;
            State = state;
        }

        public int Position { get; }
        public DoubleBondState State { get; }

        public bool Determined => State != DoubleBondState.Unknown;

        public static DoubleBondInfo E(int position) => Create(position, DoubleBondState.E);

        public static DoubleBondInfo Z(int position) => Create(position, DoubleBondState.Z);

        public override string ToString() {
            switch (State) {
                case DoubleBondState.E:
                case DoubleBondState.Z:
                    return $"{Position}{State}";
                case DoubleBondState.Unknown:
                default:
                    return $"{Position}";
            }
        }

        public override bool Equals(object obj) {
            return obj is DoubleBondInfo info && Position == info.Position && State == info.State;
        }

        public override int GetHashCode() {
            return (Position, State).GetHashCode();
        }
    }
}
