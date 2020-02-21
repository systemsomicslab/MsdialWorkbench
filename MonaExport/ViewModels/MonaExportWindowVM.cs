using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using Rfx.Riken.OsakaUniv;
using edu.ucdavis.fiehnlab.MonaRestApi;
using edu.ucdavis.fiehnlab.MonaExport.UtilClasses;
using edu.ucdavis.fiehnlab.MonaExport.DataObjects;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using edu.ucdavis.fiehnlab.MonaRestApi.client;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using edu.ucdavis.fiehnlab.MonaExport.Windows;
using Newtonsoft.Json.Linq;
using System.Security;
using System.Collections.Generic;
using System.Configuration;

namespace edu.ucdavis.fiehnlab.MonaExport.ViewModels {
    public class MonaExportWindowVM : ViewModelBase {

		private IMonaRestClient client;
		private bool IsUploading = false;

		#region VM fields
		private MonaProjectData projectData;
		private MonaSpectrum selectedSpectrum;
		private MetaData selectedMetadata;
		private Tag selectedTag;
        private Submitter submitterData;

		private ObservableCollection<MonaSpectrum> spectra = new ObservableCollection<MonaSpectrum>();
		private ObservableCollection<MetaData> selectedSpectrumMetadata = new ObservableCollection<MetaData>();
		private ObservableCollection<Tag> selectedSpectrumTagList = new ObservableCollection<Tag>();

		private string infoBoxText = string.Empty;

        //private bool closeTrigger;

        //compound type and sample species/origin
        private bool isStandard = false;
        private string selectedSpecies;
        private string selectedOrgan;
        private ObservableCollection<string> speciesList = new ObservableCollection<string>();
        private ObservableCollection<string> organList = new ObservableCollection<string>();

        //project buttons fields
        private bool loginButtonEnabled = true;
		private bool loadButtonEnabled = false;
		private bool saveButtonEnabled = false;
#if(DEBUG)
		private bool uploadButtonEnabled = true;
		private bool spectraGroupEnabled = true;
        private bool sampleOriginEnabled = true;
#else
        private bool uploadButtonEnabled = false;
        private bool spectraGroupEnabled = false;
        private bool sampleOriginEnabled = false;
#endif
        private TokenInfo tokenInfo;
        private string username;
        private SecureString password;
        
        //metadata buttons fields
        private bool addMetadataButtonEnabled = false;
		private bool removeMetadataButtonEnabled = false;
		private bool clearMetadataButtonEnabled = false;
		private bool addToAllMetadataButtonEnabled = false;
		private bool removeFromAllMetadataButtonEnabled = false;
		private string addMetadataButtonCaption = "Add";
		private string addTagButtonCaption = "Add";
		private string addAllMetadataButtonCaption = "Add to all";
		private string addAllTagButtonCaption = "Add to all";

		//tags buttons fields
		private bool addTagButtonEnabled = false;
		private bool addCommonTagButtonEnabled = true;
		private bool removeTagButtonEnabled = false;
		private bool clearTagsButtonEnabled = false;
		private bool addAllTagButtonEnabled = false;
		private bool removeAllTagButtonEnabled = false;

		//used to add/update a metadata item to the selected spectrum metadata list
		private string selectedMetadataName;
		private string newMetadataValue;

		//used to add a new tag 
		private string newTagValue;

		//holders of metadata names and tags
		private ObservableCollection<string> metadataNameList = new ObservableCollection<string>();
		private ObservableCollection<Tag> commonTagList = new ObservableCollection<Tag>();

