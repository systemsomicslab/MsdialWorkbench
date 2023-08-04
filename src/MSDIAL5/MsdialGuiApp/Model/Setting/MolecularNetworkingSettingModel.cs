using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Clustering;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting {
    internal sealed class MolecularNetworkingSettingModel : BindableBase {

        private readonly MsdialLcmsParameter _parameter;
        private readonly MsmsClustering _clustering;
        
        public MolecularNetworkingSettingModel(MsdialLcmsParameter parameter, List<AlignmentSpotPropertyModel> spots) {
            //_clustering = new MsmsClustering(spots.Select(s => s.innerModel).ToList());
            _parameter = parameter;
        }
        public double MassTolerance
        {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance;

        public double RelativeAbundanceCutoff {
            get => relativeAbundanceCutoff;
            set => SetProperty(ref relativeAbundanceCutoff, value);
        }
        private double relativeAbundanceCutoff;

        public double SimilarityCutoff {
            get => similarityCutoff;
            set => SetProperty(ref similarityCutoff, value);
        }
        private double similarityCutoff;

        public void RunMolecularNetworking() {
            //var edges =_clustering.GetEdgeInformations(RelativeAbundanceCutoff, MassTolerance, SimilarityCutoff);
            //Console.WriteLine(MassTolerance);
        }
    }
}
