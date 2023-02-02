using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class ProcessBaseParameterViewModel : ViewModelBase
    {
        private readonly ProcessBaseParameter model;

        public ReactivePropertySlim<ProcessOption> ProcessOption { get; }

        [Required(ErrorMessage = "Number of threads required.")]
        [RegularExpression("[1-9][0-9]*", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> NumThreads { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value);


        public ProcessBaseParameterViewModel(ProcessBaseParameter model) {
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            ProcessOption = new ReactivePropertySlim<ProcessOption>(this.model.ProcessOption)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => ProcessOption)
                .Subscribe(x => this.model.ProcessOption = x)
                .AddTo(Disposables);

            NumThreads = new ReactiveProperty<string>(this.model.NumThreads.ToString())
                .SetValidateAttribute(() => NumThreads)
                .AddTo(Disposables);
            CommitAsObservable
                .SelectMany(_ => NumThreads)
                .Select(x => Math.Max(1, Math.Min(Environment.ProcessorCount, int.Parse(x))))
                .Subscribe(x => this.model.NumThreads = x)
                .AddTo(Disposables);

            HasErrors = new[]
            {
                NumThreads.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim();
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }
}
