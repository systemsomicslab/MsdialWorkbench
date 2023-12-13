using CompMs.Common.Interfaces;
using MessagePack;
using System;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromXs: IChromXs
    {
        [Key(0)]
        public IChromX InnerRT {
            get => RT;
            set => RT = (RetentionTime)value;
        }
        [Key(1)]
        public IChromX InnerRI {
            get => RI;
            set => RI = (RetentionIndex)value;
        }
        [Key(2)]
        public IChromX InnerDrift {
            get => Drift;
            set => Drift = (DriftTime)value;
        }
        [Key(3)]
        public IChromX InnerMz {
            get => Mz;
            set => Mz = (MzValue)value;
        }

        [IgnoreMember]
        public RetentionTime RT { get; set; }

        [IgnoreMember]
        public RetentionIndex RI { get; set; }

        [IgnoreMember]
        public DriftTime Drift { get; set; }

        [IgnoreMember]
        public MzValue Mz { get; set; }

        [Key(4)]
        public ChromXType MainType { get; set; } = ChromXType.RT;

        [SerializationConstructor]
        [Obsolete("This constructor is for MessagePack only, don't use.")]
        public ChromXs(IChromX innerRT, IChromX innerRI, IChromX innerDrift, IChromX innerMz, ChromXType mainType) {
            InnerRT = innerRT;
            InnerRI = innerRI;
            InnerDrift = innerDrift;
            InnerMz = innerMz;
            MainType = mainType;
        }

        public ChromXs () {
            RT = RetentionTime.Default;
            RI = RetentionIndex.Default;
            Drift = DriftTime.Default;
            Mz = MzValue.Default;
            MainType = ChromXType.RT;
        }

        public ChromXs(double value, ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min) : this(ChromX.Convert(value, type, unit)) {

        }

        public ChromXs(IChromX chromX)
        {
            RT = RetentionTime.Default;
            RI = RetentionIndex.Default;
            Drift = DriftTime.Default;
            Mz = MzValue.Default;
            switch (chromX) {
                case RetentionTime rt:
                    RT = rt;
                    break;
                case RetentionIndex ri:
                    RI = ri;
                    break;
                case DriftTime dt:
                    Drift = dt;
                    break;
                case MzValue mz:
                    Mz = mz;
                    break;
                default:
                    break;
            }
            MainType = chromX.Type;
        }

        public ChromXs(RetentionTime rt, RetentionIndex ri, DriftTime dt, MzValue mz, ChromXType type) {
            RT = rt;
            RI = ri;
            Drift = dt;
            Mz = mz;
            MainType = type;
        }
   
        public ChromXs(RetentionTime retentionTime) : this(retentionTime, RetentionIndex.Default, DriftTime.Default, MzValue.Default, retentionTime.Type) {

        }
   
        public ChromXs(RetentionIndex retentionIndex) : this(RetentionTime.Default, retentionIndex, DriftTime.Default, MzValue.Default, retentionIndex.Type) {

        }
   
        public ChromXs(DriftTime driftTime) : this(RetentionTime.Default, RetentionIndex.Default, driftTime, MzValue.Default, driftTime.Type) {

        }
   
        public ChromXs(MzValue mz) : this(RetentionTime.Default, RetentionIndex.Default, DriftTime.Default, mz, mz.Type) {

        }
   
        public IChromX GetRepresentativeXAxis()
        {
            switch (MainType)
            {
                case ChromXType.RT:
                    return RT;
                case ChromXType.RI:
                    return RI;
                case ChromXType.Drift:
                    return Drift;
                case ChromXType.Mz:
                    return Mz;
                default:
                    return null;
            }
        }

        [Key(5)]
        public double Value { 
            get
            {
                switch (MainType)
                {
                    case ChromXType.RT:
                        return RT?.Value ?? -1;
                    case ChromXType.RI:
                        return RI?.Value ?? -1;
                    case ChromXType.Drift:
                        return Drift?.Value ?? -1;
                    case ChromXType.Mz:
                        return Mz?.Value ?? -1;
                    default:
                        return -1;
                }
            }
        }

        [Key(6)]
        public ChromXUnit Unit
        {
            get
            {
                switch (MainType)
                {
                    case ChromXType.RT:
                        return RT?.Unit ?? ChromXUnit.None;
                    case ChromXType.RI:
                        return RI?.Unit ?? ChromXUnit.None;
                    case ChromXType.Drift:
                        return Drift?.Unit ?? ChromXUnit.None;
                    case ChromXType.Mz:
                        return Mz?.Unit ?? ChromXUnit.None;
                    default:
                        return ChromXUnit.None;
                }
            }
        }

        [Key(7)]
        public ChromXType Type
        {
            get
            {
                return MainType;
            }
        }

        public IChromX GetChromByType(ChromXType type) {
            switch (type)
            {
                case ChromXType.RT:
                    return RT;
                case ChromXType.RI:
                    return RI;
                case ChromXType.Drift:
                    return Drift;
                case ChromXType.Mz:
                    return Mz;
                default:
                    throw new ArgumentException(nameof(type));
            }
        }

        public void SetChromX(IChromX chrom) {
            switch (chrom.Type)
            {
                case ChromXType.RT:
                    InnerRT = chrom;
                    break;
                case ChromXType.RI:
                    InnerRI = chrom;
                    break;
                case ChromXType.Drift:
                    InnerDrift = chrom;
                    break;
                case ChromXType.Mz:
                    InnerMz = chrom;
                    break;
                default:
                    throw new ArgumentException(nameof(chrom));
            }
        }

        public bool HasTimeInfo() {
            if (RT == null && RI == null && Drift == null) return false;
            if (RT.Value < 0 && RI.Value < 0 && Drift.Value < 0) return false;
            return true;
        }

        public bool HasAbsolute()
        {
            if (RT == null) return false;
            if (RT.Value < 0) return false;
            return true;
        }

        public bool HasRelative()
        {
            if (RI == null) return false;
            if (RI.Value < 0) return false;
            return true;
        }

        public bool HasDrift()
        {
            if (Drift == null) return false;
            if (Drift.Value < 0) return false;
            return true;
        }

        public static ChromXs Create<T>(T time) where T : IChromX {
            return ChromXBuilder<T>.action(time);
        }

        static class ChromXBuilder<T> where T : IChromX {
            public static readonly Func<T, ChromXs> action;

            static ChromXBuilder() {
                if (typeof(T) == typeof(RetentionTime)) {
                    action = chrom => new ChromXs(chrom as RetentionTime);
                }
                else if (typeof(T) == typeof(RetentionIndex)) {
                    action = chrom => new ChromXs(chrom as RetentionIndex);
                }
                else if (typeof(T) == typeof(DriftTime)) {
                    action = chrom => new ChromXs(chrom as DriftTime);
                }
                else if (typeof(T) == typeof(MzValue)) {
                    action = chrom => new ChromXs(chrom as MzValue);
                }
                else {
                    action = chrom => new ChromXs(chrom);
                }
            }
        }
    }
}
