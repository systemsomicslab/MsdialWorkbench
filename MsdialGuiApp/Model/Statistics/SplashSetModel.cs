using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Normalize;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class SplashSetModel : DisposableModelBase
    {
        private readonly AlignmentResultContainer _container;
        private readonly IReadOnlyList<AlignmentSpotProperty> _spots;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly ParameterBase _parameter;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMessageBroker _broker;

        public SplashSetModel(AlignmentResultContainer container, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMessageBroker broker) {
            _container = container;
            _spots = container.AlignmentSpotProperties;
            _refer = refer;
            _parameter = parameter;
            _evaluator = evaluator;
            _broker = broker;
            var targetMetabolites = LipidomicsConverter.GetLipidClasses();
            targetMetabolites.Add("Any others");
            TargetMetabolites = targetMetabolites.AsReadOnly();

            var isPrivate = Properties.Resources.VERSION.EndsWith("-tada")
                || Properties.Resources.VERSION.EndsWith("-alpha")
                || Properties.Resources.VERSION.EndsWith("-beta")
                || Properties.Resources.VERSION.EndsWith("-dev");
            SplashProducts = new ObservableCollection<SplashProduct>(isPrivate ? GetPrivateSplashResource() : GetPublicSplashResource());
            if (!parameter.AdvancedProcessOptionBaseParam.StandardCompounds.IsEmptyOrNull()) {
                var product = new SplashProduct("Previous compounds", parameter.AdvancedProcessOptionBaseParam.StandardCompounds.Select(lipid => new StandardCompoundModel(lipid)));
                SplashProducts.Add(product);
                SplashProduct = product;
            }
            else {
                SplashProduct = SplashProducts.FirstOrDefault();
            }

            OutputUnits = new ObservableCollection<IonAbundance>() {
                new IonAbundance(IonAbundanceUnit.nmol_per_microL_plasma),
                new IonAbundance(IonAbundanceUnit.pmol_per_microL_plasma),
                new IonAbundance(IonAbundanceUnit.fmol_per_microL_plasma),
                new IonAbundance(IonAbundanceUnit.nmol_per_mg_tissue),
                new IonAbundance(IonAbundanceUnit.pmol_per_mg_tissue),
                new IonAbundance(IonAbundanceUnit.fmol_per_mg_tissue),
                new IonAbundance(IonAbundanceUnit.nmol_per_10E6_cells),
                new IonAbundance(IonAbundanceUnit.pmol_per_10E6_cells),
                new IonAbundance(IonAbundanceUnit.fmol_per_10E6_cells) };
            OutputUnit = OutputUnits.FirstOrDefault();

            CanNormalizeProperty = this.ObserveProperty(m => m.SplashProduct).Select(product => product?.CanNormalize(_spots) ?? Observable.Return(false)).Switch().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ObservableCollection<SplashProduct> SplashProducts { get; }

        public SplashProduct SplashProduct {
            get => _splashProduct;
            set => SetProperty(ref _splashProduct, value);
        }
        private SplashProduct _splashProduct;

        public ObservableCollection<IonAbundance> OutputUnits { get; }

        public IonAbundance OutputUnit {
            get => _outputUnit;
            set => SetProperty(ref _outputUnit, value);
        }
        private IonAbundance _outputUnit;

        public ReadOnlyCollection<string> TargetMetabolites { get; }

        public void Find() {
            foreach (var lipid in SplashProduct.Lipids) {
                foreach (var spot in _spots) {
                    if (lipid.TrySetIdIfMatch(spot)) {
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
                var compoundModels = SplashProduct.Lipids.Where(lipid => lipid.IsRequiredFieldFilled(_spots)).ToList();
                if (compoundModels.Count == 0) {
                    MessageBox.Show("Please fill the required fields for normalization", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var compound in compoundModels) {
                    compound.Commit();
                }
                var compounds = compoundModels.Select(lipid => lipid.Compound).ToList();
                Normalization.SplashNormalize(_spots, _refer, compounds, OutputUnit.Unit, _evaluator);
                _parameter.StandardCompounds = compounds;
                foreach (var compound in compoundModels) {
                    compound.Refresh();
                }
                _container.IsNormalized = true;
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanNormalizeProperty { get; }

        public static List<SplashProduct> GetPublicSplashResource() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("CompMs.App.Msdial.Resources.SplashLipids.xml")) {
                var data = XElement.Load(stream);
                return data.Elements("Product").Select(SplashProduct.BuildPublicProduct).Where(product => !(product is null)).ToList();
            }
        }

        public static List<SplashProduct> GetPrivateSplashResource() {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("CompMs.App.Msdial.Resources.SplashLipids.xml")) {
                var data = XElement.Load(stream);
                return data.Elements("Product").Select(SplashProduct.BuildPrivateProduct).Where(product => !(product is null)).ToList();
            }
        }
    }
}
