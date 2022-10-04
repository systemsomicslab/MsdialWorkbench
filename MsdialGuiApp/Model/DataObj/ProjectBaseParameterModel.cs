using CompMs.App.Msdial.Common;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal class ProjectBaseParameterModel : BindableBase
    {
        private readonly ProjectBaseParameter _projectParameter;

        public ProjectBaseParameterModel(ProjectBaseParameter projectParameter) {
            _projectParameter = projectParameter ?? throw new ArgumentNullException(nameof(projectParameter));

            if (_projectParameter.FileID_AnalysisFileType is null) {
                _projectParameter.FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>();
            }
            _fileIdToAnalysisFileType = _projectParameter.FileID_AnalysisFileType;

            if (_projectParameter.FileID_ClassName is null) {
                _projectParameter.FileID_ClassName = new Dictionary<int, string>();
            }
            _fileIdToClassName = _projectParameter.FileID_ClassName;

            if (_projectParameter.ClassnameToOrder is null) {
                _projectParameter.ClassnameToOrder = new Dictionary<string, int>();
            }
            _classnameToOrder = _projectParameter.ClassnameToOrder;

            if (_projectParameter.ClassnameToColorBytes is null) {
                _projectParameter.ClassnameToColorBytes = new Dictionary<string, List<byte>>();
            }
            _classnameToColorBytes = _projectParameter.ClassnameToColorBytes;
        }

        public ReadOnlyDictionary<int, AnalysisFileType> FileIdToAnalysisFileType => new ReadOnlyDictionary<int, AnalysisFileType>(_fileIdToAnalysisFileType);
        private readonly Dictionary<int, AnalysisFileType> _fileIdToAnalysisFileType;

        public ReadOnlyDictionary<int, string> FileIdToClassName => new ReadOnlyDictionary<int, string>(_fileIdToClassName);
        private readonly Dictionary<int, string> _fileIdToClassName;

        public ReadOnlyDictionary<string, int> ClassnameToOrder => new ReadOnlyDictionary<string, int>(_classnameToOrder);
        private readonly Dictionary<string, int> _classnameToOrder;

        public ReadOnlyDictionary<string, List<byte>> ClassnameToColorBytes => new ReadOnlyDictionary<string, List<byte>>(_classnameToColorBytes);
        private readonly Dictionary<string, List<byte>> _classnameToColorBytes;

        public bool IsBoxPlotForAlignmentResult {
            get => _isBoxPlotForAlignmentResult;
            private set => SetProperty(ref _isBoxPlotForAlignmentResult, value);
        }
        private bool _isBoxPlotForAlignmentResult;

        public void SetFileDependentProperties(IReadOnlyCollection<AnalysisFileBean> files) {
            _fileIdToAnalysisFileType.Clear();
            foreach (var analysisfile in files) {
                _fileIdToAnalysisFileType[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileType;
            }
            OnPropertyChanged(nameof(FileIdToAnalysisFileType));

            _fileIdToClassName.Clear();
            foreach (var analysisfile in files) {
                _fileIdToClassName[analysisfile.AnalysisFileId] = analysisfile.AnalysisFileClass;
            }
            OnPropertyChanged(nameof(FileIdToClassName));

            _classnameToOrder.Clear();
            foreach (var (classId, idx) in files.Select(analysisfile => analysisfile.AnalysisFileClass).Distinct().WithIndex()) {
                _classnameToOrder[classId] = idx;
            }
            OnPropertyChanged(nameof(ClassnameToOrder));

            _classnameToColorBytes.Clear();
            foreach (var (classId, idx) in files.Select(analysisfile => analysisfile.AnalysisFileClass).Distinct().WithIndex()) {
                var brush = ChartBrushes.SolidColorBrushList[idx];
                _classnameToColorBytes[classId] = new List<byte> { brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A };
            }
            OnPropertyChanged(nameof(ClassnameToColorBytes));

            var classNumAve = files.GroupBy(analysisfile => analysisfile.AnalysisFileType)
                                   .Average(group => group.Count());
            IsBoxPlotForAlignmentResult = classNumAve > 4;
        }

        public void SetClassOrderProperties(IReadOnlyDictionary<string, int> classnameToOrder) {
            _classnameToOrder.Clear();
            foreach (var kvp in classnameToOrder) {
                _classnameToOrder[kvp.Key] = kvp.Value;
            }
            OnPropertyChanged(nameof(ClassnameToOrder));
        }

        public void SetClassColorProperties(IReadOnlyDictionary<string, Color> classnameToColor) {
            _classnameToColorBytes.Clear();
            foreach (var kvp in classnameToColor) {
                _classnameToColorBytes[kvp.Key] = new List<byte> { kvp.Value.R, kvp.Value.G, kvp.Value.B, kvp.Value.A };
            }
            OnPropertyChanged(nameof(ClassnameToColorBytes));
        }
    }
}
