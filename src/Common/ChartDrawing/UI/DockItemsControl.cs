using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

namespace CompMs.Graphics.UI;

/// <summary>
/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
///
/// Step 1a) Using this custom control in a XAML file that exists in the current project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI"
///
///
/// Step 1b) Using this custom control in a XAML file that exists in a different project.
/// Add this XmlNamespace attribute to the root element of the markup file where it is 
/// to be used:
///
///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI;assembly=CompMs.Graphics.UI"
///
/// You will also need to add a project reference from the project where the XAML file lives
/// to this project and Rebuild to avoid compilation errors:
///
///     Right click on the target project in the Solution Explorer and
///     "Add Reference"->"Projects"->[Browse to and select this project]
///
///
/// Step 2)
/// Go ahead and use your control in the XAML file.
///
///     <MyNamespace:DockItemsControl/>
///
/// </summary>
public class DockItemsControl : ItemsControl
{
    static DockItemsControl() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockItemsControl), new FrameworkPropertyMetadata(typeof(DockItemsControl)));
    }

    public readonly static RoutedCommand SplitHorizontalCommand = new(nameof(DockItemsControl), typeof(DockItemsControl));
    public readonly static RoutedCommand SplitVerticalCommand = new(nameof(DockItemsControl), typeof(DockItemsControl));
    public readonly static RoutedCommand ExportLayoutCommand = new(nameof(DockItemsControl), typeof(DockItemsControl));

    public DockItemsControl()
    {
        Containers = new NodeContainers();
        CommandBindings.Add(new CommandBinding(SplitHorizontalCommand, SplitHorizontal, CanSplitHorizontal));
        CommandBindings.Add(new CommandBinding(SplitVerticalCommand, SplitVertical, CanSplitVertical));
        CommandBindings.Add(new CommandBinding(ExportLayoutCommand, ExportLayout));
    }

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
        base.OnItemsSourceChanged(oldValue, newValue);
        foreach (var val in newValue) {
            Containers.SetLast(val);
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        switch (e.Action) {
            case NotifyCollectionChangedAction.Add:
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
        }
    }

    private static readonly DependencyPropertyKey ContainersPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Containers),
            typeof(NodeContainers),
            typeof(DockItemsControl),
            new FrameworkPropertyMetadata(null));

    private static readonly DependencyProperty ContainersProperty = ContainersPropertyKey.DependencyProperty;

    public NodeContainers? Containers {
        get => (NodeContainers?)GetValue(ContainersProperty);
        private set => SetValue(ContainersPropertyKey, value);
    }

    public static readonly DependencyProperty LayoutElementProperty =
        DependencyProperty.Register(
            nameof(LayoutElement),
            typeof(IDockLayoutElement),
            typeof(DockItemsControl),
            new FrameworkPropertyMetadata(
                null,
                OnLayoutElementChanged));

    public IDockLayoutElement? LayoutElement {
        get => (IDockLayoutElement?)GetValue(LayoutElementProperty);
        set => SetValue(LayoutElementProperty, value);
    }

    private static void OnLayoutElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is DockItemsControl dc) {
            dc.OnLayoutElementChanged((IDockLayoutElement?)e.OldValue, (IDockLayoutElement?)e.NewValue);
        }
    }

    private void OnLayoutElementChanged(IDockLayoutElement? oldValue, IDockLayoutElement? newValue) {
        var values = Containers.Leaves.Select(leaf => leaf.Content).ToArray();
        Containers.Build(newValue);
        foreach (var value in values) {
            Containers.SetLast(value);
        }
    }

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(DockItemsControl));

    public DataTemplate? ContentTemplate {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public Action<object, int, object, int> MoveNodeCallback => MoveNode;
    
    private void MoveNode(object srcCollection, int srcIndex, object dstCollection, int dstIndex) {
        if (srcCollection is IContainerNodeCollection srcs && dstCollection is IContainerNodeCollection dsts) {
            var node = srcs.ElementAt(srcIndex);
            if (dstIndex < 0) {
                dsts.Insert(node, dsts.Count);
                srcs.Remove(node);
            }
            else {
                Containers?.Move(node, dsts, dstIndex);
            }
            Containers?.Shrink();
        }
    }

    private void SplitHorizontal(object sender, ExecutedRoutedEventArgs args) {
        if (args.Parameter is IContainerNode node) {
            Containers?.SplitHorizontal(node);
        }
    }

    private void CanSplitHorizontal(object sender, CanExecuteRoutedEventArgs args) {
        args.CanExecute = args.Parameter is IContainerNode;
    }

    private void SplitVertical(object sender, ExecutedRoutedEventArgs args) {
        if (args.Parameter is IContainerNode node) {
            Containers?.SplitVertical(node);
        }
    }

    private void CanSplitVertical(object sender, CanExecuteRoutedEventArgs args) {
        args.CanExecute = args.Parameter is IContainerNode;
    }

    private void ExportLayout(object sender, ExecutedRoutedEventArgs args) {
        if (args.Parameter is TextWriter writer) {
            var converted = ContainerElement.Convert(Containers.Root);
            var serializer = new XmlSerializer(typeof(ContainerElement), [typeof(LeafElement), typeof(ContainerElement)]);
            serializer.Serialize(writer, converted);
        }
    }
}

