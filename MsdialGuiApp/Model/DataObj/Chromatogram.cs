using CompMs.App.Msdial.ViewModel;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class Chromatogram
    {
        public List<PeakItem> Peaks { get; set; }
        public List<PeakItem> PeakArea { get; set; }
        public string Class { get; set; }
    }
}
