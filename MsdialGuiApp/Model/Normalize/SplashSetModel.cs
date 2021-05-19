using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Normalize
{
    public class SplashSetModel : BindableBase
    {
        public SplashSetModel() {
            var targetMetabolites = LipidomicsConverter.GetLipidClasses();
            targetMetabolites.Add("Any others");

            SplashProducts = new ObservableCollection<string>() { "EquiSPLASH", "SPLASH LIPIDOMIX", "SPLASH II LIPIDOMIX" };
            SplashProduct = SplashProducts.FirstOrDefault();

            OutputUnits = new ObservableCollection<string>() {
                "nmol/μL plasma", "pmol/μL plasma", "fmol/μL plasma",
                "nmol/mg tissue", "pmol/mg tissue", "fmol/mg tissue",
                "nmol/10^6 cells", "pmol/10^6 cells", "fmol/10^6 cells" };
            OutputUnit = OutputUnits.FirstOrDefault();
        }

        public ObservableCollection<StandardCompound> StandardCompounds { get; set; }

        public ObservableCollection<string> SplashProducts { get; }

        public string SplashProduct {
            get => splashProduct;
            set => SetProperty(ref splashProduct, value);
        }

        private string splashProduct;

        public ObservableCollection<string> OutputUnits { get; }

        public string OutputUnit {
            get => outputUnit;
            set => SetProperty(ref outputUnit, value);
        }

        private string outputUnit;

        public void Find() {

        }

        public void Execute() {

        }
    }
}
