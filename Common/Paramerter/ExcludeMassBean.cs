using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [MessagePackObject]
    public class ExcludeMassBean
    {
        private float? excludedMass;
        private float? massTolerance;

        [Key(0)]
        public float? ExcludedMass
        {
            get { return excludedMass; }
            set { excludedMass = value; }
        }

        [Key(1)]
        public float? MassTolerance
        {
            get { return massTolerance; }
            set { massTolerance = value; }
        }
    }
}
