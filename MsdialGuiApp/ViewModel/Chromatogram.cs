using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel
{
    public class Chromatogram
    {
        public List<PeakItem> Peaks { get; set; }
        public List<PeakItem> PeakArea { get; set; }
        public string Class { get; set; }
    }
}
