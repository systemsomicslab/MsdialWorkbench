using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class LipidDbSetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        private IonMode ionMode;
        private LipidQueryBean lipidQuery;
        private CollisionType collisionType;
        private SolventType solventType;
        private ObservableCollection<LbmQueryVM> positiveQueryVMs;
        private ObservableCollection<LbmQueryVM> negativeQueryVMs;

        public LipidDbSetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;

            if (mainWindow.AnalysisParamForLC.LipidQueryBean == null)
                this.mainWindow.AnalysisParamForLC.LipidQueryBean = getLbmQueries();

            this.lipidQuery = mainWindow.AnalysisParamForLC.LipidQueryBean;
            this.collisionType = this.lipidQuery.CollisionType;
            this.solventType = this.lipidQuery.SolventType;
            this.ionMode = this.mainWindow.ProjectProperty.IonMode;

            this.positiveQueryVMs = getLbmQueryVMs(this.lipidQuery.LbmQueries, IonMode.Positive, this.solventType);
            this.negativeQueryVMs = getLbmQueryVMs(this.lipidQuery.LbmQueries, IonMode.Negative, this.solventType);
        }

        private LipidQueryBean getLbmQueries()
        {
            var lipidQueryBean = new LipidQueryBean();
            var queries = LbmQueryParcer.GetLbmQueries(this.mainWindow.ProjectProperty.IsLabPrivateVersion);
            foreach (var query in queries)
            {
                if (this.mainWindow.ProjectProperty.IonMode != IonMode.Both && query.IonMode != this.mainWindow.ProjectProperty.IonMode)
                {
                    query.IsSelected = false;
                }
            }
            lipidQueryBean.IonMode = this.mainWindow.ProjectProperty.IonMode;
            lipidQueryBean.SolventType = SolventType.CH3COONH4;
            lipidQueryBean.CollisionType = CollisionType.CID;
            lipidQueryBean.LbmQueries = queries;

            return lipidQueryBean;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            var lipidQueryBean = this.lipidQuery;
            lipidQueryBean.CollisionType = this.collisionType;
            lipidQueryBean.SolventType = this.solventType;

            foreach (var query in lipidQueryBean.LbmQueries)
            {
                query.IsSelected = false;
                if (query.IonMode == IonMode.Positive)
                {
                    foreach (var pQuery in positiveQueryVMs.Where(n => n.IsSelected == true))
                    {
                        if (query.LbmClass == pQuery.LbmClass && query.AdductIon.AdductIonName == pQuery.AdductIon.AdductIonName)
                        {
                            query.IsSelected = true; break;
                        }
                    }
                }
                else
                {
                    foreach (var nQuery in negativeQueryVMs.Where(n => n.IsSelected == true))
                    {
                        if (query.LbmClass == nQuery.LbmClass && query.AdductIon.AdductIonName == nQuery.AdductIon.AdductIonName)
                        {
                            query.IsSelected = true; break;
                        }
                    }
                }
            }

            this.mainWindow.AnalysisParamForLC.LipidQueryBean = lipidQueryBean;
            this.window.Close();
        }


        private void OnSolventTypeChanged(SolventType value)
        {
            PositiveQueryVMs = getLbmQueryVMs(this.lipidQuery.LbmQueries, IonMode.Positive, this.solventType);
            NegativeQueryVMs = getLbmQueryVMs(this.lipidQuery.LbmQueries, IonMode.Negative, this.solventType);
            this.window.UpdateLayout();
        }


        private ObservableCollection<LbmQueryVM> getLbmQueryVMs(List<LbmQuery> queries, IonMode ionMode, SolventType solventType)
        {
            var queryVMs = new ObservableCollection<LbmQueryVM>();

            foreach (var query in queries)
            {
                if (query.IonMode == ionMode)
                {
                    if (ionMode == IonMode.Positive)
                    {
                        queryVMs.Add(new LbmQueryVM(query));
                    }
                    else
                    {
                        if (solventType == SolventType.CH3COONH4)
                        {
                            if (query.AdductIon.AdductIonName != "[M+HCOO]-" && query.AdductIon.AdductIonName != "[M+FA-H]-") {
                                queryVMs.Add(new LbmQueryVM(query));
                            }
                        }
                        else if (solventType == SolventType.HCOONH4)
                        {
                            if (query.AdductIon.AdductIonName != "[M+CH3COO]-" && query.AdductIon.AdductIonName != "[M+Hac-H]-") {
                                queryVMs.Add(new LbmQueryVM(query));
                            }
                        }
                    }
                }
            }

            return queryVMs;
        }

        public CollisionType CollisionType
        {
            get { return collisionType; }
            set { if (collisionType == value) return; collisionType = value; OnPropertyChanged("CollisionType"); }
        }

        public SolventType SolventType
        {
            get { return solventType; }
            set { if (solventType == value) return; solventType = value; OnPropertyChanged("SolventType"); OnSolventTypeChanged(value); }
        }

        public ObservableCollection<LbmQueryVM> PositiveQueryVMs
        {
            get { return positiveQueryVMs; }
            set { if (positiveQueryVMs == value) return; positiveQueryVMs = value; OnPropertyChanged("PositiveQueryVMs"); }
        }

        public ObservableCollection<LbmQueryVM> NegativeQueryVMs
        {
            get { return negativeQueryVMs; }
            set { if (negativeQueryVMs == value) return; negativeQueryVMs = value; OnPropertyChanged("NegativeQueryVMs"); }
        }

        public IonMode IonMode {
            get {
                return ionMode;
            }

            set {
                ionMode = value;
            }
        }
    }
}
