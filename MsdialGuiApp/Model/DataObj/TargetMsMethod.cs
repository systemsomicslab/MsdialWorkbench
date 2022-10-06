namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class TargetMsMethod
    {
        public static TargetMsMethod Dims { get; } = new TargetMsMethod(useMz: true, useRt: false, useMobility: false);
        public static TargetMsMethod Imms { get; } = new TargetMsMethod(useMz: true, useRt: false, useMobility: true);
        public static TargetMsMethod Lcms { get; } = new TargetMsMethod(useMz: true, useRt: true, useMobility: false);
        public static TargetMsMethod Lcimms { get; } = new TargetMsMethod(useMz: true, useRt: true, useMobility: true);
        public static TargetMsMethod Gcms { get; } = new TargetMsMethod(useMz: true, useRt: true, useMobility: false);

        private TargetMsMethod(bool useMz, bool useRt, bool useMobility) {
            UseMz = useMz;
            UseRt = useRt;
            UseMobility = useMobility;
        }

        public bool UseMz { get; }
        public bool UseRt { get; }
        public bool UseMobility { get; }
    }
}
