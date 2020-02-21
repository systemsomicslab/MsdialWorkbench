namespace Rfx.Riken.OsakaUniv
{
    class HelpAboutWindowVM : ViewModelBase
    {
        private string version = "";

        public string Version
        {
            get { return version; }
            set
            {
                version = value;
                OnPropertyChanged("Version");
            }
        }

        private string date;

        public string Date
        {
            get { return date; }
            set
            {
                date = value;
                OnPropertyChanged("Date");
            }
        }


        public HelpAboutWindowVM()
        {
            version = Properties.Resources.VERSION;
            date = "Latest update: " + Properties.Resources.DATE;
        }
    }
}
