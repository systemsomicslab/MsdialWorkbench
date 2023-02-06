using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMSIonSearchQuery
    {
        IMSIonProperty LowerLimit();
        IMSIonProperty UpperLimit();
    }

    public sealed class MSIonSearchQuery : IMSIonSearchQuery, IMSSearchQuery
    {
        private readonly IMSIonProperty center;
        private readonly double massTolerance;
        private readonly double ccsTolerance;
        private readonly double rtTolerance;
        private readonly double riTolerance;
        private readonly double driftTolerance;

        private MSIonSearchQuery(
            IMSIonProperty center,
            double massTolerance, double ccsTolerance,
            double rtTolerance, double riTolerance,
            double driftTolerance) {
            this.center = center;
            this.massTolerance = massTolerance;
            this.ccsTolerance = ccsTolerance;
            this.rtTolerance = rtTolerance;
            this.riTolerance = riTolerance;
            this.driftTolerance = driftTolerance;
        }

        public MSIonSearchQuery(
            double mass, double massTolerance,
            double rt, double rtTolerance,
            double ri, double riTolerance,
            double drift, double driftTolerance,
            double ccs, double ccsTolerance,
            IonMode ionMode, AdductIon adduct) {
            center = new MSIonProperty(
                mass,
                new ChromXs
                {
                    RT = new RetentionTime(rt, ChromXUnit.Min),
                    RI = new RetentionIndex(ri, ChromXUnit.None),
                    Drift = new DriftTime(drift, ChromXUnit.Msec),
                    Mz = new MzValue(mass, ChromXUnit.Mz),
                },
                ionMode,
                adduct,
                ccs);
            this.massTolerance = massTolerance;
            this.rtTolerance = rtTolerance;
            this.riTolerance = riTolerance;
            this.driftTolerance = driftTolerance;
            this.ccsTolerance = ccsTolerance;
        }

        private MSIonProperty LowerLimitsCore() {
            return new MSIonProperty(
                center.PrecursorMz - massTolerance,
                new ChromXs
                {
                    RT = new RetentionTime(center.ChromXs.RT.Value - rtTolerance, center.ChromXs.RT.Unit),
                    RI = new RetentionIndex(center.ChromXs.RI.Value - riTolerance, center.ChromXs.RI.Unit),
                    Drift = new DriftTime(center.ChromXs.Drift.Value - driftTolerance, center.ChromXs.Drift.Unit),
                    Mz = new MzValue(center.ChromXs.Mz.Value - massTolerance, center.ChromXs.Mz.Unit),
                },
                center.IonMode,
                center.AdductType,
                center.CollisionCrossSection - ccsTolerance);
        }

        private MSIonProperty UpperLimitCore() {
            return new MSIonProperty(
                center.PrecursorMz + massTolerance,
                new ChromXs
                {
                    RT = new RetentionTime(center.ChromXs.RT.Value + rtTolerance, center.ChromXs.RT.Unit),
                    RI = new RetentionIndex(center.ChromXs.RI.Value + riTolerance, center.ChromXs.RI.Unit),
                    Drift = new DriftTime(center.ChromXs.Drift.Value + driftTolerance, center.ChromXs.Drift.Unit),
                    Mz = new MzValue(center.ChromXs.Mz.Value + massTolerance, center.ChromXs.Mz.Unit),
                },
                center.IonMode,
                center.AdductType,
                center.CollisionCrossSection + ccsTolerance);
        }

        // IMSSearchQuery
        IMSProperty IMSSearchQuery.LowerLimit() {
            return LowerLimitsCore();
        }

        IMSProperty IMSSearchQuery.UpperLimit() {
            return UpperLimitCore();
        }

        // IMSIonSearchQuery
        IMSIonProperty IMSIonSearchQuery.LowerLimit() {
            return LowerLimitsCore();
        }

        IMSIonProperty IMSIonSearchQuery.UpperLimit() {
            return UpperLimitCore();
        }

        public static MSIonSearchQuery CreateMassRtQuery(
            double mass, double massTolerance,
            double rt, double rtTolerance,
            IonMode ionMode = IonMode.Positive,
            AdductIon adduct = null) {
            return new MSIonSearchQuery(
                mass, massTolerance,
                rt, rtTolerance,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                ionMode, adduct);
        }

        public static MSIonSearchQuery CreateMassCcsQuery(
            double mass, double massTolerance,
            double ccs, double ccsTolerance,
            IonMode ionMode = IonMode.Positive,
            AdductIon adduct = null) {
            return new MSIonSearchQuery(
                mass, massTolerance,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                ccs, ccsTolerance,
                ionMode, adduct);
        }

        public static MSIonSearchQuery CreateMassRtCcsQuery(
            double mass, double massTolerance,
            double rt, double rtTolerance,
            double ccs, double ccsTolerance,
            IonMode ionMode = IonMode.Positive,
            AdductIon adduct = null) {
            return new MSIonSearchQuery(
                mass, massTolerance,
                rt, rtTolerance,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                ccs, ccsTolerance,
                ionMode, adduct);
        }
    }
}
