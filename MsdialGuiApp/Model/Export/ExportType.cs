using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportType : BindableBase {
        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            IsSelected = isSelected;
        }

        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, List<StatsValue> stats, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            IsSelected = isSelected;
            Stats = stats;
        }

        public string Label { get; }

        public IQuantValueAccessor QuantValueAccessor { get; }
        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public string TargetLabel { get; }

        public List<StatsValue> Stats { get; } = new List<StatsValue>();
    }
}