        public event EventHandler RequestClose;

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public MonaExportWindowVM() {
			Debug.WriteLine("MonaExportWindowVM - DefaultConst");
			ProjectData = new MonaProjectData();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) {
				client = new DesignTimeMonaClient();
				Spectra = client.DownloadSpectra();
				ProjectData.Spectra = Spectra;
				ProjectData.Submitter = new Submitter { firstName = "test", emailAddress = "test@mail.com", lastName = "user" };

				SelectedSpectrum = Spectra.First();

				MetadataNameList = new ObservableCollection<string> { "accurate mass", "ionization", "fragmentation mode", "precursor mass", "ion mode", "ms level" };
				SelectedMetadataName = MetadataNameList.First();

                SpeciesList.Add("-- Select a Species --");
                OrganList.Add("-- Select an Organ --");

                client.GetSpecies().ToList().ForEach(sp => SpeciesList.Add(sp));
                client.GetOrgans().ToList().ForEach(or => OrganList.Add(or));

                SelectedSpecies = SpeciesList.First();
                SelectedOrgan = OrganList.First();

                CommonTagList = new ObservableCollection<Tag> { };
            }
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initData">Object containing the selected spectra to be uploaded to Mona</param>
		public MonaExportWindowVM(MonaProjectData initData) {
            var monaHost = ConfigurationManager.AppSettings.Get("mHost");
            var bvHost = ConfigurationManager.AppSettings.Get("bvHost");

            Debug.WriteLine(String.Format("mona: {0} -- bv: {1}", monaHost, bvHost));

            client = new MonaClient(monaHost, bvHost);
            DisableAllButtons();

            //getting autofill lists
            try {
                var tmpList = new List<string>();
                client.GetMetadataNames().ToList().ForEach(i => metadataNameList.Add(i.ToLower()));
                client.GetCommonTags().ForEach(tag => commonTagList.Add(tag));

                SpeciesList.Add("-- Select a Species --");
                OrganList.Add("-- Select an Organ --");

                client.GetSpecies().ToList().ForEach(sp => SpeciesList.Add(sp));
                client.GetOrgans().ToList().ForEach(og => OrganList.Add(og));

                SelectedSpecies = SpeciesList.First();
                SelectedOrgan = OrganList.First();

            } catch (Exception) {
				InfoBoxText = "Sorry, a connection could not be made to Mona.";
				UploadButtonEnabled = false;
			}

            addEventHandlers();

			SubmitterDataVisibility = Visibility.Collapsed;

			ProjectData = initData;

			Spectra = initData.Spectra;
			if (Spectra.Count > 0) {
				SelectedSpectrum = Spectra.First();
			} else {
				SelectedSpectrum = new MonaSpectrum();
				SelectedSpectrum.metaData = new ObservableCollection<MetaData>();
				SelectedSpectrum.tags = new ObservableCollection<Tag>();
			}

			UploadData.RaiseCanExecuteChanged();

			SelectedMetadataName = MetadataNameList.First();
        }

#region EventHandlers
        private void UpdateUploadButton(object sender, NotifyCollectionChangedEventArgs e) {
			if (Spectra.Count > 0) {
				UploadButtonEnabled = true;
			}
		}

		private void UpdateUploadStatus(object sender, PropertyChangedEventArgs e) {
			MessageBox.Show("changed");
			UploadData.RaiseCanExecuteChanged();
		}

		private void SelectedSpectrum_metaData_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateClearButtons();
			OnPropertyChanged("SelectedSpectrum");
		}

		private void SelectedSpectrum_tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateClearButtons();
            OnPropertyChanged("SelectedSpectrum");
		}
#endregion

#region Properties

		public MonaProjectData ProjectData {
			get { return projectData ?? (projectData = new MonaProjectData()); }
			set {
				if (projectData == value) { return; }
				projectData = value;
				OnPropertyChanged("ProjectData");
			}
		}

		public string InfoBoxText {
			get { return infoBoxText; }
			set {
				if (value == infoBoxText) return;
				infoBoxText = value;
				OnPropertyChanged("InfoBoxText");
			}
		}

		public ObservableCollection<MonaSpectrum> Spectra {
			get { return spectra ?? (spectra = new ObservableCollection<MonaSpectrum>()); }
			set {
				if (value == spectra) return;
				spectra = value;
				OnPropertyChanged("Spectra");
			}
		}

		public MonaSpectrum SelectedSpectrum {
			get { return selectedSpectrum??(selectedSpectrum = new MonaSpectrum()); }
			set {
				if (value == selectedSpectrum) { return; }
				selectedSpectrum = value;
				UpdateMetaDataNameList();
                UpdateClearButtons();
                CheckNewMetadataAndUpdateGUI();
				SelectedMetadataName = MetadataNameList.First();
				//Debug.WriteLine("SelectedSpectrum changed");
				OnPropertyChanged("SelectedSpectrum");
			}
		}

		public MetaData SelectedMetadata {
			get { return selectedMetadata; }
			set {
				if (value == selectedMetadata) { return; }
				selectedMetadata = value;
				//Debug.WriteLine("SelectedMetadata changed (metadata list) " + value);
				OnPropertyChanged("SelectedMetadata");
            }
		}

		public ObservableCollection<string> MetadataNameList {
			get {
				return metadataNameList;
			}

			set {
				if (metadataNameList == value) { return; }
				metadataNameList.Union(value).ToList();
				metadataNameList.OrderBy(n => n);
				//Debug.WriteLine("MetadataNameList changed");
				OnPropertyChanged("MetadataNameList");
			}
		}

		public string SelectedMetadataName {
			get { return selectedMetadataName; }
			set {
				if (value == selectedMetadataName) { return; }
				selectedMetadataName = value;
				//Debug.WriteLine("changed metadata name (combobox) " + value);
				OnPropertyChanged("SelectedMetadataName");
			}
		}

		[Required]
		public string NewMetadataValue {
			get { return newMetadataValue; }
			set {
				if (value == newMetadataValue) { return; }
				newMetadataValue = value;
				CheckNewMetadataAndUpdateGUI();
				//Debug.WriteLine("changed new metadata value");
				OnPropertyChanged("NewMetadataValue");
			}
		}

        [Required]
        public string NewTagValue
        {
            get { return newTagValue; }
            set
            {
                newTagValue = value;
                CheckNewTagAndUpdateGUI();
				//Debug.WriteLine("changed new tag value");
				OnPropertyChanged("NewTagValue");
			}
		}

        public Tag SelectedTag {
            get { return selectedTag ?? (selectedTag = new Tag()); }
            set {
                selectedTag = value;
                if (selectedTag != null) {
                    RemoveTagButtonEnabled = true;
                    RemoveAllTagButtonEnabled = true;
                } else {
                    RemoveTagButtonEnabled = false;
                    RemoveAllTagButtonEnabled = false;
                }
                CheckNewTagAndUpdateGUI();
                OnPropertyChanged("SelectedTag");
            }
        }

        public ObservableCollection<Tag> CommonTagList
        {
            get { return commonTagList; }

            set { if (commonTagList == value) { return; }
                commonTagList.Union(value).ToList();
                OnPropertyChanged("CommonTagList");
            }
        }

        public Submitter SubmitterData
        {
            get { return submitterData; }
            set { if(submitterData == value) { return; }
                submitterData = value;
                OnPropertyChanged("SubmitterData");
            }
        }

        public ObservableCollection<string> SpeciesList
        {
            get { return speciesList; }
            set { if(speciesList == value) { return; }
                speciesList.Union(value);
                OnPropertyChanged("SpeciesList");
            }
        }

        public string SelectedSpecies
        {
            get { return selectedSpecies; }
            set { if (selectedSpecies == value) { return; }
                var md = new MetaData() { Name = "species", Value = value };
                UpdateSpectraMetaData(md);
                OnPropertyChanged("SelectedSpecies");
            }
        }

        public ObservableCollection<string> OrganList
        {
            get { return organList; }
            set
            {
                if (organList == value) { return; }
                organList.Union(value);
                OnPropertyChanged("OrganList");
            }
        }

        public string SelectedOrgan
        {
            get { return selectedOrgan; }
            set { if (selectedOrgan == value) { return; }
                var md = new MetaData() { Name = "organ", Value = value };
                selectedOrgan = value;
                UpdateSpectraMetaData(md);
                OnPropertyChanged("SelectedOrgan");
            }
        }

        public bool IsStandard
        {
            get { return isStandard; }
            set { if(isStandard==value) { return; }
                isStandard = value;
                Debug.WriteLine("Changed isStandard: " + value);
                //Compound cpd = ProjectData.Spectra
                //    .Where(it => it.splash.Equals(SelectedSpectrum.splash))
                //    .Single().compounds.Single();
                Compound cpd = SelectedSpectrum.compounds.Single();
                if (value) {
                    cpd.AddIsStandardMetadata();
                } else {
                    cpd.RemoveIsStandardMetadata();
                }

                OnPropertyChanged("IsStandard");
                OnPropertyChanged("SelectedSpectrum");
            }
        }