public interface IContainerNode {
    GridLength Width { get; }
    GridLength Height { get; }
}

public interface IContainerNodeCollection : IContainerNode, IEnumerable<IContainerNode> {
    int Count { get; }
    int IndexOf(IContainerNode node);
    void Insert(IContainerNode node, int index);
    void Remove(IContainerNode node);
    internal IEnumerable<ContainerLeaf> GetLeaves();
}

public sealed class NodeContainers : INotifyPropertyChanged {
    public IContainerNodeCollection? Root {
        get => _root;
        private set {
            if (_root != value) {
                _root = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Root)));
            }
        }
    }
    private IContainerNodeCollection? _root;

    internal ContainerLeaf[] Leaves {
        get => _leaves;
        private set {
            if (_leaves != value) {
                _leaves = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Leaves)));
            }
        }
    }
    private ContainerLeaf[] _leaves = [];

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetLast(object item) {
        foreach (var leaf in _leaves) {
            if (leaf.Content is null) {
                leaf.Content = item;
                return;
            }
        }
        Add(item);
    }

    public void Add(object item) {
        var node = new ContainerLeaf
        {
            Content = item,
        };
        var collection = FindLastCollection();
        if (collection is not null) {
            collection.Insert(node, collection.Count);
            Leaves = [..Root.GetLeaves()];
            Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
            return;
        }
        var root = new ContainerOver
        {
            Width = new GridLength(1, GridUnitType.Star),
            Height = new GridLength(1, GridUnitType.Star),
        };
        if (Root is not null) {
            root.Insert(Root, root.Count);
        }
        root.Insert(node, root.Count);
        Root = root;
        Leaves = [..Root.GetLeaves()];
        Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
    }

    internal void Insert(object item, IContainerNodeCollection parent, int index) {
        var node = new ContainerLeaf
        {
            Content = item,
        };
        parent.Insert(node, index);
        Leaves = [..Root.GetLeaves()];
        Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
    }

    internal void Move(IContainerNode node, IContainerNodeCollection parent, int index) {
        var currentParent = FindParent(node);
        if (currentParent is null) {
            return;
        }
        parent.Insert(node, index);
        currentParent.Remove(node);
        Leaves = [..Root.GetLeaves()];
        Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
    }

    internal void Remove(IContainerNode node) {
        var parent = FindParent(node);
        if (parent is null) {
            return;
        }
        parent.Remove(node);
        Leaves = [..Root.GetLeaves()];
        Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
    }

    internal void SplitHorizontal(IContainerNode node) {
        var parent = FindParent(node);
        if (parent is not ContainerOver) {
            return;
        }
        var grand = FindParent(parent);
        if (grand is ContainerSplit grandSplit && grandSplit.Orientation == Orientation.Horizontal) {
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            Move(node, over, over.Count);
            grandSplit.Insert(over, grand.IndexOf(parent) + 1);
            return;
        }
        if (grand is null) {
            if (parent != Root) {
                return;
            }
            var split = new ContainerSplit
            {
                Orientation = Orientation.Horizontal,
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            split.Add(parent);
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            Move(node, over, over.Count);
            split.Add(over);
            Root = split;
        }
        else {
            var split = new ContainerSplit
            {
                Orientation = Orientation.Horizontal,
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            var idx = grand.IndexOf(parent);
            grand.Insert(split, idx);
            Move(parent, split, split.Count);
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            split.Add(over);
            Move(node, over, over.Count);
        }
    }

    public void SplitVertical(IContainerNode node) {
        var parent = FindParent(node);
        if (parent is not ContainerOver) {
            return;
        }
        var grand = FindParent(parent);
        if (grand is ContainerSplit grandSplit && grandSplit.Orientation == Orientation.Vertical) {
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            Move(node, over, over.Count);
            grandSplit.Insert(over, grand.IndexOf(parent) + 1);
            return;
        }
        if (grand is null) {
            if (parent != Root) {
                return;
            }
            var split = new ContainerSplit
            {
                Orientation = Orientation.Vertical,
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            split.Add(parent);
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            Move(node, over, over.Count);
            split.Add(over);
            Root = split;
        }
        else {
            var split = new ContainerSplit
            {
                Orientation = Orientation.Vertical,
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            var idx = grand.IndexOf(parent);
            grand.Insert(split, idx);
            Move(parent, split, split.Count);
            var over = new ContainerOver
            {
                Width = new GridLength(1, GridUnitType.Star),
                Height = new GridLength(1, GridUnitType.Star),
            };
            split.Add(over);
            Move(node, over, over.Count);
        }
    }

    public void Shrink() {
        if (Root is IContainerNodeCollection collection) {
            ShrinkRec(collection);
        }
    }

    private void ShrinkRec(IContainerNodeCollection collection) {
        var unused = new List<IContainerNode>();
        foreach (var node in collection) {
            if (node is IContainerNodeCollection c) {
                ShrinkRec(c);
                if (c.Count == 0) {
                    unused.Add(c);
                }
            }
        }
        foreach (var item in unused) {
            collection.Remove(item);
        }
    }

    private IContainerNodeCollection? FindParent(IContainerNode node) {
        return FindParent(node, Root as IContainerNodeCollection);
    }

    private IContainerNodeCollection? FindParent(IContainerNode node, IContainerNodeCollection? current) {
        if (current is null) {
            return null;
        }
        foreach (var next in current) {
            if (next == node) {
                return current;
            }
            var result = FindParent(node, next as IContainerNodeCollection);
            if (result is not null) {
                return result;
            }
        }
        return null;
    }

    private IContainerNodeCollection? FindLastCollection() {
        if (Root is IContainerNodeCollection root) {
            return FindLastCollection(root);
        }
        return null;
    }

    private IContainerNodeCollection FindLastCollection(IContainerNodeCollection? current) {
        foreach (var next in current.OfType<IContainerNodeCollection>().Reverse()) {
            if (FindLastCollection(next) is IContainerNodeCollection found) {
                return found;
            }
        }
        return current;
    }

    public void Build(IDockLayoutElement? itemElement) {
        if (itemElement is null) {
            Root = null;
            Leaves = [];
            return;
        }
        Root = itemElement.Build();
        if (Root is not null) {
            Leaves = [..Root.GetLeaves()];
            Array.Sort(Leaves, (x, y) => y.Priority.CompareTo(x.Priority));
        }
    }

    public DockLayoutElement Convert() => ContainerElement.Convert(Root);

    public void Serialize(Stream stream) {
        var writer = new StreamWriter(stream);
        var converted = ContainerElement.Convert(Root);
        var serializer = new XmlSerializer(typeof(ContainerElement), [typeof(LeafElement), typeof(ContainerElement)]);
        serializer.Serialize(writer, converted);
    }

    public void Deserialize(Stream stream) {
        var serializer = new XmlSerializer(typeof(ContainerElement), [typeof(LeafElement), typeof(ContainerElement)]);
        Root = (IContainerNodeCollection)serializer.Deserialize(stream);
    }
}

internal sealed class ContainerLeaf : IContainerNode, INotifyPropertyChanged {
    public object? Content {
        get => _content;
        set {
            if (_content != value) {
                _content = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
            }
        }
    }
    private object? _content;

    public GridLength Width {
        get => _width;
        set {
            if (_width != value) {
                _width = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
            }
        }
    }
    private GridLength _width;

    public GridLength Height {
        get => _height;
        set {
            if (_height != value) {
                _height = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
            }
        }
    }
    private GridLength _height;

    public int Priority {
        get => _priority;
        set {
            if (_priority != value) {
                _priority = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Priority)));
            }
        }
    }
    private int _priority = 0;

    public event PropertyChangedEventHandler PropertyChanged;
}

