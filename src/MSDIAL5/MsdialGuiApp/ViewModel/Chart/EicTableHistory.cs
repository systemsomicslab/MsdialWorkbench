using CompMs.App.Msdial.ViewModel.Setting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace CompMs.App.Msdial.ViewModel.Chart;

/// <summary>
/// Undo and redo for the values in the EIC setting table: the name, exact mass and mass tolerance of each row.
/// </summary>
/// <remarks>
/// Every way of changing the table (editing a cell, Delete, paste, the clear buttons) changes the row view models, so the history follows those values.
/// The changes made by one action are one step: the changes made at the same time (a paste, Delete on several cells, Clear) are put together when the action is over,
/// and the changes made while a cell is being edited, one per typed character, are put together when the edit ends.
/// Only the text in the table is undone, not Apply: the chromatograms stay as they were drawn until Apply is pressed again.
/// Apply writes the numbers back in their shortest form ("100.0" becomes "100"), and that change of text is a step too,
/// so the first undo after such an Apply only brings back how the number was typed.
/// The history keeps the last <see cref="Capacity"/> steps.
/// </remarks>
internal sealed class EicTableHistory
{
    private const int Capacity = 100;
    private static readonly string[] Properties = [nameof(PeakFeatureSearchValueViewModel.Title), nameof(PeakFeatureSearchValueViewModel.Mass), nameof(PeakFeatureSearchValueViewModel.MassTolerance)];

    private readonly IReadOnlyList<PeakFeatureSearchValueViewModel> _rows;
    private readonly Dictionary<(PeakFeatureSearchValueViewModel Row, string Property), string> _recorded = [];
    private readonly LinkedList<Change[]> _undo = new();
    private readonly Stack<Change[]> _redo = new();
    private bool _isEditing, _isRecordPending, _isRestoring;

    public EicTableHistory(IReadOnlyList<PeakFeatureSearchValueViewModel> rows) {
        _rows = rows;
        foreach (var row in rows) {
            foreach (var property in Properties) {
                var value = Value(row, property);
                _recorded[(row, property)] = value.Value;
                ((INotifyPropertyChanged)value).PropertyChanged += OnValueChanged;
            }
        }
    }

    /// <summary>One value changed by a step.</summary>
    public sealed class Change(PeakFeatureSearchValueViewModel row, string property, string before, string after)
    {
        public PeakFeatureSearchValueViewModel Row { get; } = row;
        public string Property { get; } = property;
        public string Before { get; } = before;
        public string After { get; } = after;
    }

    /// <summary>A cell is being edited: between <see cref="BeginEdit"/> and <see cref="EndEdit"/>.</summary>
    public bool IsEditing => _isEditing;

    public bool CanUndo => !_isEditing && (_undo.Count > 0 || HasUnrecordedChanges());
    public bool CanRedo => !_isEditing && _redo.Count > 0 && !HasUnrecordedChanges();

    /// <summary>A cell edit starts. What was changed before it, an edit of another cell included, is a step of its own.</summary>
    public void BeginEdit() {
        _isEditing = false;
        Record();
        _isEditing = true;
    }

    /// <summary>The cell edit is over: everything it changed is one step.</summary>
    public void EndEdit() {
        _isEditing = false;
        Record();
    }

    /// <summary>Puts back the values from before the last step. Returns the values it changed, none when there is nothing to undo.</summary>
    public IReadOnlyList<Change> Undo() {
        if (_isEditing) {
            return [];
        }
        Record();
        if (_undo.Last is not { Value: var step }) {
            return [];
        }
        _undo.RemoveLast();
        Restore(step, change => change.Before);
        _redo.Push(step);
        return step;
    }

    /// <summary>Makes the last undone step again. Returns the values it changed, none when there is nothing to redo.</summary>
    public IReadOnlyList<Change> Redo() {
        if (_isEditing) {
            return [];
        }
        // A new change after an undo starts a new line of history, so the undone steps cannot be made again.
        Record();
        if (_redo.Count == 0) {
            return [];
        }
        var step = _redo.Pop();
        Restore(step, change => change.After);
        _undo.AddLast(step);
        return step;
    }

    /// <summary>Makes the changes since the last step a step of their own. False when nothing changed.</summary>
    public bool Record() {
        if (_isEditing) {
            return false;
        }
        var changes = new List<Change>();
        foreach (var row in _rows) {
            foreach (var property in Properties) {
                var after = Value(row, property).Value;
                if (_recorded.TryGetValue((row, property), out var before) && before != after) {
                    changes.Add(new Change(row, property, before, after));
                }
                _recorded[(row, property)] = after;
            }
        }
        if (changes.Count == 0) {
            return false;
        }
        _undo.AddLast(changes.ToArray());
        if (_undo.Count > Capacity) {
            _undo.RemoveFirst();
        }
        _redo.Clear();
        return true;
    }

    private bool HasUnrecordedChanges() {
        foreach (var row in _rows) {
            foreach (var property in Properties) {
                if (!_recorded.TryGetValue((row, property), out var before) || before != Value(row, property).Value) {
                    return true;
                }
            }
        }
        return false;
    }

    // The changes of one action are made one after another in the same turn of the UI thread, so they are recorded together once that turn is over.
    private void OnValueChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != nameof(IReactiveProperty.Value) || _isRestoring || _isEditing || _isRecordPending) {
            return;
        }
        if (SynchronizationContext.Current is SynchronizationContext context) {
            _isRecordPending = true;
            context.Post(_ => {
                _isRecordPending = false;
                Record();
            }, null);
        }
        else {
            Record();
        }
    }

    private void Restore(IEnumerable<Change> step, Func<Change, string> value) {
        _isRestoring = true;
        try {
            foreach (var change in step) {
                Value(change.Row, change.Property).Value = value(change);
                _recorded[(change.Row, change.Property)] = value(change);
            }
        }
        finally {
            _isRestoring = false;
        }
    }

    private static IReactiveProperty<string> Value(PeakFeatureSearchValueViewModel row, string property) {
        return property switch
        {
            nameof(row.Title) => row.Title,
            nameof(row.Mass) => row.Mass,
            nameof(row.MassTolerance) => row.MassTolerance,
            _ => throw new ArgumentOutOfRangeException(nameof(property)),
        };
    }
}
