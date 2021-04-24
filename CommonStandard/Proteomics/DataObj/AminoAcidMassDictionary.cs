using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public static class AminoAcidDictionary {
        public static Dictionary<string, double> OneLetter2Mass = new Dictionary<string, double>
        {
            {"A", 71.03711},
            {"R", 156.10111},
            {"N", 114.04293},
            {"D", 115.02694},
            {"C", 103.00919},
            {"E", 129.04259},
            {"Q", 128.05858},
            {"G", 57.02146},
            {"H", 137.05891},
            {"I", 113.08406},
            {"L", 113.08406},
            {"K", 128.09496 },
            {"M", 131.04049},
            {"F", 147.06841},
            {"P", 97.05276},
            {"S", 87.03203},
            {"T", 101.04768},
            {"W", 186.07931},
            {"Y", 163.06333},
            {"V", 99.06841}
        };

        public static Dictionary<char, double> OneChar2Mass = new Dictionary<char, double>
        {
            {'A', 71.03711},
            {'R', 156.10111},
            {'N', 114.04293},
            {'D', 115.02694},
            {'C', 103.00919},
            {'E', 129.04259},
            {'Q', 128.05858},
            {'G', 57.02146},
            {'H', 137.05891},
            {'I', 113.08406},
            {'L', 113.08406},
            {'K', 128.09496 },
            {'M', 131.04049},
            {'F', 147.06841},
            {'P', 97.05276},
            {'S', 87.03203},
            {'T', 101.04768},
            {'W', 186.07931},
            {'Y', 163.06333},
            {'V', 99.06841}
        };

        public static Dictionary<string, double> ThreeLetter2Mass = new Dictionary<string, double>
        {
            {"Ala",71.03711},
            {"Arg",156.10111},
            {"Asn",114.04293},
            {"Asp",115.02694},
            {"Cys",103.00919},
            {"Glu",129.04259},
            {"Gln",128.05858},
            {"Gly",57.02146},
            {"His",137.05891},
            {"Ile",113.08406},
            {"Leu",113.08406},
            {"Lys",128.09496},
            {"Met",131.04049},
            {"Phe",147.06841},
            {"Pro",97.05276},
            {"Ser",87.03203},
            {"Thr",101.04768},
            {"Trp",186.07931},
            {"Tyr",163.06333},
            {"Val",99.06841}
        };

        public static Dictionary<string, string> OneLetter2ThreeLetter = new Dictionary<string, string>
        {
            {"A","Ala"},
            {"R","Arg"},
            {"N","Asn"},
            {"D","Asp"},
            {"C","Cys"},
            {"E","Glu"},
            {"Q","Gln"},
            {"G","Gly"},
            {"H","His"},
            {"I","Ile"},
            {"L","Leu"},
            {"K","Lys"},
            {"M","Met"},
            {"F","Phe"},
            {"P","Pro"},
            {"S","Ser"},
            {"T","Thr"},
            {"W","Trp"},
            {"Y","Tyr"},
            {"V","Val"}
        };

        public static Dictionary<char, string> OneChar2ThreeLetter = new Dictionary<char, string>
       {
            {'A',"Ala"},
            {'R',"Arg"},
            {'N',"Asn"},
            {'D',"Asp"},
            {'C',"Cys"},
            {'E',"Glu"},
            {'Q',"Gln"},
            {'G',"Gly"},
            {'H',"His"},
            {'I',"Ile"},
            {'L',"Leu"},
            {'K',"Lys"},
            {'M',"Met"},
            {'F',"Phe"},
            {'P',"Pro"},
            {'S',"Ser"},
            {'T',"Thr"},
            {'W',"Trp"},
            {'Y',"Tyr"},
            {'V',"Val"}
        };

        public static Dictionary<string, string> ThreeLetter2OneLetter = new Dictionary<string, string>
        {
            {"Ala","A"},
            {"Arg","R"},
            {"Asn","N"},
            {"Asp","D"},
            {"Cys","C"},
            {"Glu","E"},
            {"Gln","Q"},
            {"Gly","G"},
            {"His","H"},
            {"Ile","I"},
            {"Leu","L"},
            {"Lys","K"},
            {"Met","M"},
            {"Phe","F"},
            {"Pro","P"},
            {"Ser","S"},
            {"Thr","T"},
            {"Trp","W"},
            {"Tyr","Y"},
            {"Val","V"}
        };


        public static Dictionary<string, string> OneLetter2FormulaString = new Dictionary<string, string>
        {
            {"A","C3H5ON"},
            {"R","C6H12ON4"},
            {"N","C4H6O2N2"},
            {"D","C4H5O3N"},
            {"C","C3H5ONS"},
            {"E","C5H7O3N"},
            {"Q","C5H8O2N2"},
            {"G","C2H3ON"},
            {"H","C6H7ON3"},
            {"I","C6H11ON"},
            {"L","C6H11ON"},
            {"K","C6H12ON2"},
            {"M","C5H9ONS"},
            {"F","C9H9ON"},
            {"P","C5H7ON"},
            {"S","C3H5O2N"},
            {"T","C4H7O2N"},
            {"W","C11H10ON2"},
            {"Y","C9H9O2N"},
            {"V","C5H9ON"}
        };

        public static Dictionary<char, string> OneChar2FormulaString = new Dictionary<char, string>
        {
            {'A',"C3H5ON"},
            {'R',"C6H12ON4"},
            {'N',"C4H6O2N2"},
            {'D',"C4H5O3N"},
            {'C',"C3H5ONS"},
            {'E',"C5H7O3N"},
            {'Q',"C5H8O2N2"},
            {'G',"C2H3ON"},
            {'H',"C6H7ON3"},
            {'I',"C6H11ON"},
            {'L',"C6H11ON"},
            {'K',"C6H12ON2"},
            {'M',"C5H9ONS"},
            {'F',"C9H9ON"},
            {'P',"C5H7ON"},
            {'S',"C3H5O2N"},
            {'T',"C4H7O2N"},
            {'W',"C11H10ON2"},
            {'Y',"C9H9O2N"},
            {'V',"C5H9ON"}
        };

        public static Dictionary<char, Formula> OneChar2Formula = new Dictionary<char, Formula>
        {
            {'A', new Formula("C3H5ON")},
            {'R', new Formula("C6H12ON4")},
            {'N', new Formula("C4H6O2N2")},
            {'D', new Formula("C4H5O3N")},
            {'C', new Formula("C3H5ONS")},
            {'E', new Formula("C5H7O3N")},
            {'Q', new Formula("C5H8O2N2")},
            {'G', new Formula("C2H3ON")},
            {'H', new Formula("C6H7ON3")},
            {'I', new Formula("C6H11ON")},
            {'L', new Formula("C6H11ON")},
            {'K', new Formula("C6H12ON2")},
            {'M', new Formula("C5H9ONS")},
            {'F', new Formula("C9H9ON")},
            {'P', new Formula("C5H7ON")},
            {'S', new Formula("C3H5O2N")},
            {'T', new Formula("C4H7O2N")},
            {'W', new Formula("C11H10ON2")},
            {'Y', new Formula("C9H9O2N")},
            {'V', new Formula("C5H9ON")}
        };


        public static Dictionary<string, int> OneLetter2CarbonNuber = new Dictionary<string, int>
        {
            {"A",3},
            {"R",6},
            {"N",4},
            {"D",4},
            {"C",3},
            {"E",5},
            {"Q",5},
            {"G",2},
            {"H",6},
            {"I",6},
            {"L",6},
            {"K",6},
            {"M",5},
            {"F",9},
            {"P",5},
            {"S",3},
            {"T",4},
            {"W",11},
            {"Y",9},
            {"V",5}
        };

        public static Dictionary<char, int> OneChar2CarbonNuber = new Dictionary<char, int>
        {
            {'A',3},
            {'R',6},
            {'N',4},
            {'D',4},
            {'C',3},
            {'E',5},
            {'Q',5},
            {'G',2},
            {'H',6},
            {'I',6},
            {'L',6},
            {'K',6},
            {'M',5},
            {'F',9},
            {'P',5},
            {'S',3},
            {'T',4},
            {'W',11},
            {'Y',9},
            {'V',5}
        };

        public static Dictionary<string, int> OneLetter2NitrogenNuber = new Dictionary<string, int>
        {
            {"A",1},
            {"R",4},
            {"N",2},
            {"D",1},
            {"C",1},
            {"E",1},
            {"Q",2},
            {"G",1},
            {"H",3},
            {"I",1},
            {"L",1},
            {"K",2},
            {"M",1},
            {"F",1},
            {"P",1},
            {"S",1},
            {"T",1},
            {"W",2},
            {"Y",1},
            {"V",1}

        };

        public static Dictionary<char, int> OneChar2NitrogenNuber = new Dictionary<char, int>
        {
            {'A',1},
            {'R',4},
            {'N',2},
            {'D',1},
            {'C',1},
            {'E',1},
            {'Q',2},
            {'G',1},
            {'H',3},
            {'I',1},
            {'L',1},
            {'K',2},
            {'M',1},
            {'F',1},
            {'P',1},
            {'S',1},
            {'T',1},
            {'W',2},
            {'Y',1},
            {'V',1}
        };

        public static Dictionary<string, int> OneLetter2HydrogenNuber = new Dictionary<string, int>
        {
            {"A",5},
            {"R",12},
            {"N",6},
            {"D",5},
            {"C",5},
            {"E",7},
            {"Q",8},
            {"G",3},
            {"H",7},
            {"I",11},
            {"L",11},
            {"K",12},
            {"M",9},
            {"F",9},
            {"P",7},
            {"S",5},
            {"T",7},
            {"W",10},
            {"Y",9},
            {"V",9}
        };

        public static Dictionary<char, int> OneChar2HydrogenNuber = new Dictionary<char, int>
        {
            {'A',5},
            {'R',12},
            {'N',6},
            {'D',5},
            {'C',5},
            {'E',7},
            {'Q',8},
            {'G',3},
            {'H',7},
            {'I',11},
            {'L',11},
            {'K',12},
            {'M',9},
            {'F',9},
            {'P',7},
            {'S',5},
            {'T',7},
            {'W',10},
            {'Y',9},
            {'V',9}
        };

        public static Dictionary<string, int> OneLetter2OxygenNuber = new Dictionary<string, int>
        {
            {"A",1},
            {"R",1},
            {"N",2},
            {"D",3},
            {"C",1},
            {"E",3},
            {"Q",2},
            {"G",1},
            {"H",1},
            {"I",1},
            {"L",1},
            {"K",1},
            {"M",1},
            {"F",1},
            {"P",1},
            {"S",2},
            {"T",2},
            {"W",1},
            {"Y",2},
            {"V",1}
        };

        public static Dictionary<char, int> OneChar2OxygenNuber = new Dictionary<char, int>
        {
            {'A',1},
            {'R',1},
            {'N',2},
            {'D',3},
            {'C',1},
            {'E',3},
            {'Q',2},
            {'G',1},
            {'H',1},
            {'I',1},
            {'L',1},
            {'K',1},
            {'M',1},
            {'F',1},
            {'P',1},
            {'S',2},
            {'T',2},
            {'W',1},
            {'Y',2},
            {'V',1}
        };

        public static Dictionary<string, int> OneLetter2SulfurNuber = new Dictionary<string, int>
        {
            {"A",0},
            {"R",0},
            {"N",0},
            {"D",0},
            {"C",1},
            {"E",0},
            {"Q",0},
            {"G",0},
            {"H",0},
            {"I",0},
            {"L",0},
            {"K",0},
            {"M",1},
            {"F",0},
            {"P",0},
            {"S",0},
            {"T",0},
            {"W",0},
            {"Y",0},
            {"V",0}
        };

        public static Dictionary<char, int> OneChar2SulfurNuber = new Dictionary<char, int>
        {
            {'A',0},
            {'R',0},
            {'N',0},
            {'D',0},
            {'C',1},
            {'E',0},
            {'Q',0},
            {'G',0},
            {'H',0},
            {'I',0},
            {'L',0},
            {'K',0},
            {'M',1},
            {'F',0},
            {'P',0},
            {'S',0},
            {'T',0},
            {'W',0},
            {'Y',0},
            {'V',0}
        };


      
        public static Dictionary<string, string> ThreeLetter2FormulaString = new Dictionary<string, string>
        {
            {"Ala","C3H5ON"},
            {"Arg","C6H12ON4"},
            {"Asn","C4H6O2N2"},
            {"Asp","C4H5O3N"},
            {"Cys","C3H5ONS"},
            {"Glu","C5H7O3N"},
            {"Gln","C5H8O2N2"},
            {"Gly","C2H3ON"},
            {"His","C6H7ON3"},
            {"Ile","C6H11ON"},
            {"Leu","C6H11ON"},
            {"Lys","C6H12ON2"},
            {"Met","C5H9ONS"},
            {"Phe","C9H9ON"},
            {"Pro","C5H7ON"},
            {"Ser","C3H5O2N"},
            {"Thr","C4H7O2N"},
            {"Trp","C11H10ON2"},
            {"Tyr","C9H9O2N"},
            {"Val","C5H9ON"}
        };
    }
}
