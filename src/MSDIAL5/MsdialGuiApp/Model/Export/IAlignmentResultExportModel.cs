using CompMs.App.Msdial.Model.DataObj;
using System;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentResultExportModel {
        int CountExportFiles(AlignmentFileBeanModel alignmentFile);
        void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification);
    }

    /// <summary>
    /// An export group that is told when an alignment export starts and ends, so that what it has to report is shown
    /// once every group has finished instead of holding up the groups after it.
    /// </summary>
    internal interface IAlignmentResultExportSession {
        /// <summary>Called before any group counts or exports its files.</summary>
        void BeginExport();

        /// <summary>Called after the last group has exported, or the export failed; the message to show, or null for none.</summary>
        Service.MessageRequest? EndExport();
    }
}
