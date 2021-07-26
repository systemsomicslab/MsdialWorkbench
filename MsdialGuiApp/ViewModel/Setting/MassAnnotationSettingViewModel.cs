using CompMs.App.Msdial.Model.Setting;
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
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            Source.Subscribe(s => this.model.Source = s).AddTo(Disposables);
            Label = DataBaseID.CombineLatest(Source, (id, src) => $"{id} ({src})").ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        private readonly MassAnnotationSettingModel model;
        IAnnotationSettingModel IAnnotationSettingViewModel.Model => model;

        public ReadOnlyReactivePropertySlim<string> Label { get; }

        public MsRefSearchParameterBaseViewModel ParameterVM { get; }

        [PathExists(IsFile = true, ErrorMessage = "Database path is invalid.")]
        public ReactiveProperty<string> DataBasePath { get; }

        [Required(ErrorMessage = "Database name is required.")]
        public ReactiveProperty<string> DataBaseID { get; }

        public ReadOnlyReactivePropertySlim<SourceType> Source { get; }
    }
}
