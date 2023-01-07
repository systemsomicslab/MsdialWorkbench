using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CompMs.Common.Components;

namespace CompMs.Common.Lipidomics {
    public sealed class LipoqualityRest {

        private LipoqualityRest() { }

        public static void GoToLQDB(MoleculeMsReference query, string lipidName, out string error) {
            error = string.Empty;
            var lipidinfo = LipoqualityDatabaseManagerUtility.ConvertMsdialLipidnameToLipidAnnotation(query, lipidName);
            if (lipidinfo == null) {
                error = "Type 1 error: Lipid name format invalid";
                //MessageBox.Show("Type 1 error: Lipid name format invalid", "Error", MessageBoxButton.OK);
                return;
            }
            
            var url = getLqUrl(lipidinfo);
            if (url == string.Empty) {
                error = "Type 2 error: Lipid name format invalid";
                //MessageBox.Show("Type 2 error: Lipid name format invalid", "Error", MessageBoxButton.OK);
                return;
            }

            try {
                System.Diagnostics.Process.Start(url);
            } catch (Win32Exception ex) {

            }
        }

        private static string getLqUrl(LipoqualityAnnotation lipidinfo) {
            switch (lipidinfo.LipidClass) {

                //Glycerolipid
                case "MAG":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "MAG " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "DAG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DAG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DAG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "TAG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty && lipidinfo.Sn3AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "TAG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "/" + lipidinfo.Sn3AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty || lipidinfo.Sn3AcylChain == string.Empty) 
                        && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "TAG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Lyso phospholipid
                case "LPC":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPC " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LPE":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPE " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LPG":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPG " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LPI":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPI " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LPS":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPS " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LPA":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LPA " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "LDGTS":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "LDGTS " + lipidinfo.Sn1AcylChain + "&ct=c";

                //Phospholipid
                case "PC":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PC "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PC " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PE":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PE "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PE " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PI":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PI "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PI " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PA":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PA "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PA " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "BMP":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "BMP "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "BMP " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "HBMP":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty && lipidinfo.Sn3AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "HBMP "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "/" + lipidinfo.Sn3AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty || lipidinfo.Sn3AcylChain == string.Empty)
                        && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "HBMP " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "CL":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty 
                        && lipidinfo.Sn3AcylChain != string.Empty && lipidinfo.Sn4AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "CL "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "/" + lipidinfo.Sn3AcylChain + "/" + lipidinfo.Sn4AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty 
                        || lipidinfo.Sn3AcylChain == string.Empty || lipidinfo.Sn4AcylChain == string.Empty)
                        && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "CL " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Ether linked phospholipid
                case "EtherPC":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PC "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PC " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "EtherPE":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PE "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PE " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Oxidized phospholipid
                case "OxPC":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPC "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPC " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "OxPE":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPE "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPE " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "OxPG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "OxPI":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPI "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPI " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "OxPS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPC "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPC " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "EtherOxPE":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPE "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "OxPE " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PMeOH "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PMeOH " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PEtOH":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PEtOH "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PEtOH " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "PBtOH":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PBtOH "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "PBtOH " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Plantlipid
                case "MGDG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "MGDG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "MGDG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "DGDG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DGDG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DGDG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "SQDG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SQDG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SQDG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "DGTS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DGTS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DGTS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "GlcADG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcADG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcADG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "AcylGlcADG":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty && lipidinfo.Sn3AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "AcylGlcADG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "/" + lipidinfo.Sn3AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty || lipidinfo.Sn3AcylChain == string.Empty)
                        && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "AcylGlcADG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Others
                case "CE":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "CE " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "ACar":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "ACar " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    if (lipidinfo.Sn1AcylChain == string.Empty) return string.Empty;
                    else
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "FA " + lipidinfo.Sn1AcylChain + "&ct=c";
                case "FAHFA":
                case "DMEDFAHFA":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DAG "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "DAG " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Sphingomyelin
                case "SM":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SM "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SM " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                //Ceramide
                case "Cer-ADS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-ADS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-ADS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-AS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-AS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-AS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-BDS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-BDS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-BDS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-BS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-BS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-BS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-EODS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-EODS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-EODS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-EOS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-EOS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-EOS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-NDS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NDS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NDS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-NS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-NP":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NP "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-NP " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "HexCer-NS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-NS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-NS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "HexCer-NDS":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-NDS "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-NDS " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "Cer-AP":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-AP "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "Cer-AP " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "HexCer-AP":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-AP "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GlcCer-AP " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }

                case "SHexCer":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SHexCer "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "SHexCer " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                case "GM3":
                    if (lipidinfo.Sn1AcylChain != string.Empty && lipidinfo.Sn2AcylChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GM3 "
                            + lipidinfo.Sn1AcylChain + "/" + lipidinfo.Sn2AcylChain + "&ct=c";
                    }
                    else if ((lipidinfo.Sn1AcylChain == string.Empty || lipidinfo.Sn2AcylChain == string.Empty) && lipidinfo.TotalChain != string.Empty) {
                        return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + "GM3 " + lipidinfo.TotalChain + "&ct=c";
                    }
                    else {
                        return string.Empty;
                    }
                default:
                    return string.Empty;
            }
        }
    }
}
