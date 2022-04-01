using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Search
{
    public interface ISpotFocus : INotifyPropertyChanged
    {
        string Label { get; }
        double Value { get; set; }
        void Focus();
    }
}
