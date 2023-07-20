using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal static class LipidDsl
    {
        public static LipidCharacterizationDsl<T> DefineRules<T>(this ILipidType<T> type) where T : ILipidCandidate {
            return LipidCharacterizationDsl<T>.From(type);
        }

        public static LipidCharacterizationDsl<T> IsPositive<T>(this LipidCharacterizationDsl<T> dsl) where T: ILipidCandidate {
            return dsl.Append(new IonModeCondition(Enum.IonMode.Positive));
        }

        public static LipidCharacterizationDsl<T> IsNegative<T>(this LipidCharacterizationDsl<T> dsl) where T: ILipidCandidate {
            return dsl.Append(new IonModeCondition(Enum.IonMode.Negative));
        }

        public static LipidCharacterizationDsl<T> HasAdduct<T>(this LipidCharacterizationDsl<T> dsl, params string[] adducts) where T: ILipidCandidate {
            return dsl.Append(new AdductCondition(adducts));
        }

        public static LipidCharacterizationDsl<T> IsValidMolecule<T>(this LipidCharacterizationDsl<T> dsl, Predicate<T> predicate) where T: ILipidCandidate {
            return dsl.Append(_ => new LipidChainCondition<T>(predicate));
        }

        public static LipidCharacterizationDsl<T> ExistAll<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(_ => new FragmentsExistAllCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> ExistAll<T>(this LipidCharacterizationDsl<T> dsl, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(t => new FragmentsExistAllCondition<T>(factory, t));
        }

        public static LipidCharacterizationDsl<T> ExistAny<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(_ => new FragmentsExistAnyCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> ExistAny<T>(this LipidCharacterizationDsl<T> dsl, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(t => new FragmentsExistAnyCondition<T>(factory, t));
        }

        public static LipidCharacterizationDsl<T> NotExist<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(_ => new FragmentsNotExistCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> NotExist<T>(this LipidCharacterizationDsl<T> dsl, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(t => new FragmentsNotExistCondition<T>(factory, t));
        }

        public static LipidCharacterizationDsl<T> GreaterThan<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, double> greater, Func<T, double> less) where T: ILipidCandidate {
            return dsl.Append(_ => new FragmentsGreaterThanCondition<T>(greater, less, tolerance));
        }

        public static LipidCharacterizationDsl<T> GreaterThan<T>(this LipidCharacterizationDsl<T> dsl, Func<T, double> greater, Func<T, double> less) where T: ILipidCandidate {
            return dsl.Append(t => new FragmentsGreaterThanCondition<T>(greater, less, t));
        }

        public static LipidCharacterizationDsl<T> ScoreBy<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threahold)[]> factory) where T : ILipidCandidate {
            return dsl.Set(_ => new LipidScoring<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> ScoreBy<T>(this LipidCharacterizationDsl<T> dsl, Func<T, (double mz, double threahold)[]> factory) where T : ILipidCandidate {
            return dsl.Set(t => new LipidScoring<T>(factory, t));
        }

        public static LipidCharacterizationDsl<T> Just<T>(this LipidCharacterizationDsl<T> dsl) where T: ILipidCandidate {
            return dsl.Set((t, c, s) => new IdentitySearchSpace<T>(t, c, s));
        }

        public static LipidCharacterizationDsl<PCCandidate> FromRange(this LipidCharacterizationDsl<PCCandidate> dsl, int carbonMinimum, int doubleBondMaximum) {
            return dsl.Set((t, c, s) => new PCSearchSpace(carbonMinimum, doubleBondMaximum, c, s));
        }
    }

    internal sealed class LipidCharacterizationDsl<TLipidCandidate> where TLipidCandidate : ILipidCandidate {
        private readonly ILipidType<TLipidCandidate> _type;
        private readonly ILipidPreCondition[] _preConditions;
        private readonly Func<double, ILipidCondition<TLipidCandidate>>[] _conditions;
        private readonly Func<double, ILipidScoring<TLipidCandidate>> _scoring;
        private readonly Func<ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> _search;
        private readonly double _tolerance;

        private LipidCharacterizationDsl(ILipidType<TLipidCandidate> type, ILipidPreCondition[] preConditions, Func<double, ILipidCondition<TLipidCandidate>>[] conditionsZZZ, Func<double, ILipidScoring<TLipidCandidate>> scoringZZZ, Func<ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> search, double tolerance) {
            _type = type;
            _preConditions = preConditions;
            _search = search;
            _tolerance = tolerance;
            _conditions = conditionsZZZ;
            _scoring = scoringZZZ;
        }

        public LipidCharacterizationDsl<TLipidCandidate> Append(ILipidPreCondition preCondition) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions.Append(preCondition).ToArray(), _conditions, _scoring, _search, _tolerance);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Append(Func<double, ILipidCondition<TLipidCandidate>> condition) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions.Append(condition).ToArray(), _scoring, _search, _tolerance);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Set(Func<double, ILipidScoring<TLipidCandidate>> scoring) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions, scoring, _search, _tolerance);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Set(Func<ILipidType<TLipidCandidate>, ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> search) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions, _scoring, (c, s) => search(_type, c, s), _tolerance);
        }

        public LipidCharacterizationDsl<TLipidCandidate> SetTolerance(double tolerance) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions, _scoring, _search, tolerance);
        }

        public ILipidCharacterization Compile() {
            var preConditions = _preConditions.Length == 1 ? _preConditions[0] : new MergePreCondition(_preConditions);
            var conditions = _conditions.Length == 1 ? _conditions[0](_tolerance) : new MergeCondition<TLipidCandidate>(_conditions.Select(c => c(_tolerance)).ToArray());
            return new LipidCharacterization<TLipidCandidate>(_type, preConditions, _search(conditions, _scoring(_tolerance)));
        }

        public static LipidCharacterizationDsl<TLipidCandidate> From(ILipidType<TLipidCandidate> type) {
            return new LipidCharacterizationDsl<TLipidCandidate>(type,
                Array.Empty<ILipidPreCondition>(),
                Array.Empty<Func<double, ILipidCondition<TLipidCandidate>>>(),
                _ => new LipidScoring<TLipidCandidate>(c => Array.Empty<(double, double)>(), .02d),
                (c, s) => new IdentitySearchSpace<TLipidCandidate>(type, c, s),
                .025d);
        }
    }
}
