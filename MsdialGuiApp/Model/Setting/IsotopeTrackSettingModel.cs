using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public class IsotopeTrackSettingModel : BindableBase {
        private readonly ParameterBase parameter;

        public IsotopeTrackSettingModel(ParameterBase parameter, List<AnalysisFileBean> files) {
            this.parameter = parameter;
            AnalysisFiles = files.AsReadOnly();
            TrackingIsotopeLabels = parameter.IsotopeTrackingBaseParam.TrackingIsotopeLabels;
            IsotopeTrackingDictionary = parameter.IsotopeTrackingBaseParam.IsotopeTrackingDictionary;
            NonLabeledReference = files.FirstOrDefault(f => f.AnalysisFileId == parameter.IsotopeTrackingBaseParam.NonLabeledReferenceID);
            UseTargetFormulaLibrary = parameter.IsotopeTrackingBaseParam.UseTargetFormulaLibrary;
            IsotopeTextDBFilePath = parameter.ReferenceFileParam.IsotopeTextDBFilePath;
            SetFullyLabeledReferenceFile = parameter.IsotopeTrackingBaseParam.SetFullyLabeledReferenceFile;
            FullyLabeledReference = files.FirstOrDefault(f => f.AnalysisFileId == parameter.IsotopeTrackingBaseParam.FullyLabeledReferenceID);
        }

        public bool TrackingIsotopeLabels {
            get => trackingIsotopeLabels;
            set => SetProperty(ref trackingIsotopeLabels, value);
        }
        private bool trackingIsotopeLabels;

        public IsotopeTrackingDictionary IsotopeTrackingDictionary { get; }

        public AnalysisFileBean NonLabeledReference {
            get => nonLabeledReference;
            set => SetProperty(ref nonLabeledReference, value);
        }
        private AnalysisFileBean nonLabeledReference;

        public bool UseTargetFormulaLibrary {
            get => useTargetFormulaLibrary;
            set => SetProperty(ref useTargetFormulaLibrary, value);
        }
        private bool useTargetFormulaLibrary;

        public string IsotopeTextDBFilePath {
            get => isotopeTextDBFilePath;
            set => SetProperty(ref isotopeTextDBFilePath, value);
        }
        private string isotopeTextDBFilePath;

        public bool SetFullyLabeledReferenceFile {
            get => setFullyLabeledReferenceFile;
            set => SetProperty(ref setFullyLabeledReferenceFile, value);
        }
        public bool setFullyLabeledReferenceFile;

        public AnalysisFileBean FullyLabeledReference {
            get => fullyLabeledReference;
            set => SetProperty(ref fullyLabeledReference, value);
        }
        private AnalysisFileBean fullyLabeledReference;

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles { get; }

        public void Commit() {
            parameter.IsotopeTrackingBaseParam.TrackingIsotopeLabels = TrackingIsotopeLabels;
            parameter.IsotopeTrackingBaseParam.NonLabeledReferenceID = NonLabeledReference.AnalysisFileId;
            parameter.IsotopeTrackingBaseParam.UseTargetFormulaLibrary = UseTargetFormulaLibrary;
            parameter.ReferenceFileParam.IsotopeTextDBFilePath = IsotopeTextDBFilePath;
            parameter.IsotopeTrackingBaseParam.SetFullyLabeledReferenceFile = SetFullyLabeledReferenceFile;
            parameter.IsotopeTrackingBaseParam.FullyLabeledReferenceID = FullyLabeledReference.AnalysisFileId;
        }
    }
}
