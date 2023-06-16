using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Search
{
    public interface IFilterable : IAnnotatedObject, IChromatogramPeak, IMoleculeProperty
    {
        bool IsMsmsAssigned { get; }
        bool IsMonoIsotopicIon { get; }
        bool IsBlankFiltered { get; }
        bool IsFragmentQueryExisted { get; }
        bool IsManuallyModifiedForAnnotation { get; }

        bool IsBlankFilteredByPostCurator { get; }
        bool IsBlankGhostFilteredByPostCurator { get; }
        bool IsMzFilteredByPostCurator { get; }
        bool IsRsdFilteredByPostCurator { get; }
        bool IsRmdFilteredByPostCurator { get; }

        string Comment { get; }
        string Protein { get; }
        string AdductIonName { get; }

        double RelativeAmplitudeValue { get; }

        PeakSpotTagCollection TagCollection { get; }
    }
}
