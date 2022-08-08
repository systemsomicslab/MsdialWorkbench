using CompMs.App.Msdial.Model.Information;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Information
{
    internal sealed class PeakInformationViewModel : ViewModelBase
    {
        private readonly PeakInformationModel _model;

        public PeakInformationViewModel(PeakInformationModel model) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Annotation = model.ObserveProperty(m => m.Annotation).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AdductIonName = model.ObserveProperty(m => m.AdductIonName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Formula = model.ObserveProperty(m => m.Formula).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Ontology = model.ObserveProperty(m => m.Ontology).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            InChIKey = model.ObserveProperty(m => m.InChIKey).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Comment = model.ObserveProperty(m => m.Comment).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PeakPoints = model.PeakPoint.ToReadOnlyReactiveCollection(m => new PeakPointViewModel(m)).AddTo(Disposables);
            PeakAmounts = model.PeakAmount.ToReadOnlyReactiveCollection(m => new PeakAmountViewModel(m)).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string> Annotation { get; }
        public ReadOnlyReactivePropertySlim<string> AdductIonName { get; }
        public ReadOnlyReactivePropertySlim<Formula> Formula { get; }
        public ReadOnlyReactivePropertySlim<string> Ontology { get; }
        public ReadOnlyReactivePropertySlim<string> InChIKey { get; }
        public ReadOnlyReactivePropertySlim<string> Comment { get; }
        public ReadOnlyReactiveCollection<PeakPointViewModel> PeakPoints { get; }
        public ReadOnlyReactiveCollection<PeakAmountViewModel> PeakAmounts { get; }
    }

    internal sealed class PeakPointViewModel : ViewModelBase
    {
        public PeakPointViewModel(IPeakPoint model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }
            Label = model.ObserveProperty(m => m.Label).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Point = model.ObserveProperty(m => m.Point).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
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
            Label = model.ObserveProperty(m => m.Label).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Amount = model.ObserveProperty(m => m.Amount).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string> Label { get; }
        public ReadOnlyReactivePropertySlim<double> Amount { get; }
    }
}
