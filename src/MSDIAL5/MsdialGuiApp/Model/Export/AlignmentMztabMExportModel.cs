using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export;

internal sealed class AlignmentMztabMExportModel : BindableBase, IAlignmentResultExportModel
{
    public AlignmentMztabMExportModel(DataBaseStorage databaseStorage) {
        var exporter = new MztabFormatExporter(databaseStorage);
        
    }

    public bool IsSelected {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    private bool _isSelected = false;

    public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
        return IsSelected ? 1 : 0;
    }

    public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        throw new NotImplementedException();
    }
}
