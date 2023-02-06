using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class UiAccessUtility
    {
        private UiAccessUtility() { }

        public static MassSpectrogramViewModel GetMs1MassSpectrogramVM(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;

            if (rawDataVM == null || rawDataVM.RawData.Ms1PeakNumber <= 0) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(rawDataVM.RawData.Ms1Spectrum.PeakList);

            return new MassSpectrogramViewModel(massSpectrogramBean, MassSpectrogramIntensityMode.Relative, rawDataVM.RawData.ScanNumber, (float)rawDataVM.RawData.RetentionTime, (float)rawDataVM.RawData.PrecursorMz, "MS1 spectrum");
        }

        public static BitmapImage GetSmilesAsImage(FragmenterResultVM fragmenterResultVM, double width, double height)
        {
            if (width <= 0 || height <= 0) return null;
            if (fragmenterResultVM == null) return null;

            var fragmenterVM = fragmenterResultVM;
            var smiles = fragmenterVM.Smiles;
            if (smiles == null || smiles == string.Empty) return null;

            //var drawingImage = MoleculeImage.SmilesToImage(smiles, (int)width, (int)height);

            //if (drawingImage == null) return null;
            return MoleculeImage.SmilesToMediaImageSource(smiles, (int)width, (int)height);

            //return MoleculeImage.ConvertDrawingImageToBitmap(drawingImage);
        }

        public static MassSpectrogramViewModel GetMs2MassSpectrogramVM(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;

            if (rawDataVM == null || rawDataVM.RawData.Ms2PeakNumber <= 0) return null;

            var massSpectrogramBean = getMassSpectrogramBean(rawDataVM.RawData.Ms2Spectrum.PeakList);

            return new MassSpectrogramViewModel(massSpectrogramBean, MassSpectrogramIntensityMode.Relative, rawDataVM.RawData.ScanNumber, (float)rawDataVM.RawData.RetentionTime, (float)rawDataVM.RawData.PrecursorMz, "MS/MS spectrum");
        }

        public static MassSpectrogramViewModel GetIsotopeSpectrumVM(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;
            var formulaVM = mainWindowVM.SelectedFormulaVM;

            if (rawDataVM == null || rawDataVM.RawData.Ms1PeakNumber <= 0) return null;

            double precursorIntensity = double.MinValue;
            var experimentalIsotopicIons = getExperimentalIsotopicIons(rawDataVM.PrecursorMz, rawDataVM.RawData.Ms1Spectrum.PeakList, out precursorIntensity);
            var insilicoIsotopicIons = getTheoreticalIsotopicIons(formulaVM, rawDataVM.PrecursorType, precursorIntensity);

            return new MassSpectrogramViewModel(experimentalIsotopicIons, insilicoIsotopicIons, MassSpectrogramIntensityMode.Relative, rawDataVM.ScanNumber, (float)rawDataVM.RetentionTime, "Isotopic ions");
        }

        private static MassSpectrogramBean getTheoreticalIsotopicIons(FormulaVM formulaVM, string precursorType, double precursorIntensity)
        {
            if (formulaVM == null || formulaVM.FormulaResult == null) return null;

            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            var isotopicPeaks = SevenGoldenRulesCheck.GetIsotopicPeaks(formulaVM.FormulaResult.Formula);
            var adductIon = AdductIonParcer.GetAdductIonBean(precursorType);

            foreach (var isotope in isotopicPeaks)
            {
                if (isotope.RelativeAbundance <= 0) continue;

                var precursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adductIon, isotope.Mass);
                massSpectraCollection.Add(new double[] { precursorMz, isotope.RelativeAbundance * precursorIntensity });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = precursorMz, Intensity = isotope.RelativeAbundance * precursorIntensity, Label = isotope.Comment });
            }

            return new MassSpectrogramBean(Brushes.Red, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        private static MassSpectrogramBean getExperimentalIsotopicIons(double precursorMz, List<Peak> peaklist, out double precursorIntensity)
        {
            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            var minDiff = double.MaxValue;
            precursorIntensity = double.MinValue;

            foreach (var peak in peaklist)
            {
                if (peak.Mz < precursorMz - 0.2) continue;
                if (peak.Mz > precursorMz + 2.2) break;
                if (Math.Abs(precursorMz - peak.Mz) < minDiff) { minDiff = Math.Abs(precursorMz - peak.Mz); precursorIntensity = peak.Intensity; }

                massSpectraCollection.Add(new double[] { peak.Mz, peak.Intensity });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = peak.Mz, Intensity = peak.Intensity, Label = Math.Round(peak.Mz, 4).ToString() });
            }

            return new MassSpectrogramBean(Brushes.Blue, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        public static MassSpectrogramViewModel GetActuralMsMsVsInSilicoMsMs(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;
            var fragmenterVM = mainWindowVM.SelectedFragmenterVM;

            if (rawDataVM == null) return null;
            if (rawDataVM.RawData.Ms2PeakNumber <= 0) return null;

            var fileName = mainWindowVM.AnalysisFiles[mainWindowVM.SelectedRawFileId].RawDataFileName;
            var formula = string.Empty;
            if (mainWindowVM.SelectedFormulaVM != null && mainWindowVM.SelectedFormulaVM.Formula != string.Empty) formula = mainWindowVM.SelectedFormulaVM.Formula;

            var structureID = string.Empty;
            if (fragmenterVM != null && fragmenterVM.FragmenterResult != null) structureID = fragmenterVM.FragmenterResult.ID;

            var title = string.Empty;

            MassSpectrogramBean massSpectrogram = getMassSpectrogramBean(rawDataVM.RawData.Ms2Spectrum.PeakList, rawDataVM.PrecursorMz);
            MassSpectrogramBean referenceSpectrum;
            if (fragmenterVM == null || fragmenterVM.FragmenterResult == null) referenceSpectrum = null;
            else {
                if (fragmenterVM.FragmenterResult.IsSpectrumSearchResult == true) {
                    title = "Experimental spectrum vs. Reference spectrum";
                    referenceSpectrum = getReferenceMassSpectrogramBean(fragmenterVM.FragmenterResult.ReferenceSpectrum, mainWindowVM.DataStorageBean.AnalysisParameter, rawDataVM.IonMode);
                }
                else {
                    title = "Experimental spectrum vs. In silico spectrum";
                    referenceSpectrum = getInSilicoMassSpectrogramBean(fragmenterVM.FragmenterResult.FragmentPics, mainWindowVM.DataStorageBean.AnalysisParameter, rawDataVM.IonMode);
                }
            }


            return new MassSpectrogramViewModel(massSpectrogram, referenceSpectrum, MassSpectrogramIntensityMode.Absolute, rawDataVM.ScanNumber, (float)rawDataVM.RetentionTime, fileName, formula, structureID, title);
        }

        private static MassSpectrogramBean getInSilicoMassSpectrogramBean(List<PeakFragmentPair> fragments, AnalysisParamOfMsfinder param, IonMode ionMode)
        {
            if (fragments == null || fragments.Count == 0) return null;

            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            var sortedFragments = fragments.OrderBy(n => n.Peak.Mz).ToList();
            foreach (var fragment in sortedFragments)
            {
                var matchedInfo = fragment.MatchedFragmentInfo;
                massSpectraCollection.Add(new double[] { matchedInfo.MatchedMass, matchedInfo.TotalLikelihood });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() 
                {
                    Mass = matchedInfo.MatchedMass,
                    Intensity = matchedInfo.TotalLikelihood,
                    Label = GetLabelForInsilicoSpectrum(matchedInfo.Formula, matchedInfo.RearrangedHydrogen, ionMode, matchedInfo.AssignedAdductString),
                    PeakFragmentPair = fragment 
                });
            }

            return new MassSpectrogramBean(Brushes.Red, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        private static MassSpectrogramBean getReferenceMassSpectrogramBean(List<Peak> peaks, AnalysisParamOfMsfinder param, IonMode ionMode)
        {
            if (peaks == null || peaks.Count == 0) return null;

            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            var sortedPeaks = peaks.OrderBy(n => n.Mz).ToList();
            foreach (var peak in sortedPeaks) {
                massSpectraCollection.Add(new double[] { peak.Mz, peak.Intensity });

                var label = peak.Comment;
                if (peak.Comment == null || peak.Comment == string.Empty)
                    label = Math.Round(peak.Mz, 5).ToString();

                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() {
                    Mass = peak.Mz,
                    Intensity = peak.Intensity,
                    Label = label
                });
            }

            return new MassSpectrogramBean(Brushes.Red, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        public static string GetLabelForInsilicoSpectrum(string formula, double penalty, IonMode ionMode, string adductString)
        {
            var hydrogen = (int)Math.Abs(Math.Round(penalty, 0));
            var hydrogenString = hydrogen.ToString(); if (hydrogen == 1) hydrogenString = string.Empty;
            var ionString = string.Empty; if (ionMode == IonMode.Positive) ionString = "+"; else ionString = "-";
            var frgString = "[" + formula;

            if (penalty < 0)
            {
                frgString += "-" + hydrogenString + "H";
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            }
            else if (penalty > 0)
            {
                frgString += "+" + hydrogenString + "H";
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            }
            else
            {
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            }

            frgString += "]" + ionString;

            return frgString;
        }

        private static MassSpectrogramBean getMassSpectrogramBean(List<Peak> peaklist)
        {
            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            foreach (var peak in peaklist)
            {
                massSpectraCollection.Add(new double[] { peak.Mz, peak.Intensity });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = peak.Mz, Intensity = peak.Intensity, Label = Math.Round(peak.Mz, 4).ToString() });
            }

            return new MassSpectrogramBean(Brushes.Blue, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        private static MassSpectrogramBean getMassSpectrogramBean(List<Peak> peaklist, double maxMz)
        {
            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            foreach (var peak in peaklist)
            {
                massSpectraCollection.Add(new double[] { peak.Mz, peak.Intensity });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = peak.Mz, Intensity = peak.Intensity, Label = Math.Round(peak.Mz, 4).ToString() });
            }

            return new MassSpectrogramBean(Brushes.Blue, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        public static MassSpectrogramViewModel GetProductIonSpectrumVM(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;
            var formulaVM = mainWindowVM.SelectedFormulaVM;
            var uniqueFragmentDB = mainWindowVM.FragmentOntologyDB;

            if (rawDataVM == null || rawDataVM.RawData.Ms2PeakNumber <= 0) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(rawDataVM.RawData.Ms2Spectrum.PeakList, rawDataVM.PrecursorMz);
            MassSpectrogramBean productIonSpectrum = null;

            if (formulaVM != null && formulaVM.ProductIonVMs != null && formulaVM.ProductIonVMs.Count != 0)
            {
                productIonSpectrum = getProductIonSpectrumBean(formulaVM.ProductIonVMs, uniqueFragmentDB);
            }

            return new MassSpectrogramViewModel(massSpectrogramBean, productIonSpectrum, MassSpectrogramIntensityMode.Relative, rawDataVM.RawData.ScanNumber, (float)rawDataVM.RawData.RetentionTime, "MS/MS spectrum");
        }

        private static MassSpectrogramBean getProductIonSpectrumBean(ObservableCollection<ProductIonVM> productIonVMs, List<FragmentOntology> uniqueFragmentDB)
        {
            var massSpectraCollection = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            foreach (var product in productIonVMs)
            {
                massSpectraCollection.Add(new double[] { product.AccurateMass, product.Intensity });
                var productionComment = getInChiKeyComments(product.CandidateInChIKeys, uniqueFragmentDB);
                product.ProductIon.Comment = productionComment;

                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = product.AccurateMass, Intensity = product.Intensity, Label = product.Formula, ProductIon = product.ProductIon });
            }

            return new MassSpectrogramBean(Brushes.Red, 1.0, massSpectraCollection, massSpectraDisplayLabelCollection);
        }

        public static MassSpectrogramViewModel GetNeutralLossSpectrumVM(MainWindowVM mainWindowVM)
        {
            var rawDataVM = mainWindowVM.RawDataVM;
            var formulaVM = mainWindowVM.SelectedFormulaVM;
            var uniqueFragmentDB = mainWindowVM.FragmentOntologyDB;

            if (rawDataVM == null || rawDataVM.RawData.Ms2PeakNumber <= 0) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(rawDataVM.RawData.Ms2Spectrum.PeakList, rawDataVM.PrecursorMz);
            List<NeutralLoss> neutralLosses = null;

            if (formulaVM != null && formulaVM.NeutralLossVMs != null && formulaVM.NeutralLossVMs.Count != 0)
            {
                neutralLosses = new List<NeutralLoss>();
                foreach (var loss in formulaVM.NeutralLossVMs) 
                {
                    var lossComment = getInChiKeyComments(loss.CandidateInChIKeys, uniqueFragmentDB);
                    loss.NeutralLoss.Comment = lossComment;
                    neutralLosses.Add(loss.NeutralLoss);
                }
            }

            return new MassSpectrogramViewModel(massSpectrogramBean, neutralLosses, MassSpectrogramIntensityMode.Relative, rawDataVM.RawData.ScanNumber, (float)rawDataVM.RawData.RetentionTime, (float)rawDataVM.RawData.PrecursorMz, "MS/MS spectrum");
        }

        private static string getInChiKeyComments(List<string> candidateInChIKeys, List<FragmentOntology> uniqueFragmentDB)
        {
            var comment = string.Empty;
            if (candidateInChIKeys == null || candidateInChIKeys.Count == 0) return comment;

            foreach (var inchikey in candidateInChIKeys) {

                foreach (var frag in uniqueFragmentDB) {
                    if (inchikey == frag.ShortInChIKey) {
                        comment += frag.Comment + "\r\n";
                        break;
                    }
                }
            }

            return comment;
        }

        public static FragmentOntology GetMatchedUniqueFragment(string shortInChIKey, List<FragmentOntology> fragmentDB) {
            foreach (var frag in fragmentDB)
            {
                if (shortInChIKey == frag.ShortInChIKey)
                    return frag;
            }
            return null;
        }
    }
}
