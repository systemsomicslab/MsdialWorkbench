using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting.Tests;

/// <summary>
/// The automatic alignment RT correction's settings in the GUI: a project made by the Console loads
/// with its values, they are checked as the Console checks them, and they are written back only when
/// alignment runs.
/// </summary>
[TestClass]
public class AutomaticRtCorrectionSettingModelTests
{
    private static List<AnalysisFileBean> Files() => Enumerable.Range(0, 3)
        .Select(id => new AnalysisFileBean { AnalysisFileId = id, AnalysisFileName = $"s{id}", AnalysisFileType = AnalysisFileType.Sample })
        .ToList();

    // Every value away from its default, as a Console method file can leave them.
    private static MsdialLcmsParameter ConsoleParameter(string library) {
        var parameter = new MsdialLcmsParameter();
        var correction = parameter.AlignmentBaseParam.AutomaticRtCorrection;
        correction.Execute = true;
        correction.AnchorMode = AutomaticRtCorrectionAnchorMode.Hybrid;
        correction.AnchorLibraryFilePath = library;
        correction.AnchorPeakSelectionMode = RetentionTimeCorrectionPeakSelectionMode.Weighted;
        correction.AnchorPeakSelectionRtWeight = 0.7F;
        correction.ReferenceFileId = 2;
        correction.LocalSupportRtWindow = 1.2F;
        correction.MinimumAnchorCount = 4;
        correction.MaximumAnchorCount = 9;
        correction.RtBinWidth = 0.4F;
        correction.MatchRtTolerance = 0.3F;
        correction.MinimumSampleCoverage = 0.6F;
        correction.IntensityQuantile = 0.8F;
        correction.MaximumPeakWidthQuantile = 0.45F;
        correction.MinimumSignalToNoise = 5F;
        correction.MinimumGaussianSimilarity = 0.2F;
        correction.MinimumIdealSlope = 0.1F;
        correction.OutlierMadThreshold = 4F;
        correction.ReferenceCentralityWeight = 0.25F;
        correction.InterpolateBlankByAnalyticalOrder = false;
        return parameter;
    }

    private static string Library() {
        var path = Path.Combine(Path.GetTempPath(), "msdial-gui-anchors-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "Name\tRT(min)\tRT tol.(min)\tm/z\tm/z tol.\tMinimum height\tT/F\n"
            + "IS1\t2\t1\t200.1\t0.01\t1000\tTrue\nIS2\t5\t1\t300.2\t0.01\t1000\tTrue\nIS3\t8\t1\t400.3\t0.01\t1000\tTrue\nIS4\t9\t1\t500.3\t0.01\t1000\tTrue\n");
        return path;
    }

    private static object[] Values(AutomaticAlignmentRetentionTimeCorrectionParameter p) => new object[] {
        p.Execute, p.AnchorMode, p.AnchorLibraryFilePath, p.AnchorPeakSelectionMode, p.AnchorPeakSelectionRtWeight, p.ReferenceFileId,
        p.LocalSupportRtWindow, p.MinimumAnchorCount, p.MaximumAnchorCount, p.RtBinWidth, p.MatchRtTolerance, p.MinimumSampleCoverage,
        p.IntensityQuantile, p.MaximumPeakWidthQuantile, p.MinimumSignalToNoise, p.MinimumGaussianSimilarity, p.MinimumIdealSlope,
        p.OutlierMadThreshold, p.ReferenceCentralityWeight, p.InterpolateBlankByAnalyticalOrder,
    };

    [TestMethod]
    public void AConsoleProjectLoadsWithItsValuesAndCommitsThemUnchanged() {
        var parameter = ConsoleParameter("anchors.txt");
        var before = Values(parameter.AlignmentBaseParam.AutomaticRtCorrection);
        var model = new AutomaticRtCorrectionSettingModel(parameter, Files(), isReadOnly: false);

        Assert.IsTrue(model.IsAvailable);
        CollectionAssert.AreEqual(before, Values(model.ToParameter()));
        Assert.AreEqual("s2", model.Reference.Label);

        model.Commit();
        CollectionAssert.AreEqual(before, Values(parameter.AlignmentBaseParam.AutomaticRtCorrection));
    }

