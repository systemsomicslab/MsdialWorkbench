using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class Enzyme {
        public string Title { get; set; }
        public string Description { get; set; }
        public string CreateDate { get; set; }
        public string LastModifiedDate { get; set; }
        public string User { get; set; }
        public List<string> SpecificityList { get; set; } = new List<string>();
    }
}
