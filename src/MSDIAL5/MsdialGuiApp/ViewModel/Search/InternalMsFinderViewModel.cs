using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class InternalMsFinderViewModel : ViewModelBase {

        public InternalMsFinderViewModel(InternalMsFinder model) {
            model = model;

        }

        public void ShowMetadata(InternalMsFinder metaboliteList) {
            metaboliteList = this._metaboliteList;
        }

        private readonly InternalMsFinder _model;

        private InternalMsfinderSettingModel internalMsfinderSettingModel { get; }

        public InternalMsfinderSettingViewModel InternalMsfinderSettingViewModel { get; }

        private readonly IMessageBroker _broker;

        private InternalMsFinder _metaboliteList { get; }
    }
}