    [TestMethod]
    public void ChangesReachTheParameterOnlyOnCommit() {
        var parameter = ConsoleParameter("anchors.txt");
        var model = new AutomaticRtCorrectionSettingModel(parameter, Files(), isReadOnly: false);
        model.AnchorMode = AutomaticRtCorrectionAnchorMode.UserOnly;
        model.Reference = model.ReferenceChoices[0];
        Assert.AreEqual(AutomaticRtCorrectionAnchorMode.Hybrid, parameter.AlignmentBaseParam.AutomaticRtCorrection.AnchorMode);

        model.Commit();
        Assert.AreEqual(AutomaticRtCorrectionAnchorMode.UserOnly, parameter.AlignmentBaseParam.AutomaticRtCorrection.AnchorMode);
        Assert.AreEqual(-1, parameter.AlignmentBaseParam.AutomaticRtCorrection.ReferenceFileId, "Automatic");
    }

    [TestMethod]
    public void ValuesAreCheckedAsTheConsoleChecksThem() {
        var library = Library();
        try {
            var parameter = ConsoleParameter(library);
            var model = new AutomaticRtCorrectionSettingModel(parameter, Files(), isReadOnly: false);
            Assert.IsNull(model.Validate(0.015d));

            model.AnchorLibraryFilePath = library + ".missing";
            StringAssert.StartsWith(model.Validate(0.015d), "Automatic RT correction anchor library could not be used:");

            model.AnchorMode = AutomaticRtCorrectionAnchorMode.Automatic;
            Assert.IsNull(model.Validate(0.015d), "Automatic mode reads no library");

            model.MinimumAnchorCount = 1;
            StringAssert.StartsWith(model.Validate(0.015d), "Automatic alignment RT correction settings cannot be used:");

            model.MinimumAnchorCount = 3;
            parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = true;
            StringAssert.Contains(model.Validate(0.015d), "correct the RT axis twice");

            model.Execute = false;
            Assert.IsNull(model.Validate(0.015d), "nothing is checked while it is off");
        }
        finally {
            File.Delete(library);
        }
    }

    [TestMethod]
    public void OnlyLcmsProjectsOfferIt() {
        var model = new AutomaticRtCorrectionSettingModel(new MsdialGcmsParameter(), Files(), isReadOnly: false);
        Assert.IsFalse(model.IsAvailable);
        Assert.IsNull(model.Validate(0.015d));
    }

    [TestMethod]
    public void TheAlignmentSettingsPageShowsItWithEveryBindingResolved() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            var parameter = ConsoleParameter("anchors.txt");
            var files = Files();
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), files);
            var model = new AlignmentParameterSettingModel(parameter, DateTime.Now, files, alignments, ProcessOption.All);
            using var vm = new AlignmentParameterSettingViewModel(model, Observable.Return(true));
            var errors = CollectBindingErrors(() => {
                var view = new AlignmentParameterSettingView { DataContext = vm, };
                view.Measure(new Size(800, 2000));
                view.Arrange(new Rect(0, 0, 800, 2000));
                view.UpdateLayout();
                var expander = Descendants(view).OfType<Expander>().Distinct().Single(e => AutomationProperties.GetAutomationId(e) == "Expander_AutomaticRtCorrection");
                Assert.AreEqual(Visibility.Visible, expander.Visibility);
                Assert.IsTrue(expander.IsExpanded, "open when the project runs it");
                foreach (var inner in Descendants(expander).OfType<Expander>().Distinct()) {
                    inner.IsExpanded = true;
                }
                view.UpdateLayout();
                var execute = Descendants(view).OfType<CheckBox>().Distinct().Single(c => AutomationProperties.GetAutomationId(c) == "CheckBox_ExecuteAutomaticRtCorrection");
                Assert.AreEqual(true, execute.IsChecked);
            });
            Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
            Assert.AreEqual("0.7", vm.AutomaticRtCorrection.AnchorPeakSelectionRtWeight.Value);
            vm.AutomaticRtCorrection.OutlierMadThreshold.Value = "-1";
            Assert.IsTrue(vm.ObserveHasErrors.Value, "a negative outlier threshold is refused");
            vm.AutomaticRtCorrection.OutlierMadThreshold.Value = "NaN";
            Assert.IsTrue(vm.ObserveHasErrors.Value, "NaN is refused");
            vm.AutomaticRtCorrection.OutlierMadThreshold.Value = "3.5";
            Assert.IsFalse(vm.ObserveHasErrors.Value);
        });
        TestApplication.WaitForBackgroundWork();
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

    private static List<string> CollectBindingErrors(Action action) {
        var listener = new BindingErrorListener();
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
