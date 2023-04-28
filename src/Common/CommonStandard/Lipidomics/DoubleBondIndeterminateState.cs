using System;
using System.Collections.Generic;
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
}
