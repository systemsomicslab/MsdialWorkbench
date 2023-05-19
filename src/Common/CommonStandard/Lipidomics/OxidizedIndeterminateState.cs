using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public sealed class OxidizedIndeterminateState
    {
        public static OxidizedIndeterminateState AllPositions { get; } = new OxidizedIndeterminateState(State.PositionExclude, null);
        public static OxidizedIndeterminateState Identity { get; } = new OxidizedIndeterminateState(State.None, null);

        private readonly State _state;
        private readonly int[] _positions;

        private OxidizedIndeterminateState(State state, int[] positions) {
            _state = state;
            _positions = positions;
        }

        public IReadOnlyCollection<int> Indeterminate(IReadOnlyCollection<int> infos) {
            if (infos is null) {
                throw new ArgumentNullException(nameof(infos));
            }

            switch (_state) {
                case State.PositionInclude:
                    if (_positions is null) {
                        return infos;
                    }
                    return infos.Where(info => !_positions.Contains(info)).ToArray();
                case State.PositionExclude:
                    if (_positions is null) {
                        return new int[0];
                    }
                    return infos.Where(info => _positions.Contains(info)).ToArray();
            }
            return infos;
        }

        public OxidizedIndeterminateState Include(int position) {
            switch (_state) {
                case State.PositionInclude:
                    if (_positions is null) {
                        return new OxidizedIndeterminateState(_state, new[] { position, });
                    }
                    else if (!_positions.Contains(position)) {
                        return new OxidizedIndeterminateState(_state, _positions.Append(position).ToArray());
                    }
                    break;
                case State.PositionExclude:
                    if (_positions != null && _positions.Contains(position)) {
                        return new OxidizedIndeterminateState(_state, _positions.Where(p => p != position).ToArray());
                    }
                    break;
            }
            return this;
        }

        public OxidizedIndeterminateState Exclude(int position) {
            switch (_state) {
                case State.PositionInclude:
                    if (_positions != null && _positions.Contains(position)) {
                        return new OxidizedIndeterminateState(_state, _positions.Where(p => p != position).ToArray());
                    }
                    break;
                case State.PositionExclude:
                    if (_positions is null) {
                        return new OxidizedIndeterminateState(_state, new[] { position, });
                    }
                    else if (_positions != null && !_positions.Contains(position)) {
                        return new OxidizedIndeterminateState(_state, _positions.Append(position).ToArray());
                    }
                    break;
            }
            return this;
        }

        public IVisitor<IOxidized, IOxidized> AsVisitor() => new OxidizedIndeterminateVisitor(this);

        public static OxidizedIndeterminateState Positions(params int[] positions) {
            return new OxidizedIndeterminateState(State.PositionInclude, positions);
        }

        public static OxidizedIndeterminateState PositionsExclude(params int[] positions) {
            return new OxidizedIndeterminateState(State.PositionExclude, positions);
        }

        enum State
        {
            None,
            PositionInclude,
            PositionExclude,
        }

        sealed class OxidizedIndeterminateVisitor : IVisitor<IOxidized, IOxidized>
        {
            private readonly OxidizedIndeterminateState _state;

            public OxidizedIndeterminateVisitor(OxidizedIndeterminateState state) {
                _state = state;
            }

            IOxidized IVisitor<IOxidized, IOxidized>.Visit(IOxidized item) {
                var ox = _state.Indeterminate(item.Oxidises);
                return new Oxidized(item.Count, (ox as IList<int>) ?? ox.ToArray());
            }
        }
    }
}
