using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
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
        public LcmsAnnotationSettingViewModel(LcmsAnnotationSettingModel model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
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
            Sources = new List<SourceType> { SourceType.MspDB, SourceType.TextDB };
            Source = DataBasePath
                .Select(path => Path.GetExtension(path))
                .Select(ext => {
                    if (Regex.IsMatch(ext, @"\.msp\d*")) {
                        return SourceType.MspDB;
                    }
                    else if (Regex.IsMatch(ext, @"\.lbm\d*")) {
                        return SourceType.MspDB;
                    }
                    else if (Regex.IsMatch(ext, @"\.txt\d*")) {
                        return SourceType.TextDB;
                    }
                    return SourceType.None;
                })
                .ToReactiveProperty(SourceType.None)
                .AddTo(Disposables);
            Source.Subscribe(s => this.model.Source = s).AddTo(Disposables);
            Label = DataBaseID.CombineLatest(Source, (id, src) => $"{id} ({src})").ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Implement = Source.Select<SourceType, IAnnotationSettingViewModel>(s => {
                switch (s) {
                    case SourceType.MspDB:
                        return new LcmsMspAnnotationSettingViewModel(this.model);
                    case SourceType.TextDB:
                        return new LcmsTextDBAnnotationSettingViewModel(this.model);
                    default:
                        return null;
                }
            }).DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            Implement.Subscribe(impl => this.model.Implement = impl?.Model).AddTo(Disposables);
        }

        private readonly LcmsAnnotationSettingModel model;
        IAnnotationSettingModel IAnnotationSettingViewModel.Model => model;

        public ReadOnlyReactivePropertySlim<string> Label { get; }

        public ReadOnlyReactivePropertySlim<IAnnotationSettingViewModel> Implement { get; }

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReactiveProperty<SourceType> Source { get; }

        public List<SourceType> Sources { get; }

        public DelegateCommand BrowseCommand => browseCommand ?? (browseCommand = new DelegateCommand(Browse));
        private DelegateCommand browseCommand;

        private void Browse() {
            var ofd = new OpenFileDialog
            {
                Title = "Import a library file",
                Filter = "MSP file(*.msp)|*.msp*",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                DataBasePath.Value = ofd.FileName;
            }
        }

        public static LcmsAnnotationSettingViewModel Create() {
            var m = new LcmsAnnotationSettingModel();
            return new LcmsAnnotationSettingViewModel(m);
        }
    }
}
