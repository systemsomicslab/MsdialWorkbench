using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
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

    internal sealed class AnalysisFileBeanViewModel : FileBeanViewModel<AnalysisFileBeanModel>
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

        [Required(ErrorMessage = "AcquisitionType is required.")]
        public ReactiveProperty<AcquisitionType> AcquisitionType { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value).ToUnit();

        public IObservable<Unit> ObserveChanges { get; }

        public AnalysisFileBeanViewModel(AnalysisFileBeanModel model) : base(model) {
            AnalysisFilePath = new ReactiveProperty<string>(model.AnalysisFilePath)
                .SetValidateAttribute(() => AnalysisFilePath)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFilePath, (_, x) => x)
                .Subscribe(x => model.AnalysisFilePath = x)
                .AddTo(Disposables);

            var invalidChars = Path.GetInvalidFileNameChars();
            AnalysisFileName = new ReactiveProperty<string>(model.AnalysisFileName)
                .SetValidateAttribute(() => AnalysisFileName)
                .SetValidateNotifyError(name => name.IndexOfAny(invalidChars) >= 0 ? $"{name} contains invalid character(s)" : null)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileName, (_, x) => x)
                .Subscribe(x => model.AnalysisFileName = x)
                .AddTo(Disposables);

            AnalysisFileType = new ReactiveProperty<AnalysisFileType>(model.AnalysisFileType)
                .SetValidateAttribute(() => AnalysisFileType)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileType, (_, x) => x)
                .Subscribe(x => model.AnalysisFileType = x)
                .AddTo(Disposables);

            AnalysisFileClass = new ReactiveProperty<string>(model.AnalysisFileClass)
                .SetValidateAttribute(() => AnalysisFileClass)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileClass, (_, x) => x)
                .Subscribe(x => model.AnalysisFileClass = x)
                .AddTo(Disposables);

            AnalysisFileAnalyticalOrder = new ReactiveProperty<string>(model.AnalysisFileAnalyticalOrder.ToString())
                .SetValidateAttribute(() => AnalysisFileAnalyticalOrder)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileAnalyticalOrder, (_, x) => x)
                .Subscribe(x => model.AnalysisFileAnalyticalOrder = int.Parse(x))
                .AddTo(Disposables);

            AnalysisFileId = new ReactiveProperty<string>(model.AnalysisFileId.ToString())
                .SetValidateAttribute(() => AnalysisFileId)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileId, (_, x) => x)
                .Subscribe(x => model.AnalysisFileId = int.Parse(x))
                .AddTo(Disposables);

            AnalysisFileIncluded = new ReactiveProperty<bool>(model.AnalysisFileIncluded)
                .SetValidateAttribute(() => AnalysisFileIncluded)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisFileIncluded, (_, x) => x)
                .Subscribe(x => model.AnalysisFileIncluded = x)
                .AddTo(Disposables);

            AnalysisBatch = new ReactiveProperty<string>(model.AnalysisBatch.ToString())
                .SetValidateAttribute(() => AnalysisBatch)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AnalysisBatch, (_, x) => x)
                .Subscribe(x => model.AnalysisBatch = int.Parse(x))
                .AddTo(Disposables);

            DilutionFactor = new ReactiveProperty<string>(model.DilutionFactor.ToString())
                .SetValidateAttribute(() => DilutionFactor)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(DilutionFactor, (_, x) => x)
                .Subscribe(x => model.DilutionFactor = double.Parse(x))
                .AddTo(Disposables);

            ResponseVariable = new ReactiveProperty<string>(model.ResponseVariable.ToString())
                .SetValidateAttribute(() => ResponseVariable)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(ResponseVariable, (_, x) => x)
                .Subscribe(x => model.ResponseVariable = double.Parse(x))
                .AddTo(Disposables);

            AcquisitionType = new ReactiveProperty<AcquisitionType>(model.AcquisitionType)
                .SetValidateAttribute(() => AcquisitionType)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AcquisitionType, (_, x) => x)
                .Subscribe(x => model.AcquisitionType = x)
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
                ResponseVariable.ObserveHasErrors,
                AcquisitionType.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                AnalysisFilePath.ToUnit(),
                AnalysisFileName.ToUnit(),
                AnalysisFileType.ToUnit(),
                AnalysisFileClass.ToUnit(),
                AnalysisFileAnalyticalOrder.ToUnit(),
                AnalysisFileId.ToUnit(),
                AnalysisFileIncluded.ToUnit(),
                AnalysisBatch.ToUnit(),
                DilutionFactor.ToUnit(),
                ResponseVariable.ToUnit(),
                AcquisitionType.ToUnit(),
            }.Merge();
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }
}
