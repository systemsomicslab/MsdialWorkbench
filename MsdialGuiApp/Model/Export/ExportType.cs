using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportType : BindableBase {
        public ExportType(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, bool isSelected = false) {
            Label = label;
            MetadataAccessor = metadataAccessor;
            QuantValueAccessor = quantValueAccessor;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
        }

        public ExportType(string label, IMetadataAccessor metadataAccessor, IQuantValueAccessor quantValueAccessor, string filePrefix, List<StatsValue> stats, bool isSelected = false) {
            Label = label;
            MetadataAccessor = metadataAccessor;
            QuantValueAccessor = quantValueAccessor;
            FilePrefix = filePrefix;
            IsSelected = isSelected;
            Stats = stats;
        }

        public string Label { get; }

        public IMetadataAccessor MetadataAccessor { get; }

        public IQuantValueAccessor QuantValueAccessor { get; }
        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public string FilePrefix { get; }

        public List<StatsValue> Stats { get; } = new List<StatsValue>();
    }
}
