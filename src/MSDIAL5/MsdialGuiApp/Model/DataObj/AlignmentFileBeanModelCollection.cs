using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentFileBeanModelCollection : DisposableModelBase
    {
        private readonly IList<AlignmentFileBean> _originalFiles;
        private readonly IReadOnlyList<AnalysisFileBean> _analysisFiles;
        private readonly ObservableCollection<AlignmentFileBeanModel> _files;

        public AlignmentFileBeanModelCollection(IList<AlignmentFileBean> files, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            _originalFiles = files;
            _analysisFiles = analysisFiles;
            _files = new ObservableCollection<AlignmentFileBeanModel>(files.Select(f => new AlignmentFileBeanModel(f, _analysisFiles)));
            Files = new ReadOnlyObservableCollection<AlignmentFileBeanModel>(_files);
        }

        public ReadOnlyObservableCollection<AlignmentFileBeanModel> Files { get; }

        public void Add(AlignmentFileBean file) {
            _originalFiles.Add(file);
            _files.Add(new AlignmentFileBeanModel(file, _analysisFiles).AddTo(Disposables));
        }
    }
}
