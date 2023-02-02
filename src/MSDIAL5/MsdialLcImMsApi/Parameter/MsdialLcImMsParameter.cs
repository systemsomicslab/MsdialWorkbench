using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Parameter {
    [MessagePackObject]
    public class MsdialLcImMsParameter : MsdialLcmsParameter {
        public MsdialLcImMsParameter(bool isLabUseOnly) : base(isLabUseOnly) { this.MachineCategory = MachineCategory.LCIMMS; }

        [SerializationConstructor]
        public MsdialLcImMsParameter() : this(isLabUseOnly: false) { }

        [Key(150)]
        public float DriftTimeBegin { get; set; } = 0;
        [Key(151)]
        public float DriftTimeEnd { get; set; } = 2000;
        [Key(152)]
        public float AccumulatedRtRange { get; set; } = 0.2F;
        [Key(153)]
        public bool IsAccumulateMS2Spectra { get; set; } = false;
        [Key(154)]
        public IonMobilityType IonMobilityType { get; set; } = IonMobilityType.Tims;
        [Key(155)]
        public bool IsAllCalibrantDataImported { get; set; } = false;

        [Key(156)]
        public float DriftTimeAlignmentTolerance { get; set; } = 0.02F; // msec
        [Key(157)]
        public float DriftTimeAlignmentFactor { get; set; } = 0.5F;


        [Key(158)]
        public Dictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients { get; set; } = new Dictionary<int, CoefficientsForCcsCalculation>();

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();

            pStrings.Add("\r\n");
            pStrings.Add("# IMMS specific parameters");
            pStrings.Add(String.Join(": ", new string[] { "Drift time begin", DriftTimeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time end", DriftTimeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accumulated RT ragne", AccumulatedRtRange.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accumulate MS2 spectra", IsAccumulateMS2Spectra.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time alignment tolerance", DriftTimeAlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time alignment factor", DriftTimeAlignmentFactor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Ion mobility type", IonMobilityType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "All calibrant data imported", IsAllCalibrantDataImported.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# File ID CCS coefficients");
            foreach (var item in FileID2CcsCoefficients) {

                pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, String.Join(",", new string[] {
                    "Agilent IM=" + item.Value.IsAgilentIM, "Bruker IM=" + item.Value.IsBrukerIM, "Waters IM=" + item.Value.IsWatersIM,
                    "Agilent Beta=" + item.Value.AgilentBeta, "Agilent TFix=" + item.Value.AgilentTFix,
                    "Waters coefficient=" + item.Value.WatersCoefficient, "Waters T0=" + item.Value.WatersT0, "Waters exponent=" + item.Value.WatersExponent 
                }) }));
            }
            return pStrings;
        }
    }
}
