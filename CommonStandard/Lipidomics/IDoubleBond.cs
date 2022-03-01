using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface IDoubleBond
    {
        int Count { get; }
        int DecidedCount { get; }
        int UnDecidedCount { get; }

        ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }
    }

    public class DoubleBond : IDoubleBond
    {
        public DoubleBond(int count, IList<IDoubleBondInfo> bonds) {
            Count = count;
            Bonds = new ReadOnlyCollection<IDoubleBondInfo>(bonds);
        }

        public DoubleBond(int count, params IDoubleBondInfo[] bonds) : this(count, (IList<IDoubleBondInfo>)bonds) {

        }

        public DoubleBond(params IDoubleBondInfo[] bonds) : this(bonds.Length, (IList<IDoubleBondInfo>)bonds) {

        }

        public int Count { get; }

        public int DecidedCount => Bonds.Count;

        public int UnDecidedCount => Count - DecidedCount;

        public ReadOnlyCollection<IDoubleBondInfo> Bonds { get; }

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
    }

    public enum DoubleBondState
    {
        Unknown, E, Z,
    }

    public interface IDoubleBondInfo
    {
        int Position { get; }
        DoubleBondState State { get; }
    }

    public class DoubleBondInfo : IDoubleBondInfo
    {
        private DoubleBondInfo(int position, DoubleBondState state) {
            Position = position;
            State = state;
        }

        public int Position { get; }
        public DoubleBondState State { get; }

        static readonly Dictionary<(int, DoubleBondState), DoubleBondInfo> cache = new Dictionary<(int, DoubleBondState), DoubleBondInfo>();
        private static readonly object lockobj = new object();

        public static DoubleBondInfo Create(int position, DoubleBondState state = DoubleBondState.Unknown) {
            DoubleBondInfo info;
            lock (lockobj) {
                if (!cache.TryGetValue((position, state), out info)) {
                    return cache[(position, state)] = new DoubleBondInfo(position, state);
                }
            }
            return info;
        }

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
    }
}
