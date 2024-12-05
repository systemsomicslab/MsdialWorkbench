using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CompMs.Common.FormulaGenerator.DataObj;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class FormulaSearchQuery
    {
        public FormulaSearchQuery(
            List<ProductIon> productIonDB,
            List<NeutralLoss> neutralLossDB,
            List<ExistFormulaQuery> existFormulaDB,
            ChromatogramPeakFeature feature,
            List<SpectrumPeak> ms2Spectrum,
            AnalysisParamOfMsfinder msfinderParam) {

            ProductIonDB = productIonDB;
            NeutralLossDB = neutralLossDB;
            ExistFormulaDB = existFormulaDB;
            MsfinderParam = msfinderParam;

            MsfinderRawData = new RawData()
            {
                ScanNumber = feature.PeakID,
                PrecursorMz = feature.PrecursorMz,
                PrecursorType = feature.AdductType.ToString(),
                IonMode = feature.IonMode,
                Intensity = (int)feature.PeakFeature.PeakHeightTop,
                Ms2PeakNumber = ms2Spectrum.Count,
                Ms2Spectrum = ms2Spectrum,
            };
        }

        public FormulaSearchQuery(
            List<ProductIon> productIonDB,
            List<NeutralLoss> neutralLossDB,
            List<ExistFormulaQuery> existFormulaDB,
            ChromatogramPeakFeature feature,
            List<SpectrumPeak> ms2Spectrum,
            RawSpectrum ms1RawSpectrum,
            ParameterBase param,
            AnalysisParamOfMsfinder msfinderParam) {

            ProductIonDB = productIonDB;
            NeutralLossDB = neutralLossDB;
            ExistFormulaDB = existFormulaDB;
            MsfinderParam = msfinderParam;

            var _isotopes = DataAccess.GetIsotopicPeaks(ms1RawSpectrum.Spectrum, (float)feature.PrecursorMz, param.CentroidMs1Tolerance);
            var isotopes = _isotopes.Where(spec => spec.AbsoluteAbundance > 0).ToList();
            var ms1CentroidSpectrum = isotopes.Select(n => new SpectrumPeak() { Mass = n.Mass, Intensity = n.AbsoluteAbundance }).ToList();

            MsfinderRawData = new RawData()
            {
                ScanNumber = feature.PeakID,
                PrecursorMz = feature.PrecursorMz,
                PrecursorType = feature.AdductType.ToString(),
                IonMode = feature.IonMode,
                Intensity = (int)feature.PeakFeature.PeakHeightTop,
                Ms1PeakNumber = ms1CentroidSpectrum.Count,
                Ms1Spectrum = ms1CentroidSpectrum,
                Ms2PeakNumber = ms2Spectrum.Count,
                Ms2Spectrum = ms2Spectrum,
                NominalIsotopicPeakList = isotopes,
            };

        }

        public List<ProductIon> ProductIonDB { get; }
        public List<NeutralLoss> NeutralLossDB { get; }
        public List<ExistFormulaQuery> ExistFormulaDB { get; }
        public RawData MsfinderRawData { get; }
        public AnalysisParamOfMsfinder MsfinderParam { get; }

        public List<FormulaResult> FindCandidates()
        {
            return MolecularFormulaFinder.GetMolecularFormulaList(ProductIonDB, NeutralLossDB, ExistFormulaDB, MsfinderRawData, MsfinderParam);
        }
    }
}
