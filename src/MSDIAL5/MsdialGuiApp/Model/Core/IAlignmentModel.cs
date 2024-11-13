using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IAlignmentModel : IResultModel, INotifyPropertyChanged
    {
        AlignmentFileBeanModel AlignmentFile { get; }
        AlignmentResultContainer AlignmentResult { get; }
        AlignmentSpotSource AlignmentSpotSource { get; }
        Task SaveAsync();
    }
}
