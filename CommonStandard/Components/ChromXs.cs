using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class ChromXs: IChromXs
    {
        [Key(0)]
        public ChromX RT { get; set; }
        [Key(1)]
        public ChromX RI { get; set; }
        [Key(2)]
        public ChromX Drift { get; set; }
        [Key(3)]
        public ChromX Mz { get; set; }
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
            switch (chromX.Type)
            {
                case ChromXType.RT:
                    RT = chromX;
                    break;
                case ChromXType.RI:
                    RI = chromX;
                    break;
                case ChromXType.Drift:
                    Drift = chromX;
                    break;
                case ChromXType.Mz:
                    Mz = chromX;
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
                        return RT.Value;
                    case ChromXType.RI:
                        return RI.Value;
                    case ChromXType.Drift:
                        return Drift.Value;
                    case ChromXType.Mz:
                        return Mz.Value;
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
                        return RT.Unit;
                    case ChromXType.RI:
                        return RI.Unit;
                    case ChromXType.Drift:
                        return Drift.Unit;
                    case ChromXType.Mz:
                        return Mz.Unit;
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
