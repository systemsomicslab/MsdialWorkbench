using System;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface IDoubleBondInfo : IEquatable<IDoubleBondInfo>
    {
        int Position { get; }
        DoubleBondState State { get; }
        bool Determined { get; }
        bool Includes(IDoubleBondInfo info);
    }

    public enum DoubleBondState
    {
        Unknown, E, Z,
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

        public bool Includes(IDoubleBondInfo info) {
            return Position == info.Position && (State == DoubleBondState.Unknown || State == info.State);
        }

        public bool Equals(IDoubleBondInfo other) {
            return Position == other.Position && State == other.State;
        }
    }
}
