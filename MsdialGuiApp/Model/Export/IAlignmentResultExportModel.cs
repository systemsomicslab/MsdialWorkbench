using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentResultExportModel {
        int CountExportFiles();
        void Export(AlignmentFileBean alignmentFile, string exportDirectory, Action<string> notification);
    }
}
