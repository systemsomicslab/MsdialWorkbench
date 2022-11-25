using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public sealed class AlignmentMspExporter : IAlignmentExporter
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly ParameterBase _parameter;

        public AlignmentMspExporter(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public void Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults) {
            foreach (var (spot, result) in spots.Zip(msdecResults, (s, r) => (s, r))) {
                SpectraExport.SaveSpectraTableAsNistFormat(stream, spot, result.Spectrum, _refer, _parameter);
            }
        }

        void IAlignmentExporter.Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults, IReadOnlyList<AnalysisFileBean> files, IMetadataAccessor metaFormatter, IQuantValueAccessor quantAccessor, IReadOnlyList<StatsValue> stats) {
            foreach (var (spot, result) in spots.Zip(msdecResults, (s, r) => (s, r))) {
                SpectraExport.SaveSpectraTableAsNistFormat(stream, spot, result.Spectrum, _refer, _parameter);
            }
        }
    }
}
