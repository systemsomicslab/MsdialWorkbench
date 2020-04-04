using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class ErrorHandler
    {
        private ErrorHandler() { }

        public static bool IsFileLocked(string file, out string error)
        {
            error = string.Empty;
            var fileinfo = new FileInfo(file);
            FileStream fs = null;

            try {
                fs = fileinfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                error = System.IO.Path.GetFileName(file) + " in use by another process.";
                return true;
            }
            finally {
                if (fs != null)
                    fs.Close();
            }
            return false;

        }
    }
}
