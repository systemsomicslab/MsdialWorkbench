using System;
using System.IO;

namespace CompMs.MsdialCore.Utility;

/// <summary>
/// Creates the temporary files of an alignment in the folder that receives the alignment result.
/// </summary>
/// <remarks>
/// Before it writes the EIC file, the alignment spills one chromatogram file per analysis file, and the light-mode
/// alignment keeps its peak matrix in a file. Together these can reach tens of gigabytes. They were created with
/// <see cref="Path.GetTempFileName"/>, in %TEMP% on the system drive, whatever drive the project is on. They are now
/// created beside the alignment result (the .arf2, .dcl and .EIC.aef), on the drive the person chose for the data.
/// Only when no such folder is known, because the result path is empty or its folder does not exist, do they fall
/// back to the system temporary folder as before.
/// <para>
/// Each file is created empty, as <see cref="Path.GetTempFileName"/> does, so the name is reserved; the caller deletes
/// it when it is done, and on failure.
/// </para>
/// </remarks>
public static class AlignmentTemporaryFile
{
    private const int MaximumAttempts = 100;

    /// <summary>
    /// Creates an empty, uniquely named file in the folder of <paramref name="resultFilePath"/>, named after that file,
    /// and returns its path. Falls back to <see cref="Path.GetTempFileName"/> when the folder is not known.
    /// </summary>
    /// <param name="resultFilePath">A file of the alignment result being written, such as its EIC file.</param>
    public static string CreateBeside(string? resultFilePath) {
        var folder = string.IsNullOrEmpty(resultFilePath) ? null : Path.GetDirectoryName(resultFilePath);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) {
            return Path.GetTempFileName();
        }

        var name = Path.GetFileName(resultFilePath);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++) {
            var path = Path.Combine(folder, $"{name}.{Guid.NewGuid():N}.tmp");
            try {
                using (new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                return path;
            }
            catch (IOException) when (File.Exists(path)) {
                // The name is taken, which a random GUID makes all but impossible: try another.
            }
        }
        throw new IOException($"Could not create a temporary file beside {resultFilePath}.");
    }
}
