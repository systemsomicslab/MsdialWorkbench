using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    // TODO: Implement functions for isotope tracking analysis.
    public sealed class AlignmentMatExporter : IAlignmentSpectraExporter
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly ParameterBase _parameter;

        public AlignmentMatExporter(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        void IAlignmentSpectraExporter.Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults) {
            foreach (var spot in spots) {
                SpectraExport.SaveSpectraTableAsMatFormat(stream, spot, msdecResults[spot.MasterAlignmentID].Spectrum, _refer, _parameter, isotopeTrackedLastSpot: null);
            }
        }
    }
}
