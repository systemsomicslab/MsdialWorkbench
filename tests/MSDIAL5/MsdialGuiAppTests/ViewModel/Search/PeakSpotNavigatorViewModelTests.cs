using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace CompMs.App.Msdial.ViewModel.Search.Tests;

/// <summary>
/// A filter change in the navigator refreshes the peak spot views on the UI thread.
/// </summary>
[TestClass]
public class PeakSpotNavigatorViewModelTests
{
    private sealed class Evaluator : IMatchResultEvaluator<AlignmentSpotPropertyModel>
    {
        public List<AlignmentSpotPropertyModel> FilterByThreshold(IEnumerable<AlignmentSpotPropertyModel> results) => results.ToList();
        public bool IsAnnotationSuggested(AlignmentSpotPropertyModel result) => false;
        public bool IsReferenceMatched(AlignmentSpotPropertyModel result) => false;
        public List<AlignmentSpotPropertyModel> SelectReferenceMatchResults(IEnumerable<AlignmentSpotPropertyModel> results) => results.ToList();
        public AlignmentSpotPropertyModel SelectTopHit(IEnumerable<AlignmentSpotPropertyModel> results) => results.First();
    }

    private static void OnUi(Action action) => Application.Current.Dispatcher.Invoke(action);

    [TestMethod]
    public void AFailedRefreshIsReportedAndLaterFilterChangesStillRefresh() {
        TestApplication.Ensure();
        PeakSpotNavigatorViewModel navigator = null!;
        ListCollectionView extraView = null!;
        var failing = false;
        var refreshes = 0;
        var errors = new List<Exception>();
        DispatcherUnhandledExceptionEventHandler onError = (s, e) => { errors.Add(e.Exception); e.Handled = true; };
        OnUi(() => {
            Application.Current.DispatcherUnhandledException += onError;
            var spots = new ObservableCollection<AlignmentSpotPropertyModel>(Enumerable.Range(0, 3).Select(i => new AlignmentSpotPropertyModel(new AlignmentSpotProperty { AlignmentID = i, MasterAlignmentID = i, MassCenter = 100d + i, TimesCenter = new CompMs.Common.Components.ChromXs(1d + i), })));
            var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
            var manager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(spots, filtering);
            manager.AttachFilter(spots, new PeakFilterModel(DisplayFilter.All), new Evaluator(), FilterEnableStatus.All);
            navigator = new PeakSpotNavigatorViewModel(manager.PeakSpotNavigatorModel);

            // Another view of the navigator, whose refresh fails while failing is set.
            extraView = new ListCollectionView(new[] { 1, 2, 3 }) { Filter = _ => failing ? throw new InvalidOperationException("refresh failed") : true };
            ((INotifyCollectionChanged)extraView).CollectionChanged += (s, e) => {
                if (e.Action == NotifyCollectionChangedAction.Reset) {
                    refreshes++;
                }
            };
            manager.PeakSpotNavigatorModel.PeakSpotsCollection.Add(extraView);
        });
        try {
            OnUi(() => {
                failing = true;
                navigator.KeywordFilterViewModels.First(vm => vm.Label == "Name filter").Keywords.Value = "a";
            });
            TestApplication.WaitForBackgroundWork();

            // The failure reaches the application's unhandled-exception handling.
            Assert.IsTrue(errors.Count > 0 && errors.All(e => e is InvalidOperationException && e.Message == "refresh failed"), string.Join(" ---- ", errors));

            OnUi(() => {
                failing = false;
                refreshes = 0;
                navigator.KeywordFilterViewModels.First(vm => vm.Label == "Name filter").Keywords.Value = "b";
            });
            TestApplication.WaitForBackgroundWork();

            Assert.IsTrue(refreshes > 0, "a filter change after the failure did not refresh the view");
        }
        finally {
            OnUi(() => {
                Application.Current.DispatcherUnhandledException -= onError;
                navigator.Dispose();
            });
        }
    }
}
