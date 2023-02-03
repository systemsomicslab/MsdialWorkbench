using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisModel : IAnalysisModel
    {
        ObservableCollection<ChromatogramPeakFeatureModel> IAnalysisModel.Ms1Peaks => throw new NotImplementedException();

        IReadOnlyReactiveProperty<ChromatogramPeakFeatureModel> IAnalysisModel.Target => throw new NotImplementedException();

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
