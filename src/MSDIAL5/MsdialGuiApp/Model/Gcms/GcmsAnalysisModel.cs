using CompMs.App.Msdial.Model.Core;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisModel : IAnalysisModel
    {
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add {
                throw new NotImplementedException();
            }

            remove {
                throw new NotImplementedException();
            }
        }

        void IResultModel.InvokeMsfinder() {
            throw new NotImplementedException();
        }

        Task IAnalysisModel.SaveAsync(CancellationToken token) {
            throw new NotImplementedException();
        }

        void IResultModel.SearchFragment() {
            throw new NotImplementedException();
        }
    }
}
