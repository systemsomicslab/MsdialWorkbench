using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class FormulaVM : ViewModelBase
    {
        private readonly FormulaResult formulaResult;

        private bool isSelected;

        private ObservableCollection<IsotopeVM> isotopeVMs;
        private ObservableCollection<NeutralLossVM> neutralLossVMs;
        private ObservableCollection<ProductIonVM> productIonVMs;

        public FormulaVM(FormulaResult formulaResult, RawData rawData)
        {
            this.formulaResult = formulaResult;
            this.isSelected = formulaResult.IsSelected;

            this.isotopeVMs = new ObservableCollection<IsotopeVM>();
            this.productIonVMs = new ObservableCollection<ProductIonVM>();
            this.neutralLossVMs = new ObservableCollection<NeutralLossVM>();

            this.isotopeVMs = getIsotopeVMs(formulaResult, rawData);

            if (formulaResult.ProductIonResult != null && formulaResult.ProductIonResult.Count != 0)
            {
                foreach (var product in formulaResult.ProductIonResult) { this.productIonVMs.Add(new ProductIonVM(product)); }
            }

            if (formulaResult.NeutralLossResult != null && formulaResult.NeutralLossResult.Count != 0)
            {
                foreach (var loss in formulaResult.NeutralLossResult) { this.neutralLossVMs.Add(new NeutralLossVM(loss)); }
            }
        }

        private ObservableCollection<IsotopeVM> getIsotopeVMs(FormulaResult formulaResult, RawData rawData)
        {
            var isotopeVMs = new ObservableCollection<IsotopeVM>();
            isotopeVMs.Add(new IsotopeVM() { IsotopeNum = 0, Ion = "M", AbundanceDiff = 0, RelativeIntensity = 1, TheoreticalIntensity = 1 });
            isotopeVMs.Add(new IsotopeVM() { IsotopeNum = 1, Ion = "M + 1", AbundanceDiff = formulaResult.M1IsotopicDiff, RelativeIntensity = rawData.NominalIsotopicPeakList[1].Intensity, TheoreticalIntensity = formulaResult.M1IsotopicIntensity });
            isotopeVMs.Add(new IsotopeVM() { IsotopeNum = 2, Ion = "M + 2", AbundanceDiff = formulaResult.M2IsotopicDiff, RelativeIntensity = rawData.NominalIsotopicPeakList[2].Intensity, TheoreticalIntensity = formulaResult.M2IsotopicIntensity });
            return isotopeVMs;
        }

        public FormulaResult FormulaResult
        {
            get { return formulaResult; }
        } 

        public string Formula
        {
            get { return this.formulaResult.Formula.FormulaString; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; OnPropertyChanged("IsSelected"); }
        }

        public double ExactMass
        {
            get { return formulaResult.Formula.Mass; }
        }

        public double MassError 
        { 
            get { return formulaResult.MassDiff * 1000; } 
        }

        public double PpmError {
            get { return MolecularFormulaUtility.PpmCalculator(ExactMass, ExactMass + formulaResult.MassDiff); }
        }

        public double IsotopicScore
        {
            get { return formulaResult.IsotopicScore; }
        }

        public double FragmentHits
        {
            get { return formulaResult.ProductIonScore; }
        }

        public double NeutralLossHits
        {
            get { return formulaResult.NeutralLossScore; }
        }

        public double TotalScore
        {
            get { return formulaResult.TotalScore; }
        }

        public string Resource
        {
            get { return formulaResult.ResourceNames; }
        }

        public ObservableCollection<IsotopeVM> IsotopeVMs
        {
            get { return isotopeVMs; }
            set { isotopeVMs = value; }
        }

        public ObservableCollection<NeutralLossVM> NeutralLossVMs
        {
            get { return neutralLossVMs; }
            set { neutralLossVMs = value; }
        }

        public ObservableCollection<ProductIonVM> ProductIonVMs
        {
            get { return productIonVMs; }
            set { productIonVMs = value; }
        }
    }
}
