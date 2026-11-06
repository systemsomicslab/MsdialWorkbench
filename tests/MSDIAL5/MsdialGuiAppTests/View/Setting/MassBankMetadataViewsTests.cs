using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Parameter;
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
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Setting.Tests
{
    /// <summary>
    /// The views of the MassBank format constraint, laid out without a window: every binding resolves, the
    /// free-text and constrained controls swap with the toggle, and the lists show the saved values.
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
