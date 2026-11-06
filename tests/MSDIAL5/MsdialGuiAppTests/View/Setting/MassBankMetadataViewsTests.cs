using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Parser;
using CompMs.Graphics.UI;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Setting.Tests
{
    /// <summary>
    /// The views of the MassBank format constraint: every binding resolves, the free-text and constrained controls
    /// swap with the toggle, and the lists show the saved values. Most are laid out without a window; the checks of
    /// what the person sees (messages under the fields) show the view in a SettingDialog
    /// off screen, as MainWindow does.
    /// </summary>
    [TestClass]
    public class MassBankMetadataViewsTests
    {
        [TestMethod]
        public void ProjectPropertySettingSwapsFreeTextForMassBankListsWithTheToggle() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var parameter = new ProjectBaseParameter
                {
                    InstrumentType = "LC-ESI-QFT",
                    Instrument = "Q Exactive Plus, Thermo Scientific",
                    Authors = "MS-DIAL test",
                    License = "CC BY-SA",
                };
                using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));
                var errors = CollectBindingErrors(() => {
                    var view = new ProjectPropertySettingView { DataContext = vm, };
                    Layout(view);

                    Assert.IsTrue(IsShown(Find(view, "TextBox_InstrumentType")), "free text while the project is not constrained");
                    Assert.IsTrue(IsShown(Find(view, "TextBox_License")));
                    Assert.IsFalse(IsShown(Find(view, "ComboBox_InstrumentIonisation")));
                    Assert.IsFalse(IsShown(Find(view, "ComboBox_License")));
                    Assert.IsTrue(IsShown(Find(view, "TextBox_MassBankContributorID")));

                    vm.ConstrainToMassBankFormat.Value = true;
                    Layout(view);

                    Assert.IsFalse(IsShown(Find(view, "TextBox_InstrumentType")));
                    Assert.IsFalse(IsShown(Find(view, "TextBox_License")));
                    Assert.IsTrue(IsShown(Find(view, "ComboBox_InstrumentSeparation")));
                    Assert.AreEqual("LC", ((ComboBox)Find(view, "ComboBox_InstrumentSeparation")).SelectedValue);
                    Assert.AreEqual("ESI", ((ComboBox)Find(view, "ComboBox_InstrumentIonisation")).SelectedItem);
                    Assert.AreEqual("QFT", (((ComboBox)Find(view, "ComboBox_InstrumentAnalyzer")).SelectedItem as MassBankAnalyzerPreset)?.Analyzer);
                    Assert.AreEqual("CC BY-SA", ((ComboBox)Find(view, "ComboBox_License")).SelectedItem);
                    Assert.AreEqual("AC$INSTRUMENT_TYPE: LC-ESI-QFT", ((TextBlock)Find(view, "TextBlock_InstrumentTypePreview")).Text);

                    ((ComboBox)Find(view, "ComboBox_InstrumentSeparation")).SelectedValue = "GC";
                    Assert.AreEqual("GC-ESI-QFT", vm.InstrumentType.Value, "choosing in the list composes the instrument type");
                });
                Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
            });
            TestApplication.WaitForBackgroundWork();
        }

        [TestMethod]
        public void TheProjectWizardShowsTheMassBankStyleChoices() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var date = new DateTime(2026, 11, 6);
                var model = new DatasetParameterSettingModel(date, new DatasetFileSettingModel(date), _ => { }, new MessageBroker());
                using var vm = new DatasetParameterSettingViewModel(model, Observable.Return(true));
                var errors = CollectBindingErrors(() => {
                    var view = new DatasetParameterSettingView { DataContext = vm, };
                    Layout(view);

                    var analyzer = (ComboBox)Find(view, "ComboBox_InstrumentAnalyzer");
                    Assert.IsTrue(analyzer.IsEnabled, "a new project can use the helper");
                    ((ComboBox)Find(view, "ComboBox_InstrumentIonisation")).SelectedItem = "ESI";
                    analyzer.SelectedItem = vm.InstrumentTypeComposer.Analyzers.Single(p => p.Analyzer == "QTOF");
                    Assert.AreEqual("ESI-QTOF", ((TextBox)Find(view, "TextBox_InstrumentType")).Text);
                    Assert.AreEqual("ESI-QTOF", model.InstrumentType);
                });
                Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
            });
            TestApplication.WaitForBackgroundWork();
        }

        [TestMethod]
        public void TheProjectWizardHelperIsDisabledForAnExistingProject() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var parameter = new MsdialLcmsParameter();
                parameter.ProjectParam.InstrumentType = "LC-ESI-QTOF";
                var model = new DatasetParameterSettingModel(parameter, new DatasetFileSettingModel(new DateTime(2026, 11, 6)), new MessageBroker());
                using var vm = new DatasetParameterSettingViewModel(model, Observable.Return(true));
                Assert.IsTrue(vm.IsReadOnly);
                var errors = CollectBindingErrors(() => {
                    var view = new DatasetParameterSettingView { DataContext = vm, };
                    Layout(view);

                    foreach (var id in new[] { "ComboBox_InstrumentSeparation", "ComboBox_InstrumentIonisation", "ComboBox_InstrumentAnalyzer", }) {
                        Assert.IsFalse(Find(view, id).IsEnabled, $"#{id} is read-only for an existing project");
                    }
                    Assert.AreEqual("QTOF", (((ComboBox)Find(view, "ComboBox_InstrumentAnalyzer")).SelectedItem as MassBankAnalyzerPreset)?.Analyzer,
                        "the saved value still shows");
                });
                Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
            });
            TestApplication.WaitForBackgroundWork();
        }

        // Hosted as MainWindow hosts it: in a SettingDialog, whose window template has no AdornerDecorator of its own.
        [TestMethod]
        public void TheSettingDialogShowsWhatAConstrainedFieldNeeds() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var parameter = new ProjectBaseParameter { InstrumentType = "LC-MS/MS", License = "CC-BY", };
                using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));
                var dialog = OffscreenDialog(vm);
                var errors = CollectBindingErrors(() => {
                    try {
                        dialog.Show();
                        Settle(dialog);
                        foreach (var id in new[] { "TextBox_Authors", "TextBox_Instrument", "TextBox_MassBankContributorID", }) {
                            Assert.AreEqual(0, Adorners(Find(dialog, id)).Length, $"#{id} has nothing to say while the project is not constrained");
                        }

                        vm.ConstrainToMassBankFormat.Value = true;
                        Settle(dialog);

                        Assert.IsFalse(vm.FinishCommand.CanExecute(null));
                        var authors = Find(dialog, "TextBox_Authors");
                        Assert.IsTrue(Validation.GetHasError(authors));
                        Assert.IsNotNull(AdornerLayer.GetAdornerLayer(authors), "the view brings its own adorner layer");
                        CollectionAssert.Contains(AdornerTexts(authors), ProjectPropertySettingViewModel.AuthorsRequired, "the message is drawn under the field");
                        CollectionAssert.Contains(AdornerTexts(Find(dialog, "TextBox_Instrument")), ProjectPropertySettingViewModel.InstrumentRequired);
                        Assert.IsTrue(AdornerTexts(Find(dialog, "ComboBox_License")).Any(t => t.Contains("\"CC-BY\"")));
                        foreach (var id in new[] { "ComboBox_InstrumentIonisation", "ComboBox_InstrumentAnalyzer", }) {
                            Assert.AreEqual(1, Adorners(Find(dialog, id)).Length, $"#{id} is marked");
                        }
                        // The composer's own messages show as the tooltip of its lists, and its preview says what is wrong.
                        Assert.AreEqual(MassBankInstrumentTypeViewModel.IonisationRequired, ((ComboBox)Find(dialog, "ComboBox_InstrumentIonisation")).ToolTip);
                        Assert.AreEqual(MassBankInstrumentTypeViewModel.AnalyzerRequired, ((ComboBox)Find(dialog, "ComboBox_InstrumentAnalyzer")).ToolTip);
                        StringAssert.Contains(((TextBlock)Find(dialog, "TextBlock_InstrumentTypePreview")).Text, "\"LC-MS/MS\" is not a MassBank instrument type");

                        // A wrong contributor ID says why Finish is disabled, whether or not the project is constrained.
                        vm.ConstrainToMassBankFormat.Value = false;
                        vm.MassBankContributorId.Value = "riken-ims";
                        Settle(dialog);
                        Assert.AreEqual(0, Adorners(authors).Length, "turning the constraint off clears the messages");
                        CollectionAssert.Contains(AdornerTexts(Find(dialog, "TextBox_MassBankContributorID")), ProjectPropertySettingViewModel.ContributorIdInvalid);
                        Assert.AreEqual("Separation: none, LC, GC or CE", ((ComboBox)Find(dialog, "ComboBox_InstrumentSeparation")).ToolTip);
                    }
                    finally {
                        dialog.Close();
                    }
                });
                Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
            });
            TestApplication.WaitForBackgroundWork();
        }

        private static SettingDialog OffscreenDialog(object viewModel) {
            return new SettingDialog
            {
                Height = 600, Width = 560,
                Title = "Project property setting",
                DataContext = viewModel,
                Content = new ProjectPropertySettingView(),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000, Top = -20000,
                ShowActivated = false,
                ShowInTaskbar = false,
            };
        }

        // Runs what the dispatcher has queued (layout, validation adorners) before the checks; called on its thread.
        private static void Settle(Window window) {
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }

        private static Adorner[] Adorners(UIElement element) {
            return AdornerLayer.GetAdornerLayer(element)?.GetAdorners(element) ?? new Adorner[0];
        }

        private static List<string> AdornerTexts(UIElement element) {
            return Adorners(element).SelectMany(a => Descendants(a)).OfType<TextBlock>().Select(t => t.Text).ToList();
        }

        private static void Layout(FrameworkElement view) {
            view.Measure(new Size(800, 900));
            view.Arrange(new Rect(0, 0, 800, 900));
            view.UpdateLayout();
        }

        private static FrameworkElement Find(DependencyObject root, string automationId) {
            var found = Descendants(root).OfType<FrameworkElement>()
                .FirstOrDefault(e => AutomationProperties.GetAutomationId(e) == automationId || e.Name == automationId);
            Assert.IsNotNull(found, $"#{automationId} is not in the view");
            return found!;
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root) {
            var children = LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().ToList();
            if (root is Visual) {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
                    var child = VisualTreeHelper.GetChild(root, i);
                    if (!children.Contains(child)) {
                        children.Add(child);
                    }
                }
            }
            foreach (var child in children) {
                yield return child;
                foreach (var descendant in Descendants(child)) {
                    yield return descendant;
                }
            }
        }

        // Without a window IsVisible is always false, so the element and its ancestors are checked instead.
        private static bool IsShown(FrameworkElement element) {
            DependencyObject? current = element;
            while (current is not null) {
                if (current is UIElement ui && ui.Visibility != Visibility.Visible) {
                    return false;
                }
                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
            return true;
        }

        private static List<string> CollectBindingErrors(Action action) {
            var listener = new BindingErrorListener();
            // Without a debugger WPF traces nothing until the trace sources are refreshed.
            PresentationTraceSources.Refresh();
            var source = PresentationTraceSources.DataBindingSource;
            var level = source.Switch.Level;
            source.Switch.Level = SourceLevels.Warning;
            source.Listeners.Add(listener);
            try {
                action();
            }
            finally {
                source.Listeners.Remove(listener);
                source.Switch.Level = level;
            }
            return listener.Errors;
        }

        private sealed class BindingErrorListener : TraceListener
        {
            public List<string> Errors { get; } = new List<string>();

            public override void Write(string? message) { }

            public override void WriteLine(string? message) {
                if (message is not null) {
                    Errors.Add(message);
                }
            }
        }
    }
}
