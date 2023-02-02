using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Search
{
    public interface ISpotFocus : INotifyPropertyChanged
    {
        string Label { get; }
        bool IsItalic { get; }
        double Value { get; set; }
        string Format { get; }
        void Focus();
    }
}
