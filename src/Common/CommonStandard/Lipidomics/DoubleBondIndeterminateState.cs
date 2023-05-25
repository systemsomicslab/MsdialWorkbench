using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public sealed class DoubleBondIndeterminateState {
        public static DoubleBondIndeterminateState AllPositions { get; } = new DoubleBondIndeterminateState(State.PositionExclude, null);
        public static DoubleBondIndeterminateState AllCisTransIsomers { get; } = new DoubleBondIndeterminateState(State.CisTransIsomerExclude, null);
        public static DoubleBondIndeterminateState Identity { get; } = new DoubleBondIndeterminateState(State.None, null);

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
                case State.PositionInclude:
                    if (_positions is null) {
                        return infos;
                    }
                    return infos.Where(info => !_positions.Contains(info.Position)).ToList();
                case State.PositionExclude:
                    if (_positions is null) {
                        return new IDoubleBondInfo[0];
                    }
                    return infos.Where(info => _positions.Contains(info.Position)).ToList();
                case State.CisTransIsomerInclude:
                    if (_positions is null || infos.All(info => !info.Determined)) {
                        return infos;
                    }
                    return infos.Select(info => _positions.Contains(info.Position) && info.Determined ? DoubleBondInfo.Create(info.Position) : info).ToArray();
                case State.CisTransIsomerExclude:
                    if (infos.All(info => !info.Determined)) {
                        return infos;
                    }
                    if (_positions is null) {
                        return infos.Select(info => info.Determined ? DoubleBondInfo.Create(info.Position) : info).ToArray();
                    }
                    return infos.Select(info => !_positions.Contains(info.Position) && info.Determined ? DoubleBondInfo.Create(info.Position) : info).ToArray();
            }
            return infos;
        }

        public DoubleBondIndeterminateState Include(int position) {
            switch (_state) {
                case State.PositionInclude:
                case State.CisTransIsomerInclude:
                    if (_positions is null) {
                        return new DoubleBondIndeterminateState(_state, new[] { position, });
                    }
                    if (!_positions.Contains(position)) {
                        return new DoubleBondIndeterminateState(_state, _positions.Append(position).ToArray());
                    }
                    break;
                case State.PositionExclude:
                case State.CisTransIsomerExclude:
                    if (_positions != null && _positions.Contains(position)) {
                        return new DoubleBondIndeterminateState(_state, _positions.Where(p => p != position).ToArray());
                    }
                    break;
            }
            return this;
        }

        public DoubleBondIndeterminateState Exclude(int position) {
            switch (_state) {
                case State.PositionInclude:
                case State.CisTransIsomerInclude:
                    if (_positions != null && _positions.Contains(position)) {
                        return new DoubleBondIndeterminateState(_state, _positions.Where(p => p != position).ToArray());
                    }
                    break;
                case State.PositionExclude:
                case State.CisTransIsomerExclude:
                    if (_positions is null) {
                        return new DoubleBondIndeterminateState(_state, new[] { position, });
                    }
                    if (!_positions.Contains(position)) {
                        return new DoubleBondIndeterminateState(_state, _positions.Append(position).ToArray());
                    }
                    break;
            }
            return this;
        }

        public IVisitor<IDoubleBond, IDoubleBond> AsVisitor() => new DoubleBondIndeterminateVisitor(this);

        public static DoubleBondIndeterminateState Positions(params int[] positions) {
            return new DoubleBondIndeterminateState(State.PositionInclude, positions);
        }

        public static DoubleBondIndeterminateState PositionsExclude(params int[] positions) {
            return new DoubleBondIndeterminateState(State.PositionExclude, positions);
        }

        public static DoubleBondIndeterminateState CisTransIsomer(params int[] positions) {
            return new DoubleBondIndeterminateState(State.CisTransIsomerInclude, positions);
        }

        public static DoubleBondIndeterminateState CisTransIsomerExclude(params int[] positions) {
            return new DoubleBondIndeterminateState(State.CisTransIsomerExclude, positions);
        }

        enum State {
            None,
            PositionInclude,
            PositionExclude,
            CisTransIsomerInclude,
            CisTransIsomerExclude,
        }

        sealed class DoubleBondIndeterminateVisitor : IVisitor<IDoubleBond, IDoubleBond> {
            private readonly DoubleBondIndeterminateState _state;

            public DoubleBondIndeterminateVisitor(DoubleBondIndeterminateState state) {
                _state = state;
            }

            IDoubleBond IVisitor<IDoubleBond, IDoubleBond>.Visit(IDoubleBond item) {
                return item.Indeterminate(_state);
            }
        }
    }
}
