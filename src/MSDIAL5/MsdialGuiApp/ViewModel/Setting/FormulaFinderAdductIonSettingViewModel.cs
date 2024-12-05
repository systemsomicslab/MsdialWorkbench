using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    class FormulaFinderAdductIonSettingViewModel : ViewModelBase
    {
        public FormulaFinderAdductIonSettingViewModel(FormulaFinderAdductIonSettingModel model)
        {
            Model = model;
            Ms1AdductIonSettingViewModel = new AdductIonSettingViewModel(model.Ms1AdductIonSetting, Observable.Return(true)).AddTo(Disposables);
            Ms2AdductIonSettingViewModel = new AdductIonSettingViewModel(model.Ms2AdductIonSetting, Observable.Return(true)).AddTo(Disposables);
        }

        public FormulaFinderAdductIonSettingModel Model { get; }
        public AdductIonSettingViewModel Ms1AdductIonSettingViewModel { get; }
        public AdductIonSettingViewModel Ms2AdductIonSettingViewModel { get; }

    }
}
