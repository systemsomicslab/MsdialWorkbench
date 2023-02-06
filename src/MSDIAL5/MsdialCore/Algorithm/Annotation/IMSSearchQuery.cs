using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMSSearchQuery

    {
        IMSProperty LowerLimit();
        IMSProperty UpperLimit();
    }

    public sealed class MassSearchQuery : IMSSearchQuery
    {
        private readonly double mass;
        private readonly double tolerance;

        public MassSearchQuery(double mass, double tolerance) {
            this.mass = mass;
            this.tolerance = tolerance;
        }

        IMSProperty IMSSearchQuery.LowerLimit() {
            return new MSProperty(mass - tolerance, new ChromXs(double.NegativeInfinity), IonMode.Positive);
        }

        IMSProperty IMSSearchQuery.UpperLimit() {
            return new MSProperty(mass + tolerance, new ChromXs(double.PositiveInfinity), IonMode.Positive);
        }
    }

    public sealed class MSSearchQuery : IMSSearchQuery
    {
        private IMSProperty center;
        private readonly double massTolerance;
        private readonly double rtTolerance;
        private readonly double riTolerance;
        private readonly double driftTolerance;

        private MSSearchQuery(
            IMSProperty center,
            double massTolerance,
            double rtTolerance, double riTolerance,
            double driftTolerance) {
            this.center = center;
            this.massTolerance = massTolerance;
            this.rtTolerance = rtTolerance;
            this.riTolerance = riTolerance;
            this.driftTolerance = driftTolerance;
        }

        public MSSearchQuery(
            double mass, double massTolerance,
            double rt, double rtTolerance,
            double ri, double riTolerance,
            double drift, double driftTolerance,
            IonMode ionMode) {
            center = new MSProperty(
                mass,
                new ChromXs
                {
                    RT = new RetentionTime(rt, ChromXUnit.Min),
                    RI = new RetentionIndex(ri, ChromXUnit.None),
                    Drift = new DriftTime(drift, ChromXUnit.Msec),
                    Mz = new MzValue(mass, ChromXUnit.Mz),
                },
                ionMode);
            this.massTolerance = massTolerance;
            this.rtTolerance = rtTolerance;
            this.riTolerance = riTolerance;
            this.driftTolerance = driftTolerance;
        }

        private MSProperty LowerLimitsCore() {
            return new MSProperty(
                center.PrecursorMz - massTolerance,
                new ChromXs
                {
                    RT = new RetentionTime(center.ChromXs.RT.Value - rtTolerance, center.ChromXs.RT.Unit),
                    RI = new RetentionIndex(center.ChromXs.RI.Value - riTolerance, center.ChromXs.RI.Unit),
                    Drift = new DriftTime(center.ChromXs.Drift.Value - driftTolerance, center.ChromXs.Drift.Unit),
                    Mz = new MzValue(center.ChromXs.Mz.Value - massTolerance, center.ChromXs.Mz.Unit),
                },
                center.IonMode);
        }

        private MSProperty UpperLimitCore() {
            return new MSProperty(
                center.PrecursorMz + massTolerance,
                new ChromXs
                {
                    RT = new RetentionTime(center.ChromXs.RT.Value + rtTolerance, center.ChromXs.RT.Unit),
                    RI = new RetentionIndex(center.ChromXs.RI.Value + riTolerance, center.ChromXs.RI.Unit),
                    Drift = new DriftTime(center.ChromXs.Drift.Value + driftTolerance, center.ChromXs.Drift.Unit),
                    Mz = new MzValue(center.ChromXs.Mz.Value + massTolerance, center.ChromXs.Mz.Unit),
                },
                center.IonMode);
        }

        // IMassSearchQuery
        IMSProperty IMSSearchQuery.LowerLimit() {
            return LowerLimitsCore();
        }

        IMSProperty IMSSearchQuery.UpperLimit() {
            return UpperLimitCore();
        }

        public static MSSearchQuery CreateMassQuery(
            double mass, double massTolerance,
            IonMode ionMode = IonMode.Positive) {
            return new MSSearchQuery(
                mass, massTolerance,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                ionMode);
        }

        public static MSSearchQuery CreateMassRtQuery(
            double mass, double massTolerance,
            double rt, double rtTolerance,
            IonMode ionMode = IonMode.Positive) {
            return new MSSearchQuery(
                mass, massTolerance,
                rt, rtTolerance,
                0, double.PositiveInfinity,
                0, double.PositiveInfinity,
                ionMode);
        }
    }
}
