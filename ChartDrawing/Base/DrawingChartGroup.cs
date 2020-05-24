using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Base
{
    public class DrawingChartGroup : DrawingChartBase
    {
        public ObservableCollection<DrawingChartBase> Children { get; set; }

        public DrawingChartGroup()
        {
            Children = new ObservableCollection<DrawingChartBase>();
            Children.CollectionChanged += OnCollectionChanged;
            PropertyChanged += OnDrawingChanged;
        }

        public override Drawing CreateChart()
        {
            if (InitialArea == default && Children.Count() > 0)
            {
                InitialArea = Children[0].InitialArea;
            }
            if (ChartArea == default)
            {
                ChartArea = InitialArea;
            }
            var group = new DrawingGroup();
            foreach (var drawing in Children)
                group.Children.Add(drawing.CreateChart());
            return group;
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach(var item in e.NewItems)
            {
                if (item is DrawingChartBase drawing)
                    drawing.PropertyChanged += OnChildDrawingChanged;
            }
            foreach(var item in e.OldItems)
            {
                if (item is DrawingChartBase drawing)
                    drawing.PropertyChanged -= OnChildDrawingChanged;
            }
            OnPropertyChanged("Children");
        }

        void OnChildDrawingChanged(object sender, PropertyChangedEventArgs e)
        {
            var drawing = sender as IDrawingChart;
            if (drawing == null) return;
            switch (e.PropertyName)
            {
                case "ChartArea":
                    ChartArea = drawing.ChartArea;
                    break;
            }
        }

        void OnDrawingChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ChartArea":
                    foreach (var drawing in Children)
                        drawing.ChartArea = ChartArea;
                    break;
                case "InitailArea":
                    foreach (var drawing in Children)
                        drawing.InitialArea = InitialArea;
                    break;
                case "RenderSize":
                    foreach (var drawing in Children)
                        drawing.RenderSize = RenderSize;
                    break;
            }
        }

        public override Point RealToImagine(Point point)
        {
            if (Children.Count <= 0)
                return base.RealToImagine(point);
            return Children[0].RealToImagine(point);
        }

        public override Point ImagineToReal(Point point)
        {
            if (Children.Count <= 0)
                return base.ImagineToReal(point);
            return Children[0].ImagineToReal(point);
        }
    }
}
