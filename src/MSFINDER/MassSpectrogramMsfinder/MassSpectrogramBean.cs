using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public enum MassSpectrogramIntensityMode
    {
        Absolute,
        Relative
    }

    public class MassSpectrogramBean
    {
        private ObservableCollection<double[]> massSpectraCollection;
        private ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection;
        private SolidColorBrush displayBrush;
        private double lineTickness;
        private float minIntensity;
        private float maxIntensity;
        private float minMass;
        private float maxMass;

        public MassSpectrogramBean(SolidColorBrush displayBrush, double lineTickness, ObservableCollection<double[]> massSpectraCollection)
        {
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.massSpectraCollection = massSpectraCollection;
            SetInitializeValue();
        }

        public MassSpectrogramBean(SolidColorBrush displayBrush, double lineTickness, ObservableCollection<double[]> massSpectraCollection, ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection)
        {
            this.displayBrush = displayBrush;
            this.lineTickness = lineTickness;
            this.massSpectraCollection = massSpectraCollection;
            this.massSpectraDisplayLabelCollection = massSpectraDisplayLabelCollection;
            SetInitializeValue();
        }

        private void SetInitializeValue()
        {
            if (massSpectraCollection == null || massSpectraCollection.Count == 0)
            {
                this.minMass = 0;
                this.maxMass = 100;
                this.minIntensity = 0;
                this.maxIntensity = 100;
                return;
            }
            this.minMass = (float)this.massSpectraCollection[0][0];
            this.maxMass = (float)this.massSpectraCollection[this.massSpectraCollection.Count - 1][0];

            float maxIntensity = float.MinValue, minIntensity = float.MaxValue;
            for (int i = 0; i < this.massSpectraCollection.Count; i++)
            {
                if (maxIntensity < this.massSpectraCollection[i][1])
                    maxIntensity = (float)this.massSpectraCollection[i][1];
                if (minIntensity > this.massSpectraCollection[i][1])
                    minIntensity = (float)this.massSpectraCollection[i][1];
            }
            this.maxIntensity = maxIntensity;
            this.minIntensity = minIntensity;
        }

        public ObservableCollection<double[]> MassSpectraCollection
        {
            get { return massSpectraCollection; }
            set { massSpectraCollection = value; }
        }

        public ObservableCollection<MassSpectrogramDisplayLabel> MassSpectraDisplayLabelCollection
        {
            get { return massSpectraDisplayLabelCollection; }
            set { massSpectraDisplayLabelCollection = value; }
        }

        public SolidColorBrush DisplayBrush
        {
            get { return displayBrush; }
            set { displayBrush = value; }
        }

        public double LineTickness
        {
            get { return lineTickness; }
            set { lineTickness = value; }
        }

        public float MinIntensity
        {
            get { return minIntensity; }
            set { minIntensity = value; }
        }

        public float MaxIntensity
        {
            get { return maxIntensity; }
            set { maxIntensity = value; }
        }

        public float MinMass
        {
            get { return minMass; }
            set { minMass = value; }
        }

        public float MaxMass
        {
            get { return maxMass; }
            set { maxMass = value; }
        }
    }
}
