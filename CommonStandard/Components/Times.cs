using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class Times: ITimes
    {
        public Time RT { get; set; }
        public Time RI { get; set; }
        public Time Drift { get; set; }
        public TimeType MainType { get; set; } = TimeType.RT;
        public Times () { }
        public Times(Time time)
        {
            switch (time.Type)
            {
                case TimeType.RT:
                    RT = time;
                    break;
                case TimeType.RI:
                    RI = time;
                    break;
                case TimeType.Drift:
                    Drift = time;
                    break;
                default:
                    break;
            }
            MainType = time.Type;
        }

        public Time GetRepresentativeTime()
        {
            switch (MainType)
            {
                case TimeType.RT:
                    return RT;
                case TimeType.RI:
                    return RI;
                case TimeType.Drift:
                    return Drift;
                default:
                    return null;
            }
        }

        public double Value { 
            get
            {
                switch (MainType)
                {
                    case TimeType.RT:
                        return RT.Value;
                    case TimeType.RI:
                        return RI.Value;
                    case TimeType.Drift:
                        return Drift.Value;
                    default:
                        return -1;
                }
            }
        }

        public TimeUnit Unit
        {
            get
            {
                switch (MainType)
                {
                    case TimeType.RT:
                        return RT.Unit;
                    case TimeType.RI:
                        return RI.Unit;
                    case TimeType.Drift:
                        return Drift.Unit;
                    default:
                        return TimeUnit.None;
                }
            }
        }

        public TimeType Type
        {
            get
            {
                return MainType;
            }
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
