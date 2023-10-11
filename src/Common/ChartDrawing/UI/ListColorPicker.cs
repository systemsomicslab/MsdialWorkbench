using CompMs.Graphics.Helper;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
    ///     <MyNamespace:ListColorPicker/>
    ///
    /// </summary>
    public class ListColorPicker : BaseColorPicker
    {
        public static Color[] GrayScale { get; }
        public static Color[] BasicColors { get; }
        public static Color[,] ExtraColors { get; }
        private static ColorPickerItem[] DefaultColorPickerItems { get; }

        static ListColorPicker() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListColorPicker), new FrameworkPropertyMetadata(typeof(ListColorPicker)));
            SelectedColorProperty.OverrideMetadata(typeof(ListColorPicker), new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged, CoerceSelectedColor));

            GrayScale = Enumerable.Range(0, 12).Select(v => ColorHelper.FromNormalizedHsv(0d, 0d, v / 11d)).ToArray();
            BasicColors = Enumerable.Range(0, 12).Select(v => ColorHelper.FromNormalizedHsv(v / 12d, 1d, 1d)).ToArray();
            ExtraColors = new Color[7, 12];
            for (int i = 0; i < 12; i++) {
                for (int j = 0; j < 4; j++) {
                    ExtraColors[j, i] = ColorHelper.FromNormalizedHsv(i / 12d, (j + 1) / 5d, 1d);
                }
                for (int j = 0; j < 3; j++) {
                    ExtraColors[j + 4, i] = ColorHelper.FromNormalizedHsv(i / 12d, 1d, 1d - (j + 1) / 5d);
                }
            }
            DefaultColorPickerItems = new[]
            {
                GrayScale.Select(color => new ColorPickerItem(color, "GrayScale")),
                BasicColors.Select(color => new ColorPickerItem(color, "Basic")),
                ExtraColors.Cast<Color>().Select(color => new ColorPickerItem(color, "Others")),
            }.SelectMany(items => items)
            .ToArray();
        }

        private readonly CustomColorPickerItem _custom;

        public ListColorPicker() {
            _custom = new CustomColorPickerItem("Custom");
            var cv = new ListCollectionView(DefaultColorPickerItems.Append<object>(_custom).ToArray());
            cv.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ColorPickerItem.Category)));
            (cv as ICollectionView).MoveCurrentTo(_custom);
            ColorPickerItems = cv;
            SetCurrentValue(SelectedItemProperty, _custom);
        }

        public ICollectionView ColorPickerItems { get; }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (ListColorPicker)d;
            cp.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        private void OnSelectedColorChanged(Color oldValue, Color newValue) {
            IColorPickerItem item = DefaultColorPickerItems.FirstOrDefault(item_ => item_.Color == newValue);
            if (item is null) {
                _custom.Color = newValue;
                item = _custom;
            }
            SetCurrentValue(SelectedItemProperty, item);
        }

        private static object CoerceSelectedColor(DependencyObject d, object value) {
            if (value is null) {
                return d.GetValue(SelectedColorProperty);
            }
            return value;
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(IColorPickerItem),
                typeof(ListColorPicker),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged,
                    CoerceSelectedItem));

        internal IColorPickerItem SelectedItem {
            get => (IColorPickerItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue is IColorPickerItem item) {
                d.SetCurrentValue(SelectedColorProperty, item.Color);
            }
        }

        private static object CoerceSelectedItem(DependencyObject d, object value) {
            if (value is null) {
                return ((ListColorPicker)d)._custom;
            }
            return value;
        }
    }
}
