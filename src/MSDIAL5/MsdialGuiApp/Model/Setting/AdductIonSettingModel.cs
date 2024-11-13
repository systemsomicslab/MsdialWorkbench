using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting
{
    public class AdductIonSettingModel : BindableBase, IAdductIonSettingModel
    {
        public AdductIonSettingModel(ParameterBase parameter, ProcessOption process) {
            referenceParameter = parameter.ReferenceFileParam;
            if (referenceParameter.SearchedAdductIons.IsEmptyOrNull()) {
                referenceParameter.SearchedAdductIons = AdductResourceParser.GetAdductIonInformationList(parameter.IonMode);
            }
            AdductIons = new ObservableCollection<AdductIon>(referenceParameter.SearchedAdductIons);
            if (AdductIons.FirstOrDefault() is AdductIon firstAdduct) {
                firstAdduct.IsIncluded = true;
            }
            IsReadOnly = (process & ProcessOption.Identification) == 0;
        }

        public bool IsReadOnly { get; }

        private readonly ReferenceBaseParameter referenceParameter;

        public string UserDefinedAdductName {
            get => userDefinedAdductName;
            set => SetProperty(ref userDefinedAdductName, value);
        }
        private string userDefinedAdductName = string.Empty;

        public AdductIon UserDefinedAdduct => AdductIon.GetAdductIon(userDefinedAdductName);

        public ObservableCollection<AdductIon> AdductIons { get; }

        public void AddAdductIon() {
            var adduct = UserDefinedAdduct;
            if (adduct?.FormatCheck ?? false) {
                adduct.IsIncluded = true;
                AdductIons.Add(adduct);
            }
        }

        public void RemoveAdductIon(AdductIon adduct) {
            if (AdductIons.Contains(adduct)) {
                AdductIons.Remove(adduct);
            }
        }

        public bool TryCommit() {
            if (IsReadOnly) {
                return true;
            }
            if (AdductIons.All(ion => !ion.IsIncluded || (ion.AdductIonName != "[M+H]+" && ion.AdductIonName != "[M-H]-"))) {
                var request = new ErrorMessageBoxRequest
                {
                    Caption = "Error",
                    Content = "M + H or M - H must be included.",
                    ButtonType = MessageBoxButton.OK,
                };
                MessageBroker.Default.Publish(request);
                return false;
            }
            referenceParameter.SearchedAdductIons = AdductIons.ToList();
            return true;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }
            var referenceParameter = parameter.ReferenceFileParam;
            AdductIons.Clear();
            foreach (var ion in referenceParameter.SearchedAdductIons) {
                AdductIons.Add(ion);
            }
        }
    }
}
