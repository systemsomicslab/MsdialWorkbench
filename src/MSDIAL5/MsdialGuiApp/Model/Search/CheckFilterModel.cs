using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Search
{
    /// <summary>
    /// An on/off filter: while checked, only the peaks that satisfy its condition stay in the table.
    /// </summary>
    public sealed class CheckFilterModel : BindableBase
    {
        public CheckFilterModel(string label, string description) {
            Label = label;
            Description = description;
        }

        public string Label { get; }
        public string Description { get; }

        public bool IsEnabled {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        private bool _isEnabled;

        public void Reset() => IsEnabled = false;
    }
}
