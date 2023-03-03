using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentFileBeanModelCollection : BindableBase
    {
        private readonly IList<AlignmentFileBean> _originalFiles;
        private readonly ObservableCollection<AlignmentFileBeanModel> _files;

        public AlignmentFileBeanModelCollection(IList<AlignmentFileBean> files)
        {
            _originalFiles = files;
            _files = new ObservableCollection<AlignmentFileBeanModel>(files.Select(f => new AlignmentFileBeanModel(f)));
            Files = new ReadOnlyObservableCollection<AlignmentFileBeanModel>(_files);
        }

        public ReadOnlyObservableCollection<AlignmentFileBeanModel> Files { get; }

        public void Add(AlignmentFileBean file) {
            _originalFiles.Add(file);
            _files.Add(new AlignmentFileBeanModel(file));
        }
    }
}
