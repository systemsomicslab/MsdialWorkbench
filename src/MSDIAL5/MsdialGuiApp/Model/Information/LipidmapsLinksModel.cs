using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Information;

/// <summary>
/// Provides functionality to retrieve and manage links to LIPIDMAPS entries based on molecular properties.
/// </summary>
/// <remarks>
/// This class interacts with the Lipidmaps REST API to fetch lipid information using a molecule's
/// InChIKey. It maintains the current state of retrieved items and whether a retrieval operation is in
/// progress.
/// </remarks>
public sealed class LipidmapsLinksModel : DisposableModelBase
{
    private readonly LipidmapsRestAPIHandler _handler;
    private readonly Subject<LipidmapsLinkItem[]> _currentItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="LipidmapsLinksModel"/> class, which manages the retrieval and
    /// observation of lipid data from the LIPIDMAPS database.
    /// </summary>
    /// <remarks>
    /// This constructor sets up the necessary subscriptions to observe changes in the provided
    /// molecule sequence and retrieve corresponding lipid data asynchronously. The retrieved lipid data is published to
    /// the <see cref="CurrentItems"/> observable sequence.
    /// </remarks>
    /// <param name="handler">The <see cref="LipidmapsRestAPIHandler"/> used to perform API requests to the Lipidmaps database.</param>
    /// <param name="molecule">An observable sequence of <see cref="IMoleculeProperty"/> instances representing the molecules for which lipid
    /// data is retrieved.</param>
    public LipidmapsLinksModel(LipidmapsRestAPIHandler handler, IObservable<MsScanMatchResult?> molecule) {
        _handler = handler;
        _currentItems = new Subject<LipidmapsLinkItem[]>().AddTo(Disposables);
        CurrentItems = _currentItems;

        Disposables.Add(
            molecule.Select(m => {
                if (m?.Name is null) {
                    _currentItems.OnNext([]);
                    return Observable.Return((hasItems: false, retrieving: false));
                }
                return Observable.Create<(bool hasItems, bool retrieving)>(observer => {
                    observer.OnNext((hasItems: false, retrieving: true));
                    var disposable = Observable.FromAsync(async token => {
                        var lipids = await _handler.RetrieveLipidsAsync(m.Name, token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        _currentItems.OnNext(lipids.Select(l => new LipidmapsLinkItem(l.LipidName, l.LipidmapsPage, l.PubChemPage)).ToArray());
                        return (hasItems: lipids.Length > 0, retrieving: false);
                    }).Subscribe(observer);
                    return disposable;
                });
            }).Switch().Subscribe(results => {
                HasItems = results.hasItems;
                Retrieving = results.retrieving;
            })
        );
    }

    /// <summary>
    /// Gets a value indicating whether the retrieval process is currently active.
    /// </summary>
    public bool Retrieving {
        get => _retrieving;
        private set => SetProperty(ref _retrieving, value);
    }
    private bool _retrieving;

    /// <summary>
    /// Gets a value indicating whether the collection contains any items.
    /// </summary>
    public bool HasItems {
        get => _hasItems;
        private set => SetProperty(ref _hasItems, value);
    }
    private bool _hasItems;

    /// <summary>
    /// Gets an observable sequence of the current Lipidmaps link items.
    /// </summary>
    public IObservable<LipidmapsLinkItem[]> CurrentItems { get; }
}
