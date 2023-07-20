using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal static class LipidDsl
    {
        public static LipidCharacterizationDsl<T> ToDsl<T>(this ILipidType<T> type) where T : ILipidCandidate {
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
            return dsl.Append(new LipidChainCondition<T>(predicate));
        }

        public static LipidCharacterizationDsl<T> ExistAll<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(new FragmentsExistAllCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> ExistAny<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(new FragmentsExistAnyCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> NotExist<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threshold)[]> factory) where T: ILipidCandidate {
            return dsl.Append(new FragmentsNotExistCondition<T>(factory, tolerance));
        }

        public static LipidCharacterizationDsl<T> GreaterThan<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, double> greater, Func<T, double> less) where T: ILipidCandidate {
            return dsl.Append(new FragmentsGreaterThanCondition<T>(greater, less, tolerance));
        }

        public static LipidCharacterizationDsl<T> ScoreBy<T>(this LipidCharacterizationDsl<T> dsl, double tolerance, Func<T, (double mz, double threahold)[]> factory) where T : ILipidCandidate {
            return dsl.Set(new LipidScoring<T>(factory, tolerance));
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
        private readonly ILipidCondition<TLipidCandidate>[] _conditions;
        private readonly ILipidScoring<TLipidCandidate> _scoring;
        private readonly Func<ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> _search;

        private LipidCharacterizationDsl(ILipidType<TLipidCandidate> type, ILipidPreCondition[] preConditions, ILipidCondition<TLipidCandidate>[] conditions, ILipidScoring<TLipidCandidate> scoring, Func<ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> search) {
            _type = type;
            _preConditions = preConditions;
            _conditions = conditions;
            _scoring = scoring;
            _search = search;
        }

        public LipidCharacterizationDsl<TLipidCandidate> Append(ILipidPreCondition preCondition) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions.Append(preCondition).ToArray(), _conditions, _scoring, _search);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Append(ILipidCondition<TLipidCandidate> condition) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions.Append(condition).ToArray(), _scoring, _search);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Set(ILipidScoring<TLipidCandidate> scoring) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions, scoring, _search);
        }

        public LipidCharacterizationDsl<TLipidCandidate> Set(Func<ILipidType<TLipidCandidate>, ILipidCondition<TLipidCandidate>, ILipidScoring<TLipidCandidate>, ILipidSearchSpace> search) {
            return new LipidCharacterizationDsl<TLipidCandidate>(_type, _preConditions, _conditions, _scoring, (c, s) => search(_type, c, s));
        }

        public ILipidCharacterization Compile() {
            var preConditions = _preConditions.Length == 1 ? _preConditions[0] : new MergePreCondition(_preConditions);
            var conditions = _conditions.Length == 1 ? _conditions[0] : new MergeCondition<TLipidCandidate>(_conditions);
            return new LipidCharacterization<TLipidCandidate>(_type, preConditions, _search(conditions, _scoring));
        }

        public static LipidCharacterizationDsl<TLipidCandidate> From(ILipidType<TLipidCandidate> type) {
            return new LipidCharacterizationDsl<TLipidCandidate>(type,
                Array.Empty<ILipidPreCondition>(),
                Array.Empty<ILipidCondition<TLipidCandidate>>(),
                new LipidScoring<TLipidCandidate>(c => Array.Empty<(double, double)>(), .02d),
                (c, s) => new IdentitySearchSpace<TLipidCandidate>(type, c, s));
        }
    }
}
