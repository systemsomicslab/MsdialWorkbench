using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using CompMs.MsdialCore.Parameter;
using System;
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
        public SplashSetModel(AlignmentResultContainer container, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) {
            this.container = container;
            this.spots = container.AlignmentSpotProperties;
            this.refer = refer;
            this.parameter = parameter;

            var targetMetabolites = LipidomicsConverter.GetLipidClasses();
            targetMetabolites.Add("Any others");
            TargetMetabolites = targetMetabolites.AsReadOnly();

            SplashProducts = new ObservableCollection<SplashProduct>(GetSplashResource());
            SplashProduct = SplashProducts.FirstOrDefault();

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
            var compounds = StandardCompounds.Where(IsRequiredFieldFilled).ToList();
            if (compounds.Count == 0) {
                MessageBox.Show("Please fill the required fields for normalization", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            parameter.StandardCompounds = compounds;
            var unit = OutputUnit.Unit;
            SplashNormalization.Normalize(spots, refer, compounds, unit);
            container.IsNormalized = true;
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

        private static List<SplashProduct> GetSplashResource() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("CompMs.App.Msdial.Resources.SplashLipids.xml")) {
                var data = XElement.Load(stream);
                return data.Elements("Product").Select(ToProduct).ToList();
            }
        }

        private static SplashProduct ToProduct(XElement element) {
            return new SplashProduct
            {
                Label = element.Element("Label").Value,
                Lipids = new ObservableCollection<StandardCompound>(element.Element("Lipids").Elements("Lipid").Select(ToCompound)),
            };
        }

        private static StandardCompound ToCompound(XElement element) {
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
