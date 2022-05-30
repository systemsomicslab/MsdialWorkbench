using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromXs: IChromXs
    {
        [Key(0)]
        public ChromX InnerRT { get; set; } = new RetentionTime(-1);
        [Key(1)]
        public ChromX InnerRI { get; set; } = new RetentionIndex(-1);
        [Key(2)]
        public ChromX InnerDrift { get; set; } = new DriftTime(-1);
        [Key(3)]
        public ChromX InnerMz { get; set; } = new MzValue(-1);

        [IgnoreMember]
        public RetentionTime RT {
            get => (RetentionTime)InnerRT;
            set => InnerRT = value;
        }
        [IgnoreMember]
        public RetentionIndex RI {
            get => (RetentionIndex)InnerRI;
            set => InnerRI = value;
        }
        [IgnoreMember]
        public DriftTime Drift {
            get => (DriftTime)InnerDrift;
            set => InnerDrift = value;
        }
        [IgnoreMember]
        public MzValue Mz {
            get => (MzValue)InnerMz;
            set => InnerMz = value;
        }

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

        public ChromXs(ChromX chromX)
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
   
        public ChromX GetRepresentativeXAxis()
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
    }
}
