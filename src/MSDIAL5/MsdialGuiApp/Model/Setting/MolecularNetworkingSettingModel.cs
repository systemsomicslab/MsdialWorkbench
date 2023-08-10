using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.Common.Algorithm.Function;
using System;
using System.Collections.Generic;
using CompMs.App.Msdial.Model.Lcms;

namespace CompMs.App.Msdial.Model.Setting {
    internal sealed class MolecularNetworkingSettingModel : BindableBase {

        private readonly MsdialLcmsParameter _parameter;
        //private readonly MsmsClustering _clustering;
        //private readonly MoleculerNetworking _molnet;
        private readonly AlignmentFileBeanModel _model;
        public MolecularNetworkingSettingModel(MsdialLcmsParameter parameter, AlignmentFileBeanModel model) {
            //public MolecularNetworkingSettingModel(MsdialLcmsParameter parameter, List<AlignmentSpotPropertyModel> spots) {
            //_clustering = new MsmsClustering(spots.Select(s => s.innerModel).ToList());
            //_molnet = new MoleculerNetworking();
            _model = model;
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
            var edges = MoleculerNetworking.GenerateEdges(_model.LoadMSDecResultsAsync().Result, 6.0, 0.95, 5, 400.0, 100.0, false, null);
            //var edges =_clustering.GetEdgeInformations(RelativeAbundanceCutoff, MassTolerance, SimilarityCutoff);
            //Console.WriteLine(MassTolerance);
        }
    }
}
