using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Normalize
{
    internal class SplashProduct
    {
        public SplashProduct(string label, IEnumerable<StandardCompoundModel> lipids) {
            if (lipids is null) {
                throw new ArgumentNullException(nameof(lipids));
            }

            Label = label;
            var lipids_ = new ObservableCollection<StandardCompoundModel>(lipids);
            Lipids = new ReadOnlyObservableCollection<StandardCompoundModel>(lipids_);
        }

        public string Label { get; }

        public ReadOnlyObservableCollection<StandardCompoundModel> Lipids { get; }

        public IObservable<bool> CanNormalize(IReadOnlyList<AlignmentSpotProperty> spots) {
            return Lipids.ObserveElementPropertyChanged()
                .Select(_ => Observable.Defer(() => Observable.Return(Lipids.Any(lipid => lipid.IsRequiredFieldFilled(spots)))))
                .Switch();
        }

        public static SplashProduct BuildPublicProduct(XElement element) {
            return ToProduct(element, false);
        }

        public static SplashProduct BuildPrivateProduct(XElement element) {
            return ToProduct(element, true);
        }

        private static SplashProduct ToProduct(XElement element, bool isPrivate = false) {
            if (!isPrivate && element.Element("IsLabPrivateVersion")?.Value == "true") {
                return null;
            }
            if (isPrivate && element.Element("IsPublicVersion")?.Value == "true") {
                return null;
            }
            var rawLipids = element.Element("Lipids").Elements("Lipid");
            var lipids = rawLipids
                .Select(compound => ToCompound(compound, isPrivate))
                .Where(compound => !(compound is null));
            return new SplashProduct(element.Element("Label").Value, lipids);
        }

        private static StandardCompoundModel ToCompound(XElement element, bool isPrivate = false) {
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