#region login fields and properties
        private MonaToken Token { get; set; }

        public TokenInfo TokenInfo {
			get { return tokenInfo; }
			set { if(value == tokenInfo) { return; }
				tokenInfo = value;
				OnPropertyChanged("TokenInfo");
				if (string.IsNullOrWhiteSpace(TokenInfo.username)) {
					SubmitterLoginVisibility = Visibility.Visible;
					SubmitterDataVisibility = Visibility.Collapsed;
				} else {
					SubmitterLoginVisibility = Visibility.Collapsed;
					SubmitterDataVisibility = Visibility.Visible;
				}
			}
		}

        [Required(ErrorMessage = "Username is required to login")]
		public string Username {
			get { return username; }
			set { if (value == username) { return; }
				username = value;
				OnPropertyChanged("Username");
			}
		}

        [Required(ErrorMessage = "Password is required to login")]
		public SecureString Password {
			get { return password; }
			set {
                Debug.WriteLine("changing password: " + value);
				if (value == password) { return; }
				password = value;
				OnPropertyChanged("Password");
			}
		}

		public bool SpectraGroupEnabled {
			get { return spectraGroupEnabled; }
			set { if(value == spectraGroupEnabled) { return; }
				spectraGroupEnabled = value;
				OnPropertyChanged("SpectraGroupEnabled");
			}
		}

        public bool SampleOriginEnabled {
            get { return sampleOriginEnabled; }
            set { if (value == sampleOriginEnabled) { return; }
                sampleOriginEnabled = value;
                OnPropertyChanged("SampleOriginEnabled");
            }
        }

        //visibility
        private Visibility submitterLoginVisibility;
		public Visibility SubmitterLoginVisibility {
			get { return submitterLoginVisibility; }
			set {
				submitterLoginVisibility = value;
				OnPropertyChanged("SubmitterLoginVisibility");
			}
		}

		private Visibility submitterDataVisibility;
		public Visibility SubmitterDataVisibility {
			get { return submitterDataVisibility; }
			set {
				submitterDataVisibility = value;
				OnPropertyChanged("SubmitterDataVisibility");
			}
		}

#endregion //login fields and properties

