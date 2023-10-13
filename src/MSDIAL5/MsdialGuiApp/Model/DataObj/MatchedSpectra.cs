using CompMs.App.Msdial.Model.Chart;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class MatchedSpectra
    {
        public MatchedSpectra(MsSpectrum referenceSpectrum, double[,] intensities, IReadOnlyList<AnalysisFileBeanModel> files)
        {
            ReferenceSpectrum = referenceSpectrum;
            Intensities = intensities;
            Files = files;
        }

        public IReadOnlyList<AnalysisFileBeanModel> Files { get; }
        public MsSpectrum ReferenceSpectrum { get; }
        public double[,] Intensities { get; } // File x Peak

        public void Export(Stream stream) {
            var reference = ReferenceSpectrum.Spectrum;
            using (var sw = new StreamWriter(stream, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true)) {
                sw.WriteLine($"m/z\tReference intensity\tSpectrum\tComment\t{string.Join("\t", Files.Select(f => f.AnalysisFileName))}");
                for (int i = 0; i < Intensities.GetLength(0); i++) {
                    sw.WriteLine($"{reference[i].Mass}\t{reference[i].Intensity}\t{reference[i].SpectrumComment}\t{reference[i].Comment}\t{string.Join("\t", Enumerable.Range(0, Intensities.GetLength(1)).Select(j => Intensities[i, j]))}");
                }
            }
        }
    }
}
