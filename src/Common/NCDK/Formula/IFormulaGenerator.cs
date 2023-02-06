namespace NCDK.Formula
{
    internal interface IFormulaGenerator
    {
        IMolecularFormula GetNextFormula();
        IMolecularFormulaSet GetAllFormulas();
        double GetFinishedPercentage();
        void Cancel();
    }
}
