using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Components
{
    public sealed class MatchedSpectrumPair
    {
        private readonly List<MatchedSpectrumPeakPair> _pairs;

        public MatchedSpectrumPair(List<MatchedSpectrumPeakPair> pairs) {
            _pairs = pairs;
        }

        public ReadOnlyCollection<SpectrumPeak> Experiment => _pairs.Select(p => p.Experiment).ToList().AsReadOnly();
        public ReadOnlyCollection<SpectrumPeak> Reference => _pairs.Select(p => p.Reference).ToList().AsReadOnly();

        public void Save(Stream stream) {
            using (var sw = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true)) {
                sw.WriteLine("m/z\tReference intensity\tExperiment intensity\tSpectrum\tComment");
                foreach (var pair in _pairs) {
                    sw.WriteLine($"{pair.Reference.Mass}\t{pair.Reference.Intensity}\t{pair.Experiment.Intensity}\t{pair.Reference.SpectrumComment}\t{pair.Reference.Comment}");
                }
                //sw.WriteLine("Experiment m/z\tExperiment intensity\tReference m/z\tReference intensity");
                //foreach (var pair in _pairs) {
                //    sw.WriteLine($"{pair.Experiment.Mass}\t{pair.Experiment.Intensity}\t{pair.Reference.Intensity}\t{pair.Reference.Intensity}");
                //}
            }
        }
    }
}
