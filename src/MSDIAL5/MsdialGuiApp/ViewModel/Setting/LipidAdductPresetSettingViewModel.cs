using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>
    /// Picks the lipid adduct preset of a staged lipid database, and imports preset files into the user's
    /// preset folder.
    /// </summary>
    public sealed class LipidAdductPresetSettingViewModel : ViewModelBase
    {
        private readonly DataBaseSettingModel _model;

        public LipidAdductPresetSettingViewModel(DataBaseSettingModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Presets = new ObservableCollection<LipidAdductPreset>(model.GetAdductPresets());
            var initial = model.AdductPreset;
            Preset = new ReactivePropertySlim<LipidAdductPreset?>(Presets.FirstOrDefault(p => p.Name == initial?.Name) ?? initial).AddTo(Disposables);
            // Only a choice made here counts as a change: a restored database keeps its stored adducts otherwise.
            Preset.Skip(1).Where(p => p != null).Subscribe(p => _model.AdductPreset = p).AddTo(Disposables);
            ImportCommand = new ReactiveCommand().WithSubscribe(Import).AddTo(Disposables);
        }

        public ObservableCollection<LipidAdductPreset> Presets { get; }

        public ReactivePropertySlim<LipidAdductPreset?> Preset { get; }

        public ReactiveCommand ImportCommand { get; }

        /// <summary>Where user preset files are kept, for the tooltip.</summary>
        public string PresetFolder => _model.AdductPresetStore.Folder;

        private void Import() {
            var dialog = new OpenFileDialog
            {
                Title = "Load a lipid adduct preset",
                Filter = "Lipid adduct preset(*.txt;*.tsv)|*.txt;*.tsv|All(*)|*",
                RestoreDirectory = true,
            };
            if (dialog.ShowDialog() != true) {
                return;
            }
            LipidAdductPreset preset;
            try {
                preset = _model.AdductPresetStore.Import(dialog.FileName, out var errors);
                if (errors.Count > 0) {
                    MessageBroker.Default.Publish(new ErrorMessageBoxRequest
                    {
                        ButtonType = System.Windows.MessageBoxButton.OK,
                        Caption = "Some preset lines could not be read.",
                        Content = string.Join(Environment.NewLine, errors.Take(10)),
                    });
                }
            }
            catch (IOException ex) {
                MessageBroker.Default.Publish(new ErrorMessageBoxRequest
                {
                    ButtonType = System.Windows.MessageBoxButton.OK,
                    Caption = "The preset could not be loaded.",
                    Content = ex.Message,
                });
                return;
            }
            Presets.Clear();
            foreach (var p in _model.GetAdductPresets()) {
                Presets.Add(p);
            }
            Preset.Value = Presets.FirstOrDefault(p => p.Name == preset.Name) ?? preset;
        }
    }
}
