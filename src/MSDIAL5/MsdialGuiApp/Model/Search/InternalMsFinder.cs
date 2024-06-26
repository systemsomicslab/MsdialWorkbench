using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search {
    internal sealed class InternalMsFinder : BindableBase {
        public AlignmentFileBeanModel File { get; }
        public AnalysisParamOfMsfinder Parameter { get; }
        private AlignmentSpotPropertyModelCollection spots { get; }

        public InternalMsFinder(InternalMsFinderMetaboliteList metaboliteList) {
            InternalMsFinderMetaboliteList = metaboliteList;
            MsfinderObservedMetabolites = InternalMsFinderMetaboliteList.ObservedMetabolites;
            MsfinderSelectedMetabolite = InternalMsFinderMetaboliteList.SelectedObservedMetabolite;
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; }
        public ReadOnlyObservableCollection<MsfinderObservedMetabolite> MsfinderObservedMetabolites { get; }
        public MsfinderObservedMetabolite? MsfinderSelectedMetabolite {
            get => _msfinderObservedMetabolite;
            set => SetProperty(ref _msfinderObservedMetabolite, value);
        }
        private MsfinderObservedMetabolite? _msfinderObservedMetabolite;
        public InternalMsfinderSettingModel SettingModel;
    }
}