using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public class AdductIon
    {
        private string adductIonName;
        private string ionMode;
        private string adductSurfix;
        private double adductIonMass;

        public AdductIon()
        {
            adductIonName = string.Empty;
            ionMode = string.Empty;
            adductSurfix = string.Empty;
            adductIonMass = 0.0;
        }

        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        public string IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public string AdductSurfix
        {
            get { return adductSurfix; }
            set { adductSurfix = value; }
        }

        public double AdductIonMass
        {
            get { return adductIonMass; }
            set { adductIonMass = value; }
        }



    }
}
