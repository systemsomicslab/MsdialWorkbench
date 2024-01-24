using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
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
        private readonly CheckChromatogramsModel _model;
        private readonly IMessageBroker _broker;

        public CheckChromatogramsViewModel(CheckChromatogramsModel model, IMessageBroker broker = null) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? MessageBroker.Default;

            var chromatograms = model.ObserveProperty(m => m.Chromatograms).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            DisplayChromatograms = chromatograms.Select(c => c.DisplayChromatograms).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var horizontalItem = chromatograms.Select(c => c.ChromAxisItemSelector.ObserveProperty(s => s.SelectedAxisItem)).Switch();
            var verticalItem = chromatograms.Select(c => c.AbundanceAxisItemSelector.ObserveProperty(s => s.SelectedAxisItem)).Switch();
            HorizontalAxis = horizontalItem.Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxis = verticalItem.Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            GraphTitle = chromatograms.Select(c => c.GraphTitle).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalTitle = horizontalItem.Select(item => item.GraphLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalTitle = verticalItem.Select(item => item.GraphLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalProperty = chromatograms.Select(c => c.HorizontalProperty).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalProperty = chromatograms.Select(c => c.VerticalProperty).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            InsertTic = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertTic).AddTo(Disposables);
            InsertBpc = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertBpc).AddTo(Disposables);
            InsertHighestEic = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertHighestEic).AddTo(Disposables);

            CopyAsTableCommand = new ReactiveCommand()
                .WithSubscribe(CopyAsTable)
                .AddTo(Disposables);
            SaveAsTableCommand = new AsyncReactiveCommand()
                .WithSubscribe(SaveAsTableAsync)
                .AddTo(Disposables);

            DiplayEicSettingValues = model.DisplayEicSettingValues
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
               .WithSubscribe(Apply)
               .AddTo(Disposables);
            ClearCommand = new ReactiveCommand()
                .WithSubscribe(model.Clear)
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ReadOnlyObservableCollection<DisplayChromatogram>> DisplayChromatograms { get; }

        public ReadOnlyReactivePropertySlim<IAxisManager<double>> HorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> VerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }
        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }
        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReactiveCommand CopyAsTableCommand { get; }

        private void CopyAsTable() {
            using var stream = new MemoryStream();
            _model.ExportAsync(stream, "\t").Wait();
            Clipboard.SetDataObject(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetString(stream.ToArray()));
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
        public ReactivePropertySlim<bool> InsertTic { get; }
        public ReactivePropertySlim<bool> InsertBpc { get; }
        public ReactivePropertySlim<bool> InsertHighestEic { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReactiveCommand ApplyCommand { get; }
        public ReactiveCommand ClearCommand { get; }

        private void Apply() {
            foreach (var value in DiplayEicSettingValues) {
                value.Commit();
            }
            _model.Update();
        }
    }
}
