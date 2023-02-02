using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Rfx.Riken.OsakaUniv.IonMobility
{
    [MessagePackObject]
    public class Spectra
    {
        [Key(0)]
        /// Index: OriginalIndex of RAW_SPECTRUM
        public Dictionary<int, Spectrum> SpectrumDic { get; set; } = new Dictionary<int, Spectrum>();
        public Spectra() { }
    }


    [MessagePackObject]
    public class Spectrum
    {    
        [Key(0)]
        public int OriginalIndex { get; set; }
        [Key(1)]
        public List<double[]> PeakList { get; set; } = new List<double[]>();

        public List<Peak> GetPeaks()
        {
            var res = new List<Peak>();
            foreach (var p in PeakList)
            {
                res.Add(new Peak() { Mz = p[0], Intensity = p[1] });
            }
            return res;
        }
    }    
}
