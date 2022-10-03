using CompMs.CommonMVVM;
using CompMs.Graphics.Helper;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
    ///     <MyNamespace:ColorPicker/>
    ///
    /// </summary>
    public sealed class ColorPicker : Control
    {
        static ColorPicker() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));

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

        public static Color[] GrayScale { get; }
        public static Color[] BasicColors { get; }
        public static Color[,] ExtraColors { get; }
        private static ColorPickerItem[] DefaultColorPickerItems { get; }

        public ColorPicker() {
            var cv = new ListCollectionView(DefaultColorPickerItems);
            cv.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ColorPickerItem.Category)));
            (cv as ICollectionView).MoveCurrentTo(null);
            ColorPickerItems = cv;
        }

        public ICollectionView ColorPickerItems { get; }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor), typeof(Color), typeof(ColorPicker),
                new FrameworkPropertyMetadata(
                    Colors.White,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColorChanged,
                    CoerceSelectedColor));

        public Color SelectedColor {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

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
                typeof(ColorPickerItem),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(
                    null,
                    OnSelectedItemChanged));

        internal ColorPickerItem SelectedItem {
            get => (ColorPickerItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue is ColorPickerItem item) {
                d.SetValue(SelectedColorProperty, item.Color);
            }
        }
    }

    internal sealed class ColorPickerItem : ViewModelBase {
        public ColorPickerItem(Color color, string category) {
            Color = color;
            Category = category;
        }

        public Color Color { get; }
        public string Category { get; }
    }
}
