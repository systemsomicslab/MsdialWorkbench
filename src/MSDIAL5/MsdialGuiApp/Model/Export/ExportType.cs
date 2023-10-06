using CompMs.CommonMVVM;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    public sealed class ExportType : BindableBase {
        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            IsSelected = isSelected;
            Enabled = Observable.Return(true).ToReadOnlyReactivePropertySlim();
        }

        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, List<StatsValue> stats, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            IsSelected = isSelected;
            Stats = stats;
            Enabled = Observable.Return(true).ToReadOnlyReactivePropertySlim();
        }

        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, IReadOnlyReactiveProperty<bool> enabled, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            Enabled = enabled;
            IsSelected = isSelected;
        }

        public ExportType(string label, IQuantValueAccessor quantValueAccessor, string targetLabel, List<StatsValue> stats, IReadOnlyReactiveProperty<bool> enabled, bool isSelected = false) {
            Label = label;
            QuantValueAccessor = quantValueAccessor;
            TargetLabel = targetLabel;
            IsSelected = isSelected;
            Stats = stats;
            Enabled = enabled;
        }

        public string Label { get; }

        public IQuantValueAccessor QuantValueAccessor { get; }
        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public bool ShouldExport => IsSelected && Enabled.Value;

        public string TargetLabel { get; }

        public List<StatsValue> Stats { get; } = new List<StatsValue>();

        public IReadOnlyReactiveProperty<bool> Enabled { get; }
    }
}
