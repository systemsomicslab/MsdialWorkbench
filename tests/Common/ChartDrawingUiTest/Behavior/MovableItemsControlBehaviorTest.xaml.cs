using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ChartDrawingUiTest.Behavior
{
    /// <summary>
    /// Interaction logic for MovableItemsControlBehaviorTest.xaml
    /// </summary>
    public partial class MovableItemsControlBehaviorTest : Page
    {
        public MovableItemsControlBehaviorTest() {
            InitializeComponent();
            DataContext = this;
        }

        public ObservableCollection<string> ItemsA { get; } = ["Item1", "Item2", "Item3", "Item4", "Item5"];
        public ObservableCollection<string> ItemsB { get; } = ["ItemA", "ItemB", "ItemC", "ItemD", "ItemE"];

        public Action<object, int, object, int> DropCallbackA { get; } = OnDrop;
        public Action<object, int, object, int> DropCallbackB { get; } = OnDrop;

        static void OnDrop(object src, int srcIndex, object dst, int dstIndex) {
            if (src is IList srcList && dst is IList dstList) {
                var item = srcList[srcIndex];
                srcList.RemoveAt(srcIndex);
                if ((uint)dstIndex < dstList.Count) {
                    dstList.Insert(dstIndex, item);
                }
                else {
                    dstList.Add(item);
                }
            }
        }
    }
}
