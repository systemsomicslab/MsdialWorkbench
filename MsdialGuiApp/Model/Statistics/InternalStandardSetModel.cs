using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class InternalStandardSetModel : BindableBase
    {
        public InternalStandardSetModel(IEnumerable<AlignmentSpotPropertyModel> spots, TargetMsMethod targetMsMethod) {
            if (spots is null) {
                throw new ArgumentNullException(nameof(spots));
            }

            var spotModels = spots.Select(spot => new NormalizationSpotPropertyModel(spot.innerModel));
            Spots = new ObservableCollection<NormalizationSpotPropertyModel>(spotModels);
            TargetMsMethod = targetMsMethod ?? throw new ArgumentNullException(nameof(targetMsMethod));
        }

        public ObservableCollection<NormalizationSpotPropertyModel> Spots { get; }
        public TargetMsMethod TargetMsMethod { get; }
    }

    internal sealed class NormalizationSpotPropertyModel : BindableBase
    {
        private readonly AlignmentSpotProperty _spot;

        public NormalizationSpotPropertyModel(AlignmentSpotProperty spot) {
            _spot = spot;
            Id = _spot.MasterAlignmentID;
            Metabolite = _spot.Name;
            Adduct = _spot.AdductType.AdductIonName;
            Mz = _spot.MassCenter;
            Rt = _spot.TimesCenter.RT.Value;
            Mobility = _spot.TimesCenter.Drift.Value;
            InternalStandardId = _spot.InternalStandardAlignmentID;
        }

        public int Id { get; }
        public string Metabolite { get; }
        public string Adduct { get; }
        public double Mz { get; }
        public double Rt { get; }
        public double Mobility { get; }
        public int InternalStandardId {
            get => _internalStandardId;
            set => SetProperty(ref _internalStandardId, value);
        }
        private int _internalStandardId;
    }
}
