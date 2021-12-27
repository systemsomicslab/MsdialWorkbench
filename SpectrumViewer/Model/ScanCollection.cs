using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.SpectrumViewer.Model
{
    public class ScanCollection : BindableBase, IScanCollection
    {
        public ScanCollection(string name, IReadOnlyList<IMSScanProperty> scans) {
            if (scans is null) {
                throw new ArgumentNullException(nameof(scans));
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Scans = scans as ObservableCollection<IMSScanProperty> ?? new ObservableCollection<IMSScanProperty>(scans);
        }

        public string Name { get; }

        public ObservableCollection<IMSScanProperty> Scans { get; }
    }
}
