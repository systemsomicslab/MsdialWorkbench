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
    public partial class SpotRelationTable : Window {
        public SpotRelationTableVM vm { get; set; }
        public SpotRelationTable(AlignmentSpotTableViewer tableViewer) {
            InitializeComponent();
            this.vm = new SpotRelationTableVM(tableViewer);
            this.DataContext = this.vm;
        }

        public void ChangeCharacter(AlignmentSpotTableViewer tableViewer) {
            this.vm = new SpotRelationTableVM(tableViewer);
            this.DataContext = this.vm;
        }

    }

    public class SpotRelationTableVM {
        public AlignmentSpotTableViewer TableViewer { get; set; }
        public AlignmentSpotTableViewerVM TableViewerVM { get; set; }
        public ObservableCollection<SpotRelations> Source { get; set; }

        public SpotRelationTableVM() { }
        public SpotRelationTableVM(AlignmentSpotTableViewer tableViewer) {
            TableViewer = tableViewer;
            TableViewerVM = tableViewer.AlignmentSpotTableViewerVM;
            Source = FilterAlignmentSpots(this.TableViewerVM.Source, this.TableViewerVM.SelectedData);
        }

        private ObservableCollection<SpotRelations> FilterAlignmentSpots(ObservableCollection<AlignmentSpotRow> rows, AlignmentSpotRow spot) {
            var spots = new ObservableCollection<SpotRelations>();
            var links = spot.AlignmentPropertyBean.PeakLinks;
            if (links == null || links.Count == 0) return spots;
            var gId = spot.AlignmentPropertyBean.PeakGroupID;

            foreach (var linkedPeakCharacter in links) {
                var character = linkedPeakCharacter.Character;
                var linkedPeak = rows[linkedPeakCharacter.LinkedPeakID];

                spots.Add(new SpotRelations(linkedPeak, character));
            }
            return spots;
        }

    }

    public class SpotRelations {
        public AlignmentSpotRow AlignmentSpotRow { get; set; }
        public PeakLinkFeatureEnum Character { get; set; }
        public SpotRelations(AlignmentSpotRow row, PeakLinkFeatureEnum c) {
            Character = c;
            AlignmentSpotRow = row;
        }
    }
}