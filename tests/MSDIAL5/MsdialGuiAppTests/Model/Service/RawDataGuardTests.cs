using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var requests = Listen(broker, false);

            Assert.IsTrue(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, new AnalysisFileBeanModel[0], 3));
            Assert.AreEqual(0, requests.Count);
        }

        [TestMethod()]
        [DataRow(true, true)]
        [DataRow(false, false)]
        public void ConfirmOpenForLcmsAsksYesOrNo(bool answer, bool expected) {
            var broker = new MessageBroker();
            var requests = Listen(broker, answer);
            var missing = Missing("a", "b");

            Assert.AreEqual(expected, RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, missing, 4));

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual("Raw data not found", requests[0].Caption);
            Assert.IsTrue(requests[0].AsksYesNo);
            Assert.AreEqual(MessageSeverity.Warning, requests[0].Severity);
            StringAssert.StartsWith(requests[0].Content, "Raw data are not available for 2 of 4 samples in this dataset, so only the information saved in the project will be drawn for them.");
            StringAssert.Contains(requests[0].Content, $"  {missing[0].AnalysisFilePath}\n  {missing[1].AnalysisFilePath}");
            StringAssert.EndsWith(requests[0].Content, "Continue?");
        }

        [TestMethod()]
        public void ConfirmOpenForAlignmentsOnlySaysTheViewerReadsTheRawData() {
            var broker = new MessageBroker();
            var requests = Listen(broker, true);

            Assert.IsTrue(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.AlignmentsOnly, Missing("a"), 2));

            Assert.IsTrue(requests.Single().AsksYesNo);
            Assert.AreEqual(MessageSeverity.Warning, requests[0].Severity);
            StringAssert.Contains(requests[0].Content, "For this type of project the peak spot viewer reads the raw data");
        }

        [TestMethod()]
        public void ConfirmOpenForImagingRefuses() {
            var broker = new MessageBroker();
            var requests = Listen(broker, true);

            Assert.IsFalse(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.NotSupported, Missing("a"), 1));

            Assert.IsFalse(requests.Single().AsksYesNo);
            Assert.AreEqual(MessageSeverity.Error, requests[0].Severity);
        }

        [TestMethod()]
        public void ConfirmOpenWithoutAnyoneToAnswerDeclines() {
            var broker = new MessageBroker();

            Assert.IsFalse(RawDataGuard.ConfirmOpen(broker, BrowsingWithoutRawData.Supported, Missing("a"), 1));
        }

        [TestMethod()]
        public void EnsureSaysWhyOnlyWhenTheRawDataAreMissing() {
            var broker = new MessageBroker();
            var requests = Listen(broker, true);
            var file = Missing("a")[0];

            Assert.IsTrue(RawDataGuard.Ensure(true, file, broker, "Save raw spectra"));
            Assert.AreEqual(0, requests.Count);

            Assert.IsFalse(RawDataGuard.Ensure(false, file, broker, "Save raw spectra"));
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual("Raw data not available", requests[0].Caption);
            Assert.IsFalse(requests[0].AsksYesNo);
            Assert.AreEqual(MessageSeverity.Information, requests[0].Severity);
            StringAssert.StartsWith(requests[0].Content, "Save raw spectra needs the raw data, and the raw data file of a was not found:");
        }

        [TestMethod()]
        public void EnsureAllChecksEveryFileAgain() {
            var broker = new MessageBroker();
            var requests = Listen(broker, true);
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
            Assert.AreEqual("Raw data not available", requests[0].Caption);
            Assert.IsFalse(requests[0].AsksYesNo);
            Assert.AreEqual(MessageSeverity.Information, requests[0].Severity);
            StringAssert.StartsWith(requests[0].Content, "Alignment cannot run because the raw data files of 2 of 3 samples were not found.");
        }

        // The refusals and the open failures keep the caption, the OK button and the icon they had as ErrorMessageBoxRequests.
        [TestMethod()]
        public void RefusalsAreInformationAndFailuresAreErrorsWithAnOkButton() {
            var file = Missing("a")[0];
            var alignment = new AlignmentFileBeanModel(new AlignmentFileBean { FileName = "alignment", }, new List<AnalysisFileBean>());
            var error = new InvalidOperationException("broken");

            var expected = new (MessageRequest Request, string Caption, MessageSeverity Severity)[]
            {
                (RawDataGuard.SampleNeedsRawData(file), "Raw data not available", MessageSeverity.Information),
                (RawDataGuard.PeakListMissing(file), "Saved results not found", MessageSeverity.Information),
                (RawDataGuard.MsfinderWithoutMs1(file), "Raw data not available", MessageSeverity.Information),
                (RawDataGuard.SampleOpenFailed(file, error), "Sample could not be opened", MessageSeverity.Error),
                (RawDataGuard.AlignmentOpenFailed(alignment, error), "Alignment result could not be opened", MessageSeverity.Error),
            };

            foreach (var (request, caption, severity) in expected) {
                Assert.AreEqual(caption, request.Caption);
                Assert.AreEqual(severity, request.Severity, caption);
                Assert.IsFalse(request.AsksYesNo, caption);
            }
        }

        /// <summary>
        /// Keeps what is published, and answers it as the view does: true for Yes or OK, false for No.
        /// </summary>
        private static List<MessageRequest> Listen(MessageBroker broker, bool answer) {
            var requests = new List<MessageRequest>();
            broker.Subscribe<MessageRequest>(request => {
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
