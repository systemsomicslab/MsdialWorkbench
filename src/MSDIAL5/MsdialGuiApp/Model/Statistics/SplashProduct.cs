using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class SplashProduct : BindableBase
    {
        public SplashProduct(string label, IEnumerable<StandardCompoundModel> lipids) {
            if (lipids is null) {
                throw new ArgumentNullException(nameof(lipids));
            }

            Label = label;
            Lipids = new ObservableCollection<StandardCompoundModel>(lipids);
        }

        public string Label { get; }

        public ObservableCollection<StandardCompoundModel> Lipids { get; }

        public StandardCompoundModel? SelectedLipid {
            get => _selectedLipid;
            set => SetProperty(ref _selectedLipid, value);
        }
        private StandardCompoundModel? _selectedLipid;

        public void AddLast() {
            var compound = new StandardCompound
            {
                Concentration = 1d,
                DilutionRate = 1d,
                PeakID = -1,
                TargetClass = StandardCompound.AnyOthers,
            };
            Lipids.Add(new StandardCompoundModel(compound));
        }

        public void Delete() {
            if (SelectedLipid is null) {
                return;
            }
            var idx = Lipids.IndexOf(SelectedLipid);
            if (idx < 0) {
                return;
            }
            Lipids.RemoveAt(idx);
            SelectedLipid = Lipids.ElementAtOrDefault(idx);
        }

        public IObservable<bool> CanNormalize(IReadOnlyList<AlignmentSpotProperty> spots) {
            return Lipids.ObserveElementPropertyChanged().ToUnit()
                .StartWith(Unit.Default)
                .Select(_ => Lipids.Any(lipid => lipid.IsRequiredFieldFilled(spots)));
        }

        public static SplashProduct? BuildPublicProduct(XElement element) {
            return ToProduct(element, false);
        }

        public static SplashProduct? BuildPrivateProduct(XElement element) {
            return ToProduct(element, true);
        }

        private static SplashProduct? ToProduct(XElement element, bool isPrivate = false) {
            if (!isPrivate && element.Element("IsLabPrivateVersion")?.Value == "true") {
                return null;
            }
            if (isPrivate && element.Element("IsPublicVersion")?.Value == "true") {
                return null;
            }
            IEnumerable<XElement> rawLipids = element.Element("Lipids").Elements("Lipid");
            IEnumerable<StandardCompoundModel> lipids = rawLipids
                .Select(compound => ToCompound(compound, isPrivate))
                .OfType<StandardCompoundModel>();
            return new SplashProduct(element.Element("Label").Value, lipids);
        }

        private static StandardCompoundModel? ToCompound(XElement element, bool isPrivate = false) {
            if (!isPrivate && element.Element("IsLabPrivateVersion")?.Value == "true") {
                return null;
            }
            if (isPrivate && element.Element("IsPublicVersion")?.Value == "true") {
                return null;
            }
            var compound = new StandardCompound
            {
                StandardName = element.Element("StandardName").Value,
                Concentration = double.TryParse(element.Element("Concentration").Value, out var conc) ? conc : 0d,
                DilutionRate = double.TryParse(element.Element("DilutionRate").Value, out var rate) ? rate : 0d,
                MolecularWeight = double.TryParse(element.Element("MolecularWeight").Value, out var weight) ? weight : 0d,
                PeakID = int.TryParse(element.Element("PeakID").Value, out var id) ? id : -1,
                TargetClass = element.Element("TargetClass").Value,
            };
            return new StandardCompoundModel(compound);
        }
    }
}
