using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv {
    public class LipoqualityQuantTree : ObservableCollection<LipoqualityQuantTree> {
        public string ClassName { get; set; }
        public double StudyCount { get; set; }
        public double Intensity { get; set; }
        public double Error { get; set; }
        public string Description { get; set; }
        public LipoqualityQuantTree SubClass { get; set; }
    }
}
