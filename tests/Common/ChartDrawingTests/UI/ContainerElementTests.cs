using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace CompMs.Graphics.UI.Tests
{
    [TestClass()]
    public class ContainerElementTests
    {
        [TestMethod()]
        public void SerializeTest() {
            var layout = new ContainerElement
            {
                Orientation = Orientation.Horizontal,
                Items = [
                    new LeafElement {
                        Size = 1,
                        Width = new GridLength(2, GridUnitType.Star),
                        Priorities = [4],
                    },
                    new ContainerElement {
                        Orientation = Orientation.Vertical,
                        Width = new GridLength(3, GridUnitType.Star),
                        Items = [
                            new LeafElement {
                                Size = 1,
                                Height = new GridLength(1, GridUnitType.Star),
                                Priorities = [3],
                            },
                            new LeafElement {
                                Size = 2,
                                Height = new GridLength(2, GridUnitType.Star),
                                Priorities = [2, 1],
                            },
                        ],
                    },
                ],
            };

            var serializer = new XmlSerializer(typeof(ContainerElement), [typeof(LeafElement), typeof(ContainerElement)]);
            var memory = new MemoryStream();
            var writer = new StreamWriter(memory);
            serializer.Serialize(writer, layout);

            System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetString(memory.ToArray()));

            memory.Seek(0, SeekOrigin.Begin);
            var actual = (ContainerElement)serializer.Deserialize(memory);
            Equal(layout, actual);
        }

        void Equal(IDockLayoutElement expected, IDockLayoutElement actual) {
            Assert.IsInstanceOfType(actual, expected.GetType());
            switch ((expected, actual)) {
                case (LeafElement e, LeafElement a):
                    Assert.AreEqual(e.Size, a.Size);
                    Assert.AreEqual(e.Width, a.Width);
                    Assert.AreEqual(e.Height, a.Height);
                    CollectionAssert.AreEqual(e.Priorities, a.Priorities);
                    break;
                case (ContainerElement e, ContainerElement a):
                    Assert.AreEqual(e.Orientation, a.Orientation);
                    Assert.AreEqual(e.Width, a.Width);
                    Assert.AreEqual(e.Height, a.Height);
                    Assert.AreEqual(e.Items.Count, a.Items.Count);
                    for (var i = 0; i < e.Items.Count; i++) {
                        Equal(e.Items[i], a.Items[i]);
                    }
                    break;
                default:
                    Assert.Fail();
                    break;
            }
        }
    }
}