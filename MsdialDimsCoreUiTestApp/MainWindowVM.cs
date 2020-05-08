using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using Rfx.Riken.OsakaUniv;

namespace MsdialDimsCoreUiTestApp
{
    internal class MainWindowVM
    {
        public ChromatogramXicViewModel ChromatogramXicViewModel { get; set; }

        public MainWindowVM()
        {
            // testfiles
            var filepath = @"C:\Users\Matsuzawa\workspace\riken\abf\704_Egg2 Egg Yolk.abf";
            var lbmFile = @"C:\Users\Matsuzawa\workspace\riken\MSDIAL_LipidDB_Test.lbm2";
            var param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile, 
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() { 
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            var spectras = DataAccess.GetAllSpectra(filepath);
            var ms1spectra = spectras.Where(spectra => spectra.MsLevel == 1)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .Max(spectra => (length: spectra.Spectrum.Length, spectra: spectra))
                                     .spectra.Spectrum
                                     .Select((peak, index) => new double[] { index, peak.Mz, peak.Mz, peak.Intensity });
            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, "test", new ObservableCollection<double[]>(ms1spectra), new ObservableCollection<PeakAreaBean> { new PeakAreaBean() });
            ChromatogramXicViewModel = new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display,
                                                                    ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height,
                                                                    ChromatogramIntensityMode.Absolute, 0, "viewmodel test", 0, 1000, 0);
        }

        
    }
}
