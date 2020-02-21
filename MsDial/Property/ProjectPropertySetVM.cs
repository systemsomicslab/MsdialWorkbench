using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class ProjectPropertySetVM : ViewModelBase
    {
        private ProjectPropertyBean projectProperty;

        private string instrumentType;
        private string instrument;
        private string authors;
        private string license;
        private string collisionEnergy;
        private string comment;

        #region properties
        public string InstrumentType
        {
            get { return instrumentType; }
            set { instrumentType = value; OnPropertyChanged("InstrumentType"); }
        }

        public string Instrument
        {
            get { return instrument; }
            set { instrument = value; OnPropertyChanged("Instrument"); }
        }

        public string Authors
        {
            get { return authors; }
            set { authors = value; OnPropertyChanged("Authors"); }
        }

        public string License
        {
            get { return license; }
            set { license = value; OnPropertyChanged("License"); }
        }

        public string CollisionEnergy
        {
            get { return collisionEnergy; }
            set { collisionEnergy = value; OnPropertyChanged("CollisionEnergy"); }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; OnPropertyChanged("Comment"); }
        }
        #endregion

        /// <summary>
        /// Sets up the view model for the PeakListExport window
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded
        {
            get
            {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }
        /// <summary>
        /// Action for the WindowLoaded command
        /// </summary>
        /// <param name="obj"></param>
        private void Window_Loaded(object obj)
        {
            var view = (ProjectPropertySetWin)obj;
            var mainWindow = (MainWindow)view.Owner;
            this.projectProperty = mainWindow.ProjectProperty;

            this.InstrumentType = this.projectProperty.InstrumentType;
            this.Instrument = this.projectProperty.Instrument;
            this.Authors = this.projectProperty.Authors;
            this.CollisionEnergy = this.projectProperty.CollisionEnergy;
            this.License = this.projectProperty.License;
            this.Comment = this.projectProperty.Comment;
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand closeWindow;
        public DelegateCommand CloseWindow
        {
            get
            {
                return closeWindow ?? (closeWindow = new DelegateCommand(obj => {
                    var view = (ProjectPropertySetWin)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Saves the Peak list and closes the window
        /// </summary>
        private DelegateCommand setProjectProperty;
        public DelegateCommand SetProjectProperty
        {
            get
            {
                return setProjectProperty ?? (setProjectProperty = new DelegateCommand(winobj => {

                    this.projectProperty.InstrumentType = this.instrumentType;
                    this.projectProperty.Instrument = this.instrument;
                    this.projectProperty.Authors = this.authors;
                    this.projectProperty.License = this.license;
                    this.projectProperty.CollisionEnergy = this.collisionEnergy;

                    if (this.comment != null && this.comment != string.Empty)
                        this.comment = this.comment.Replace("\r", "").Replace("\n", " ");

                    this.projectProperty.Comment = this.comment;

                    var view = (ProjectPropertySetWin)winobj;
                    view.Close();
                }, CanSetProjectProperty));
            }
        }

        private bool CanSetProjectProperty(object arg)
        {
            return true;
        }
    }
}
