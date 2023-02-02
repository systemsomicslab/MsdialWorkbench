/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Dict;
using System;
using System.Diagnostics;

namespace NCDK.Validate
{
    /// <summary>
    /// Validates the existence of references to dictionaries.
    /// </summary>
    // @author   Egon Willighagen
    // @cdk.created  2003-03-28
    public class DictionaryValidator 
        : AbstractValidator
    {
        private DictionaryDatabase db;

        public DictionaryValidator(DictionaryDatabase db)
        {
            this.db = db;
        }

        public override ValidationReport ValidateChemObject(IChemObject subject)
        {
            var report = new ValidationReport();
            var properties = subject.GetProperties();
            var iter = properties.Keys;
            var noNamespace = new ValidationTest(subject,"Dictionary Reference lacks a namespace indicating the dictionary.");
            var noDict = new ValidationTest(subject, "The referenced dictionary does not exist.");
            var noEntry = new ValidationTest(subject, "The referenced entry does not exist in the dictionary.");
            foreach (var key in iter)
            {
                if (key is string keyName)
                {
                    if (keyName.StartsWith(DictionaryDatabase.DictRefPropertyName, StringComparison.Ordinal))
                    {
                        string dictRef = (string)properties[keyName];
                        string details = "Dictref being anaylyzed: " + dictRef + ". ";
                        noNamespace.Details = details;
                        noDict.Details = details;
                        noEntry.Details = details;
                        int index = dictRef.IndexOf(':');
                        if (index != -1)
                        {
                            report.OKs.Add(noNamespace);
                            string dict = dictRef.Substring(0, index);
                            Debug.WriteLine($"Looking for dictionary:{dict}");
                            if (db.HasDictionary(dict))
                            {
                                report.OKs.Add(noDict);
                                if (dictRef.Length > index + 1)
                                {
                                    string entry = dictRef.Substring(index + 1);
                                    Debug.WriteLine($"Looking for entry:{entry}");
                                    if (db.HasEntry(dict, entry))
                                    {
                                        report.OKs.Add(noEntry);
                                    }
                                    else
                                    {
                                        report.Errors.Add(noEntry);
                                    }
                                }
                                else
                                {
                                    report.Errors.Add(noEntry);
                                }
                            }
                            else
                            {
                                details += "The dictionary searched: " + dict + ".";
                                noDict.Details = details;
                                report.Errors.Add(noDict);
                                report.Errors.Add(noEntry);
                            }
                        }
                        else
                        {
                            // The dictRef has no namespace
                            details += "There is not a namespace given.";
                            noNamespace.Details = details;
                            report.Errors.Add(noNamespace);
                            report.Errors.Add(noDict);
                            report.Errors.Add(noEntry);
                        }
                    }
                    else
                    {
                        // not a dictref
                    }
                }
                else
                {
                    // not a dictref
                }
            }
            return report;
        }
    }
}