#region buttons properties
		public bool AddMetadataButtonEnabled {
			get { return addMetadataButtonEnabled; }
			set {
				if (value == addMetadataButtonEnabled) { return; }
				addMetadataButtonEnabled = value;
				printButtonStatus("add", addMetadataButtonEnabled);
				OnPropertyChanged("AddMetadataButtonEnabled");
            }
        }

        public bool RemoveMetadataButtonEnabled {
			get { return removeMetadataButtonEnabled; }
			set {
				if (value == removeMetadataButtonEnabled) { return; }
				removeMetadataButtonEnabled = value;
				printButtonStatus("remove", removeMetadataButtonEnabled);
				OnPropertyChanged("RemoveMetadataButtonEnabled");
			}
		}

        public bool ClearMetadataButtonEnabled {
			get { return clearMetadataButtonEnabled; }
			set {
				if (value == clearMetadataButtonEnabled) { return; }
				clearMetadataButtonEnabled = value;
				printButtonStatus("clear", clearMetadataButtonEnabled);
				OnPropertyChanged("ClearMetadataButtonEnabled");
			}
		}

        public bool AddToAllMetadataButtonEnabled {
			get { return addToAllMetadataButtonEnabled; }
			set {
				if (value == addToAllMetadataButtonEnabled) { return; }
				addToAllMetadataButtonEnabled = value;
				printButtonStatus("addAll", addToAllMetadataButtonEnabled);
				OnPropertyChanged("AddToAllMetadataButtonEnabled");
			}
		}

        public bool RemoveFromAllMetadataButtonEnabled {
			get { return removeFromAllMetadataButtonEnabled; }
			set {
				if (value == removeFromAllMetadataButtonEnabled) { return; }
				removeFromAllMetadataButtonEnabled = value;
				printButtonStatus("remove all", removeFromAllMetadataButtonEnabled);
				OnPropertyChanged("RemoveFromAllMetadataButtonEnabled");
			}
		}

		public bool AddTagButtonEnabled {
			get { return addTagButtonEnabled; }
			set {
				if (value == addTagButtonEnabled) { return; }
				addTagButtonEnabled = value;
				printButtonStatus("addTag", addTagButtonEnabled);
				OnPropertyChanged("AddTagButtonEnabled");
			}
		}

        public bool RemoveTagButtonEnabled {
			get { return removeTagButtonEnabled; }
			set {
				if (value == removeTagButtonEnabled) { return; }
				removeTagButtonEnabled = value;
				printButtonStatus("remove tag", removeTagButtonEnabled);
				OnPropertyChanged("RemoveTagButtonEnabled");
			}
		}

        public bool ClearTagsButtonEnabled {
			get { return clearTagsButtonEnabled; }
			set {
				if (value == clearTagsButtonEnabled) { return; }
				clearTagsButtonEnabled = value;
				printButtonStatus("clear tag", clearTagsButtonEnabled);
				OnPropertyChanged("ClearTagsButtonEnabled");
			}
		}

        public bool AddAllTagButtonEnabled {
			get { return addAllTagButtonEnabled; }
			set {
				if (value == addAllTagButtonEnabled) { return; }
				addAllTagButtonEnabled = value;
				printButtonStatus("addAll Tag", addAllTagButtonEnabled);
				OnPropertyChanged("AddAllTagButtonEnabled");
			}
		}

        public bool RemoveAllTagButtonEnabled {
			get { return removeAllTagButtonEnabled; }
			set {
				if (value == removeAllTagButtonEnabled) { return; }
				removeAllTagButtonEnabled = value;
				printButtonStatus("removeAll tag", removeAllTagButtonEnabled);
				OnPropertyChanged("RemoveAllTagButtonEnabled");
			}
		}

        public bool AddCommonTagButtonEnabled {
            get { return addCommonTagButtonEnabled; }
            set {
                if (value == addCommonTagButtonEnabled) { return; }
                addCommonTagButtonEnabled = value;
				printButtonStatus("addCommon Tag", addCommonTagButtonEnabled);
				OnPropertyChanged("AddCommonTagButtonEnabled");
			}
		}

		public bool LoginButtonEnabled {
			get { return loginButtonEnabled; }
			set { if(value == loginButtonEnabled) { return; }
				loginButtonEnabled = value;
				OnPropertyChanged("LoginButtonEnabled");
			}
		}

        public bool LoadButtonEnabled {
			get { return loadButtonEnabled; }
			set { loadButtonEnabled = value;
				OnPropertyChanged("LoadButtonEnabled");
			}
		}

		public bool SaveButtonEnabled {
			get { return saveButtonEnabled; }
			set {
				saveButtonEnabled = value;
				OnPropertyChanged("SaveButtonEnabled");
			}
		}

		public bool UploadButtonEnabled {
			get { return uploadButtonEnabled; }
			set {
				uploadButtonEnabled = value;
				OnPropertyChanged("UploadButtonEnabled");
			}
		}

        public string AddMetadataButtonCaption {
            get { return addMetadataButtonCaption ?? (addMetadataButtonCaption = "Add"); }
            private set {
                addMetadataButtonCaption = value;
                OnPropertyChanged("AddMetadataButtonCaption");
            }
        }

        public string AddTagButtonCaption {
            get { return addTagButtonCaption ?? (addTagButtonCaption = "Add"); }
            private set {
                addTagButtonCaption = value;
                OnPropertyChanged("AddTagButtonCaption");
            }
        }

		public string AddAllMetadataButtonCaption {
            get { return addAllMetadataButtonCaption ?? (addAllMetadataButtonCaption = "Add to all"); }
            private set {
                addAllMetadataButtonCaption = value;
                OnPropertyChanged("AddAllMetadataButtonCaption");
            }
        }

        public string AddAllTagButtonCaption {
            get { return addAllTagButtonCaption ?? (addAllTagButtonCaption = "Add to all"); }
            private set {
                addAllTagButtonCaption = value;
                OnPropertyChanged("AddAllTagButtonCaption");
            }
        }
