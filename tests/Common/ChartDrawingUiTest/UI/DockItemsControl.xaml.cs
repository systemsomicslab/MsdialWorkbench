using CompMs.CommonMVVM;
using CompMs.Graphics.UI;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace ChartDrawingUiTest.UI
{
    /// <summary>
    /// Interaction logic for DockItemsControl.xaml
    /// </summary>
    public partial class DockItemsControl : Page
    {
        public DockItemsControl() {
            InitializeComponent();

            DataContext = this;
        }

        public ObservableCollection<int> Items { get; } = [1, 2, 3, 4, 5,];

        public IDockLayoutElement Layout { get; } =
            new ContainerElement {
                Orientation = Orientation.Horizontal,
                Items = [
                    new LeafElement {
                        Size = 1,
                        Width = new GridLength(2, GridUnitType.Star),
                    },
                    new ContainerElement {
                        Orientation = Orientation.Vertical,
                        Width = new GridLength(3, GridUnitType.Star),
                        Items = [
                            new LeafElement {
                                Size = 1,
                                Height = new GridLength(1, GridUnitType.Star),
                            },
                            new LeafElement {
                                Size = 2,
                                Height = new GridLength(2, GridUnitType.Star),
                            },
                        ],
                    },
                ],
            };

        public DelegateCommand<NodeContainers> SerializeCommand => _serializeCommand ??= new DelegateCommand<NodeContainers>(Serialize);
        private DelegateCommand<NodeContainers>? _serializeCommand = null;

        private void Serialize(NodeContainers containers) {
            var memory = new MemoryStream();
            containers.Serialize(memory);
            Serialized.Text = Encoding.UTF8.GetString(memory.ToArray());
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            var builder = new StringBuilder();
            var writer = new StringWriter(builder);
            var converted = ContainerElement.Convert(Container.Containers.Root);
            var serializer = new XmlSerializer(typeof(ContainerElement), [typeof(LeafElement), typeof(ContainerElement)]);
            serializer.Serialize(writer, converted);
            Serialized.Text = builder.ToString();
        }
    }
}
