using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.Lipidomics
{
    class LipidDbSetVM : ViewModelBase
    {
        public IonMode IonMode { get; }

        public LipidQueryBeanVM QueryVM {
            get => queryVM;
            set => SetProperty(ref queryVM, value);
        }

        public ICollectionView LbmQueryView {
            get => lbmQueryView;
            set => SetProperty(ref lbmQueryView, value);
        }

        private LipidQueryBeanVM queryVM;
        private readonly ObservableCollection<LbmQueryVM> lbmQueryVMs;
        private ICollectionView lbmQueryView;

        public LipidDbSetVM(LipidQueryBean queryBean, IonMode ionMode) {
            IonMode = ionMode;

            queryBean.IonMode = IonMode;
            queryVM = new LipidQueryBeanVM(queryBean);

            lbmQueryVMs = new ObservableCollection<LbmQueryVM>(queryBean.LbmQueries.Select(query => new LbmQueryVM(query)));

            lbmQueryView = CollectionViewSource.GetDefaultView(lbmQueryVMs);
            lbmQueryView.Filter += QueryFilter;

            queryVM.PropertyChanged += (s, e) => LbmQueryView.Refresh();
        }

        private bool QueryFilter(object obj) {
            var query = (LbmQueryVM)obj;

            if (query.IonMode != IonMode)
                return false;
            if (query.IonMode == IonMode.Positive)
                return true;
            if (QueryVM.SolventType == SolventType.CH3COONH4 && query.IonMode == IonMode.Negative && !query.AdductType.IsFA)
                return true;
            if (QueryVM.SolventType == SolventType.HCOONH4 && query.IonMode == IonMode.Negative && !query.AdductType.IsHac)
                return true;

            return false;
        }

        #region Command
        public DelegateCommand<Window> ContinueProcessCommand => continueProcessCommand ??= new DelegateCommand<Window>(ContinueProcess, ValidateLipidDbSetWindow);
        private DelegateCommand<Window>? continueProcessCommand;

        private void ContinueProcess(Window window) {
            if (ClosingMethod()) {
                window.DialogResult = true;
                window.Close();
            }
        }

        private bool ClosingMethod() {
            if (!QueryVM.Sync()) return false;
            if (!lbmQueryVMs.All(vm => vm.Sync())) return false;
            return true;
        }

        private bool ValidateLipidDbSetWindow(Window window) {
            return true;
        }

        public DelegateCommand<Window> CancelProcessCommand => cancelProcessCommand ??= new DelegateCommand<Window>(CancelProcess);
        private DelegateCommand<Window>? cancelProcessCommand;

        private void CancelProcess(Window window) {
            window.DialogResult = false;
            window.Close();
        }

        public DelegateCommand CheckAllCommand => checkAllCommand ??= new DelegateCommand(CheckAllProcess);
        private DelegateCommand? checkAllCommand;

        private void CheckAllProcess() {
            foreach (var obj in LbmQueryView) {
                ((LbmQueryVM)obj).IsSelected = true;
            }
        }

        public DelegateCommand RemoveAllCommand => removeAllCommand ??= new DelegateCommand(RemoveAllProcess);
        private DelegateCommand? removeAllCommand;

        private void RemoveAllProcess() {
            foreach (var obj in LbmQueryView) {
                ((LbmQueryVM)obj).IsSelected = false;
            }
        }

        #endregion
    }

    class LipidQueryBeanVM : ViewModelBase
    {
        public IonMode IonMode {
            get => ionMode;
            set => SetProperty(ref ionMode, value);
        }

        public CollisionType CollisionType {
            get => collisionType;
            set => SetProperty(ref collisionType, value);
        }

        public SolventType SolventType {
            get => solventType;
            set => SetProperty(ref solventType, value);
        }

        private IonMode ionMode;
        private CollisionType collisionType;
        private SolventType solventType;
        private readonly LipidQueryBean innerModel;

        public LipidQueryBeanVM(LipidQueryBean innerModel) {
            IonMode = innerModel.IonMode;
            CollisionType = innerModel.CollisionType;
            SolventType = innerModel.SolventType;
            this.innerModel = innerModel;
        }

        public bool Sync() {
            innerModel.CollisionType = CollisionType;
            innerModel.SolventType = SolventType;
            return true;
        }
    }

    class LbmQueryVM : ViewModelBase
    {
        public LbmClass LbmClass { get; }
        public AdductIon AdductType { get; }
        public IonMode IonMode { get; }

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private bool isSelected;

        private readonly LbmQuery innerModel;
        public LbmQueryVM(LbmQuery innerModel) {
            this.innerModel = innerModel;
            LbmClass = innerModel.LbmClass;
            AdductType = innerModel.AdductType;
            IonMode = innerModel.IonMode;
            IsSelected = innerModel.IsSelected;
        }

        public bool Sync() {
            innerModel.IsSelected = IsSelected;
            return true;
        }
    }
}
