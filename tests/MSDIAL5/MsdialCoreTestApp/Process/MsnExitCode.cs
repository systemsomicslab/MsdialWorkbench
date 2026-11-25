namespace CompMs.App.MsdialConsole.Process
{
    /// <summary>
    /// Exit codes of the <c>msn</c> command. Command-line parse errors keep System.CommandLine's own code.
    /// </summary>
    public static class MsnExitCode
    {
        /// <summary>The output was written completely.</summary>
        public const int Success = 0;

        /// <summary>The run failed (an exception or an I/O error). No output file was produced.</summary>
        public const int Failure = 1;

        /// <summary>
        /// The run did not start: a method-file value it cannot use, a --targetFile that does not
        /// exist, or an option that does not apply to this input.
        /// </summary>
        public const int UnusableInput = 2;
    }
}
