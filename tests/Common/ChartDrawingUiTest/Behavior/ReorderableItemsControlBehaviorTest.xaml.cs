using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ChartDrawingUiTest.Behavior
{
    /// <summary>
    /// Interaction logic for ReorderableItemsControlBehaviorTest.xaml
    /// </summary>
    public partial class ReorderableItemsControlBehaviorTest : Page
    {
        public ReorderableItemsControlBehaviorTest() {
            InitializeComponent();
            DataContext = this;
        }

        public ObservableCollection<string> Items { get; } = ["Item1", "Item2", "Item3", "Item4", "Item5"];

        public int CurrentIndex { get; set; }

        public Action<int> DropCallback => OnDrop;

        private void OnDrop(int index) {
            if (index >= 0) {
                Items.Move(CurrentIndex, index);
            }
        }
    }
}
