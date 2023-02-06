using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv.TableViewer
{
    /// <summary>
    /// IdentifiedCompoundGroup.xaml の相互作用ロジック
    /// </summary>
    public partial class IdentifiedCompoundGroup : Window
    {
        public IdentifiedCompoundGroupVM vm { get; set; }
        public IdentifiedCompoundGroup(AlignmentSpotTableViewer tableViewer, List<MspFormatCompoundInformationBean> msps) {
            InitializeComponent();
            this.vm = new IdentifiedCompoundGroupVM(tableViewer, msps);
            this.DataContext = this.vm;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            this.vm.TableViewerVM.Settings.ShortInchiKeyFilter = "";
        }
    }

    public class IdentifiedCompoundGroupVM
    {
        public AlignmentSpotTableViewer TableViewer { get; set; }
        public AlignmentSpotTableViewerVM TableViewerVM { get; set; }
        public List<CompoundGroup> Source { get; set; }
        public FilteredTable FilteredTable { get; set; }
        public FilterSettings Settings { get; set; }

        private CompoundGroup _group;
        public CompoundGroup SelectedData { get { return _group; } set { if (_group == value || value == null) return;
                this.TableViewer.DataGrid_RawData.CommitEdit();
                this.TableViewer.DataGrid_RawData.CancelEdit();
                _group = value; TableViewerVM.Settings.ShortInchiKeyFilter = value.InchiKey; } }
        private string _metabolite;
        public string MetaboliteName { get { return _metabolite; } set { if (_metabolite == value) return;  _metabolite = value; this.Settings.MetaboliteNameFilter = value; } }

        public IdentifiedCompoundGroupVM() { }
        public IdentifiedCompoundGroupVM(AlignmentSpotTableViewer tableViewer, List<MspFormatCompoundInformationBean> msps) {
            TableViewer = tableViewer;
            TableViewerVM = tableViewer.AlignmentSpotTableViewerVM;
            TableViewerVM.InitializeFilter();
            Source = GetCompoundGroups(this.TableViewerVM.Source, msps);
            this.FilteredTable = new TableViewer.FilteredTable(Source);
            this.Settings = new TableViewer.FilterSettings(this.FilteredTable.View, FilterSettings.ViewerType.Compound);
            this.FilteredTable.View.Filter = Settings.SpotFilter;

        }

        private List<CompoundGroup> GetCompoundGroups(ObservableCollection<AlignmentSpotRow> rows, List<MspFormatCompoundInformationBean> msps) {
            var inchiKeys = new List<string>();
            var dic = new Dictionary<string, CompoundGroup>();
            foreach (var r in rows) {
                if (r.ShortInchiKey == null || r.ShortInchiKey == "") continue;
                if (r.AlignmentPropertyBean.LibraryID < 0) continue; 
                if (inchiKeys.Contains(r.ShortInchiKey)) {
                    // by identification Rank 
                    /*
                                        if (r.AlignmentPropertyBean.IdentificationRank > dic[r.ShortInchiKey].IdentificationRank) {
                                            dic[r.ShortInchiKey].IdentificationRank = r.AlignmentPropertyBean.IdentificationRank;
                                            dic[r.ShortInchiKey].MetaboliteName = msps[r.AlignmentPropertyBean.LibraryID].Name;
                                        }
                      */
                    //
                    if (dic[r.ShortInchiKey].IdentificationRank == "Annotated" && !r.AlignmentPropertyBean.MetaboliteName.Contains("w/o ")) {
                        dic[r.ShortInchiKey].MetaboliteName = msps[r.AlignmentPropertyBean.LibraryID].Name;
                        dic[r.ShortInchiKey].IdentificationRank = "Identified";
                    }
                    dic[r.ShortInchiKey].NameList.Add(msps[r.AlignmentPropertyBean.LibraryID].Name);
                }
                else {
                    var rank = r.AlignmentPropertyBean.MetaboliteName.Contains("w/o ") ? "Annotated" : "Identified";
                    var group = new CompoundGroup() {
                        ID = inchiKeys.Count,
                        MetaboliteName = msps[r.AlignmentPropertyBean.LibraryID].Name,
                        InchiKey = r.ShortInchiKey,
                        IdentificationRank = rank
                    };
                    group.NameList.Add(group.MetaboliteName);
                    dic.Add(r.ShortInchiKey, group);
                    inchiKeys.Add(r.ShortInchiKey);
                }
            }

            return FinalizeGrouping(dic);
        }

        private List<CompoundGroup> FinalizeGrouping(Dictionary<string, CompoundGroup> dic) {
            var compoundGroupList = new List<CompoundGroup>();
            foreach (var i in dic) {
                var comp = i.Value;
                comp.MakeNameString();
                compoundGroupList.Add(comp);
            }
            return compoundGroupList;
        }

    }

    public class CompoundGroup
    {
        public int ID { get; set; }
        public int Counter { get; set; }
   //     public int IdentificationRank { get; set; }
        public string IdentificationRank { get; set; }
        public string InchiKey { get; set; }
        public string MetaboliteName { get; set; }
        public string Names { get; set; }
        public List<string> NameList { get; set; }
        public CompoundGroup() {
            ID = -1;
  //          IdentificationRank = -1;
            InchiKey = "";
            MetaboliteName = "";
            NameList = new List<string>();
            Names = "";
            Counter = 0;
        }

        public void MakeNameString() {
            if (NameList.Count > 1) {
                var s = "";
                foreach (var arr in NameList.OrderBy(x => x).Distinct().ToArray()) {
                    s = s + arr + "; ";
                }
                Names = s;
            }
            else if (NameList.Count == 1) {
                Names = NameList[0];
            }
            Counter = NameList.Count;
        }
    }

}
