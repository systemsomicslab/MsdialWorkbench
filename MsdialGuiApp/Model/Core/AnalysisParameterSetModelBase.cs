using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Core
{
    public abstract class AnalysisParameterSetModelBase : BindableBase
    {
        public AnalysisParameterSetModelBase(ParameterBase parameter, IEnumerable<AnalysisFileBean> files) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            ParameterBase = parameter;
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(files);

            var dt = DateTime.Now;
            AlignmentResultFileName = $"AlignmentResult_{dt:yyyy_MM_dd_hh_mm_ss}";

            ExcludedMassList = new ObservableCollection<MzSearchQuery>(parameter.ExcludedMassList);

            if (parameter.SearchedAdductIons.IsEmptyOrNull()) {
                parameter.SearchedAdductIons = AdductResourceParser.GetAdductIonInformationList(parameter.IonMode);
            }
            parameter.SearchedAdductIons[0].IsIncluded = true;
            SearchedAdductIons = new ObservableCollection<AdductIon>(parameter.SearchedAdductIons);

            parameter.QcAtLeastFilter = false;

            AnnotationProcessSettingModel = new AnnotationProcessSettingModel();
        }
        public ParameterBase ParameterBase { get; }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }
        private string alignmentResultFileName;

        public ObservableCollection<MzSearchQuery> ExcludedMassList { get; }

        public ObservableCollection<AdductIon> SearchedAdductIons { get; }

        public bool TogetherWithAlignment {
            get {
                return ParameterBase.ProcessOption.HasFlag(ProcessOption.Alignment);
            }
            set {
                if (ParameterBase.ProcessOption.HasFlag(ProcessOption.Alignment) == value) {
                    return;
                }
                if (value) {
                    ParameterBase.ProcessOption |= ProcessOption.Alignment;
                }
                else {
                    ParameterBase.ProcessOption &= ~ProcessOption.Alignment;
                }
                OnPropertyChanged(nameof(TogetherWithAlignment));
            }
        }

        [Obsolete("AnnotationProcessSettingModel will be removed.")]
        public AnnotationProcessSettingModel AnnotationProcessSettingModel { get; }

        public DataBaseMapper BuildAnnotator() {
            var dbm = new DataBaseMapper();
            foreach (var annotation in AnnotationProcessSettingModel.Annotations) {
                dbm.Add(annotation.Build(ParameterBase));
            }
            return dbm;
        }

        public void ClosingMethod() {
            if (ParameterBase.MaxChargeNumber <= 0) {
                ParameterBase.MaxChargeNumber = 2;
            }

            ParameterBase.ExcludedMassList = ExcludedMassList.ToList();

            ParameterBase.TogetherWithAlignment = TogetherWithAlignment;
            if (ParameterBase.TogetherWithAlignment && AnalysisFiles.Count >= 2) {
                ParameterBase.QcAtLeastFilter = false;
            }

            ParameterBase.SearchedAdductIons = SearchedAdductIons.ToList();
        }
    }
}
