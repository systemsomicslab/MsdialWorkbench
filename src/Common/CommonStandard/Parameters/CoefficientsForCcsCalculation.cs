using CompMs.Common.DataObj;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Parameter {
    [MessagePackObject]
    public class CoefficientsForCcsCalculation {
        public CoefficientsForCcsCalculation() { }

        public CoefficientsForCcsCalculation(RawCalibrationInfo calinfo) {
            if (calinfo == null) {
                return;
            }
            IsAgilentIM = calinfo.IsAgilentIM;
            IsBrukerIM = calinfo.IsBrukerIM;
            IsWatersIM = calinfo.IsWatersIM;
            AgilentBeta = calinfo.AgilentBeta;
            AgilentTFix = calinfo.AgilentTFix;
            WatersCoefficient = calinfo.WatersCoefficient;
            WatersT0 = calinfo.WatersT0;
            WatersExponent = calinfo.WatersExponent;
        }

        [Key(0)]
        public bool IsAgilentIM { get; set; } = false;
        [Key(1)]
        public bool IsBrukerIM { get; set; } = false;
        [Key(2)]
        public bool IsWatersIM { get; set; } = false;

        [Key(3)]
        public double AgilentBeta { get; set; } = -1.0;
        [Key(4)]
        public double AgilentTFix { get; set; } = -1.0;
        [Key(5)]
        public double WatersCoefficient { get; set; } = -1.0;
        [Key(6)]
        public double WatersT0 { get; set; } = -1.0;
        [Key(7)]
        public double WatersExponent { get; set; } = -1.0;
    }
}
