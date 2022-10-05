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
    internal sealed class ProjectBaseParameterModel : BindableBase
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

            _fileClasses = new ObservableCollection<string>(Enumerable.Union(_classnameToOrder.Keys, _classnameToColorBytes.Keys));
            FileClasses = new ReadOnlyObservableCollection<string>(_fileClasses);
        }

        public ReadOnlyDictionary<int, AnalysisFileType> FileIdToAnalysisFileType => new ReadOnlyDictionary<int, AnalysisFileType>(_fileIdToAnalysisFileType);
        private readonly Dictionary<int, AnalysisFileType> _fileIdToAnalysisFileType;

        public ReadOnlyDictionary<int, string> FileIdToClassName => new ReadOnlyDictionary<int, string>(_fileIdToClassName);
        private readonly Dictionary<int, string> _fileIdToClassName;

        public ReadOnlyDictionary<string, int> ClassnameToOrder => new ReadOnlyDictionary<string, int>(_classnameToOrder);
        private readonly Dictionary<string, int> _classnameToOrder;

        public ReadOnlyDictionary<string, List<byte>> ClassnameToColorBytes => new ReadOnlyDictionary<string, List<byte>>(_classnameToColorBytes);
        private readonly Dictionary<string, List<byte>> _classnameToColorBytes;

        public ReadOnlyObservableCollection<string> FileClasses { get; }
        private readonly ObservableCollection<string> _fileClasses;

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

            var newClasses = files.Select(analysisFile => analysisFile.AnalysisFileClass).ToHashSet();
            {
                foreach (var fileClass in _fileClasses.Where(fileClass => !newClasses.Contains(fileClass)).ToArray()) {
                    _fileClasses.Remove(fileClass);
                }
                foreach (var newClass in newClasses.Where(newClass => !_fileClasses.Contains(newClass))) {
                    _fileClasses.Add(newClass);
                }
            }

            {
                foreach (var fileClass in _classnameToOrder.Keys.Where(key => !newClasses.Contains(key)).ToArray()) {
                    _classnameToOrder.Remove(fileClass);
                }
                var order = _classnameToOrder.Values.Max() + 1;
                foreach (var fileClass in newClasses) {
                    if (!_classnameToOrder.ContainsKey(fileClass)) {
                        _classnameToOrder[fileClass] = order++;
                    }
                }
                OnPropertyChanged(nameof(ClassnameToOrder));
            }

            {
                foreach (var fileClass in _classnameToColorBytes.Keys.Where(key => !newClasses.Contains(key)).ToArray()) {
                    _classnameToColorBytes.Remove(fileClass);
                }
                var colors = _classnameToColorBytes.Values.Select(color => Color.FromArgb(color[3], color[0], color[1], color[2])).ToHashSet();
                var idx = 0;
                foreach (var fileClass in newClasses) {
                    if (!_classnameToColorBytes.ContainsKey(fileClass)) {
                        while (idx < ChartBrushes.SolidColorBrushList.Count && colors.Contains(ChartBrushes.SolidColorBrushList[idx].Color)) {
                            idx++;
                        }
                        var color = idx < ChartBrushes.SolidColorBrushList.Count ? ChartBrushes.SolidColorBrushList[idx++].Color : Colors.Gray;
                        _classnameToColorBytes[fileClass] = new List<byte> { color.R, color.G, color.B, color.A, };
                    }
                }
                OnPropertyChanged(nameof(ClassnameToColorBytes));
            }

            var classNumAve = files.GroupBy(analysisfile => analysisfile.AnalysisFileType)
                                   .Average(group => group.Count());
            IsBoxPlotForAlignmentResult = classNumAve > 4;
        }

        public void SetClassOrderProperties(IReadOnlyDictionary<string, int> classnameToOrder) {
            foreach (var name in _classnameToOrder.Keys.ToArray()) {
                if (!classnameToOrder.ContainsKey(name)) {
                    _classnameToOrder.Remove(name);
                }
            }
            foreach (var kvp in classnameToOrder) {
                _classnameToOrder[kvp.Key] = kvp.Value;
            }
            OnPropertyChanged(nameof(ClassnameToOrder));
        }

        public void SetClassColorProperties(IReadOnlyDictionary<string, Color> classnameToColor) {
            foreach (var name in _classnameToColorBytes.Keys.ToArray()) {
                if (!classnameToColor.ContainsKey(name)) {
                    _classnameToColorBytes.Remove(name);
                }
            }
            foreach (var kvp in classnameToColor) {
                _classnameToColorBytes[kvp.Key] = new List<byte> { kvp.Value.R, kvp.Value.G, kvp.Value.B, kvp.Value.A };
            }
            OnPropertyChanged(nameof(ClassnameToColorBytes));
        }
    }
}
