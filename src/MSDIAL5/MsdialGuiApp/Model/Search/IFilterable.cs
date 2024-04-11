using CompMs.Common.DataObj.Property;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Search
{
    public interface IFilterable
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
        string? AdductIonName { get; }
        AdductIon AdductType { get; }

        double RelativeAmplitudeValue { get; }

        PeakSpotTagCollection TagCollection { get; }
    }
}
