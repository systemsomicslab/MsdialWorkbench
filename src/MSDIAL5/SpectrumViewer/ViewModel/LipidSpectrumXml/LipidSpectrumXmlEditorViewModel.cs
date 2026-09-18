using CompMs.App.SpectrumViewer.Model.LipidSpectrumXml;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.SpectrumViewer.ViewModel.LipidSpectrumXml
{
    public class LipidSpectrumXmlEditorViewModel : ViewModelBase
    {
        public LipidSpectrumXmlEditorViewModel(LipidSpectrumXmlEditorModel model) {
            Model = model;

            Name = Observable.Return("Lipid spectrum XML editor").ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            FilePath = Model.ObserveProperty(m => m.FilePath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ConstantsFilePath = Model.ToReactivePropertySlimAsSynchronized(m => m.ConstantsFilePath).AddTo(Disposables);

            Entries = Model.Entries.ToReadOnlyReactiveCollection(m => new LipidMsEntryViewModel(m)).AddTo(Disposables);
            Filter = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            MatchCurrentLipid = Model.ToReactivePropertySlimAsSynchronized(m => m.MatchCurrentLipid).AddTo(Disposables);
            FilteredEntries = new ObservableCollection<LipidMsEntryViewModel>();

            PreviewLipidViewModel = new LipidSelectionViewModel(Model.PreviewLipidModel).AddTo(Disposables);
            Adducts = Model.Adducts;
            PreviewAdduct = Model.ToReactivePropertySlimAsSynchronized(m => m.PreviewAdduct).AddTo(Disposables);

            // Two independent narrowings over the same list: free-text Filter, and (when
            // MatchCurrentLipid is on) membership in Model.GeneratorCandidates, which the Model keeps
            // in sync with the lipid class/adduct built on the left (see RefreshGeneratorCandidates).
            var refreshFilter = new[]
            {
                Filter.ToUnit(),
                MatchCurrentLipid.ToUnit(),
                PreviewLipidViewModel.LipidClass.ToUnit(),
                PreviewAdduct.ToUnit(),
                Entries.ObserveAddChanged().ToUnit(),
                Entries.ObserveRemoveChanged().ToUnit(),
                Entries.ObserveResetChanged().ToUnit(),
            }.Merge();
            refreshFilter.Subscribe(_ => {
                FilteredEntries.Clear();
                var keyword = Filter.Value ?? string.Empty;
                foreach (var e in Entries.Where(e =>
                        (keyword.Length == 0 || e.DisplayName.Value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        && (!MatchCurrentLipid.Value || Model.GeneratorCandidates.Contains(e.Model)))) {
                    FilteredEntries.Add(e);
                }
            }).AddTo(Disposables);

            SelectedEntry = new ReactivePropertySlim<LipidMsEntryViewModel>().AddTo(Disposables);
            SelectedEntry.Subscribe(vm => Model.SelectedEntry = vm?.Model).AddTo(Disposables);
            // Unlike before, the Model can now also set SelectedEntry itself (auto-picking a
            // generator candidate when the built lipid/adduct changes), so this side has to mirror
            // the Model back into the VM too, not just push VM selections down to it.
            Model.ObserveProperty(m => m.SelectedEntry).Subscribe(m => {
                var vm = Entries.FirstOrDefault(c => c.Model == m);
                if (SelectedEntry.Value != vm) {
                    SelectedEntry.Value = vm;
                }
            }).AddTo(Disposables);

            PreviewSpectrumViewModel = new SpectrumViewModel(Model.PreviewSpectrumModel).AddTo(Disposables);
            PreviewMessages = Model.ObserveProperty(m => m.LastPreviewMessages)
                .Select(msgs => string.Join("\n", msgs))
                .ToReadOnlyReactivePropertySlim(string.Empty)
                .AddTo(Disposables);

            GeneratePreviewCommand = new ReactiveCommand().WithSubscribe(Model.GeneratePreview).AddTo(Disposables);
            SaveCommand = new ReactiveCommand().WithSubscribe(Model.Save).AddTo(Disposables);
            OpenXmlCommand = new ReactiveCommand().WithSubscribe(OpenXmlViaDialog).AddTo(Disposables);
            OpenConstantsCommand = new ReactiveCommand().WithSubscribe(OpenConstantsViaDialog).AddTo(Disposables);
            CloseCommand = new ReactiveCommand().AddTo(Disposables);
        }

        public LipidSpectrumXmlEditorModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReadOnlyReactivePropertySlim<string> FilePath { get; }

        public ReactivePropertySlim<string> ConstantsFilePath { get; }

        public ReadOnlyReactiveCollection<LipidMsEntryViewModel> Entries { get; }

        public ReactivePropertySlim<string> Filter { get; }

        public ReactivePropertySlim<bool> MatchCurrentLipid { get; }

        public ObservableCollection<LipidMsEntryViewModel> FilteredEntries { get; }

        public ReactivePropertySlim<LipidMsEntryViewModel> SelectedEntry { get; }

        public LipidSelectionViewModel PreviewLipidViewModel { get; }

        public ObservableCollection<AdductIon> Adducts { get; }

        public ReactivePropertySlim<AdductIon> PreviewAdduct { get; }

        public SpectrumViewModel PreviewSpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<string> PreviewMessages { get; }

        public ReactiveCommand GeneratePreviewCommand { get; }

        public ReactiveCommand SaveCommand { get; }

        public ReactiveCommand OpenXmlCommand { get; }

        public ReactiveCommand OpenConstantsCommand { get; }

        public ReactiveCommand CloseCommand { get; }

        public void Open(string path) => Model.Open(path);

        public void SaveAs(string path) => Model.SaveAs(path);

        public void OpenConstants(string path) => Model.OpenConstants(path);

        private void OpenXmlViaDialog() {
            var dialog = new OpenFileDialog { Filter = "Lipid spectrum model XML (*.xml)|*.xml|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) {
                Open(dialog.FileName);
            }
        }

        private void OpenConstantsViaDialog() {
            var dialog = new OpenFileDialog { Filter = "Constants XML (*.xml)|*.xml|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) {
                OpenConstants(dialog.FileName);
            }
        }
    }
}
