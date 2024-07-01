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
    class ModificationSettingViewModel : ViewModelBase {
        public ModificationSettingViewModel(ProteomicsParameter Parameter) : this(new ModificationSettingModel(Parameter)) {

        }

        public ModificationSettingViewModel(ModificationSettingModel model) {
            Model = model;

            maxNumberOfModificationsPerPeptide = model.MaxNumberOfModificationsPerPeptide;
            
            SelectedVariableModificationFrom = Model.UnSelectedVariableModifications.ToMappedReadOnlyObservableCollection(modification => new ModificationBeanSelection(modification));
            Disposables.Add(SelectedVariableModificationFrom);
            SelectedVariableModificationTo = Model.SelectedVariableModifications.ToMappedReadOnlyObservableCollection(modification => new ModificationBeanSelection(modification));
            Disposables.Add(SelectedVariableModificationTo);

            SelectedFixedModificationFrom = Model.UnSelectedFixedModifications.ToMappedReadOnlyObservableCollection(modification => new ModificationBeanSelection(modification));
            Disposables.Add(SelectedFixedModificationFrom);
            SelectedFixedModificationTo = Model.SelectedFixedModifications.ToMappedReadOnlyObservableCollection(modification => new ModificationBeanSelection(modification));
            Disposables.Add(SelectedFixedModificationTo);

            var notifir = new PropertyChangedNotifier(Model);
            Disposables.Add(notifir);
            notifir
                .SubscribeTo(nameof(Model.MaxNumberOfModificationsPerPeptide), () => MaxNumberOfModificationsPerPeptide = Model.MaxNumberOfModificationsPerPeptide);

        }

        public ModificationSettingModel Model { get; }

        public DelegateCommand SetCommand => setCommand ??= new DelegateCommand(SetParam, CanSet);
        private DelegateCommand? setCommand;

        private void SetParam() {
            result = Task.Run(() => Model.Set());
            //SetCommand.RaiseCanExecuteChanged();
        }
        private Task? result;

        private bool CanSet() {
            return result?.Status != TaskStatus.Running && !HasValidationErrors;
        }
        public MappedReadOnlyObservableCollection<Modification, ModificationBeanSelection> SelectedVariableModificationFrom { get; }

        public MappedReadOnlyObservableCollection<Modification, ModificationBeanSelection> SelectedVariableModificationTo { get; }

        public DelegateCommand AddVariableModItemsCommand => addVariableModItemsCommand ??= new DelegateCommand(AddVariableModItems);
        private DelegateCommand? addVariableModItemsCommand;

        private void AddVariableModItems() {
            Model.SelectsVariableModifications(SelectedVariableModificationFrom.Where(modification => modification.IsChecked).Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand AddAllVariableModItemsCommand => addAllVariableModItemsCommand ??= new DelegateCommand(AddAllVariableModItems);
        private DelegateCommand? addAllVariableModItemsCommand;

        private void AddAllVariableModItems() {
            Model.SelectsVariableModifications(SelectedVariableModificationFrom.Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand RemoveVariableModItemsCommand => removeVariableModItemsCommand ??= new DelegateCommand(RemoveVariableModItems);
        private DelegateCommand? removeVariableModItemsCommand;

        private void RemoveVariableModItems() {
            Model.UnSelectsVariableModifications(SelectedVariableModificationTo.Where(modification => modification.IsChecked).Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand RemoveAllVariableModItemsCommand => removeAllVariableModItemsCommand ??= new DelegateCommand(RemoveAllVariableModItems);
        private DelegateCommand? removeAllVariableModItemsCommand;

        private void RemoveAllVariableModItems() {
            Model.UnSelectsVariableModifications(SelectedVariableModificationTo.Select(modification => modification.Modification).ToList());
        }

        public MappedReadOnlyObservableCollection<Modification, ModificationBeanSelection> SelectedFixedModificationFrom { get; }

        public MappedReadOnlyObservableCollection<Modification, ModificationBeanSelection> SelectedFixedModificationTo { get; }

        public DelegateCommand AddFixedModItemsCommand => addFixedModItemsCommand ??= new DelegateCommand(AddFixedModItems);
        private DelegateCommand? addFixedModItemsCommand;

        private void AddFixedModItems() {
            Model.SelectsFixedModifications(SelectedFixedModificationFrom.Where(modification => modification.IsChecked).Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand AddAllFixedModItemsCommand => addAllFixedModItemsCommand ??= new DelegateCommand(AddAllFixedModItems);
        private DelegateCommand? addAllFixedModItemsCommand;

        private void AddAllFixedModItems() {
            Model.SelectsFixedModifications(SelectedFixedModificationFrom.Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand RemoveFixedModItemsCommand => removeFixedModItemsCommand ??= new DelegateCommand(RemoveFixedModItems);
        private DelegateCommand? removeFixedModItemsCommand;

        private void RemoveFixedModItems() {
            Model.UnSelectsFixedModifications(SelectedFixedModificationTo.Where(modification => modification.IsChecked).Select(modification => modification.Modification).ToList());
        }

        public DelegateCommand RemoveAllFixedModItemsCommand => removeAllFixedModItemsCommand ??= new DelegateCommand(RemoveAllFixedModItems);
        private DelegateCommand? removeAllFixedModItemsCommand;

        private void RemoveAllFixedModItems() {
            Model.UnSelectsFixedModifications(SelectedFixedModificationTo.Select(modification => modification.Modification).ToList());
        }

        public int MaxNumberOfModificationsPerPeptide {
            get {
                return maxNumberOfModificationsPerPeptide;
            }
            set {
                if (SetProperty(ref maxNumberOfModificationsPerPeptide, value)) {
                    if (!ContainsError(nameof(maxNumberOfModificationsPerPeptide))) {
                        Model.MaxNumberOfModificationsPerPeptide = maxNumberOfModificationsPerPeptide;
                    }
                }
            }
        }

        private int maxNumberOfModificationsPerPeptide;

        protected override void OnErrorsChanged([CallerMemberName] string propertyname = "") {
            base.OnErrorsChanged(propertyname);
            SetCommand.RaiseCanExecuteChanged();
        }
    }

    class ModificationBeanSelection : ViewModelBase {
        public ModificationBeanSelection(Modification modification) {
            Modification = modification;
        }

        public Modification Modification { get; }

        public string Title => Modification.Title;

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
