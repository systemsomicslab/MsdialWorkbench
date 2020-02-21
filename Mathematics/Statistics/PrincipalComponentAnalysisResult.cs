using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class PrincipalComponentAnalysisResult
    {
        private ObservableCollection<int> scoreIdCollection;
        private ObservableCollection<int> loadingIdCollection;
        private ObservableCollection<string> scoreLabelCollection;
        private ObservableCollection<string> loadingLabelCollection;
        private ObservableCollection<double[]> scorePointsCollection;
        private ObservableCollection<double[]> loadingPointsCollection;
        private ObservableCollection<double> contributionCollection;
        private ObservableCollection<SolidColorBrush> scoreBrushCollection;
        private ObservableCollection<SolidColorBrush> loadingBrushCollection;

        public PrincipalComponentAnalysisResult()
        {
            scoreIdCollection = new ObservableCollection<int>();
            loadingIdCollection = new ObservableCollection<int>();
            scoreLabelCollection = new ObservableCollection<string>();
            loadingLabelCollection = new ObservableCollection<string>();
            scorePointsCollection = new ObservableCollection<double[]>();
            loadingPointsCollection = new ObservableCollection<double[]>();
            contributionCollection = new ObservableCollection<double>();
            scoreBrushCollection = new ObservableCollection<SolidColorBrush>();
            loadingBrushCollection = new ObservableCollection<SolidColorBrush>();
        }

        #region // properties
        
        public ObservableCollection<double[]> ScorePointsCollection
        {
            get { return scorePointsCollection; }
            set { scorePointsCollection = value; }
        }

        public ObservableCollection<double[]> LoadingPointsCollection
        {
            get { return loadingPointsCollection; }
            set { loadingPointsCollection = value; }
        }

        public ObservableCollection<string> ScoreLabelCollection
        {
            get { return scoreLabelCollection; }
            set { scoreLabelCollection = value; }
        }

        public ObservableCollection<string> LoadingLabelCollecction
        {
            get { return loadingLabelCollection; }
            set { loadingLabelCollection = value; }
        }

        public ObservableCollection<double> ContributionCollection
        {
            get { return contributionCollection; }
            set { contributionCollection = value; }
        }

        public ObservableCollection<SolidColorBrush> ScoreBrushCollection
        {
            get { return scoreBrushCollection; }
            set { scoreBrushCollection = value; }
        }

        public ObservableCollection<SolidColorBrush> LoadingBrushCollection
        {
            get { return loadingBrushCollection; }
            set { loadingBrushCollection = value; }
        }

        public ObservableCollection<int> ScoreIdCollection {
            get {
                return scoreIdCollection;
            }

            set {
                scoreIdCollection = value;
            }
        }

        public ObservableCollection<int> LoadingIdCollection {
            get {
                return loadingIdCollection;
            }

            set {
                loadingIdCollection = value;
            }
        }

        #endregion
    }
}
