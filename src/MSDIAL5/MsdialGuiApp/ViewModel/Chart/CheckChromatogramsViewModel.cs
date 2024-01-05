using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class CheckChromatogramsViewModel : ViewModelBase
    {
        private readonly ChromatogramsModel _model;
        private readonly IMessageBroker _broker;

        public CheckChromatogramsViewModel(ChromatogramsModel chromatogramsModel, DisplayEicSettingModel settingModel, IMessageBroker broker = null) {
            _model = chromatogramsModel ?? throw new ArgumentNullException(nameof(chromatogramsModel));
            _broker = broker ?? MessageBroker.Default;

            CopyAsTableCommand = new ReactiveCommand()
                .WithSubscribe(CopyAsTable)
                .AddTo(Disposables);
            SaveAsTableCommand = new AsyncReactiveCommand()
                .WithSubscribe(SaveAsTableAsync)
                .AddTo(Disposables);

            DiplayEicSettingValues = settingModel.DisplayEicSettingValueModels
                .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
                .AddTo(Disposables);

            var settingHasError = new[] {
                DiplayEicSettingValues.ObserveAddChanged().ToUnit(),
                DiplayEicSettingValues.ObserveRemoveChanged().ToUnit(),
                DiplayEicSettingValues.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
            }.Merge()
            .Select(_ => DiplayEicSettingValues.Any(vm => vm.HasErrors.Value));

            ObserveHasErrors = new[]
            {
                settingHasError
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ApplyCommand = ObserveHasErrors.Inverse()
               .ToReactiveCommand()
               .WithSubscribe(Commit)
               .AddTo(Disposables);
            ClearCommand = new ReactiveCommand()
                .WithSubscribe(settingModel.Clear)
                .AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<DisplayChromatogram> DisplayChromatograms => _model.DisplayChromatograms;

        public IAxisManager<double> HorizontalAxis => _model.ChromAxis;
        public IAxisManager<double> VerticalAxis => _model.AbundanceAxis;

        public string GraphTitle => _model.GraphTitle;

        public string HorizontalTitle => _model.HorizontalTitle;
        public string VerticalTitle => _model.VerticalTitle;

        public string HorizontalProperty => _model.HorizontalProperty;
        public string VerticalProperty => _model.VerticalProperty;

        public ReactiveCommand CopyAsTableCommand { get; }

        private void CopyAsTable() {
            using var stream = new MemoryStream();
            _model.ExportAsync(stream, "\t").Wait();
            Clipboard.SetDataObject(Encoding.UTF8.GetString(stream.ToArray()));
        }

        public AsyncReactiveCommand SaveAsTableCommand { get; }

        private async Task SaveAsTableAsync() {
            var fileName = string.Empty;
            var request = new SaveFileNameRequest(name => fileName = name)
            {
                AddExtension = true,
                Filter = "tab separated values|*.txt",
                RestoreDirectory = true,
            };
            _broker.Publish(request);
            if (request.Result == true) {
                using var stream = File.Open(fileName, FileMode.Create);
                await _model.ExportAsync(stream, "\t").ConfigureAwait(false);
            }
        }

        public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> DiplayEicSettingValues { get; }
        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand ApplyCommand { get; }
        public ReactiveCommand ClearCommand { get; }

        private void Commit() {
            foreach (var value in DiplayEicSettingValues) {
                value.Commit();
            }
        }
    }
}
