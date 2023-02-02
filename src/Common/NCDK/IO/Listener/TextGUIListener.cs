/* Copyright (C) 2003-2007  The Jmol Development Team
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.IO.Setting;
using System;
using System.Globalization;
using System.IO;

namespace NCDK.IO.Listener
{
    /// <summary>
    /// Allows processing of IOSetting quesions which are passed to the user
    /// by using the System.out and System.in by default.
    /// </summary>
    /// <remarks>
    /// This listener can also be used to list all the questions a ChemObjectWriter
    /// has, by using a dummy StringWriter, and a <see langword="null"/> Reader.
    /// </remarks>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class TextGUIListener : IReaderListener, IWriterListener
    {
        private TextReader ins;
        private TextWriter output;

        private Importance level = Importance.High;

        public TextGUIListener(Importance level)
        {
            this.level = level;
            this.SetInputReader(System.Console.In);
            this.SetOutputWriter(System.Console.Out);
        }

        public void SetLevel(Importance level)
        {
            this.level = level;
        }

        /// <summary>
        /// Overwrites the default writer to which the output is directed.
        /// </summary>
        public void SetOutputWriter(TextWriter writer)
        {
            output = writer;
        }

        /// <summary>
        /// Overwrites the default reader from which the input is taken.
        /// </summary>
        public void SetInputReader(TextReader reader)
        {
            ins = reader;
        }

        public void FrameRead(ReaderEvent evt) { }

        /// <summary>
        /// Processes the IOSettings by listing the question, giving the options
        /// and asking the user to provide their choice.
        /// </summary>
        /// <remarks>
        /// Note: if the input reader is <see langword="null"/>, then the method
        /// does not wait for an answer, and takes the default.</remarks>
        public void ProcessIOSettingQuestion(IOSetting setting)
        {
            // post the question
            if (setting.Level.Ordinal <= this.level.Ordinal)
            {
                // output the option name
                this.output.Write("[" + setting.Name + "]: ");
                // post the question
                this.output.Write(setting.Question);
                if (setting is BooleanIOSetting boolSet)
                {
                    bool set = boolSet.IsSet;
                    if (set)
                    {
                        this.output.Write(" [Yn]");
                    }
                    else
                    {
                        this.output.Write(" [yN]");
                    }
                }
                else if (setting is OptionIOSetting optionSet)
                {
                    var settings = optionSet.GetOptions();
                    for (int i = 0; i < settings.Count; i++)
                    {
                        this.output.Write('\n');
                        string option = settings[i];
                        this.output.Write((i + 1) + ". " + option);
                        if (string.Equals(option, setting.Setting, StringComparison.Ordinal))
                        {
                            this.output.Write(" (Default)");
                        }
                    }
                }
                else
                {
                    this.output.Write(" [" + setting.Setting + "]");
                }
                this.output.Write('\n');
                this.output.Flush();

                // get the answer, only if input != null
                if (this.ins == null)
                {
                    // don't really ask questions. This is intentional behaviour to
                    // allow for listing all questions. The settings is now defaulted,
                    // which is the intention too.
                }
                else
                {
                    bool gotAnswer = false;
                    while (!gotAnswer)
                    {
                        try
                        {
                            this.output.Write("> ");
                            this.output.Flush();
                            string answer = ins.ReadLine();
                            if (answer.Length == 0)
                            {
                                // pressed ENTER -> take default
                            }
                            else if (setting is OptionIOSetting)
                            {
                                ((OptionIOSetting)setting).SetSetting(int.Parse(answer, NumberFormatInfo.InvariantInfo));
                            }
                            else if (setting is BooleanIOSetting)
                            {
                                switch (answer.ToUpperInvariant())
                                {
                                    case "N":
                                    case "NO":
                                        answer = "false";
                                        break;
                                    case "Y":
                                    case "YES":
                                        answer = "true";
                                        break;
                                }
                                setting.Setting = answer;
                            }
                            else
                            {
                                setting.Setting = answer;
                            }
                            gotAnswer = true;
                        }
                        catch (IOException)
                        {
                            this.output.WriteLine("Cannot read from STDIN. Skipping question.");
                        }
                        catch (FormatException)
                        {
                            this.output.WriteLine("Answer is not a number.");
                        }
                        catch (CDKException exception)
                        {
                            this.output.Write('\n');
                            this.output.WriteLine(exception.ToString());
                        }
                    }
                }
            }
        }
    }
}
