using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

    public DockItemsControl()
    {
        var leaf1 = new ContainerLeaf();
        var overlay1 = new ContainerOver
        {
            Width = new GridLength(2, GridUnitType.Star),
        };
        overlay1.Add(leaf1);
        var leaf2 = new ContainerLeaf();
        var overlay2 = new ContainerOver
        {
            Height = new GridLength(1, GridUnitType.Star),
        };
        overlay2.Add(leaf2);
        var leaf3 = new ContainerLeaf();
        var leaf4 = new ContainerLeaf();
        var overlay34 = new ContainerOver
        {
            Height = new GridLength(2, GridUnitType.Star),
        };
        overlay34.Add(leaf3);
        overlay34.Add(leaf4);
        var vertical = new ContainerSplit
        {
            Orientation = Orientation.Vertical,
            Width = new GridLength(3, GridUnitType.Star),
        };
        vertical.Add(overlay2);
        vertical.Add(overlay34);
        var horizontal = new ContainerSplit
        {
            Orientation = Orientation.Horizontal,
        };
        horizontal.Add(overlay1);
        horizontal.Add(vertical); 

        Containers = new NodeContainers
        {
            Root = horizontal,
        };

        Leaves.Add(leaf1);
        Leaves.Add(leaf2);
        Leaves.Add(leaf3);
        Leaves.Add(leaf4);
    }

    private List<ContainerLeaf> Leaves { get; } = [];

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
        base.OnItemsSourceChanged(oldValue, newValue);
        var idx = 0;
        foreach (var val in newValue) {
            // Temporary ignore
            if (idx >= Leaves.Count) {
                break;
            }
            Leaves[idx++].Content = val;
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

    internal static readonly DependencyProperty ContainersProperty =
        DependencyProperty.Register(
            nameof(Containers),
            typeof(NodeContainers),
            typeof(DockItemsControl),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public NodeContainers? Containers {
        get => (NodeContainers)GetValue(ContainersProperty);
        private set => SetValue(ContainersProperty, value);
    }
}

public interface IContainerNode {

}

public interface IContainerNodeCollection : IContainerNode, IEnumerable<IContainerNode> {
    int Count { get; }
    void Insert(IContainerNode node, int index);
    void Remove(IContainerNode node);
}

public sealed class NodeContainers {
    public IContainerNode? Root { get; set; }

    public void Add(object item) {
        var node = new ContainerLeaf
        {
            Content = item,
            Width = new GridLength(1, GridUnitType.Star),
            Height = new GridLength(1, GridUnitType.Star),
        };
        var collection = FindLastCollection();
        if (collection is not null) {
            collection.Insert(node, collection.Count);
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
    }

    public void Insert(object item, IContainerNodeCollection parent, int index) {
        var node = new ContainerLeaf
        {
            Content = item,
            Width = new GridLength(1, GridUnitType.Star),
            Height = new GridLength(1, GridUnitType.Star),
        };
        parent.Insert(node, index);
    }

    public void Move(IContainerNode node, IContainerNodeCollection parent, int index) {
        var currentParent = FindParent(node);
        if (currentParent is null) {
            return;
        }
        parent.Insert(node, index);
        currentParent.Remove(node);
    }

    public void Remove(IContainerNode node) {
        var parent = FindParent(node);
        if (parent is null) {
            return;
        }
        parent.Remove(node);
    }

    private IContainerNodeCollection FindParent(IContainerNode node) {
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

    public event PropertyChangedEventHandler? PropertyChanged;
}

