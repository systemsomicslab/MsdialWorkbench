using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.Annotation
{
    public class LipoqualityAnnotation
    {
        private int spotID;
        private float mz;
        private float rt;
        private float averagedIntensity;
        private float standardDeviation;
        private string name;
        private string lipidClass;
        private string lipidSuperClass;

        private string totalChain;
        private string sn1AcylChain;
        private string sn2AcylChain;
        private string sn3AcylChain;
        private string sn4AcylChain;
        private AdductIon adduct;
        private IonMode ionMode;
        private string formula;
        private string smiles;
        private string inchikey;
        private List<double> intensities;

        public LipoqualityAnnotation()
        {
            mz = 0.0F;
            rt = 0.0F;
            lipidClass = string.Empty;
            totalChain = string.Empty;
            sn1AcylChain = string.Empty;
            sn2AcylChain = string.Empty;
            sn3AcylChain = string.Empty;
            sn4AcylChain = string.Empty;
            smiles = string.Empty;
            intensities = new List<double>();
        }

        public float Mz
        {
            get { return mz; }
            set { mz = value; }
        }

        public float Rt
        {
            get { return rt; }
            set { rt = value; }
        }

        public float AveragedIntensity
        {
            get { return averagedIntensity; }
            set { averagedIntensity = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string LipidClass
        {
            get { return lipidClass; }
            set { lipidClass = value; }
        }

        public string LipidSuperClass
        {
            get { return lipidSuperClass; }
            set { lipidSuperClass = value; }
        }

        public string TotalChain
        {
            get { return totalChain; }
            set { totalChain = value; }
        }

        public string Sn1AcylChain
        {
            get { return sn1AcylChain; }
            set { sn1AcylChain = value; }
        }

        public string Sn2AcylChain
        {
            get { return sn2AcylChain; }
            set { sn2AcylChain = value; }
        }

        public string Sn3AcylChain
        {
            get { return sn3AcylChain; }
            set { sn3AcylChain = value; }
        }

        public string Sn4AcylChain
        {
            get { return sn4AcylChain; }
            set { sn4AcylChain = value; }
        }

        public AdductIon Adduct
        {
            get { return adduct; }
            set { adduct = value; }
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Inchikey
        {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        public float StandardDeviation
        {
            get {
                return standardDeviation;
            }

            set {
                standardDeviation = value;
            }
        }

        public int SpotID
        {
            get {
                return spotID;
            }

            set {
                spotID = value;
            }
        }

        public string Formula
        {
            get {
                return formula;
            }

            set {
                formula = value;
            }
        }

        public List<double> Intensities {
            get {
                return intensities;
            }

            set {
                intensities = value;
            }
        }
    }
}
