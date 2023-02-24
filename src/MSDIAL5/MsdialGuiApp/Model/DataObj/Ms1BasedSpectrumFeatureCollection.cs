using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class Ms1BasedSpectrumFeatureCollection : BindableBase
    {
        public Ms1BasedSpectrumFeatureCollection(SpectrumFeatureCollection spectrumFeatures) {
            Items = new ObservableCollection<Ms1BasedSpectrumFeature>(spectrumFeatures.Items.Select(item => new Ms1BasedSpectrumFeature(item)));
        }

        public ObservableCollection<Ms1BasedSpectrumFeature> Items { get; }
    }
}
