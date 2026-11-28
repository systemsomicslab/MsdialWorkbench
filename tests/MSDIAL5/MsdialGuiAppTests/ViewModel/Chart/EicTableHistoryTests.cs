﻿using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Chart.Tests;

/// <summary>
/// The undo history of the EIC setting table, on the UI thread as in the dialog: the values raise their changes there, and one action ends when the thread has run the rest of its queue.
/// </summary>
[TestClass]
public class EicTableHistoryTests
{
    private static void OnUi(Action action) => Application.Current.Dispatcher.Invoke(action);

    // The UI thread finishes what the action left queued, as it does before it handles the next key.
    private static void Settle() => TestApplication.WaitForBackgroundWork();

    private static (List<PeakFeatureSearchValueViewModel> Rows, EicTableHistory History) Open(int count) {
        TestApplication.Ensure();
        List<PeakFeatureSearchValueViewModel> rows = null!;
        EicTableHistory history = null!;
        OnUi(() => {
            rows = Enumerable.Range(0, count)
                .Select(i => new PeakFeatureSearchValueViewModel(new PeakFeatureSearchValueModel(new PeakFeatureSearchValue { Title = "n" + i, Mass = 100 + i, MassTolerance = 0.01, })))
                .ToList();
            history = new EicTableHistory(rows);
        });
        Settle();
        return (rows, history);
    }

    private static string[] Masses(IEnumerable<PeakFeatureSearchValueViewModel> rows) => rows.Select(row => row.Mass.Value).ToArray();

    [TestMethod]
    [Timeout(60000)]
    public void ChangesMadeTogetherAreUndoneTogether() {
        var (rows, history) = Open(3);
        OnUi(() => Assert.IsFalse(history.CanUndo));

        // Like a paste into two cells.
        OnUi(() => {
            rows[0].Mass.Value = "200";
            rows[2].MassTolerance.Value = "0.5";
        });
        Settle();
        OnUi(() => rows[1].Title.Value = "x");
        Settle();

        OnUi(() => {
            Assert.IsTrue(history.CanUndo);
            var undone = history.Undo();
            Assert.AreEqual(1, undone.Count);
            Assert.AreEqual("n1", rows[1].Title.Value);
            Assert.AreEqual("200", rows[0].Mass.Value);

            undone = history.Undo();
            CollectionAssert.AreEquivalent(new[] { "Mass", "MassTolerance" }, undone.Select(change => change.Property).ToArray());
            CollectionAssert.AreEqual(new[] { "100", "101", "102" }, Masses(rows));
            Assert.AreEqual("0.01", rows[2].MassTolerance.Value);
            Assert.IsFalse(history.CanUndo);
            Assert.AreEqual(0, history.Undo().Count);
        });
        Settle();
        OnUi(() => {
            Assert.IsFalse(history.CanUndo, "Undoing is not a change of its own.");
            history.Redo();
            CollectionAssert.AreEqual(new[] { "200", "101", "102" }, Masses(rows));
            Assert.AreEqual("0.5", rows[2].MassTolerance.Value);
            history.Redo();
            Assert.AreEqual("x", rows[1].Title.Value);
            Assert.IsFalse(history.CanRedo);
        });
    }

    [TestMethod]
    [Timeout(60000)]
    public void ACellEditIsOneStep() {
        var (rows, history) = Open(2);

        OnUi(history.BeginEdit);
        foreach (var typed in new[] { "1", "12", "123" }) {
            OnUi(() => rows[0].Mass.Value = typed);
            Settle();
        }
        OnUi(() => {
            Assert.IsFalse(history.CanUndo, "The typing in the cell is undone by its TextBox while the cell is edited.");
            history.EndEdit();

            history.Undo();
            Assert.AreEqual("100", rows[0].Mass.Value);
            history.Redo();
            Assert.AreEqual("123", rows[0].Mass.Value);
        });
    }

    [TestMethod]
    [Timeout(60000)]
    public void ANewChangeAfterAnUndoDropsWhatWasUndone() {
        var (rows, history) = Open(1);
        OnUi(() => rows[0].Mass.Value = "200");
        Settle();
        OnUi(() => {
            history.Undo();
            Assert.IsTrue(history.CanRedo);
            rows[0].Title.Value = "x";
            Assert.IsFalse(history.CanRedo, "A change not recorded yet already counts.");
        });
        Settle();
        OnUi(() => {
            Assert.AreEqual(0, history.Redo().Count);
            Assert.AreEqual("100", rows[0].Mass.Value);

            history.Undo();
            Assert.AreEqual("n0", rows[0].Title.Value);
            Assert.IsFalse(history.CanUndo);
        });
    }

    [TestMethod]
    [Timeout(60000)]
    public void AnUndoBeforeTheActionIsRecordedUndoesIt() {
        var (rows, history) = Open(1);
        OnUi(() => {
            rows[0].Mass.Value = "200";
            Assert.IsTrue(history.CanUndo);
            history.Undo();
            Assert.AreEqual("100", rows[0].Mass.Value);
        });
        Settle();
        OnUi(() => {
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);
        });
    }
}
