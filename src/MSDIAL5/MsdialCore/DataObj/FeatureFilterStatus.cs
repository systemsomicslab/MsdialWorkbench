using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class FeatureFilterStatus {
        [Key(0)]
        public bool IsAbundanceFiltered { get; set; }
        [Key(1)]
        public bool IsRefMatchedFiltered { get; set; }
        [Key(2)]
        public bool IsSuggestedFiltered { get; set; }
        [Key(3)]
        public bool IsUnknownFiltered { get; set; }
        [Key(4)]
        public bool IsBlankFiltered { get; set; }
        [Key(5)]
        public bool IsMsmsContainedFiltered { get; set; }
        [Key(6)]
        public bool IsFragmentExistFiltered { get; set; }
        [Key(7)]
        public bool IsCommentFiltered { get; set; }
     
        public bool IsFiltered() {
            if (IsAbundanceFiltered) return true;
            if (IsRefMatchedFiltered) return true;
            if (IsSuggestedFiltered) return true;
            if (IsUnknownFiltered) return true;
            if (IsBlankFiltered) return true;
            if (IsFragmentExistFiltered) return true;
            if (IsMsmsContainedFiltered) return true;
            if (IsCommentFiltered) return true;
            return false;
        }
    }
}
