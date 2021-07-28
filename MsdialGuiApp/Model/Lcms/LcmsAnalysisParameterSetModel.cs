using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsAnalysisParameterSetModel : BindableBase
    {
        public LcmsAnalysisParameterSetModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            Parameter = parameter;
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

        public MsdialLcmsParameter Parameter { get; }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }
        private string alignmentResultFileName;

        public ObservableCollection<MzSearchQuery> ExcludedMassList { get; }

        public ObservableCollection<AdductIon> SearchedAdductIons { get; }

        public AnnotationProcessSettingModel AnnotationProcessSettingModel { get; }

        public void ClosingMethod() {
            if (Parameter.MaxChargeNumber <= 0) {
                Parameter.MaxChargeNumber = 2;
            }

            Parameter.ExcludedMassList = ExcludedMassList.ToList();

            if (Parameter.TogetherWithAlignment && AnalysisFiles.Count >= 2) {
                Parameter.QcAtLeastFilter = false;
            }
        }

        public DataBaseMapper BuildAnnotator() {
            var dbm = new DataBaseMapper();
            foreach (var annotation in AnnotationProcessSettingModel.Annotations) {
                var db = annotation.LoadDataBase(Parameter);
                var annotator = annotation.Build(Parameter.ProjectParam, db);
                dbm.Add(annotator.Annotator, db);
            }
            return dbm;
        }
    }
}
