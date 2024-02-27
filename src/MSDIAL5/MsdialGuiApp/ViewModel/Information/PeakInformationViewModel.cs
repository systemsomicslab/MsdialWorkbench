using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Information
{
    internal sealed class PeakInformationViewModel : ViewModelBase
    {
        private readonly IPeakInformationModel _model;

        public PeakInformationViewModel(IPeakInformationModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Annotation = model.ObserveProperty(m => m.Annotation).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AdductIonName = model.ObserveProperty(m => m.AdductIonName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Formula = model.ObserveProperty(m => m.Formula).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Ontology = model.ObserveProperty(m => m.Ontology).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            InChIKey = model.ObserveProperty(m => m.InChIKey).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Comment = model.ObserveProperty(m => m.Comment).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PeakPoints = model.ObserveProperty(m => m.PeakPoints)
                .SkipNull()
                .Select(ps => ps.ToReadOnlyReactiveCollection(m => new PeakPointViewModel(m)))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PeakAmounts = model.ObserveProperty(m => m.PeakAmounts)
                .SkipNull()
                .Select(ps => ps.ToReadOnlyReactiveCollection(m => new PeakAmountViewModel(m)))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string?> Annotation { get; }
        public ReadOnlyReactivePropertySlim<string?> AdductIonName { get; }
        public ReadOnlyReactivePropertySlim<string?> Formula { get; }
        public ReadOnlyReactivePropertySlim<string?> Ontology { get; }
        public ReadOnlyReactivePropertySlim<string?> InChIKey { get; }
        public ReadOnlyReactivePropertySlim<string?> Comment { get; }
        public ReadOnlyReactivePropertySlim<ReadOnlyReactiveCollection<PeakPointViewModel>> PeakPoints { get; }
        public ReadOnlyReactivePropertySlim<ReadOnlyReactiveCollection<PeakAmountViewModel>> PeakAmounts { get; }
    }

    internal sealed class PeakPointViewModel : ViewModelBase
    {
        public PeakPointViewModel(IPeakPoint model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            Label = model.ObserveProperty(m => m.Label).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
            Point = model.ObserveProperty(m => m.Point).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string> Label { get; }
        public ReadOnlyReactivePropertySlim<string> Point { get; }
    }

    internal sealed class PeakAmountViewModel : ViewModelBase
    {
        public PeakAmountViewModel(IPeakAmount model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            Label = model.ObserveProperty(m => m.Label).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
            Amount = model.ObserveProperty(m => m.Amount).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string> Label { get; }
        public ReadOnlyReactivePropertySlim<double> Amount { get; }
    }
}
