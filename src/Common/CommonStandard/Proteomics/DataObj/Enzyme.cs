using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    [MessagePackObject]
    public class Enzyme {
        [Key(0)]
        public string Title { get; set; }
        [Key(1)]
        public string Description { get; set; }
        [Key(2)]
        public string CreateDate { get; set; }
        [Key(3)]
        public string LastModifiedDate { get; set; }
        [Key(4)]
        public string User { get; set; }
        [Key(5)]
        public List<string> SpecificityList { get; set; } = new List<string>();
        [Key(6)]
        public bool IsSelected { get; set; }
    }
}