internal sealed class ContainerSplit : IContainerNode, IContainerNodeCollection, IEnumerable<IContainerNode>, INotifyPropertyChanged
{
    public ObservableCollection<IContainerNode> Children { get; } = [];

    public int Count => Children.Count;

    public Orientation Orientation {
        get => _orientation;
        set {
            if (_orientation != value) {
                _orientation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Orientation)));
            }
        }
    }
    private Orientation _orientation;

    public GridLength Width {
        get => _width;
        set {
            if (_width != value) {
                _width = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
            }
        }
    }
    private GridLength _width;

    public GridLength Height {
        get => _height;
        set {
            if (_height != value) {
                _height = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
            }
        }
    }
    private GridLength _height;

    public void Add(IContainerNode node) {
        Children.Add(node);
    }

    public IEnumerator<IContainerNode> GetEnumerator() {
        return ((IEnumerable<IContainerNode>)Children).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Children).GetEnumerator();
    }

    public void Insert(IContainerNode node, int index) {
        if ((uint)index <= Children.Count) {
            Children.Insert(index, node);
        }
    }

    public void Remove(IContainerNode node) {
        Children.Remove(node);
    }

    public int IndexOf(IContainerNode node) {
        return Children.IndexOf(node);
    }

    public IEnumerable<ContainerLeaf> GetLeaves() {
        foreach (var child in Children) {
            if (child is IContainerNodeCollection collection) {
                foreach (var leaf in collection.GetLeaves()) {
                    yield return leaf;
                }
            }
            else if (child is ContainerLeaf leaf) {
                yield return leaf;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class ContainerOver : IContainerNode, IContainerNodeCollection, IEnumerable<IContainerNode>, INotifyPropertyChanged {
    public ObservableCollection<IContainerNode> Children { get; } = [];

    public int Count => Children.Count;

    public GridLength Width {
        get => _width;
        set {
            if (_width != value) {
                _width = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
            }
        }
    }
    private GridLength _width;

    public GridLength Height {
        get => _height;
        set {
            if (_height != value) {
                _height = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
            }
        }
    }
    private GridLength _height;

    public void Add(IContainerNode node) {
        Children.Add(node);
    }

    public IEnumerator<IContainerNode> GetEnumerator() {
        return ((IEnumerable<IContainerNode>)Children).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Children).GetEnumerator();
    }

    public void Insert(IContainerNode node, int index) {
        if ((uint)index <= Children.Count) {
            Children.Insert(index, node);
        }
    }

    public void Remove(IContainerNode node) {
        Children.Remove(node);
    }

    public int IndexOf(IContainerNode node) {
        return Children.IndexOf(node);
    }

    public IEnumerable<ContainerLeaf> GetLeaves() {
        foreach (var child in Children) {
            if (child is IContainerNodeCollection collection) {
                foreach (var leaf in collection.GetLeaves()) {
                    yield return leaf;
                }
            }
            else if (child is ContainerLeaf leaf) {
                yield return leaf;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public interface IDockLayoutElement {
    internal IContainerNodeCollection Build();
}

[XmlInclude(typeof(LeafElement))]
[XmlInclude(typeof(ContainerElement))]
public abstract class DockLayoutElement : IDockLayoutElement
{
    IContainerNodeCollection IDockLayoutElement.Build() {
        return BuildCore();
    }

    protected abstract IContainerNodeCollection BuildCore();
}

[System.Windows.Markup.ContentProperty("Items")]
[XmlRoot("ContainerElement")]
[XmlInclude(typeof(LeafElement))]
public sealed class ContainerElement : DockLayoutElement {
    [XmlArrayItem("LeafElement", typeof(LeafElement))]
    [XmlArrayItem("ContainerElement", typeof(ContainerElement))]
    public List<DockLayoutElement> Items { get; set; } = [];
    [XmlElement("Orientation")]
    public Orientation Orientation { get; set; }
    [XmlIgnore]
    public GridLength Width {
        get => new(WidthValue, WidthUnitType);
        set {
            WidthValue = value.Value;
            WidthUnitType = value.GridUnitType;
        }
    }
    [XmlIgnore]
    public GridLength Height {
        get => new(HeightValue, HeightUnitType);
        set {
            HeightValue = value.Value;
            HeightUnitType = value.GridUnitType;
        }
    }
    [XmlElement("WidthValue")]
    public double WidthValue { get; set; }
    [XmlElement("HeightValue")]
    public double HeightValue { get; set; }
    [XmlElement("WidthUnitType")]
    public GridUnitType WidthUnitType { get; set; }
    [XmlElement("HeightUnitType")]
    public GridUnitType HeightUnitType { get; set; }

    protected override IContainerNodeCollection BuildCore() {
        var container = new ContainerSplit
        {
            Orientation = Orientation,
            Width = Width,
            Height = Height,
        };
        foreach (var item in Items) {
            container.Add(((IDockLayoutElement)item).Build());
        }
        return container;
    }

    public static DockLayoutElement Convert(IContainerNode node) {
        return node switch
        {
            ContainerSplit split => new ContainerElement
            {
                Width = split.Width,
                Height = split.Height,
                Orientation = split.Orientation,
                Items = split.Children.Select(Convert).Where(e => e is not null).ToList(),
            },
            ContainerOver over => new LeafElement
            {
                Width = over.Width,
                Height = over.Height,
                Size = over.Count,
                Priorities = over.Children.OfType<ContainerLeaf>().Select(leaf => leaf.Priority).ToArray(),
            },
            _ => new LeafElement
            {
                Size = 0,
                Priorities = [],
            }
        };
    }
}

[XmlRoot("LeafElement")]
public sealed class LeafElement : DockLayoutElement {
    [XmlElement("Size")]
    public int Size { get; set; }
    [XmlIgnore]
    public GridLength Width {
        get => new(WidthValue, WidthUnitType);
        set {
            WidthValue = value.Value;
            WidthUnitType = value.GridUnitType;
        }
    }
    [XmlIgnore]
    public GridLength Height {
        get => new(HeightValue, HeightUnitType);
        set {
            HeightValue = value.Value;
            HeightUnitType = value.GridUnitType;
        }
    }
    [XmlElement("WidthValue")]
    public double WidthValue { get; set; }
    [XmlElement("HeightValue")]
    public double HeightValue { get; set; }
    [XmlElement("WidthUnitType")]
    public GridUnitType WidthUnitType { get; set; }
    [XmlElement("HeightUnitType")]
    public GridUnitType HeightUnitType { get; set; }

    [XmlElement("Priorities")]
    public int[] Priorities { get; set; } = [];

    protected override IContainerNodeCollection BuildCore() {
        var container = new ContainerOver
        {
            Width = Width,
            Height = Height,
        };
        for (int i = 0; i < Size; i++) {
            container.Add(new ContainerLeaf
            {
                Priority = (Priorities is not null && i < Priorities.Length) ? Priorities[i] : 0,
            });
        }
        return container;
    }
}

