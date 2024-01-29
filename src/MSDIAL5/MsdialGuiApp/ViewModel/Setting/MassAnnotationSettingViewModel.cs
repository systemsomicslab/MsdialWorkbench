using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    class MassAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public MassAnnotationSettingViewModel(MassAnnotationSettingModel model) {
            this.model = model;
            ParameterVM = new MsRefSearchParameterBaseViewModel(this.model.Parameter);
            DataBasePath = this.model.ToReactivePropertyAsSynchronized(m => m.DataBasePath)
                .SetValidateAttribute(() => DataBasePath)
                .AddTo(Disposables);
            DataBaseID = this.model.ToReactivePropertyAsSynchronized(m => m.DataBaseID)
                .SetValidateAttribute(() => DataBaseID)
                .AddTo(Disposables);
            DBSource = DataBasePath
                .Select(path => Path.GetExtension(path))
                .Select(ext => {
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        return DataBaseSource.Msp;
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        return DataBaseSource.Lbm;
                    }
                    else if (Regex.IsMatch(ext, @"\.txt\d*")) {
                        return DataBaseSource.Text;
                    }
                    return DataBaseSource.None;
                })
                .ToReadOnlyReactivePropertySlim(DataBaseSource.None)
                .AddTo(Disposables);
            DBSource.Subscribe(s => this.model.DBSource = s).AddTo(Disposables);
            AnnotationSource = DBSource
                .Select(src => {
                    switch (src) {
                        case DataBaseSource.Msp:
                        case DataBaseSource.Lbm:
                            return SourceType.MspDB;
                        case DataBaseSource.Text:
                            return SourceType.TextDB;
                        default:
                            return SourceType.None;
                    }
                })
                .ToReadOnlyReactivePropertySlim(SourceType.None)
                .AddTo(Disposables);
            AnnotationSource.Subscribe(s => this.model.AnnotationSource = s).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            Label = DataBaseID.CombineLatest(DBSource, (id, src) => $"{id} ({src})").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            hasErrors = new[]
            {
                DataBasePath.ObserveHasErrors,
                DataBaseID.ObserveHasErrors,
                ParameterVM.Ms1Tolerance.ObserveHasErrors,
                ParameterVM.Ms2Tolerance.ObserveHasErrors,
                ParameterVM.RtTolerance.ObserveHasErrors,
                ParameterVM.RelativeAmpCutoff.ObserveHasErrors,
                ParameterVM.AbsoluteAmpCutoff.ObserveHasErrors,
                ParameterVM.MassRangeBegin.ObserveHasErrors,
                ParameterVM.MassRangeEnd.ObserveHasErrors,
                ParameterVM.SimpleDotProductCutOff.ObserveHasErrors,
                ParameterVM.WeightedDotProductCutOff.ObserveHasErrors,
                ParameterVM.ReverseDotProductCutOff.ObserveHasErrors,
                ParameterVM.MatchedPeaksPercentageCutOff.ObserveHasErrors,
                ParameterVM.MinimumSpectrumMatch.ObserveHasErrors,
                ParameterVM.TotalScoreCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly MassAnnotationSettingModel model;
        IAnnotationSettingModel IAnnotationSettingViewModel.Model => model;
        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public ReactivePropertySlim<string> AnnotatorID { get; }
        public ReadOnlyReactivePropertySlim<string?> Label { get; }

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReadOnlyReactivePropertySlim<DataBaseSource> DBSource { get; }

        public ReadOnlyReactivePropertySlim<SourceType> AnnotationSource { get; }
    }
}
