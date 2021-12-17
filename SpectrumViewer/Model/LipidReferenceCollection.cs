using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
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
            var facadeParser = new FacadeLipidParser(); // TODO: build by static methods.
            facadeParser.Add(new PCLipidParser());
            facadeParser.Add(new EtherPELipidParser());
            facadeParser.Add(new PGLipidParser());
            lipidParser = facadeParser;
            lipidGenerator = new LipidGenerator();
            var facadeGenerator = new FacadeLipidSpectrumGenerator(); // TODO: build by static methods.
            facadeGenerator.Add(LbmClass.PC, new PCSpectrumGenerator());
            facadeGenerator.Add(LbmClass.EtherPE, new EtherPESpectrumGenerator());
            facadeGenerator.Add(LbmClass.PG, new PGSpectrumGenerator());
            spectrumGenerator = facadeGenerator;
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

        private readonly ILipidSpectrumGenerator spectrumGenerator;
        private readonly ILipidParser lipidParser;

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
