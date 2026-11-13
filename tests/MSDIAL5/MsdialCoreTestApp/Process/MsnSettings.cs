using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CompMs.App.MsdialConsole.Process.MoleculerNetworking
{
    /// <summary>The method file's networking settings as the kernel layer reads them.</summary>
    internal static class MsnSettings
    {
        private static readonly ConditionalWeakTable<MolecularSpectrumNetworkingBaseParameter, Func<IReadOnlyList<SpectrumPeak>, double, List<SpectrumPeak>>> Preprocessors
            = new ConditionalWeakTable<MolecularSpectrumNetworkingBaseParameter, Func<IReadOnlyList<SpectrumPeak>, double, List<SpectrumPeak>>>();

        public static MolecularNetworkingQuery ToQuery(MolecularSpectrumNetworkingBaseParameter parameter) {
            return new MolecularNetworkingQuery {
                MsmsSimilarityCalc = parameter.MsmsSimilarityCalc,
                MassTolerance = parameter.MnMassTolerance,
                AbsoluteAbundanceCutOff = parameter.MnAbsoluteAbundanceCutOff,
                RelativeAbundanceCutOff = parameter.MnRelativeAbundanceCutOff,
                SpectrumSimilarityCutOff = parameter.MnSpectrumSimilarityCutOff,
                MinimumPeakMatch = parameter.MinimumPeakMatch,
                MaxEdgeNumberPerNode = parameter.MaxEdgeNumberPerNode,
                MaxPrecursorDifference = parameter.MaxPrecursorDifference,
                MaxPrecursorDifferenceAsPercent = parameter.MaxPrecursorDifferenceAsPercent,
                PrecursorWindow = parameter.MnPrecursorWindow,
                IntensityPower = parameter.MnIntensityPower,
            };
        }

        /// <summary>
        /// The preprocessing of the method file's kernel. For the legacy kernels it is
        /// GetProcessedSpectrum with the method file's abundance cutoffs, as before.
        /// </summary>
        public static List<SpectrumPeak> Preprocess(MolecularSpectrumNetworkingBaseParameter parameter, IReadOnlyList<SpectrumPeak> peaks, double precursorMz) {
            var preprocess = Preprocessors.GetValue(parameter,
                p => MsmsSimilarityKernelFactory.CreatePreprocessor(p.MsmsSimilarityCalc, ToQuery(p)));
            return preprocess(peaks, precursorMz);
        }
    }
}
