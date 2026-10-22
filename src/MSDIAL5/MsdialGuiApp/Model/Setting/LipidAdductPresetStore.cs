using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// The lipid adduct presets a user can pick from: their own preset files plus the MS-DIAL ones, and
    /// which one they used last per polarity.
    /// </summary>
    public sealed class LipidAdductPresetStore
    {
        private static readonly string[] _extensions = { ".txt", ".tsv" };

        private readonly string _folder;
        private readonly Func<IonMode, string?> _getRecent;
        private readonly Action<IonMode, string> _setRecent;

        public LipidAdductPresetStore(string folder, Func<IonMode, string?> getRecent, Action<IonMode, string> setRecent) {
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
            _getRecent = getRecent ?? throw new ArgumentNullException(nameof(getRecent));
            _setRecent = setRecent ?? throw new ArgumentNullException(nameof(setRecent));
        }

        /// <summary>User presets in %LocalAppData%\MSDIAL\LipidAdductPresets; the last used ones in the user settings.</summary>
        public static LipidAdductPresetStore Default { get; } = new LipidAdductPresetStore(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSDIAL", "LipidAdductPresets"),
            mode => mode == IonMode.Positive ? Properties.Settings.Default.RecentPositiveLipidAdductPreset : Properties.Settings.Default.RecentNegativeLipidAdductPreset,
            (mode, name) => {
                if (mode == IonMode.Positive) {
                    Properties.Settings.Default.RecentPositiveLipidAdductPreset = name;
                }
                else {
                    Properties.Settings.Default.RecentNegativeLipidAdductPreset = name;
                }
                Properties.Settings.Default.Save();
            });

        public string Folder => _folder;

        /// <summary>The user's presets for this polarity (unreadable files skipped), then the MS-DIAL ones.</summary>
        public IReadOnlyList<LipidAdductPreset> GetPresets(IonMode ionMode) {
            return UserPresets().Concat(LipidAdductPreset.BuiltIn)
                .Where(p => p.IonMode is null || p.IonMode == ionMode)
                .ToList();
        }

        /// <summary>The last used preset if it still exists, else the user's first, else the MS-DIAL default.</summary>
        public LipidAdductPreset GetDefault(IonMode ionMode) {
            var presets = GetPresets(ionMode);
            var recent = _getRecent(ionMode);
            return presets.FirstOrDefault(p => p.Name == recent)
                ?? UserPresets().FirstOrDefault(p => p.IonMode is null || p.IonMode == ionMode)
                ?? LipidAdductPreset.DefaultFor(ionMode);
        }

        public void MarkUsed(LipidAdductPreset preset, IonMode ionMode) {
            _setRecent(ionMode, preset.Name);
        }

        /// <summary>Reads a preset file and copies it into the user's preset folder.</summary>
        public LipidAdductPreset Import(string path, out IReadOnlyList<string> errors) {
            var preset = LipidAdductPreset.Load(path, out errors);
            Directory.CreateDirectory(_folder);
            var destination = Path.Combine(_folder, Path.GetFileName(path));
            if (!string.Equals(Path.GetFullPath(destination), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)) {
                File.Copy(path, destination, overwrite: true);
            }
            return preset;
        }

        private IEnumerable<LipidAdductPreset> UserPresets() {
            if (!Directory.Exists(_folder)) {
                yield break;
            }
            foreach (var file in Directory.EnumerateFiles(_folder).Where(f => _extensions.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f, StringComparer.OrdinalIgnoreCase)) {
                LipidAdductPreset preset;
                try {
                    preset = LipidAdductPreset.Load(file, out _);
                }
                catch (IOException) {
                    continue;
                }
                yield return preset;
            }
        }
    }
}
