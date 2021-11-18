using CompMs.Common.Interfaces;
using System.Collections.ObjectModel;

namespace CompMs.App.SpectrumViewer.Model
{
    public interface IScanCollection
    {
        string Name { get; }
        ObservableCollection<IMSScanProperty> Scans { get; }
    }
}
