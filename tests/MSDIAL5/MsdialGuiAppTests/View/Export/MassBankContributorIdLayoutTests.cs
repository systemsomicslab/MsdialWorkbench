using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Export.Tests
{
    /// <summary>
    /// The MassBank contributor ID of the two export dialogs (Export &gt; Peak list result and Export &gt; Alignment
    /// result): the message that says what the ID must be takes a line of its own under the text box, so it never
    /// covers the check boxes below it. The section's own DataTemplate, taken from the dialog, is shown off screen.
    /// </summary>
    [TestClass]
    public class MassBankContributorIdLayoutTests
    {
        private const string Contributor = "MSDIAL_TEST";

        [TestMethod]
        public void PeakListDialog_TheContributorIdMessageHasALineOfItsOwn() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
                var model = new MsdialAnalysisMassBankRecordExportModel(parameter, ValidStudyContext(parameter), new FlagEvaluator(), new MessageBroker());
                using var vm = new MsdialAnalysisMassBankRecordExportViewModel(model);
                AssertTheMessageHasALineOfItsOwn(new AnalysisResultExportWin(), vm,
                    select: () => vm.ShouldExport.Value = true,
                    fix: () => vm.ContributorID.Value = Contributor);
            });
            TestApplication.WaitForBackgroundWork();
        }

        [TestMethod]
        public void AlignmentDialog_TheContributorIdMessageHasALineOfItsOwn() {
            TestApplication.Ensure();
            Application.Current.Dispatcher.Invoke(() => {
                var evaluator = new FlagEvaluator();
                using var filtering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(FilterEnableStatus.All);
                var filter = filtering.CreateFilter(new PeakFilterModel(DisplayFilter.All), evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
                var supplyer = new AlignmentPeakSpotSupplyer(new ReactivePropertySlim<IAlignmentModel?>(null), filter);
                var parameter = new ProjectBaseParameter { IonMode = IonMode.Positive, };
                var model = new AlignmentResultMassBankRecordExportModel(supplyer, parameter, ValidStudyContext(parameter), evaluator, new MessageBroker());
                using var vm = new AlignmentResultMassBankRecordExportViewModel(model) { IsExpanded = true, };
                AssertTheMessageHasALineOfItsOwn(new AlignmentResultExportWin(), vm,
                    select: () => vm.IsSelected.Value = true,
                    fix: () => vm.ContributorID.Value = Contributor);
            });
            TestApplication.WaitForBackgroundWork();
        }

        private static void AssertTheMessageHasALineOfItsOwn(Window dialog, object viewModel, Action select, Action fix) {
            var template = FindTemplate(dialog, viewModel.GetType());
            dialog.Close();
            var host = new Window
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000, Top = -20000,
                Width = 560, Height = 400,
                ShowActivated = false,
                ShowInTaskbar = false,
                Content = new AdornerDecorator { Child = new ContentControl { Content = viewModel, ContentTemplate = template, VerticalAlignment = VerticalAlignment.Top, }, },
            };
            try {
                host.Show();
                select();
                Settle(host);

                var box = (TextBox)Find(host, "TextBox_MassBankContributorID");
                var message = (TextBlock)Find(host, "TextBlock_MassBankContributorIDError");
                var suggested = Descendants(host).OfType<CheckBox>().Distinct().Single(c => Equals(c.Content, "Include suggested annotations"));
                Assert.IsTrue(Validation.GetHasError(box), "an empty contributor ID is refused once MassBank is chosen");
                Assert.AreEqual(Visibility.Visible, message.Visibility);
                Assert.AreEqual(AlignmentResultMassBankRecordExportViewModel.ContributorIdError, message.Text);

                var boxBounds = Bounds(box, host);
                var messageBounds = Bounds(message, host);
                var checkBoxBounds = Bounds(suggested, host);
                Assert.IsTrue(messageBounds.Top >= boxBounds.Bottom - 0.5, $"the message ({messageBounds}) starts under the text box ({boxBounds})");
                Assert.IsTrue(checkBoxBounds.Top >= messageBounds.Bottom - 0.5, $"the check box ({checkBoxBounds}) starts under the message ({messageBounds})");
                Assert.IsTrue(messageBounds.Right <= host.ActualWidth, "the message wraps inside the dialog");

                // The adorner marks the field with "!" beside it and draws nothing under it.
                var adorners = AdornerLayer.GetAdornerLayer(box)?.GetAdorners(box) ?? new Adorner[0];
                Assert.AreEqual(1, adorners.Length);
                var texts = Descendants(adorners[0]).OfType<TextBlock>().Distinct().ToList();
                CollectionAssert.AreEqual(new[] { "!" }, texts.Select(t => t.Text).ToArray());
                foreach (var text in texts) {
                    Assert.IsTrue(Bounds(text, host).Bottom <= checkBoxBounds.Top + 0.5, "the mark stays clear of the check box");
                }

                fix();
                Settle(host);
                Assert.IsFalse(Validation.GetHasError(box));
                Assert.AreEqual(Visibility.Collapsed, message.Visibility, "a valid ID takes no space for a message");
            }
            finally {
                host.Close();
            }
        }

        private static DataTemplate FindTemplate(DependencyObject root, Type viewModelType) {
            var key = new DataTemplateKey(viewModelType);
            var template = Descendants(root).OfType<FrameworkElement>()
                .Select(e => e.Resources.Contains(key) ? e.Resources[key] as DataTemplate : null)
                .FirstOrDefault(t => t is not null);
            Assert.IsNotNull(template, $"the dialog has a DataTemplate for {viewModelType.Name}");
            return template!;
        }

        private static StudyContextModel ValidStudyContext(ProjectBaseParameter parameter) {
            return new StudyContextModel(parameter)
            {
                InstrumentType = "LC-ESI-QTOF",
                Instrument = "TripleTOF 6600, SCIEX",
                Authors = "MS-DIAL test",
                License = "CC BY",
            };
        }

        private static Rect Bounds(FrameworkElement element, Visual root) {
            return element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize));
        }

        private static void Settle(Window window) {
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            window.UpdateLayout();
        }

        private static FrameworkElement Find(DependencyObject root, string automationId) {
            var found = Descendants(root).OfType<FrameworkElement>().FirstOrDefault(e => AutomationProperties.GetAutomationId(e) == automationId);
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

        private sealed class FlagEvaluator : IMatchResultEvaluator<MsScanMatchResult>
        {
            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) => results.First();
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) => results.ToList();
            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) => results.Where(r => r.IsReferenceMatched).ToList();
            public bool IsReferenceMatched(MsScanMatchResult result) => result.IsReferenceMatched;
            public bool IsAnnotationSuggested(MsScanMatchResult result) => result.IsAnnotationSuggested;
        }
    }
}
