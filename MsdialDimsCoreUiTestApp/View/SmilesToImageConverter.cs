using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Riken.Metabolomics.StructureFinder.Utility;

namespace MsdialDimsCoreUiTestApp.View
{
    public class SmilesToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (cache == null)
                cache = new Dictionary<string, BitmapImage>();
            if (page == null)
                page = new Queue<string>(20);
            if (value is string smiles)
            {
                if (!cache.ContainsKey(smiles))
                {
                    if (cache.Count == 20)
                    {
                        var rem = page.Dequeue();
                        cache.Remove(rem);
                    }
                    cache[smiles] = MoleculeImage.SmilesToMediaImageSource(smiles, 100, 100);
                    page.Enqueue(smiles);
                }
                return cache[smiles];
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<string, BitmapImage> cache;
        private static Queue<string> page;
    }
}
