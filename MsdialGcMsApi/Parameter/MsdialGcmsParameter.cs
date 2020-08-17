using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialGcMsApi.Parameter {
    [MessagePackObject]
    public class MsdialGcmsParameter : ParameterBase {
        [Key(149)]
        public string RiDictionaryFilePath { get; set; } = string.Empty;
        [Key(150)]
        public RiCompoundType RiCompoundType { get; set; } = RiCompoundType.Alkanes;
        [Key(151)]
        public RetentionType RetentionType { get; set; } = RetentionType.RT;
        [Key(152)]
        public AlignmentIndexType AlignmentIndexType { get; set; } = AlignmentIndexType.RT;
        [Key(153)]
        public float RetentionIndexAlignmentTolerance { get; set; } = 20;

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();

            pStrings.Add("\r\n");
            pStrings.Add("# GCMS specific parameters");
            pStrings.Add(String.Join(": ", new string[] { "RI dictionary file path", RiDictionaryFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RI compound type", RiCompoundType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention type", RetentionType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Alignment index type", AlignmentIndexType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention index alignment tolerance", RetentionIndexAlignmentTolerance.ToString() }));

            return pStrings;
        }
    }
}
