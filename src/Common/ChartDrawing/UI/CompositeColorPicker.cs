using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace CompMs.Graphics.UI
{
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
    ///     <MyNamespace:CompositeColorPicker/>
    ///
    /// </summary>
    [ContentProperty(nameof(Pickers))]
    public class CompositeColorPicker : BaseColorPicker
    {
        static CompositeColorPicker() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CompositeColorPicker), new FrameworkPropertyMetadata(typeof(CompositeColorPicker)));
            SelectedColorProperty.OverrideMetadata(typeof(CompositeColorPicker), new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));
        }

        private bool _colorChanging = false;
        private Dictionary<BaseColorPicker, EventHandler> _handlers;

        public CompositeColorPicker() {
            _handlers = new Dictionary<BaseColorPicker, EventHandler>();
            Pickers = new ObservableCollection<CompositeColorPickerItem>();
            Pickers.CollectionChanged += OnPickersCollectionChanged;
        }

        [Bindable(true)]
        public ObservableCollection<CompositeColorPickerItem> Pickers { get; }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((CompositeColorPicker)d).OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        private void OnSelectedColorChanged(Color oldValue, Color newValue) {
            if (_colorChanging) {
                return;
            }
            _colorChanging = true;
            foreach (var item in Pickers) {
                item.Picker.SetCurrentValue(SelectedColorProperty, newValue);
            }
            _colorChanging = false;
        }

        private void OnPickersCollectionChanged(object obj, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (CompositeColorPickerItem item in e.NewItems) {
                        AddedNewPicker(item.Picker);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (CompositeColorPickerItem item in e.OldItems) {
                        RemovePicker(item.Picker);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (CompositeColorPickerItem item in e.OldItems) {
                        RemovePicker(item.Picker);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (CompositeColorPickerItem item in e.OldItems) {
                        RemovePicker(item.Picker);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

        private void AddedNewPicker(BaseColorPicker picker) {
            var desc = DependencyPropertyDescriptor.FromProperty(SelectedColorProperty, picker.GetType());
            EventHandler handle = (s, e) => SetCurrentValue(SelectedColorProperty, picker.SelectedColor);
            desc.AddValueChanged(picker, handle);
            _handlers.Add(picker, handle);
        }

        private void RemovePicker(BaseColorPicker picker) {
            var desc = DependencyPropertyDescriptor.FromProperty(SelectedColorProperty, picker.GetType());
            if (_handlers.TryGetValue(picker, out var handle)) {
                desc.RemoveValueChanged(picker, handle);
                _handlers.Remove(picker);
            }
        }

        public static readonly DependencyProperty ContentWidthProperty =
            DependencyProperty.Register(
                nameof(ContentWidth),
                typeof(double),
                typeof(CompositeColorPicker));

        public double ContentWidth {
            get => (double)GetValue(ContentWidthProperty);
            set => SetValue(ContentWidthProperty, value);
        }

        public static readonly DependencyProperty ContentHeightProperty =
            DependencyProperty.Register(
                nameof(ContentHeight),
                typeof(double),
                typeof(CompositeColorPicker));

        public double ContentHeight {
            get => (double)GetValue(ContentHeightProperty);
            set => SetValue(ContentWidthProperty, value);
        }
    }

    [ContentProperty(nameof(Picker))]
    public sealed class CompositeColorPickerItem : DependencyObject {
        public CompositeColorPickerItem() {
            
        }

        public CompositeColorPickerItem(BaseColorPicker picker, string label) {
            Picker = picker;
            Label = label;
        }

        public static readonly DependencyProperty PickerProperty =
            DependencyProperty.Register(
                nameof(Picker),
                typeof(BaseColorPicker),
                typeof(CompositeColorPickerItem));

        public BaseColorPicker Picker {
            get => (BaseColorPicker)GetValue(PickerProperty);
            set => SetValue(PickerProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(CompositeColorPickerItem));

        public string Label {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
