using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.BinVestigate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.BinVestigateRestApiConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var peaks = new List<Peak>() {
                new Peak() { Mz = 100, Intensity = 24444 },  new Peak() { Mz = 116, Intensity = 395401 },  new Peak() { Mz = 147, Intensity = 78930 }
            };
            var minSimilarity = 700.0;
            var kovatsWindows = 1000.0;
            var kovatsRI = 244189;

            var similaritySearch = new BinVestigateRestProtocol().SimilaritySearch(kovatsRI, kovatsWindows, peaks, minSimilarity);
            var binBaseDiagnostics = new BinVestigateRestProtocol().GetBinBaseDiagnosis(2);
            var binBaseQuant = new BinVestigateRestProtocol().GetBinBaseQuantStatisticsResults(2);
        }
    }
}
