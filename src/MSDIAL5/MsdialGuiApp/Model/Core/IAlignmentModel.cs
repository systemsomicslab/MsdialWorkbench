using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IAlignmentModel : IResultModel, INotifyPropertyChanged
    {
        string DisplayLabel { get; set; }

        Task SaveAsync();
    }
}
