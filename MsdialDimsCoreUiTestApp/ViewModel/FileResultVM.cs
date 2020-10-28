using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace MsdialDimsCoreUiTestApp.ViewModel
{
    class FileResultVM : ViewModelBase
    {
        public ICollectionView Ms2CollectionView {
            get => ms2CollectionView;
            set {
                var oldCollection = ms2CollectionView;
                if (SetProperty(ref ms2CollectionView, value)) {
                    ms2CollectionView.Filter += Ms2CollectionFilter;
                    if (oldCollection != null)
                        oldCollection.Filter -= Ms2CollectionFilter;
                }
            }
        }
        
        public Rect Ms1Area {
            get => ms1Area;
            set => SetProperty(ref ms1Area, value);
        }

        public bool RefMatchedChecked => _vm.RefMatchedChecked;
        public bool SuggestedChecked => _vm.SuggestedChecked;
        public bool UnknownChecked => _vm.UnknownChecked;

        private ICollectionView ms2CollectionView;
        private Rect ms1Area;
        private MainWindowVM _vm;
        private MsdialDimsParameter _param;

        public FileResultVM(MainWindowVM vm, MsdialDimsParameter param) {
            _vm = vm;
            _param = param;
            Ms2CollectionView = _vm.Ms2CollectionView;
            _vm.PropertyChanged += Ms2CollectionViewChanged;
            _vm.PropertyChanged += FilterPropertyChanged;
        }

        bool Ms2CollectionFilter(object obj) {
            var ms2 = (Model.Ms2Info)obj;

            return RefMatchedChecked && ms2.RefMatched
                || SuggestedChecked && ms2.Suggested
                || UnknownChecked && ms2.Unknown;
        }

        void Ms2CollectionViewChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(_vm.Ms2CollectionView))
                Ms2CollectionView = _vm.Ms2CollectionView;
        }

        void FilterPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(_vm.RefMatchedChecked)
                || e.PropertyName == nameof(_vm.SuggestedChecked)
                || e.PropertyName == nameof(_vm.UnknownChecked))
                Ms2CollectionView.Refresh();
        }
    }
}
