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
        public RetentionTime RT { get; set; } = new RetentionTime(-1);

        [IgnoreMember]
        public RetentionIndex RI { get; set; } = new RetentionIndex(-1);

        [IgnoreMember]
        public DriftTime Drift { get; set; } = new DriftTime(-1);

        [IgnoreMember]
        public MzValue Mz { get; set; } = new MzValue(-1);

        [Key(4)]
        public ChromXType MainType { get; set; } = ChromXType.RT;
        public ChromXs () { }
        public ChromXs(double value, ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min) {
            switch (type) {
                case ChromXType.RT:
                    RT = new RetentionTime(value, unit);
                    break;
                case ChromXType.RI:
                    RI = new RetentionIndex(value, unit);
                    break;
                case ChromXType.Drift:
                    Drift = new DriftTime(value, unit);
                    break;
                case ChromXType.Mz:
                    Mz = new MzValue(value, unit);
                    break;
                default:
                    break;
            }
            MainType = type;
        }

        public ChromXs(IChromX chromX)
        {
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
   
        public ChromXs(RetentionTime retentionTime)
        {
            RT = retentionTime;
            MainType = retentionTime.Type;
        }
   
        public ChromXs(RetentionIndex retentionIndex)
        {
            RI = retentionIndex;
            MainType = retentionIndex.Type;
        }
   
        public ChromXs(DriftTime driftTime)
        {
            Drift = driftTime;
            MainType = driftTime.Type;
        }
   
        public ChromXs(MzValue mz)
        {
            Mz = mz;
            MainType = mz.Type;
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
