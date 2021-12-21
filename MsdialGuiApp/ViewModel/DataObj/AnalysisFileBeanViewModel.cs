using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public abstract class FileBeanViewModel<T> : ViewModelBase where T: IFileBean
    {
        public FileBeanViewModel(T file) {
            File = file;
        }
        public T File { get; }

        public string FileName => File.FileName;

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected;
    }

    public class AnalysisFileBeanViewModel : FileBeanViewModel<AnalysisFileBean>
    {
        [Required(ErrorMessage = "AnalysisFilePath is required.")]
        [PathExists(IsDirectory = true, IsFile = true)]
        public ReactiveProperty<string> AnalysisFilePath { get; }

        [Required(ErrorMessage = "AnalysisFileName is required.")]
        public ReactiveProperty<string> AnalysisFileName { get; }

        [Required(ErrorMessage = "AnalysisFileType is required.")]
        public ReactiveProperty<AnalysisFileType> AnalysisFileType { get; }

        [Required(ErrorMessage = "AnalysisFileClass is required.")]
        public ReactiveProperty<string> AnalysisFileClass { get; }

        [Required(ErrorMessage = "AnalysisFileAnalyiticalOrder is required.")]
        [RegularExpression("[1-9]+[0-9]*")]
        public ReactiveProperty<string> AnalysisFileAnalyticalOrder { get; }

        [Required(ErrorMessage = "AnalysisFileId is required.")]
        [RegularExpression("[0-9]+")]
        public ReactiveProperty<string> AnalysisFileId { get; }

        [Required(ErrorMessage = "AnalysisFileIncluded is required.")]
        public ReactiveProperty<bool> AnalysisFileIncluded { get; }

        [Required(ErrorMessage = "AnalysisBatch is required.")]
        [RegularExpression("[0-9]+")]
        public ReactiveProperty<string> AnalysisBatch { get; }

        [Required(ErrorMessage = "Dilution factor is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> DilutionFactor { get; }

        [Required(ErrorMessage = "Response variable is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> ResponseVariable { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value).ToUnit();

        public AnalysisFileBeanViewModel(AnalysisFileBean file) : base(file) {
            AnalysisFilePath = new ReactiveProperty<string>(File.AnalysisFilePath)
                .SetValidateAttribute(() => AnalysisFilePath)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFilePath, (_, x) => x)
                .Subscribe(x => File.AnalysisFilePath = x)
                .AddTo(Disposables);

            var invalidChars = Path.GetInvalidFileNameChars();
            AnalysisFileName = new ReactiveProperty<string>(File.AnalysisFileName)
                .SetValidateAttribute(() => AnalysisFileName)
                .SetValidateNotifyError(name => name.IndexOfAny(invalidChars) >= 0 ? $"{name} contains invalid character(s)" : null)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileName, (_, x) => x)
                .Subscribe(x => File.AnalysisFileName = x)
                .AddTo(Disposables);

            AnalysisFileType = new ReactiveProperty<AnalysisFileType>(File.AnalysisFileType)
                .SetValidateAttribute(() => AnalysisFileType)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileType, (_, x) => x)
                .Subscribe(x => File.AnalysisFileType = x)
                .AddTo(Disposables);

            AnalysisFileClass = new ReactiveProperty<string>(File.AnalysisFileClass)
                .SetValidateAttribute(() => AnalysisFileClass)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileClass, (_, x) => x)
                .Subscribe(x => File.AnalysisFileClass = x)
                .AddTo(Disposables);

            AnalysisFileAnalyticalOrder = new ReactiveProperty<string>(File.AnalysisFileAnalyticalOrder.ToString())
                .SetValidateAttribute(() => AnalysisFileAnalyticalOrder)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileAnalyticalOrder, (_, x) => x)
                .Subscribe(x => File.AnalysisFileAnalyticalOrder = int.Parse(x))
                .AddTo(Disposables);

            AnalysisFileId = new ReactiveProperty<string>(File.AnalysisFileId.ToString())
                .SetValidateAttribute(() => AnalysisFileId)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileId, (_, x) => x)
                .Subscribe(x => File.AnalysisFileId = int.Parse(x))
                .AddTo(Disposables);

            AnalysisFileIncluded = new ReactiveProperty<bool>(File.AnalysisFileIncluded)
                .SetValidateAttribute(() => AnalysisFileIncluded)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileIncluded, (_, x) => x)
                .Subscribe(x => File.AnalysisFileIncluded = x)
                .AddTo(Disposables);

            AnalysisBatch = new ReactiveProperty<string>(File.AnalysisBatch.ToString())
                .SetValidateAttribute(() => AnalysisBatch)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisBatch, (_, x) => x)
                .Subscribe(x => File.AnalysisBatch = int.Parse(x))
                .AddTo(Disposables);

            DilutionFactor = new ReactiveProperty<string>(File.DilutionFactor.ToString())
                .SetValidateAttribute(() => DilutionFactor)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(DilutionFactor, (_, x) => x)
                .Subscribe(x => File.DilutionFactor = double.Parse(x))
                .AddTo(Disposables);

            ResponseVariable = new ReactiveProperty<string>(File.ResponseVariable.ToString())
                .SetValidateAttribute(() => ResponseVariable)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(ResponseVariable, (_, x) => x)
                .Subscribe(x => File.ResponseVariable = double.Parse(x))
                .AddTo(Disposables);

            HasErrors = new[]
            {
                AnalysisFilePath.ObserveHasErrors,
                AnalysisFileName.ObserveHasErrors,
                AnalysisFileType.ObserveHasErrors,
                AnalysisFileClass.ObserveHasErrors,
                AnalysisFileAnalyticalOrder.ObserveHasErrors,
                AnalysisFileId.ObserveHasErrors,
                AnalysisFileIncluded.ObserveHasErrors,
                AnalysisBatch.ObserveHasErrors,
                DilutionFactor.ObserveHasErrors,
                ResponseVariable.ObserveHasErrors
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);
            HasErrors.Subscribe(_ => Console.WriteLine($"{AnalysisFileName.Value} fired!"));
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }

    class AlignmentFileBeanViewModel : FileBeanViewModel<AlignmentFileBean>
    {
        public AlignmentFileBeanViewModel(AlignmentFileBean file) : base(file) {
        }
    }
}
