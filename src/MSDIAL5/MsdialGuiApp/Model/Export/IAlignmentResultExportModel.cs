using CompMs.App.Msdial.Model.DataObj;
using System;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IAlignmentResultExportModel {
        int CountExportFiles();
        void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification);
    }
}
