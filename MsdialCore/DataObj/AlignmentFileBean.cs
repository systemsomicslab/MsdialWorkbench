using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentFileBean : IFileBean {
        [Key(0)]
        public int FileID { get; set; }
        [Key(1)]
        public string FilePath { get; set; } = string.Empty;
        [Key(2)]
        public string FileName { get; set; } = string.Empty;
        [Key(3)]
        public string SpectraFilePath { get; set; } = string.Empty;
        [Key(4)]
        public string EicFilePath { get; set; } = string.Empty;
    }
}
