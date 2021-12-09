using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.SpectrumViewer.Model
{
    public class LipidReferenceCollection : BindableBase, IScanCollection
    {
        public LipidReferenceCollection() {
            Adducts = new List<AdductIon> { AdductIonParser.GetAdductIonBean("[M+H]+") }.AsReadOnly();
            Adduct = Adducts.First();
            Scans = new ObservableCollection<IMSScanProperty>();

            lipidParser = new PCLipidParser();
            lipidGenerator = new LipidGenerator();
            spectrumGenerator = new PCSpectrumGenerator();
        }

        public string Name { get => Lipid.ToString(); }

        public ILipid Lipid {
            get => lipid;
            private set {
                if (SetProperty(ref lipid, value)) {
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private ILipid lipid;

        public AdductIon Adduct {
            get => adduct;
            set => SetProperty(ref adduct, value);
        }
        private AdductIon adduct;

        public ReadOnlyCollection<AdductIon> Adducts { get; }

        public ObservableCollection<IMSScanProperty> Scans { get; }

        private readonly LipidGenerator lipidGenerator;
        private readonly PCSpectrumGenerator spectrumGenerator;
        private readonly PCLipidParser lipidParser;

        public void SetLipid(string lipidStr) {
            Lipid = lipidParser.Parse(lipidStr);
        }

        public void Generate(string lipidStr) {
            SetLipid(lipidStr);
            Scans.Clear();
            if (Lipid == null) {
                return;
            }
            foreach (var lipid in GenerateLipid(Lipid, lipidGenerator)) {
                Scans.Add(lipid.GenerateSpectrum(spectrumGenerator, Adduct));
            }
        }

        private static IEnumerable<ILipid> GenerateLipid(ILipid lipid, ILipidGenerator generator) {
            yield return lipid;
            foreach (var lipid_ in lipid.Generate(generator).SelectMany(l => GenerateLipid(l, generator))) {
                yield return lipid_;
            }
        }
    }
}
