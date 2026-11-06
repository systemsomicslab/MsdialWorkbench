using CompMs.Common;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    /// <summary>
    /// MassBank records of alignment spots, with the project metadata, as the GUI's alignment MassBank export writes them.
    /// </summary>
    /// <remarks>
    /// Create one per export, so that DATE and the accession date are the export time. Check
    /// <see cref="ValidateMetadata"/> before exporting and refuse to export while it reports anything.
    /// </remarks>
    internal sealed class AlignmentMassBankRecordExporter : IAlignmentSpectraExporter
    {
        private readonly MassBankRecordHandler _handler;

        public AlignmentMassBankRecordExporter(ProjectBaseParameter project, string contributorId) {
            if (project is null) {
                throw new ArgumentNullException(nameof(project));
            }
            _handler = new MassBankRecordHandler(project.IonMode, project.InstrumentType ?? string.Empty, MassBankSplash.Compute)
            {
                Authors = project.Authors ?? string.Empty,
                License = project.License ?? string.Empty,
                Instrument = project.Instrument ?? string.Empty,
                CollisionEnergy = project.CollisionEnergy ?? string.Empty,
                Comment = project.Comment ?? string.Empty,
                ContributorIdentifier = contributorId ?? string.Empty,
                MsdialVersion = MsdialBuildIdentity.FullIdentity,
            };
        }

        /// <summary>The problems with the project metadata or contributor ID; empty when records can be written.</summary>
        public IReadOnlyList<string> ValidateMetadata() => _handler.ValidateMetadata();

        public string CreateExportFileName(AlignmentSpotProperty spot) {
            return _handler.GetAccession(spot) + ".txt";
        }

        /// <summary>Writes the record of <paramref name="spot"/>, or nothing, returning false with the reason.</summary>
        public bool TryExport(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult, MassBankAnnotationLevel annotationLevel, out string reason) {
            return _handler.TryWriteRecord(stream, spot, spot, msdecResult, spot, annotationLevel, out reason);
        }

        /// <summary>
        /// Writes the record of <paramref name="spot"/>, or nothing for a spot that cannot become a valid record, so that
        /// one such spot does not stop the export of the others. <see cref="TryExport"/> also says why.
        /// </summary>
        void IAlignmentSpectraExporter.Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult) {
            _handler.TryWriteRecord(stream, spot, spot, msdecResult, spot, out _);
        }
    }
}
