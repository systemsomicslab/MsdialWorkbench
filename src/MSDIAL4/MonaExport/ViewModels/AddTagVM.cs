using edu.ucdavis.fiehnlab.MonaRestApi;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using Rfx.Riken.OsakaUniv;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace edu.ucdavis.fiehnlab.MonaExport.ViewModels {
	public class AddTagVM : ViewModelBase {
		private ObservableCollection<Tag> tagList;
		private ObservableCollection<Tag> selectedTags;
		//private bool closeTrigger;

		public ObservableCollection<Tag> TagList {
			get { return tagList; }
			set {
				tagList = value;
				OnPropertyChanged("TagList");
				Debug.WriteLine("tags changed");
			}
		}

		public ObservableCollection<Tag> SelectedTags {
			get { return selectedTags ?? (selectedTags = new ObservableCollection<Tag>()); }
			set {
				if(value == selectedTags) { return; }
				selectedTags = value;
				OnPropertyChanged("SelectedTags");
			}
		}

		//public bool CloseTrigger {
		//	get { return closeTrigger; }
		//	set {
		//		closeTrigger = value;
		//		OnPropertyChanged("CloseTrigger");
		//	}
		//}

		public AddTagVM() { }
		public AddTagVM(IMonaRestClient client) {
			var newList = new ObservableCollection<Tag>();
			client.GetCommonTags().ForEach(it => newList.Add(it));
			TagList = newList;
		}

		private DelegateCommand updateTagSelection;
		public DelegateCommand UpdateTagSelection {
			get { return updateTagSelection ?? (updateTagSelection = new DelegateCommand(
					it => {
						var container = (ListBox)it;
						var newlist = new ObservableCollection<Tag>();
						foreach(var t in container.SelectedItems) {
							newlist.Add((Tag)t);
						}
						SelectedTags = newlist;
					},
					it => true
				));
			}
		}


		private DelegateCommand exitCommand;
		public DelegateCommand ExitCommand {
			get {
				return exitCommand ?? (exitCommand = new DelegateCommand(obj => {
					((Window)obj).Close();
				}, obj => true));
			}
		}

		private DelegateCommand cancelCommand;
		public DelegateCommand CancelCommand {
			get {
				return cancelCommand ?? (cancelCommand = new DelegateCommand(obj => {
					SelectedTags.Clear();
					((Window)obj).Close();
				}, obj => true));
			}
		}
	}
}
