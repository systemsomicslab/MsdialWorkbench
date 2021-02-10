using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class UserDefinedAdductSetVM : ViewModelBase
    {
        public AdductIon AdductIon {
            get => adductIon;
            set => SetProperty(ref adductIon, value);
        }
        public string AdductString {
            get => adductString;
            set => SetProperty(ref adductString, value);
        }

        private AdductIon adductIon;
        private string adductString;

        public UserDefinedAdductSetVM() {
            AdductString = string.Empty;
            PropertyChanged += UpdateIon;
        }

        private void UpdateIon(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(AdductString)) {
                AdductIon = AdductIonParser.GetAdductIonBean(AdductString);
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand<Window> AddCommand {
            get => addCommand ?? (addCommand = new DelegateCommand<Window>(AddIon, CanAddIon));
        }
        private DelegateCommand<Window> addCommand;

        private bool CanAddIon(Window window) => AdductIon?.FormatCheck ?? false;

        private void AddIon(Window window) {
            window.DialogResult = true;
            window.Close();
        }
    }
}
