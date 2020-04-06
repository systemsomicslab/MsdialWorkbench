using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class Times: ITimes
    {
        public Time Absolute { get; set; }
        public Time Relative { get; set; }
        public bool IsAbsolute { get; set; } = true;

        public double Value { 
            get
            {
                return IsAbsolute ? Absolute.Value : Relative.Value;
            }
        }

        public TimeUnit Unit
        {
            get
            {
                return IsAbsolute ? Absolute.Unit : Relative.Unit;
            }
        }

        public TimeType Type
        {
            get
            {
                return IsAbsolute ? Absolute.Type : Relative.Type;
            }
        }

    }
}
