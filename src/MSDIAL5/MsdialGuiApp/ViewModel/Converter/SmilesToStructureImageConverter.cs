using CompMs.App.Msdial.Model.Information;
using CompMs.Common.Components;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Converter;

internal class SmilesToStructureImageConverter : IValueConverter
{
    private readonly MoleculeStructureModel _model = new();
    private readonly object _lock = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string smiles)
        {
            lock (_lock)
            {
                _model.UpdateMolecule(new MoleculeProperty { SMILES = smiles });
                return _model.Current;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}