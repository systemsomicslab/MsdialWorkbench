using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsAnnotationSettingViewModel : ViewModelBase, IAnnotationSettingViewModel
    {
        public LcmsAnnotationSettingViewModel(DelegateDataBaseAnnotationSettingModel model, MsdialLcmsParameter parameter) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            this.model = model;
            Parameter = parameter;
            ParameterVM = new MsRefSearchParameterBaseViewModel(this.model.Parameter);
            DataBasePath = this.model.ToReactivePropertyAsSynchronized(m => m.DataBasePath)
                .SetValidateAttribute(() => DataBasePath)
                .AddTo(Disposables);
            DataBaseID = new[]
            {
                DataBasePath.Select(path => Path.GetFileNameWithoutExtension(path)),
                this.model.ObserveProperty(m => m.DataBaseID),
            }.Merge()
            .ToReactiveProperty()
            .SetValidateAttribute(() => DataBaseID)
            .AddTo(Disposables);
            DataBaseID.Subscribe(id => this.model.DataBaseID = this.model.AnnotatorID = id).AddTo(Disposables);
            AnnotatorID = model.ToReactivePropertySlimAsSynchronized(m => m.AnnotatorID).AddTo(Disposables);
            DBSources = new List<DataBaseSource> { DataBaseSource.Msp, DataBaseSource.Lbm, DataBaseSource.Text, DataBaseSource.Fasta };
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
                    else if (Regex.IsMatch(ext, @"\.fasta\d*")) {
                        return DataBaseSource.Fasta;
                    }
                    return DataBaseSource.None;
                })
                .ToReactiveProperty(DataBaseSource.None)
                .SetValidateNotifyError(src => DBSources.Contains(src) ? null : "Unknown database")
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
                        case DataBaseSource.Fasta:
                            return SourceType.FastaDB;
                        default:
                            return SourceType.None;
                    }
                })
                .ToReadOnlyReactivePropertySlim(SourceType.None)
                .AddTo(Disposables);
            AnnotationSource.Subscribe(s => this.model.AnnotationSource = s).AddTo(Disposables);

            Label = DataBaseID.CombineLatest(DBSource, (id, src) => $"{id} ({src})")
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Implement = DBSource.Select<DataBaseSource, IAnnotationSettingViewModel>(s => {
                switch (s) {
                    case DataBaseSource.Msp:
                        return new LcmsMspAnnotationSettingViewModel(this.model);
                    case DataBaseSource.Lbm:
                        return new LcmsLbmAnnotationSettingViewModel(this.model, Parameter);
                    case DataBaseSource.Text:
                        return new LcmsTextDBAnnotationSettingViewModel(this.model);
                    case DataBaseSource.Fasta:
                        return new LcmsFastaAnnotationSettingViewModel(this.model, Parameter);
                    default:
                        return null;
                }
            }).DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            Implement.Subscribe(impl => this.model.Implement = impl?.Model).AddTo(Disposables);

            hasErrors = new IObservable<bool>[]
            {
                Implement.Select(impl => impl is null || impl.ObserveHasErrors.Value),
                DataBasePath.ObserveHasErrors,
                DataBaseID.ObserveHasErrors,
                DBSource.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        private readonly DelegateDataBaseAnnotationSettingModel model;
        IAnnotationSettingModel IAnnotationSettingViewModel.Model => model;
        ReadOnlyReactivePropertySlim<bool> IAnnotationSettingViewModel.ObserveHasErrors => hasErrors;
        private readonly ReadOnlyReactivePropertySlim<bool> hasErrors;

        public ReadOnlyReactivePropertySlim<string> Label { get; }

        public ReadOnlyReactivePropertySlim<IAnnotationSettingViewModel> Implement { get; }

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReactiveProperty<DataBaseSource> DBSource { get; }
        public ReactivePropertySlim<string> AnnotatorID { get; }
        public List<DataBaseSource> DBSources { get; }

        public ReadOnlyReactivePropertySlim<SourceType> AnnotationSource { get; }

        public DelegateCommand BrowseCommand => browseCommand ?? (browseCommand = new DelegateCommand(Browse));

        public MsdialLcmsParameter Parameter { get; }

        private DelegateCommand browseCommand;

        private void Browse() {
            var ofd = new OpenFileDialog
            {
                Title = "Import a library file",
                Filter = "MSP file(*.msp)|*.msp?|Lbm file(*.lbm)|*.lbm?|Text library(*.txt)|*.txt|All(*)|*",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                DataBasePath.Value = ofd.FileName;
            }
        }
    }

    sealed class LcmsAnnotationSettingViewModelModelFactory
    {
        public LcmsAnnotationSettingViewModelModelFactory(MsdialLcmsParameter parameter) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            Parameter = parameter;
        }

        public MsdialLcmsParameter Parameter { get; }

        public LcmsAnnotationSettingViewModel Create() {
            var m = new DelegateDataBaseAnnotationSettingModel();
            return new LcmsAnnotationSettingViewModel(m, Parameter);
        }
    }
}
