using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Proteomics.DataObj;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.Parameter;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    class EnzymeSettingViewModel : ViewModelBase {
        public EnzymeSettingViewModel(ProteomicsParameter Parameter) : this(new EnzymeSettingModel(Parameter)) {

        }

        public EnzymeSettingViewModel(EnzymeSettingModel model) {
            Model = model;
            SelectedFrom = Model.UnSelectedEnzymes.ToMappedReadOnlyObservableCollection(enzyme => new EnzymeBeanSelection(enzyme));
            Disposables.Add(SelectedFrom);
            SelectedTo = Model.SelectedEnzymes.ToMappedReadOnlyObservableCollection(enzyme => new EnzymeBeanSelection(enzyme));
            Disposables.Add(SelectedTo);

            maxMissedCleavage = model.MaxMissedCleavage;

            var notifir = new PropertyChangedNotifier(Model);
            Disposables.Add(notifir);
            notifir
                .SubscribeTo(nameof(Model.MaxMissedCleavage), () => MaxMissedCleavage = Model.MaxMissedCleavage);

        }

        public EnzymeSettingModel Model { get; }

        public DelegateCommand SetCommand => setCommand ??= new DelegateCommand(SetParam, CanSet);
        private DelegateCommand? setCommand;

        private void SetParam() {
            result = Task.Run(() => Model.Set());
            SetCommand.RaiseCanExecuteChanged();
        }
        private Task? result;

        private bool CanSet() {
            return result?.Status != TaskStatus.Running && !HasValidationErrors;
        }
        public MappedReadOnlyObservableCollection<Enzyme, EnzymeBeanSelection> SelectedFrom { get; }

        public MappedReadOnlyObservableCollection<Enzyme, EnzymeBeanSelection> SelectedTo { get; }

        public DelegateCommand AddItemsCommand => addItemsCommand ??= new DelegateCommand(AddItems);
        private DelegateCommand? addItemsCommand;

        private void AddItems() {
            Model.Selects(SelectedFrom.Where(enzyme => enzyme.IsChecked).Select(enzyme => enzyme.Enzyme).ToList());
        }

        public DelegateCommand AddAllItemsCommand => addAllItemsCommand ??= new DelegateCommand(AddAllItems);
        private DelegateCommand? addAllItemsCommand;

        private void AddAllItems() {
            Model.Selects(SelectedFrom.Select(enzyme => enzyme.Enzyme).ToList());
        }

        public DelegateCommand RemoveItemsCommand => removeItemsCommand ??= new DelegateCommand(RemoveItems);
        private DelegateCommand? removeItemsCommand;

        private void RemoveItems() {
            Model.UnSelects(SelectedTo.Where(enzyme => enzyme.IsChecked).Select(enzyme => enzyme.Enzyme).ToList());
        }

        public DelegateCommand RemoveAllItemsCommand => removeAllItemsCommand ??= new DelegateCommand(RemoveAllItems);
        private DelegateCommand? removeAllItemsCommand;

        private void RemoveAllItems() {
            Model.UnSelects(SelectedTo.Select(enzyme => enzyme.Enzyme).ToList());
        }

        public int MaxMissedCleavage {
            get {
                return maxMissedCleavage;
            }
            set {
                if (SetProperty(ref maxMissedCleavage, value)) {
                    if (!ContainsError(nameof(maxMissedCleavage))) {
                        Model.MaxMissedCleavage = maxMissedCleavage;
                    }
                }
            }
        }

        private int maxMissedCleavage;

        protected override void OnErrorsChanged([CallerMemberName] string propertyname = "") {
            base.OnErrorsChanged(propertyname);
            SetCommand.RaiseCanExecuteChanged();
        }
    }

    class EnzymeBeanSelection : ViewModelBase {
        public EnzymeBeanSelection(Enzyme enzyme) {
            Enzyme = enzyme;
        }

        public Enzyme Enzyme { get; }

        public string Title => Enzyme.Title;

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private bool isSelected = false;

        public bool IsChecked {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        private bool isChecked;
    }
}
