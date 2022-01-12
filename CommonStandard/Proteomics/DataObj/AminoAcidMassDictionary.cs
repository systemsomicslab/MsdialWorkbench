using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public static class AminoAcidObjUtility {


        public static bool IsAAEqual(char letter1, char letter2) {
            if (letter1 == 'J' && (letter2 == 'J' || letter2 == 'L' || letter2 == 'I')) return true;
            if (letter2 == 'J' && (letter1 == 'J' || letter1 == 'L' || letter1 == 'I')) return true;
            if (letter1 == letter2) return true;
            return false;
        }

        public static bool IsAAEqual(string letter1, string letter2) {
            if (letter1 == "J" && (letter2 == "J" || letter2 == "L" || letter2 == "I")) return true;
            if (letter2 == "J" && (letter1 == "J" || letter1 == "L" || letter1 == "I")) return true;
            if (letter1 == letter2) return true;
            return false;
        }

        public static bool IsAAEqual(char letter1, string letter2) {
            if (letter1 == 'J' && (letter2 == "J" || letter2 == "L" || letter2 == "I")) return true;
            if (letter2 == "J" && (letter1 == 'J' || letter1 == 'L' || letter1 == 'I')) return true;
            if (letter1.ToString() == letter2) return true;
            return false;
        }

        public static List<char> AminoAcidLetters = new List<char>() {
            'A', 'R', 'N', 'D', 'C', 'E', 'Q', 'G', 'H', 'I', 'L', 'K', 'M', 'F', 'P', 'S', 'T', 'W', 'Y', 'V', 'U', 'O', 'J'
        };

        public static Dictionary<string, double> OneLetter2Mass = new Dictionary<string, double>
        {
            {"A", 71.037113805},
            {"R", 156.101111050},
            {"N", 114.042927470},
            {"D", 115.026943065},
            {"C", 103.009184505},
            {"E", 129.042593135},
            {"Q", 128.058577540},
            {"G", 57.021463735},
            {"H", 137.058911875},
            {"I", 113.084064015},
            {"L", 113.084064015},
            {"J", 113.084064015},
            {"K", 128.094963050},
            {"M", 131.040484645},
            {"F", 147.068413945},
            {"P", 97.052763875},
            {"S", 87.032028435},
            {"T", 101.047678505},
            {"W", 186.079312980},
            {"Y", 163.063328575},
            {"V", 99.068413945},
            {"U", 150.953633405},
            {"O", 237.147726925}
        };

        public static Dictionary<char, double> OneChar2Mass = new Dictionary<char, double>
        {
            {'A', 71.037113805},
            {'R', 156.101111050},
            {'N', 114.042927470},
            {'D', 115.026943065},
            {'C', 103.009184505},
            {'E', 129.042593135},
            {'Q', 128.058577540},
            {'G', 57.021463735},
            {'H', 137.058911875},
            {'I', 113.084064015},
            {'L', 113.084064015},
            {'J', 113.084064015},
            {'K', 128.094963050},
            {'M', 131.040484645},
            {'F', 147.068413945},
            {'P', 97.052763875},
            {'S', 87.032028435},
            {'T', 101.047678505},
            {'W', 186.079312980},
            {'Y', 163.063328575},
            {'V', 99.068413945},
            {'U', 150.953633405},
            {'O', 237.147726925}
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
            {"J","Xle"},
            {"K","Lys"},
            {"M","Met"},
            {"F","Phe"},
            {"P","Pro"},
            {"S","Ser"},
            {"T","Thr"},
            {"W","Trp"},
            {"Y","Tyr"},
            {"V","Val"},
            {"O","Pyl"},
            {"U","Sec"},
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
            {'J',"Xle"},
            {'K',"Lys"},
            {'M',"Met"},
            {'F',"Phe"},
            {'P',"Pro"},
            {'S',"Ser"},
            {'T',"Thr"},
            {'W',"Trp"},
            {'Y',"Tyr"},
            {'V',"Val"},
            {'O',"Pyl"},
            {'U',"Sec"}
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
            {"Xle","J"},
            {"Lys","K"},
            {"Met","M"},
            {"Phe","F"},
            {"Pro","P"},
            {"Ser","S"},
            {"Thr","T"},
            {"Trp","W"},
            {"Tyr","Y"},
            {"Val","V"},
            {"Pyl","O"},
            {"Sec","U"},
        };

        public static Dictionary<string, char> ThreeLetter2OneChar = new Dictionary<string, char>
        {
            {"Ala",'A'},
            {"Arg",'R'},
            {"Asn",'N'},
            {"Asp",'D'},
            {"Cys",'C'},
            {"Glu",'E'},
            {"Gln",'Q'},
            {"Gly",'G'},
            {"His",'H'},
            {"Ile",'I'},
            {"Leu",'L'},
            {"Xle",'J'},
            {"Lys",'K'},
            {"Met",'M'},
            {"Phe",'F'},
            {"Pro",'P'},
            {"Ser",'S'},
            {"Thr",'T'},
            {"Trp",'W'},
            {"Tyr",'Y'},
            {"Val",'V'},
            {"Pyl",'O'},
            {"Sec",'U'},
        };


        public static Dictionary<string, string> OneLetter2FormulaString = new Dictionary<string, string>
        {
            {"A","C3H7O2N"},
            {"R","C6H14O2N4"},
            {"N","C4H8O3N2"},
            {"D","C4H7O4N"},
            {"C","C3H7O2NS"},
            {"E","C5H9O4N"},
            {"Q","C5H10O3N2"},
            {"G","C2H5O2N"},
            {"H","C6H9O2N3"},
            {"I","C6H13O2N"},
            {"L","C6H13O2N"},
            {"J","C6H13O2N"},
            {"K","C6H14O2N2"},
            {"M","C5H11O2NS"},
            {"F","C9H11O2N"},
            {"P","C5H9O2N"},
            {"S","C3H7O3N"},
            {"T","C4H9O3N"},
            {"W","C11H12O2N2"},
            {"Y","C9H11O3N"},
            {"V","C5H11O2N"},
            {"O","C12H21N3O3"},
            {"U","C3H7NO2Se"},
        };

        public static Dictionary<char, string> OneChar2FormulaString = new Dictionary<char, string>
        {
            {'A',"C3H7O2N"},
            {'R',"C6H14O2N4"},
            {'N',"C4H8O3N2"},
            {'D',"C4H7O4N"},
            {'C',"C3H7O2NS"},
            {'E',"C5H9O4N"},
            {'Q',"C5H10O3N2"},
            {'G',"C2H5O2N"},
            {'H',"C6H9O2N3"},
            {'I',"C6H13O2N"},
            {'L',"C6H13O2N"},
            {'J',"C6H13O2N"},
            {'K',"C6H14O2N2"},
            {'M',"C5H11O2NS"},
            {'F',"C9H11O2N"},
            {'P',"C5H9O2N"},
            {'S',"C3H7O3N"},
            {'T',"C4H9O3N"},
            {'W',"C11H12O2N2"},
            {'Y',"C9H11O3N"},
            {'V',"C5H11O2N"},
            {'O',"C12H21N3O3"},
            {'U',"C3H7NO2Se"}
        };

        public static Dictionary<char, Formula> OneChar2Formula = new Dictionary<char, Formula>
        {
            {'A', FormulaStringParcer.Convert2FormulaObjV2("C3H7O2N")},
            {'R', FormulaStringParcer.Convert2FormulaObjV2("C6H14O2N4")},
            {'N', FormulaStringParcer.Convert2FormulaObjV2("C4H8O3N2")},
            {'D', FormulaStringParcer.Convert2FormulaObjV2("C4H7O4N")},
            {'C', FormulaStringParcer.Convert2FormulaObjV2("C3H7O2NS")},
            {'E', FormulaStringParcer.Convert2FormulaObjV2("C5H9O4N")},
            {'Q', FormulaStringParcer.Convert2FormulaObjV2("C5H10O3N2")},
            {'G', FormulaStringParcer.Convert2FormulaObjV2("C2H5O2N")},
            {'H', FormulaStringParcer.Convert2FormulaObjV2("C6H9O2N3")},
            {'I', FormulaStringParcer.Convert2FormulaObjV2("C6H13O2N")},
            {'L', FormulaStringParcer.Convert2FormulaObjV2("C6H13O2N")},
            {'J', FormulaStringParcer.Convert2FormulaObjV2("C6H13O2N")},
            {'K', FormulaStringParcer.Convert2FormulaObjV2("C6H14O2N2")},
            {'M', FormulaStringParcer.Convert2FormulaObjV2("C5H11O2NS")},
            {'F', FormulaStringParcer.Convert2FormulaObjV2("C9H11O2N")},
            {'P', FormulaStringParcer.Convert2FormulaObjV2("C5H9O2N")},
            {'S', FormulaStringParcer.Convert2FormulaObjV2("C3H7O3N")},
            {'T', FormulaStringParcer.Convert2FormulaObjV2("C4H9O3N")},
            {'W', FormulaStringParcer.Convert2FormulaObjV2("C11H12O2N2")},
            {'Y', FormulaStringParcer.Convert2FormulaObjV2("C9H11O3N")},
            {'V', FormulaStringParcer.Convert2FormulaObjV2("C5H11O2N")},
            {'O', FormulaStringParcer.Convert2FormulaObjV2("C12H21N3O3")},
            {'U', FormulaStringParcer.Convert2FormulaObjV2("C3H7NO2Se")}
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
            {"J",6},
            {"K",6},
            {"M",5},
            {"F",9},
            {"P",5},
            {"S",3},
            {"T",4},
            {"W",11},
            {"Y",9},
            {"V",5},
            {"O",12},
            {"U",3}
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
            {'J',6},
            {'K',6},
            {'M',5},
            {'F',9},
            {'P',5},
            {'S',3},
            {'T',4},
            {'W',11},
            {'Y',9},
            {'V',5},
            {'O',12},
            {'U',3},
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
            {"J",1},
            {"K",2},
            {"M",1},
            {"F",1},
            {"P",1},
            {"S",1},
            {"T",1},
            {"W",2},
            {"Y",1},
            {"V",1},
            {"O",3},
            {"U",1}

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
            {'J',1},
            {'K',2},
            {'M',1},
            {'F',1},
            {'P',1},
            {'S',1},
            {'T',1},
            {'W',2},
            {'Y',1},
            {'V',1},
            {'O',3},
            {'U',1}
        };

        public static Dictionary<string, int> OneLetter2HydrogenNuber = new Dictionary<string, int>
        {
            {"A",7},
            {"R",14},
            {"N",8},
            {"D",7},
            {"C",7},
            {"E",9},
            {"Q",10},
            {"G",5},
            {"H",9},
            {"I",13},
            {"L",13},
            {"J",13},
            {"K",14},
            {"M",11},
            {"F",11},
            {"P",9},
            {"S",7},
            {"T",9},
            {"W",12},
            {"Y",11},
            {"V",11},
            {"O",21},
            {"U",7}
        };

        public static Dictionary<char, int> OneChar2HydrogenNuber = new Dictionary<char, int>
        {
            {'A',7},
            {'R',14},
            {'N',8},
            {'D',7},
            {'C',7},
            {'E',9},
            {'Q',10},
            {'G',5},
            {'H',9},
            {'I',13},
            {'L',13},
            {'J',13},
            {'K',14},
            {'M',11},
            {'F',11},
            {'P',9},
            {'S',7},
            {'T',9},
            {'W',12},
            {'Y',11},
            {'V',11},
            {'O',21},
            {'U',7}
        };

        public static Dictionary<string, int> OneLetter2OxygenNuber = new Dictionary<string, int>
        {
            {"A",2},
            {"R",2},
            {"N",3},
            {"D",4},
            {"C",2},
            {"E",4},
            {"Q",3},
            {"G",2},
            {"H",2},
            {"I",2},
            {"L",2},
            {"J",2},
            {"K",2},
            {"M",2},
            {"F",2},
            {"P",2},
            {"S",3},
            {"T",3},
            {"W",2},
            {"Y",3},
            {"V",2},
            {"O",3},
            {"U",2}
        };

        public static Dictionary<char, int> OneChar2OxygenNuber = new Dictionary<char, int>
        {
            {'A',2},
            {'R',2},
            {'N',3},
            {'D',4},
            {'C',2},
            {'E',4},
            {'Q',3},
            {'G',2},
            {'H',2},
            {'I',2},
            {'L',2},
            {'J',2},
            {'K',2},
            {'M',2},
            {'F',2},
            {'P',2},
            {'S',3},
            {'T',3},
            {'W',2},
            {'Y',3},
            {'V',2},
            {'O',3},
            {'U',2}
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
            {"J",0},
            {"K",0},
            {"M",1},
            {"F",0},
            {"P",0},
            {"S",0},
            {"T",0},
            {"W",0},
            {"Y",0},
            {"V",0},
            {"O",0},
            {"U",0}
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
            {'J',0},
            {'K',0},
            {'M',1},
            {'F',0},
            {'P',0},
            {'S',0},
            {'T',0},
            {'W',0},
            {'Y',0},
            {'V',0},
            {'O',0},
            {'U',0}
        };
    }
}
