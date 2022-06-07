using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IAlignmentModel : INotifyPropertyChanged
    {
        string DisplayLabel { get; set; }

        Task SaveAsync();

    }
}
