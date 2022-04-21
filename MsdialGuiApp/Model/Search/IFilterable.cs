using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Search
{
    public interface IFilterable : IAnnotatedObject, IChromatogramPeak, IMoleculeProperty
    {
        bool IsMsmsAssigned { get; }
        bool IsBaseIsotopeIon { get; }
        bool IsBlankFiltered { get; }
        bool IsFragmentQueryExisted { get; }
        bool IsManuallyModifiedForAnnotation { get; }

        string Comment { get; }
        string Protein { get; }

        double RelativeAmplitudeValue { get; }
    }
}
