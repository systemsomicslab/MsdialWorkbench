using CompMs.Common.Query;
using CompMs.CommonSourceGenerator.MVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj;


[BufferedBindableType(typeof(PeakPickBaseParameter))]
public partial class PeakPickBaseParameterModel
{
    private readonly ObservableCollection<MzSearchQuery> _excludedMzQueries = new();

    public ReadOnlyObservableCollection<MzSearchQuery> ExcludedMzQueries { get; private set; } = default!;

    partial void Initialized() {
        ExcludedMzQueries = new ReadOnlyObservableCollection<MzSearchQuery>(_excludedMzQueries);
        LoadExcludedMassList(_innerModel.ExcludedMassList);
    }

    partial void Committed() {
        _innerModel.ExcludedMassList = ExcludedMzQueries.ToList();
    }

    public void LoadParameter(PeakPickBaseParameter parameter) {
        SmoothingMethod = parameter.SmoothingMethod;
        SmoothingLevel = parameter.SmoothingLevel;
        MinimumAmplitude = parameter.MinimumAmplitude;
        MinimumDatapoints = parameter.MinimumDatapoints;
        MassSliceWidth = parameter.MassSliceWidth;
        RetentionTimeBegin = parameter.RetentionTimeBegin;
        RetentionTimeEnd = parameter.RetentionTimeEnd;
        MassRangeBegin = parameter.MassRangeBegin;
        MassRangeEnd = parameter.MassRangeEnd;
        Ms2MassRangeBegin = parameter.Ms2MassRangeBegin;
        Ms2MassRangeEnd = parameter.Ms2MassRangeEnd;
        CentroidMs1Tolerance = parameter.CentroidMs1Tolerance;
        CentroidMs2Tolerance = parameter.CentroidMs2Tolerance;
        MaxChargeNumber = parameter.MaxChargeNumber;
        IsBrClConsideredForIsotopes = parameter.IsBrClConsideredForIsotopes;

        LoadExcludedMassList(parameter.ExcludedMassList);
        MaxIsotopesDetectedInMs1Spectrum = parameter.MaxIsotopesDetectedInMs1Spectrum;
    }

    private void LoadExcludedMassList(IEnumerable<MzSearchQuery> queries) {
        _excludedMzQueries.Clear();
        foreach (var query in queries) {
            _excludedMzQueries.Add(new MzSearchQuery {
                Mass = query.Mass,
                RelativeIntensity = query.RelativeIntensity,
                SearchType = query.SearchType,
                MassTolerance = query.MassTolerance,
            });
        }
        ExcludedMassList = _excludedMzQueries.ToList();
    }

    public void AddQuery(double mass, double tolerance) {
        var query = new MzSearchQuery { Mass = mass, MassTolerance = tolerance };
        _excludedMzQueries.Add(query);
        ExcludedMassList.Add(query);
    }

    public void RemoveQuery(MzSearchQuery query) {
        _excludedMzQueries.Remove(query);
        ExcludedMassList.Remove(query);
    }
}
