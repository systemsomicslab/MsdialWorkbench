using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class CheckChromatogramsViewModel : ViewModelBase
    {
        private readonly CheckChromatogramsModel _model;
        private readonly IMessageBroker _broker;

        public CheckChromatogramsViewModel(CheckChromatogramsModel model, IMessageBroker? broker = null) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? MessageBroker.Default;

            RangeSelectableChromatogramViewModel = model.ObserveProperty(m => m.RangeSelectableChromatogramModel)
                .Select(m => m is null ? null : new RangeSelectableChromatogramViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ChromatogramsViewModel = RangeSelectableChromatogramViewModel.Select(vm => vm?.ChromatogramsViewModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AccumulatedMs2SpectrumViewModels = model.ObserveProperty(m => m.AccumulatedMs2SpectrumModels)
                .Select(ms => ms.Select(m => new AccumulatedMs2SpectrumViewModel(m)).ToArray())
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim(Array.Empty<AccumulatedMs2SpectrumViewModel>())
                .AddTo(Disposables);

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

            ShowAccumulatedSpectrumCommand = RangeSelectableChromatogramViewModel.Select(vm => vm is not null)
                .ToAsyncReactiveCommand<AccumulatedMs2SpectrumViewModel>()
                .WithSubscribe(vm => ShowAccumulatedSpectrumAsync(vm, default)).AddTo(Disposables);
            DetectPeaksCommand = new ReactiveCommand().WithSubscribe(model.DetectPeaks).AddTo(Disposables);
            ResetPeaksCommand = new ReactiveCommand().WithSubscribe(model.ResetPeaks).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ChromatogramsViewModel { get; }
        public ReadOnlyReactivePropertySlim<RangeSelectableChromatogramViewModel?> RangeSelectableChromatogramViewModel { get; }
        public ReadOnlyReactivePropertySlim<AccumulatedMs2SpectrumViewModel[]> AccumulatedMs2SpectrumViewModels { get; }

        public AsyncReactiveCommand<AccumulatedMs2SpectrumViewModel> ShowAccumulatedSpectrumCommand { get; }

        private async Task ShowAccumulatedSpectrumAsync(AccumulatedMs2SpectrumViewModel vm, CancellationToken token) {
            var task = _model.AccumulateAsync(vm.Model, token);
            _broker.Publish(vm);
            await task;
        }

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

        public ReactiveCommand DetectPeaksCommand { get; }
        public ReactiveCommand ResetPeaksCommand { get; }

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
