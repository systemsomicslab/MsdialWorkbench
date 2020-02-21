using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class MgfRecord
    {
        private string title;
        private float pepmass;
        private int charge;
        private int mslevel;
        private string source_instrument;
        private string filename;
        private string seq;
        private IonMode ionMode;
        private string organism;
        private string name;
        private string pi;
        private string datacollector;
        private string smiles;
        private string inchi;
        private string inchiaux;
        private string pubmed;
        private string submituser;
        private string libraryQuality;
        private string spectrumID;
        private int scan;
        private float rt;
        private AdductIon adduct;
        private List<Peak> spectrum;



        public MgfRecord() {
            title = string.Empty;
            pepmass = 0.0F;
            rt = 0.0F;
            charge = 1;
            adduct = new AdductIon();
            spectrum = new List<Peak>();
        }

        #region properties
        public string Title {
            get { return title; }
            set { title = value; }
        }

        public float Pepmass {
            get { return pepmass; }
            set { pepmass = value; }
        }

        public float Rt {
            get { return rt; }
            set { rt = value; }
        }

        public List<Peak> Spectrum {
            get { return spectrum; }
            set { spectrum = value; }
        }

        public AdductIon Adduct {
            get { return adduct; }
            set { adduct = value; }
        }

        public int Charge {
            get { return charge; }
            set { charge = value; }
        }

        public int Mslevel {
            get { return mslevel; }
            set { mslevel = value; }
        }

        public string Source_instrument {
            get { return source_instrument; }
            set { source_instrument = value; }
        }

        public string Filename {
            get { return filename; }
            set { filename = value; }
        }

        public string Seq {
            get { return seq; }
            set { seq = value; }
        }

        public IonMode IonMode {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public string Organism {
            get { return organism; }
            set { organism = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Pi {
            get { return pi; }
            set { pi = value; }
        }

        public string Datacollector {
            get { return datacollector; }
            set { datacollector = value; }
        }

        public string Smiles {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Inchi {
            get { return inchi; }
            set { inchi = value; }
        }

        public string Inchiaux {
            get { return inchiaux; }
            set { inchiaux = value; }
        }

        public string Pubmed {
            get { return pubmed; }
            set { pubmed = value; }
        }

        public string Submituser {
            get { return submituser; }
            set { submituser = value; }
        }

        public string LibraryQuality {
            get { return libraryQuality; }
            set { libraryQuality = value; }
        }

        public string SpectrumID {
            get { return spectrumID; }
            set { spectrumID = value; }
        }

        public int Scan {
            get { return scan; }
            set { scan = value; }
        }
        #endregion
    }
}
