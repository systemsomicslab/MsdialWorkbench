using CompMs.Common.DataObj.Property;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAdductIonSettingModel : INotifyPropertyChanged {
        bool IsReadOnly { get; }
        string UserDefinedAdductName { get; set; }
        AdductIon UserDefinedAdduct { get; }
        ObservableCollection<AdductIon> AdductIons { get; }
        void AddAdductIon();
        void RemoveAdductIon(AdductIon adduct);
    }
}