#endregion //button properties
#endregion //Properties

#region Commands
		private DelegateCommand windowLoadedCommand;
		public DelegateCommand WindowLoadedCommand {
			get {
				return windowLoadedCommand ?? (windowLoadedCommand = new DelegateCommand(initializeLoadedWindow, it => true));
			}
		}
		private void initializeLoadedWindow(object sender) {
			if (Spectra.Count > 0) {
                UploadButtonEnabled = false;
                LoadButtonEnabled = false;
                SaveButtonEnabled = false;
				SelectedSpectrum = Spectra.First();

                SpectraGroupEnabled = false;
                SampleOriginEnabled = false;
            }
        }

		private DelegateCommand exitCommand;
		public DelegateCommand ExitCommand {
			get {
				Debug.WriteLine("Exit command called");
				return exitCommand ?? (exitCommand = new DelegateCommand(obj => {
                    if (obj != null && !IsUploading) {
                        ((Window)obj).Close();
                    }
                }, obj => true));
			}
		}

		private DelegateCommand changeSelectedSpectrum;
		public DelegateCommand ChangeSelectedSpectrum {
			get { return changeSelectedSpectrum ?? (changeSelectedSpectrum = new DelegateCommand(updateRelatedControls, spec => Spectra.Count > 0)); }
		}
		private void updateRelatedControls(object obj) {
			var spec = obj as MonaSpectrum;

			//clear textBoxes for new values
			SelectedMetadata = null;
			SelectedTag = null;
			NewMetadataValue = string.Empty;
			NewTagValue = string.Empty;

			UpdateRemoveButtons(null);

			// update metadata buttons
			if (SelectedSpectrum.metaData.Count > 0) {
				ClearMetadataButtonEnabled = true;
			} else {
				ClearMetadataButtonEnabled = false;
			}

			//update tag buttons
			if (SelectedSpectrum.tags.Count > 0) {
				ClearTagsButtonEnabled = true;
			} else {
				ClearTagsButtonEnabled = false;
			}
		}

		private DelegateCommand changeMetadataListSelection;
		public DelegateCommand ChangeMetadataListSelection {
			get {
				return changeMetadataListSelection ?? (changeMetadataListSelection = new DelegateCommand(
					list => {
						var control = ((ListBox)list);
						var currentMD = (MetaData)control.SelectedItem;

						SelectedMetadataName = currentMD.Name;
						NewMetadataValue = currentMD.Value.ToString();
						UpdateRemoveButtons(new object());
					},
					list => (((ListBox)list).SelectedIndex > -1 && SelectedSpectrum.metaData.Count > 0)
				));
			}
		}

		private DelegateCommand changeMetadataNameSelection;
		public DelegateCommand ChangeMetadataNameSelection {
			get {
				return changeMetadataNameSelection ?? (changeMetadataNameSelection = new DelegateCommand(
				  it => {
					  //SelectedMetadata = null;
					  UpdateRemoveButtons(SelectedMetadata);
					  NewMetadataValue = string.Empty;
				  },
				  it => true
			  ));
			}
		}

		private DelegateCommand addMetadataToCurrentSpectrum;
		public DelegateCommand AddMetadataToCurrentSpectrum {
			get {
				return addMetadataToCurrentSpectrum ?? (addMetadataToCurrentSpectrum = new DelegateCommand(AddToMetadataList, CanAddMetadataList));
			}
		}
		private void AddToMetadataList(object metadata) {
			var md = metadata as MetaData;

            var existentMetaData = SelectedSpectrum.metaData.ToList().Find(item => item.Name == md.Name);
            if (existentMetaData != null) {
				var update = MessageBox.Show("This metadata already exists.\nWould you like to update its value?", "Update Metadata value?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

				if(MessageBoxResult.Yes == update) {
					SelectedSpectrum.metaData.ToList().Find(it => it.Name == md.Name).Value = md.Value;
				}
			} else {
				SelectedSpectrum.metaData.Add(new MetaData { Name = SelectedMetadataName, Value = NewMetadataValue });
			}

            CheckNewMetadataAndUpdateGUI();
            NewMetadataValue = string.Empty;
		}
		private bool CanAddMetadataList(object metadata) {
			return AddMetadataButtonEnabled;
		}

		private DelegateCommand removeMetadataFromSelectedSpectrum;
		public DelegateCommand RemoveMetadataFromSelectedSpectrum {
			get {
				return removeMetadataFromSelectedSpectrum ?? (removeMetadataFromSelectedSpectrum = new DelegateCommand(
			  listbox => {
				  var item = ((ListBox)listbox).SelectedItem as MetaData;
				  SelectedSpectrum.metaData.Remove(item);
				  SelectedMetadata = null;
				  UpdateRemoveButtons(SelectedMetadata);
			  }, CanRemoveMetadata
			  ));
			}
		}
		private bool CanRemoveMetadata(object listbox) {
			return RemoveMetadataButtonEnabled;
		}

		private DelegateCommand clearSelectedSpectrumMetadata;
		public DelegateCommand ClearSelectedSpectrumMetadata {
			get { return clearSelectedSpectrumMetadata ?? (clearSelectedSpectrumMetadata = new DelegateCommand(
					obj => {
						SelectedSpectrum.metaData.Clear();
					},
					CanClearMetadata
				));
			}
		}
		private bool CanClearMetadata(object obj) {
			return ClearMetadataButtonEnabled;
		}

		private DelegateCommand login;
		public DelegateCommand Login {
			get { return login ?? (login = new DelegateCommand(
				credentials => {
					try {
						Token = client.Login(Username, Password);
                        if (Token == null) return;
						if (!string.IsNullOrWhiteSpace(Token.Token)) {
							TokenInfo = ((MonaClient)client).GetTokenInfo(Token.Token);

                            SubmitterData = client.GetSubmitterInfo(Username, Token);

                            foreach(MonaSpectrum spec in Spectra) {
                                spec.submitter = submitterData;
                            }
                            OnPropertyChanged("Spectra");

                            SpectraGroupEnabled = true;
                            SampleOriginEnabled = true;
							LoginButtonEnabled = false;

                            LoadButtonEnabled = true;
                            SaveButtonEnabled = true;

							UploadButtonEnabled = true;
						}
					} catch (WebException e) {
						Debug.WriteLine("Error login to mona.\n" + e.Message + " (" + e.GetType() + ")");
						var alert = new LoginError();
						alert.DataContext = new LoginErrorVM();
						alert.Show();

						UploadButtonEnabled = false;
					}

				},
				credentials => true
			));
			}
		}

		private DelegateCommand addMetadataToAllSpectra;
		public DelegateCommand AddMetadataToAllSpectra {
			get {
				return addMetadataToAllSpectra ?? (addMetadataToAllSpectra = new DelegateCommand(AddMetadataInAllSpectra, CanAddMetadataList));
			}
		}
		private void AddMetadataInAllSpectra(object md) {
			var metadata = md as MetaData;

			foreach (var spec in Spectra) {
				//check if metadata exists and replace or add new
				var oldMetadata = spec.metaData.ToList().Find(it => it.Name == metadata.Name);
				if (oldMetadata != null) {
					oldMetadata.Value = metadata.Value;
				} else {
					spec.metaData.Add(new MetaData { Name = metadata.Name, Value = metadata.Value });
				}
			}

			CheckNewMetadataAndUpdateGUI();
			NewMetadataValue = string.Empty;
		}

		private DelegateCommand removeMetadataFromAllSpectra;
		public DelegateCommand RemoveMetadataFromAllSpectra {
			get {
				return removeMetadataFromAllSpectra ?? (removeMetadataFromAllSpectra = new DelegateCommand(RemoveMetadataInAllSpectra, CanRemoveMetadataFromAll));
			}
		}
		private void RemoveMetadataInAllSpectra(object md) {
			var metadata = (md as ListBox).SelectedItem as MetaData;

			foreach (var spec in Spectra) {
				//check if metadata exists and remove or skip
				var mdlist = spec.metaData;
				try {
					var toRemove = mdlist.Single(it => it.Name == metadata.Name);
					mdlist.Remove(toRemove);
				} catch(Exception e) { Debug.WriteLine("Nothing to remove"); }
			}

			SelectedMetadata = null;
			CheckNewMetadataAndUpdateGUI();
		}
		private bool CanRemoveMetadataFromAll(object md) {
			return RemoveFromAllMetadataButtonEnabled;
		}

		private DelegateCommand loadProject;
		public DelegateCommand LoadProject {
			get {
				return loadProject ?? (loadProject = new DelegateCommand(
					it => MessageBox.Show("Coming soon.", "Info", MessageBoxButton.OK, MessageBoxImage.Information),
					it => true
				));
			}
		}

		private DelegateCommand saveProject;
		public DelegateCommand SaveProject {
			get {
				return saveProject ?? (saveProject = new DelegateCommand(
					it => MessageBox.Show("Coming soon.", "Info", MessageBoxButton.OK, MessageBoxImage.Information),
					it => true
				));
			}
		}

		private DelegateCommand uploadData;
		public DelegateCommand UploadData {
			get { return uploadData ?? (uploadData = new DelegateCommand(ConsolidateDataAndUpload, CanUploadData)); }
		}
		private void ConsolidateDataAndUpload(object obj) {
			foreach(var spec in ProjectData.Spectra) {
                spec.submitter = new Submitter {
                    id = submitterData.id,
                    emailAddress = submitterData.emailAddress,
                    firstName = submitterData.firstName,
                    lastName = submitterData.lastName,
                    institution = submitterData.institution
                };
				var msdt = new Tag { text = "uploaded with MS-Dial" };
				if (!spec.tags.Contains(msdt)) {
                    spec.tags.Add(msdt);
				}
			}

			var toExport = JArray.FromObject(ProjectData.Spectra).ToString();
            try {
                Debug.WriteLine(toExport);
                client.UploadSpectra(ProjectData.Spectra.ToList(), Token.Token);

                if (MessageBox.Show("Spectra submitted.\nWould you like to close the export window?", "Message", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes) == MessageBoxResult.Yes) {
                    RequestClose.Invoke(this, null);
                }
			} catch (WebException e) {
                MessageBox.Show("Sorry, there is an error exporting the selected spectra.\nPlease try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine(e.Message + "\n" + e.StackTrace);
            }
		} 
		private bool CanUploadData(object sender) {
            return ProjectData.Spectra.Count > 0;
		}

        //----------- TAGS --------------
        private DelegateCommand addTag;
        public DelegateCommand AddTag {
            get {
                return addTag ?? (addTag = new DelegateCommand(
                    obj => {
                        if (obj == null) return;
						Debug.WriteLine("object type: " + obj.GetType());
                        if (SelectedSpectrum.tags.ToList().FindAll(it => it.text == NewTagValue).Count == 0) {
                            SelectedSpectrum.tags.Add(new Tag { text = NewTagValue });
                        }
                        NewTagValue = string.Empty;
                    }, 
                    obj => !string.IsNullOrWhiteSpace(NewTagValue)
                ));
            }
        }

        private DelegateCommand removeTag;
        public DelegateCommand RemoveTag {
            get { return removeTag ?? (removeTag = new DelegateCommand(
                    it => {
                        SelectedSpectrum.tags.Remove(SelectedTag);
                        SelectedTag = null;
                    },
                    it => SelectedTag != null
                ));
            }
        }

        private DelegateCommand addTagToAll;
        public DelegateCommand AddTagToAll {
            get { return addTagToAll ?? (addTagToAll = new DelegateCommand(
					obj => {
						foreach (var spec in Spectra) {
							if (spec.tags.ToList().FindAll(it => it.text == NewTagValue).Count == 0) {
								Debug.WriteLine(string.Format("Adding tag {0} to spec {1}", NewTagValue, spec.splash));
								spec.tags.Add(new Tag { text = NewTagValue });
							}
						}
						NewTagValue = string.Empty;
                    },
                    obj => !string.IsNullOrWhiteSpace(NewTagValue)
                ));
            }
        }

        private DelegateCommand removeTagFromAll;
        public DelegateCommand RemoveTagFromAll {
            get { return removeTagFromAll ?? (removeTagFromAll = new DelegateCommand(
                  it => {
					var tmpTag = SelectedTag.text;  // need a copy or the command will fail to remove tags after the first spectrum
					foreach (var spec in Spectra) {
						  try {
							  var toRemove = spec.tags.Where(t => t.text.Equals(tmpTag));
							  if (toRemove.Count() > 0) {
								  Debug.WriteLine("removing tag {0} from spec {1}", SelectedTag.text, spec.splash);
								  spec.tags.Remove(toRemove.First());
							  }
						  } catch (Exception e) { Debug.WriteLine("Nothing to remove."); }  // run when a spectrum doesn't have the tag being deleted
					  }
					  tmpTag = null;
					  NewTagValue = string.Empty;
                  },
                  it => SelectedTag != null
              ));
            }
        }

        private DelegateCommand clearTags;
        public DelegateCommand ClearTags {
            get { return clearTags ?? (clearTags = new DelegateCommand(
                    it => SelectedSpectrum.tags.Clear(),
                    it => SelectedSpectrum.tags.Count > 0
                ));
            }
        }

		private DelegateCommand showCommonTagsWindow;
		public DelegateCommand ShowCommonTagsWindow {
			get { return showCommonTagsWindow??(showCommonTagsWindow = new DelegateCommand(HandleAddTagWindow, it => true)); }
		}
		private void HandleAddTagWindow(object obj) {
			var atview = new AddTagWindow();
			var atvm = new AddTagVM(client);
			atview.DataContext = atvm;
			atview.Owner = (Window)obj;
			atview.ShowDialog();

			if (atvm.SelectedTags.Count > 0) {
				foreach (var t in atvm.SelectedTags) {
					Debug.WriteLine("Adding tag " + t);
					SelectedSpectrum.tags.Add(t);
				}
			}
		}

#endregion

#region helper methods
		private void DisableAllButtons() {
			//project buttons
			UploadButtonEnabled = false;
            LoadButtonEnabled = false;
            SaveButtonEnabled = false;

			//metadata buttons
			AddMetadataButtonEnabled = false;
			ClearMetadataButtonEnabled = false;
			AddToAllMetadataButtonEnabled = false;
			UpdateRemoveButtons(null);

			//tags buttons
			AddTagButtonEnabled = false;
			RemoveTagButtonEnabled = false;
			ClearTagsButtonEnabled = false;
			AddAllTagButtonEnabled = false;
			RemoveAllTagButtonEnabled = false;
		}

		private void addEventHandlers() {
			//eventHandlers
			Spectra.CollectionChanged += UpdateUploadButton;
			SelectedSpectrum.metaData.CollectionChanged += SelectedSpectrum_metaData_CollectionChanged;
			SelectedSpectrum.tags.CollectionChanged += SelectedSpectrum_tags_CollectionChanged;

			ProjectData.Submitter.PropertyChanged += UpdateUploadStatus;
			ProjectData.PropertyChanged += UpdateUploadStatus;
		}

		private void UpdateRemoveButtons(object value) {
            //Debug.WriteLine("selectedMetadata: " + (value == null ? "null" : value.GetType().Name));
			if (value != null) {
				RemoveMetadataButtonEnabled = true;
				RemoveFromAllMetadataButtonEnabled = true;
			} else {
				RemoveMetadataButtonEnabled = false;
				RemoveFromAllMetadataButtonEnabled = false;
			}

			RemoveMetadataFromSelectedSpectrum.RaiseCanExecuteChanged();
			RemoveMetadataFromAllSpectra.RaiseCanExecuteChanged();

			CheckNewMetadataAndUpdateGUI();
		}

		private void UpdateMetaDataNameList() {
			foreach (var md in SelectedSpectrum.metaData) {
				if (!MetadataNameList.Contains(md.Name)) {
					MetadataNameList.Add(md.Name);
				}
			}
		}

		private void CheckNewMetadataAndUpdateGUI() {
            try {
                if (SelectedSpectrum.metaData.ToList().FindAll(mdn => mdn.Name == SelectedMetadataName).Count() > 0) {
                    AddMetadataButtonCaption = "Update";
                    AddAllMetadataButtonCaption = "Update all";
                } else {
                    AddMetadataButtonCaption = "Add";
                    AddAllMetadataButtonCaption = "Add to all";
                }
            } catch (Exception e) {
                Debug.WriteLine("exception Add\n" + e.StackTrace);
                AddMetadataButtonCaption = "Add";
            }

            if (!(string.IsNullOrWhiteSpace(SelectedMetadataName) || string.IsNullOrWhiteSpace(NewMetadataValue))) {
                AddMetadataButtonEnabled = true;
				AddToAllMetadataButtonEnabled = true;
			} else {
				AddMetadataButtonEnabled = false;
				AddToAllMetadataButtonEnabled = false;
            }

			AddMetadataToCurrentSpectrum.RaiseCanExecuteChanged();
			AddMetadataToAllSpectra.RaiseCanExecuteChanged();
		}

		private void CheckNewTagAndUpdateGUI() {
            if (!string.IsNullOrWhiteSpace(NewTagValue)) {
                AddTagButtonEnabled = true;
                AddAllTagButtonEnabled = true;
            } else {
                AddTagButtonEnabled = false;
                AddAllTagButtonEnabled = false;
            }
		}

		private void UpdateClearButtons() {
            if (SelectedSpectrum.metaData.Count > 0) {
                ClearMetadataButtonEnabled = true;
            } else {
                ClearMetadataButtonEnabled = false;
            }

            if (SelectedSpectrum.tags.Count > 0) {
                ClearTagsButtonEnabled = true;
            } else {
                ClearTagsButtonEnabled = false;
            }

			ClearSelectedSpectrumMetadata.RaiseCanExecuteChanged();
			ClearTags.RaiseCanExecuteChanged();
		}

		private void printButtonStatus(string name, bool status) {
			//Debug.WriteLine(name + " button is " + (status ? "enabled" : "disabled"));
		}

        private void UpdateSpectraMetaData(MetaData md) {
            if (md.Value.StartsWith("--")) {
                Debug.WriteLine("removing MD " + md);
                foreach (MonaSpectrum spec in Spectra) {
                    List<MetaData> newMD = spec.metaData.Except(new List<MetaData>() { md }, new MetadataNameComparer()).ToList<MetaData>();
                    spec.metaData.Clear();
                    foreach (MetaData m in newMD) { spec.metaData.Add(m); };
                }
            } else {
                foreach (MonaSpectrum spec in Spectra) {
                    if (!spec.metaData.Contains(md, new MetadataNameComparer())) {
                        Debug.WriteLine("adding new MD");
                        spec.metaData.Add(md);
                    } else {
                        Debug.WriteLine("updating existing MD");
                        spec.metaData.First(it => it.Name.Equals(md.Name)).Value = md.Value;
                    }
                }
            }
        }

        #endregion
    }
}
