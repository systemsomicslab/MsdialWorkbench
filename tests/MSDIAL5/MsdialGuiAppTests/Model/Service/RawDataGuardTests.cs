using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.ViewModel.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Service.Tests
{
    [TestClass()]
    public class RawDataGuardTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-rawdataguard-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void FormatPathsListsTenAndCountsTheRest() {
            var paths = Enumerable.Range(0, 13).Select(i => $@"C:\data\{i}.mzML").ToList();

            var lines = RawDataGuard.FormatPaths(paths).Split('\n');

            Assert.AreEqual(RawDataGuard.MaxListedPaths + 1, lines.Length);
            Assert.AreEqual(@"  C:\data\0.mzML", lines[0]);
            Assert.AreEqual(@"  C:\data\9.mzML", lines[9]);
            Assert.AreEqual("  ... and 3 more", lines[10]);
            Assert.AreEqual(@"  C:\data\0.mzML", RawDataGuard.FormatPaths(paths.Take(1).ToList()));
        }

        [TestMethod()]
        public void ConfirmOpenAsksNothingWhenNothingIsMissing() {
            var broker = new MessageBroker();
            var requests = Listen(broker, MessageBoxResult.No);

            Assert.IsTrue(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, new AnalysisFileBeanModel[0], 3));
            Assert.AreEqual(0, requests.Count);
        }

        [TestMethod()]
        [DataRow(MessageBoxResult.Yes, true)]
        [DataRow(MessageBoxResult.No, false)]
        public void ConfirmOpenForLcmsAsksYesOrNo(MessageBoxResult answer, bool expected) {
            var broker = new MessageBroker();
            var requests = Listen(broker, answer);
            var missing = Missing("a", "b");

            Assert.AreEqual(expected, RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, missing, 4));

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(MessageBoxButton.YesNo, requests[0].ButtonType);
            Assert.AreEqual(MessageBoxImage.Warning, requests[0].Icon);
            StringAssert.StartsWith(requests[0].Content, "Raw data are not available for 2 of 4 samples in this dataset, so only the information saved in the project will be drawn for them.");
            StringAssert.Contains(requests[0].Content, $"  {missing[0].AnalysisFilePath}\n  {missing[1].AnalysisFilePath}");
            StringAssert.EndsWith(requests[0].Content, "Continue?");
        }

        [TestMethod()]
        public void ConfirmOpenForAlignmentsOnlySaysTheViewerReadsTheRawData() {
            var broker = new MessageBroker();
            var requests = Listen(broker, MessageBoxResult.Yes);

            Assert.IsTrue(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.AlignmentsOnly, Missing("a"), 2));

            Assert.AreEqual(MessageBoxButton.YesNo, requests.Single().ButtonType);
            StringAssert.Contains(requests[0].Content, "For this type of project the peak spot viewer reads the raw data");
        }

        [TestMethod()]
        public void ConfirmOpenForImagingRefuses() {
            var broker = new MessageBroker();
            var requests = Listen(broker, MessageBoxResult.Yes);

            Assert.IsFalse(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.NotSupported, Missing("a"), 1));

            Assert.AreEqual(MessageBoxButton.OK, requests.Single().ButtonType);
            Assert.AreEqual(MessageBoxImage.Error, requests[0].Icon);
        }

        [TestMethod()]
        public void ConfirmOpenWithoutAnyoneToAnswerDeclines() {
            var broker = new MessageBroker();

            Assert.IsFalse(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, Missing("a"), 1));
        }

        [TestMethod()]
        public void EnsureSaysWhyOnlyWhenTheRawDataAreMissing() {
            var broker = new MessageBroker();
            var requests = Listen(broker, MessageBoxResult.OK);
            var file = Missing("a")[0];

            Assert.IsTrue(RawDataGuard.Ensure(true, file, broker, "Save raw spectra"));
            Assert.AreEqual(0, requests.Count);

            Assert.IsFalse(RawDataGuard.Ensure(false, file, broker, "Save raw spectra"));
            Assert.AreEqual(1, requests.Count);
            StringAssert.StartsWith(requests[0].Content, "Save raw spectra needs the raw data, and the raw data file of a was not found:");
        }

        [TestMethod()]
        public void EnsureAllChecksEveryFileAgain() {
            var broker = new MessageBroker();
            var requests = Listen(broker, MessageBoxResult.OK);
            var path = Path.Combine(_folder, "present.mzML");
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = "present";
            file.AnalysisFilePath = path;
            Assert.IsFalse(file.IsRawDataAvailable);
            File.WriteAllBytes(path, new byte[0]);

            Assert.IsTrue(RawDataGuard.EnsureAll(new[] { file }, broker, "Alignment"));
            Assert.AreEqual(0, requests.Count);

            var missing = Missing("a", "b");
            Assert.IsFalse(RawDataGuard.EnsureAll(missing.Prepend(file), broker, "Alignment"));
            Assert.AreEqual(1, requests.Count);
            StringAssert.StartsWith(requests[0].Content, "Alignment cannot run because the raw data files of 2 of 3 samples were not found.");
        }

        private static List<ErrorMessageBoxRequest> Listen(MessageBroker broker, MessageBoxResult answer) {
            var requests = new List<ErrorMessageBoxRequest>();
            broker.Subscribe<ErrorMessageBoxRequest>(request => {
                requests.Add(request);
                request.Result = answer;
            });
            return requests;
        }

        private AnalysisFileBeanModel[] Missing(params string[] names) {
            return names.Select(name => {
                var file = AnalysisFileBeanModelTestHelper.Create();
                file.AnalysisFileName = name;
                file.AnalysisFilePath = Path.Combine(_folder, name + ".mzML");
                return file;
            }).ToArray();
        }
    }
}
