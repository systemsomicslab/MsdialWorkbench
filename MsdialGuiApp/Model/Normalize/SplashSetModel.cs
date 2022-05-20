using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Normalize
{
    class SplashSetModel : BindableBase
    {
        public SplashSetModel(AlignmentResultContainer container, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMessageBroker broker) {
            this.container = container;
            spots = container.AlignmentSpotProperties;
            this.refer = refer;
            this.parameter = parameter;
            this.evaluator = evaluator;
            _broker = broker;
            var targetMetabolites = LipidomicsConverter.GetLipidClasses();
            targetMetabolites.Add("Any others");
            TargetMetabolites = targetMetabolites.AsReadOnly();

            var isPrivate = Properties.Resources.VERSION.EndsWith("-tada")
                || Properties.Resources.VERSION.EndsWith("-alpha")
                || Properties.Resources.VERSION.EndsWith("-beta")
                || Properties.Resources.VERSION.EndsWith("-dev");
            SplashProducts = new ObservableCollection<SplashProduct>(isPrivate ? GetPrivateSplashResource() : GetPublicSplashResource());
            if (parameter.AdvancedProcessOptionBaseParam.StandardCompounds != null) {
                var product = new SplashProduct
                {
                    Label = "Previous compounds",
                    Lipids = new ObservableCollection<StandardCompound>(parameter.AdvancedProcessOptionBaseParam.StandardCompounds),
                };
                SplashProducts.Add(product);
                SplashProduct = product;
            }
            else {
                SplashProduct = SplashProducts.FirstOrDefault();
            }

            OutputUnits = new ObservableCollection<IonAbundance>() {
                new IonAbundance(IonAbundanceUnit.nmol_per_microL_plasma, "nmol/μL plasma"),
                new IonAbundance(IonAbundanceUnit.pmol_per_microL_plasma, "pmol/μL plasma"),
                new IonAbundance(IonAbundanceUnit.fmol_per_microL_plasma, "fmol/μL plasma"),
                new IonAbundance(IonAbundanceUnit.nmol_per_mg_tissue, "nmol/mg tissue"),
                new IonAbundance(IonAbundanceUnit.pmol_per_mg_tissue, "pmol/mg tissue"),
                new IonAbundance(IonAbundanceUnit.fmol_per_mg_tissue, "fmol/mg tissue"),
                new IonAbundance(IonAbundanceUnit.nmol_per_10E6_cells, "nmol/10^6 cells"),
                new IonAbundance(IonAbundanceUnit.pmol_per_10E6_cells, "pmol/10^6 cells"),
                new IonAbundance(IonAbundanceUnit.fmol_per_10E6_cells, "fmol/10^6 cells") };
            OutputUnit = OutputUnits.FirstOrDefault();
        }

        private readonly AlignmentResultContainer container;
        private readonly IReadOnlyList<AlignmentSpotProperty> spots;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;
        private readonly ParameterBase parameter;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;
        private readonly IMessageBroker _broker;

        public ObservableCollection<StandardCompound> StandardCompounds => SplashProduct.Lipids;

        public ObservableCollection<SplashProduct> SplashProducts { get; }

        public SplashProduct SplashProduct {
            get => splashProduct;
            set => SetProperty(ref splashProduct, value);
        }

        private SplashProduct splashProduct;

        public ObservableCollection<IonAbundance> OutputUnits { get; }

        public IonAbundance OutputUnit {
            get => outputUnit;
            set => SetProperty(ref outputUnit, value);
        }

        private IonAbundance outputUnit;

        public ReadOnlyCollection<string> TargetMetabolites { get; }

        public void Find() {
            foreach (var compound in StandardCompounds) {
                foreach (var spot in spots) {
                    if (!string.IsNullOrEmpty(spot?.Name) && spot.Name.Contains(compound.StandardName)) {
                        compound.PeakID = spot.MasterAlignmentID;
                        break;
                    }
                }
            }
        }

        public void Normalize() {
            // TODO: For ion mobility, it need to flatten spots and check compound PeakID.
            var task = TaskNotification.Start("Normalize..");
            var publisher = new TaskProgressPublisher(_broker, task);
            using (publisher.Start()) {
                var compounds = StandardCompounds.Where(IsRequiredFieldFilled).ToList();
                if (compounds.Count == 0) {
                    MessageBox.Show("Please fill the required fields for normalization", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                parameter.StandardCompounds = compounds;
                var unit = OutputUnit.Unit;
                Normalization.SplashNormalize(spots, refer, compounds, unit, evaluator);
                container.IsNormalized = true;
            }
        }

        public bool CanNormalize() {
            foreach (var compound in StandardCompounds) {
                if (IsRequiredFieldFilled(compound)) {
                    return true;
                }
            }
            return false;
        }

        private bool IsRequiredFieldFilled(StandardCompound compound) {
            if (compound.Concentration <= 0) return false;
            if (string.IsNullOrEmpty(compound.TargetClass)) return false;
            if (compound.PeakID < 0 || compound.PeakID >= spots.Count) return false;
            return true;
        }

        private static List<SplashProduct> GetPublicSplashResource() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("CompMs.App.Msdial.Resources.SplashLipids.xml")) {
                var data = XElement.Load(stream);
                return data.Elements("Product").Select(rawProduct => ToProduct(rawProduct, false)).Where(product => !(product is null)).ToList();
            }
        }

        private static List<SplashProduct> GetPrivateSplashResource() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("CompMs.App.Msdial.Resources.SplashLipids.xml")) {
                var data = XElement.Load(stream);
                return data.Elements("Product").Select(rawProduct => ToProduct(rawProduct, true)).Where(product => !(product is null)).ToList();
            }
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
            return new SplashProduct
            {
                Label = element.Element("Label").Value,
                Lipids = new ObservableCollection<StandardCompound>(lipids),
            };
        }

        private static StandardCompound ToCompound(XElement element, bool isPrivate = false) {
            if (!isPrivate && element.Element("IsLabPrivateVersion")?.Value == "true") {
                return null;
            }
            if (isPrivate && element.Element("IsPublicVersion")?.Value == "true") {
                return null;
            }
            return new StandardCompound
            {
                StandardName = element.Element("StandardName").Value,
                Concentration = double.TryParse(element.Element("Concentration").Value, out var conc) ? conc : 0d,
				DilutionRate = double.TryParse(element.Element("DilutionRate").Value, out var rate) ? rate : 0d,
				MolecularWeight = double.TryParse(element.Element("MolecularWeight").Value, out var weight) ? weight : 0d,
				PeakID = int.TryParse(element.Element("PeakID").Value, out var id) ? id : -1,
				TargetClass = element.Element("TargetClass").Value,
            };
        }
    }

    class IonAbundance
    {
        public IonAbundance(IonAbundanceUnit unit, string label) {
            Unit = unit;
            Label = label;
        }

        public IonAbundanceUnit Unit { get; set; }
        public string Label { get; set; }
    }

    class SplashProduct
    {
        public string Label { get; set; }
        public ObservableCollection<StandardCompound> Lipids { get; set; }
    }
}
