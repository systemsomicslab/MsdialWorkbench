using System;
using System.Collections.Generic;

namespace CompMs.App.MsdialConsole.Parser
{
    /// <summary>
    /// A method file holds a value the command it was given to cannot use. The command stops with
    /// exit code 2 instead of running with a default the file did not ask for.
    /// </summary>
    public sealed class MethodFileException : Exception
    {
        public MethodFileException(string message, IReadOnlyList<string> unusableLines) : base(message) {
            UnusableLines = unusableLines;
        }

        /// <summary>Each unusable line as "Key: value".</summary>
        public IReadOnlyList<string> UnusableLines { get; }
    }
}
