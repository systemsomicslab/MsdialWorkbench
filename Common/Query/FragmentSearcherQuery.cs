using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;

namespace Riken.Metabolomics.Common.Query {

    public enum SearchType { ProductIon, NeutralLoss }

    [MessagePackObject]
    public class FragmentSearcherQuery {

        private double mass;
        private double relativeIntensity;
        private SearchType searchType;
        private double massTolerance;

        [Key(0)]
        public double Mass {
            get {
                return mass;
            }

            set {
                mass = value;
            }
        }

        [Key(1)]
        public double RelativeIntensity {
            get {
                return relativeIntensity;
            }

            set {
                relativeIntensity = value;
            }
        }

        [Key(2)]
        public SearchType SearchType {
            get {
                return searchType;
            }

            set {
                searchType = value;
            }
        }

        [Key(3)]
        public double MassTolerance {
            get {
                return massTolerance;
            }

            set {
                massTolerance = value;
            }
        }
    }
}
