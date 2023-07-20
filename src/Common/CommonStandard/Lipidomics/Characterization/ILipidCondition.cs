using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCondition<in TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan);
    }

    internal sealed class FragmentsExistAllCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<TLipidCandidate, (double mz, double threshold)[]> _diagnosticIonsFactory;
        private readonly double _ms2Tolerance;

        public FragmentsExistAllCondition(Func<TLipidCandidate, (double mz, double threshold)[]> diagnosticIonsFactory, double ms2Tolerance) {
            _diagnosticIonsFactory = diagnosticIonsFactory;
            _ms2Tolerance = ms2Tolerance;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return _diagnosticIonsFactory.Invoke(lipid).All(pair => LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(scan.Spectrum, _ms2Tolerance, pair.mz, pair.threshold));
        }
    }

    internal sealed class FragmentsExistAnyCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<TLipidCandidate, (double mz, double threshold)[]> _diagnosticIonsFactory;
        private readonly double _ms2Tolerance;

        public FragmentsExistAnyCondition(Func<TLipidCandidate, (double mz, double threshold)[]> diagnosticIonsFactory, double ms2Tolerance) {
            _diagnosticIonsFactory = diagnosticIonsFactory;
            _ms2Tolerance = ms2Tolerance;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return _diagnosticIonsFactory.Invoke(lipid).Any(pair => LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(scan.Spectrum, _ms2Tolerance, pair.mz, pair.threshold));
        }
    }

    internal sealed class FragmentsNotExistCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<TLipidCandidate, (double mz, double threshold)[]> _diagnosticIonsFactory;
        private readonly double _ms2Tolerance;

        public FragmentsNotExistCondition(Func<TLipidCandidate, (double mz, double threshold)[]> diagnosticIonsFactory, double ms2Tolerance) {
            _diagnosticIonsFactory = diagnosticIonsFactory;
            _ms2Tolerance = ms2Tolerance;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return _diagnosticIonsFactory.Invoke(lipid).All(pair => !LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(scan.Spectrum, _ms2Tolerance, pair.mz, pair.threshold));
        }
    }

    internal sealed class FragmentsGreaterThanCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<TLipidCandidate, double> _greaterIonFactory, _lessIonFactory;
        private readonly double _ms2Tolerance;

        public FragmentsGreaterThanCondition(Func<TLipidCandidate, double> greaterIonFactory, Func<TLipidCandidate, double> lessIonFactory, double ms2Tolerance) {
            _greaterIonFactory = greaterIonFactory;
            _lessIonFactory = lessIonFactory;
            _ms2Tolerance = ms2Tolerance;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(scan.Spectrum, _ms2Tolerance, _greaterIonFactory.Invoke(lipid), _lessIonFactory.Invoke(lipid));
        }
    }

    internal sealed class LipidChainCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate {
        private readonly Predicate<TLipidCandidate> _predicate;

        public LipidChainCondition(Predicate<TLipidCandidate> predicate) {
            _predicate = predicate;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return _predicate.Invoke(lipid);
        }
    }

    internal sealed class MergeCondition<TLipidCandidate> : ILipidCondition<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly ILipidCondition<TLipidCandidate>[] _conditions;

        public MergeCondition(params ILipidCondition<TLipidCandidate>[] conditions) {
            _conditions = conditions;
        }

        public bool Satisfy(TLipidCandidate lipid, IMSScanProperty scan) {
            return _conditions.All(condition => condition.Satisfy(lipid, scan));
        }
    }

    internal interface ILipidPreCondition {
        bool Satisfy(LipidMolecule lipid);
    }

    internal sealed class IonModeCondition : ILipidPreCondition
    {
        private readonly IonMode _ionMode;

        public IonModeCondition(IonMode ionMode) {
            _ionMode = ionMode;
        }

        public bool Satisfy(LipidMolecule lipid) {
            return lipid.Adduct.IonMode == _ionMode;
        }
    }

    internal sealed class AdductCondition : ILipidPreCondition {
        private readonly string[] _names;

        public AdductCondition(params string[] names) {
            _names = names;
        }

        public bool Satisfy(LipidMolecule lipid) {
            return _names.Any(name => lipid.Adduct.AdductIonName == name);
        }
    }

    internal sealed class MergePreCondition : ILipidPreCondition
    {
        private readonly ILipidPreCondition[] _conditions;

        public MergePreCondition(params ILipidPreCondition[] conditions) {
            _conditions = conditions;
        }

        public bool Satisfy(LipidMolecule lipid) {
            return _conditions.All(condition => condition.Satisfy(lipid));
        }
    }
}
