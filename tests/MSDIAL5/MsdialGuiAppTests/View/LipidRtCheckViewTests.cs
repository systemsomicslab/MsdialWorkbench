using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.App.Msdial.View.Information;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Tests
{
    /// <summary>
    /// The lipid RT check's XAML, loaded on a WPF dispatcher: the parts no click-through could check here. The window and
    /// panel load, their bindings reach the view models, and the ribbon icon and the tag tooltip resolve.
    /// </summary>
    [TestClass]
    public class LipidRtCheckViewTests
    {
        [TestInitialize]
        public void Setup() {
            EnsureApplication();
        }

        // As in LcmsAnalysisModelWithoutRawTests: an application whose dispatcher runs on its own STA thread.
        private static void EnsureApplication() {
            if (Application.Current is not null) {
                return;
            }
            using var ready = new ManualResetEventSlim();
            var thread = new Thread(() => {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, };
                ready.Set();
                System.Windows.Threading.Dispatcher.Run();
            })
            {
                IsBackground = true,
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
        }

        // Runs on the application's thread and lets the binding engine finish there before and after, so that no deferred
        // binding is left to run while the test host shuts down.
        private static void OnUiThread(Action action) {
            Application.Current.Dispatcher.Invoke(() => {
                try {
                    action();
                }
                finally {
                    DoEvents();
                }
            });
        }

        /// <summary>Processes everything queued on the current dispatcher down to Background priority (bindings included).</summary>
        private static void DoEvents() {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(f => {
                ((DispatcherFrame)f).Continue = false;
                return null;
            }), frame);
            Dispatcher.PushFrame(frame);
        }

        private static IEnumerable<DependencyObject> Logical(DependencyObject root) {
            yield return root;
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>()) {
                foreach (var descendant in Logical(child)) {
                    yield return descendant;
                }
            }
        }

        [TestMethod]
        public void TheSettingWindowLoadsAndBindsToItsViewModel() {
            OnUiThread(() => {
                var model = new LipidRtCheckSettingModel(LipidRtTestSpots.Lipidomics(), new AlignmentSpotPropertyModel[0], () => Task.CompletedTask);
                using var viewModel = new LipidRtCheckSettingViewModel(model);
                var view = new LipidRtCheckSettingView { DataContext = viewModel, };
                DoEvents();
                try {
                    var elements = Logical(view).ToList();
                    var combos = elements.OfType<ComboBox>().ToList();
                    Assert.AreEqual(5, combos.Count);
                    Assert.AreSame(viewModel.OperatingPoints, combos[0].ItemsSource);
                    Assert.AreEqual(viewModel.OperatingPoint.Value, combos[0].SelectedItem);
                    Assert.AreSame(viewModel.MinorDuplicatePolicies, combos[4].ItemsSource);

                    combos[2].SelectedItem = MsdialCore.Parameter.LipidRtPlausibilityProfileKind.Mammalian;
                    DoEvents();
                    Assert.AreEqual(MsdialCore.Parameter.LipidRtPlausibilityProfileKind.Mammalian, model.PlausibilityProfile, "the pick reaches the model");

                    var run = elements.OfType<Button>().Single(b => (string)b.Content == "Run");
                    Assert.AreSame(viewModel.RunCommand, run.Command);
                    var close = elements.OfType<Button>().Single(b => (string)b.Content == "Close");
                    Assert.IsTrue(((System.Windows.Input.RoutedCommand)close.Command).CanExecute(null, close), "Close is bound to the window");
                }
                finally {
                    view.Close();
                }
            });
        }

        [TestMethod]
        public void ThePeakInformationPanelShowsTheVerdictRowOnlyWhereThereIsAVerdict() {
            OnUiThread(() => {
                var spots = LipidRtTestSpots.Spots();
                LipidRtCheckHooks.RunOnSpots(spots, LipidRtTestSpots.Lipidomics());
                var target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>(new AlignmentSpotPropertyModel(LipidRtTestSpots.Find(spots, 52, 1)));
                using var information = new PeakInformationAlignmentModel(target);
                using var viewModel = new PeakInformationViewModel(information);
                var view = new PeakInformationView { DataContext = viewModel, };
                DoEvents();

                var row =Logical(view).OfType<FrameworkElement>().Single(e => Equals(e.ToolTip, viewModel.LipidRtCheckDetail.Value) && e.ToolTip != null);
                Assert.AreEqual(Visibility.Visible, row.Visibility);
                var text = Logical(row).OfType<TextBox>().Single();
                StringAssert.StartsWith(text.Text, "Confirmed: TG 52:1 elutes after ");

                target.Value = new AlignmentSpotPropertyModel(spots[0]);
                DoEvents();
                Assert.AreEqual(Visibility.Collapsed, row.Visibility);
            });
        }

        [TestMethod]
        public void TheConfirmedTagCarriesItsDetailAsTooltip() {
            OnUiThread(() => {
                var styles = new ResourceDictionary { Source = new Uri("pack://application:,,,/MSDIAL;component/View/MainWindowStyle.xaml"), };
                var template = (DataTemplate)styles["EditPeakSpotTag"];
                var panel = (Panel)template.LoadContent();
                var spot = new AlignmentSpotProperty();
                spot.TagCollection.Select(PeakSpotTag.CONFIRMED, "MsdialLipidRtCheck/1.0");
                var model = new AlignmentSpotPropertyModel(spot);
                panel.DataContext = model;
                DoEvents();

                var confirmed = panel.Children.OfType<CheckBox>().First();
                Assert.AreEqual(true, confirmed.IsChecked);
                Assert.AreEqual(model.ConfirmedDetail, confirmed.ToolTip);
                StringAssert.StartsWith((string)confirmed.ToolTip, "Confirmed: set by the lipid RT check");

                confirmed.IsChecked = false; // the person's click
                DoEvents();
                Assert.IsTrue(spot.TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
                StringAssert.StartsWith((string)confirmed.ToolTip, "Confirmed: removed by a person");
            });
        }

        [TestMethod]
        public void TheRibbonIconLoads() {
            OnUiThread(() => {
                var icons = new ResourceDictionary { Source = new Uri("pack://application:,,,/MSDIAL;component/Icons.xaml"), };
                Assert.IsInstanceOfType(icons["LipidRtCheck_icon_is"], typeof(DrawingImage));
            });
        }
    }
}
