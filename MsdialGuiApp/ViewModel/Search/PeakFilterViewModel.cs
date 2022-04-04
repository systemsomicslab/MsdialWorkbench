using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Search
{
    public class PeakFilterViewModel : ViewModelBase
    {
        private readonly PeakFilterModel model;

        public PeakFilterViewModel(PeakFilterModel model) {
            this.model = model;
            EnabledFilter = model.EnabledFilter;
            CheckedFilter = model.ObserveProperty(m => m.CheckedFilter).ToReactiveProperty().AddTo(Disposables);
            CheckedFilter.Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(filter => this.model.CheckedFilter = filter)
                .AddTo(Disposables);
        }

        public DisplayFilter EnabledFilter { get; }
        public ReactiveProperty<DisplayFilter> CheckedFilter { get; }

        public bool RefMatched {
            get => ReadFilter(DisplayFilter.RefMatched);
            set => WriteFilter(DisplayFilter.RefMatched, value);
        }
        public bool EnableRefMatched => EnabledFilter.All(DisplayFilter.RefMatched);
        public bool Suggested {
            get => ReadFilter(DisplayFilter.Suggested);
            set => WriteFilter(DisplayFilter.Suggested, value);
        }
        public bool EnableSuggested => EnabledFilter.All(DisplayFilter.Suggested);
        public bool Unknown {
            get => ReadFilter(DisplayFilter.Unknown);
            set => WriteFilter(DisplayFilter.Unknown, value);
        }
        public bool EnableUnknown => EnabledFilter.All(DisplayFilter.Unknown);
        public bool Ms2Acquired {
            get => ReadFilter(DisplayFilter.Ms2Acquired);
            set => WriteFilter(DisplayFilter.Ms2Acquired, value);
        }
        public bool EnableMs2Acquired => EnabledFilter.All(DisplayFilter.Ms2Acquired);
        public bool MolecularIon {
            get => ReadFilter(DisplayFilter.MolecularIon);
            set => WriteFilter(DisplayFilter.MolecularIon, value);
        }
        public bool EnableMolecularIon => EnabledFilter.All(DisplayFilter.MolecularIon);
        public bool Blank {
            get => ReadFilter(DisplayFilter.Blank);
            set => WriteFilter(DisplayFilter.Blank, value);
        }
        public bool EnableBlank => EnabledFilter.All(DisplayFilter.Blank);
        public bool UniqueIons {
            get => ReadFilter(DisplayFilter.UniqueIons);
            set => WriteFilter(DisplayFilter.UniqueIons, value);
        }
        public bool EnableUniqueIons => EnabledFilter.All(DisplayFilter.UniqueIons);
        public bool CcsMatched {
            get => ReadFilter(DisplayFilter.CcsMatched);
            set => WriteFilter(DisplayFilter.CcsMatched, value);
        }
        public bool EnableCcsMatched => EnabledFilter.All(DisplayFilter.CcsMatched);
        public bool ManuallyModified {
            get => ReadFilter(DisplayFilter.ManuallyModified);
            set => WriteFilter(DisplayFilter.ManuallyModified, value);
        }
        public bool EnableManuallyModified => EnabledFilter.All(DisplayFilter.ManuallyModified);

        private bool ReadFilter(DisplayFilter flag) {
            return (flag & CheckedFilter.Value & EnabledFilter) != 0;
        }

        private bool WriteFilter(DisplayFilter flag, bool value) {
            var availableFilter = flag & EnabledFilter;
            if (availableFilter != 0) {
                if (value) {
                    if (availableFilter.Any(~CheckedFilter.Value)) {
                        CheckedFilter.Value |= availableFilter;
                        return true;
                    }
                }
                else {
                    if (availableFilter.Any(CheckedFilter.Value)) {
                        CheckedFilter.Value &= ~availableFilter;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
