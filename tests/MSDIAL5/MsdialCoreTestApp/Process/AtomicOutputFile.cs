using System;
using System.IO;

namespace CompMs.App.MsdialConsole.Process.MoleculerNetworking
{
    /// <summary>
    /// Writes an output file so that it either appears complete or does not appear at all.
    /// </summary>
    /// <remarks>
    /// The content goes to <c>&lt;output&gt;.partial</c>, which is renamed to the output only after
    /// the writer has finished and the file is closed. If writing throws, the partial file is
    /// deleted and an existing output from an earlier run is left untouched, so a truncated file
    /// never looks like a finished result.
    /// </remarks>
    internal static class AtomicOutputFile
    {
        public const string PartialSuffix = ".partial";

        public static string PartialPath(string output) => output + PartialSuffix;

        /// <summary>Write through a UTF-8 (no BOM) StreamWriter, as the outputs always were.</summary>
        public static void Write(string output, Action<StreamWriter> write) {
            Produce(output, partial => {
                using (var writer = new StreamWriter(partial, false)) {
                    write(writer);
                }
            });
        }

        /// <summary>Let <paramref name="produce"/> create the file at the path it is given.</summary>
        public static void Produce(string output, Action<string> produce) {
            var partial = PartialPath(output);
            var created = false;
            try {
                if (File.Exists(partial)) {
                    // A partial file is never a result; one left by an interrupted run is replaced.
                    File.Delete(partial);
                }
                created = true;
                produce(partial);
                Commit(partial, output);
            }
            catch {
                if (created) {
                    TryDelete(partial);
                }
                throw;
            }
        }

        private static void Commit(string partial, string output) {
#if NETCOREAPP3_0_OR_GREATER
            File.Move(partial, output, overwrite: true);
#else
            if (File.Exists(output)) {
                File.Delete(output);
            }
            File.Move(partial, output);
#endif
        }

        private static void TryDelete(string path) {
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
            catch (IOException) {
            }
            catch (UnauthorizedAccessException) {
            }
        }
    }
}
