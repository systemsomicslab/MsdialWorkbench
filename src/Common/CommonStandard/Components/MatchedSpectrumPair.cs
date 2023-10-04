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
                //sw.WriteLine("m/z\tReference intensity\tExperiment intensity\tSpectrum\tComment");
                sw.WriteLine("m/z\tReference intensity\tExperiment intensity\tSpectrum\tComment\tCarbon\tType");
                foreach (var pair in _pairs) {
                    //sw.WriteLine($"{pair.Reference.Mass}\t{pair.Reference.Intensity}\t{pair.Experiment.Intensity}\t{pair.Reference.SpectrumComment}\t{pair.Reference.Comment}");
                    if (pair.Reference.SpectrumComment.HasFlag(SpectrumComment.doublebond)) {
                        ParseComment_Temp(pair.Reference.Comment, out string carbon, out string type);
                        sw.WriteLine($"{pair.Reference.Mass}\t{pair.Reference.Intensity}\t{pair.Experiment.Intensity}\t{pair.Reference.SpectrumComment}\t{pair.Reference.Comment}\t{carbon}\t{type}");
                    }
                    else {
                        sw.WriteLine($"{pair.Reference.Mass}\t{pair.Reference.Intensity}\t{pair.Experiment.Intensity}\t{pair.Reference.SpectrumComment}\t{pair.Reference.Comment}\tnull\tnull");
                    }
                }
                //sw.WriteLine("Experiment m/z\tExperiment intensity\tReference m/z\tReference intensity");
                //foreach (var pair in _pairs) {
                //    sw.WriteLine($"{pair.Experiment.Mass}\t{pair.Experiment.Intensity}\t{pair.Reference.Intensity}\t{pair.Reference.Intensity}");
                //}
            }
        }

        public void ParseComment_Temp(string sentence, out string carbon, out string type) {
            carbon = "C";
            type = string.Empty;
            for (int i = 0; i < sentence.Length; i++) {
                if (sentence[i] == 'C') {
                    for (int j = i + 1; j < sentence.Length; j++) {
                        if (char.IsNumber(sentence[j])) {
                            carbon += sentence[j];
                        }
                        else {
                            if (sentence[j] == ',' || sentence[j] == '_') {
                                type = "Radical";
                            }
                            else if (sentence[j] == '+') {
                                type = "H gain";
                            }
                            else if (sentence[j] == '-') {
                                type = "H loss";
                            }
                            else {
                                break;
                            }
                            return;
                        }
                    }
                }
            }
        }
    }
}
