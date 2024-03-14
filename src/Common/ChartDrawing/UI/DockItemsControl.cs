using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

    private List<ContainerLeaf> Leaves { get; } = [];
}

public interface IContainerNode {

}

public sealed class NodeContainers {
    public IContainerNode? Root { get; set; }

}

internal sealed class ContainerLeaf : IContainerNode, INotifyPropertyChanged {
    public List<IContainerNode> Children { get; } = [];
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

internal sealed class ContainerSplit : IContainerNode, IEnumerable<IContainerNode>, INotifyPropertyChanged
{
    public List<IContainerNode> Children { get; } = [];

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

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class ContainerOver : IContainerNode, IEnumerable<IContainerNode>, INotifyPropertyChanged {
    public List<IContainerNode> Children { get; } = [];

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

    public event PropertyChangedEventHandler? PropertyChanged;
}

