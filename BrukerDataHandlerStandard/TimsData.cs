using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.BrukerDataHandler {
    /// <summary>
    /// wrapper for timsdata.dll
    /// cite: http://www.84kure.com/blog/2014/07/16/73/
    /// </summary>
    public class TimsData {
        /// Open data set.
        ///
        /// On success, returns a non-zero instance handle that needs to be passed to
        /// subsequent API calls, in particular to the required call to tims_close().
        ///
        /// On failure, returns 0, and you can use tims_get_last_error_string() to obtain a
        /// string describing the problem.
        ///
        /// \param analysis_directory_name the name of the directory in the file system that
        /// contains the analysis data, in UTF-8 encoding.
        ///
        /// \param use_recalibrated_state if non-zero, use the most recent recalibrated state
        /// of the analysis, if there is one; if zero, use the original "raw" calibration
        /// written during acquisition time.
        ///
        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong tims_open(string analysis_directory_name, uint use_recalibrated_state);

        /// Close data set.
        ///
        /// \param handle obtained by tims_open(); passing 0 is ok and has no effect.
        ///
        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void tims_close(ulong handle);

        /// Return the last error as a string (thread-local).
        ///
        /// \param buf pointer to a buffer into which the error string will be written.
        ///
        /// \param len length of the buffer
        ///
        /// \returns the actual length of the error message (including the final zero
        /// byte). If this is longer than the input parameter 'len', you know that the
        /// returned error string was truncated to fit in the provided buffer.
        ///
        [DllImport("timsdata.dll")]
        public static extern uint tims_get_last_error_string(StringBuilder buf, uint len);

        /// Returns 1 if the raw data have been recalibrated after acquisition, e.g. in the
        /// DataAnalysis software. Note that masses and 1/K0 values in the raw-data SQLite
        /// file are always in the raw calibration state, not the recalibrated state.
        ///
        [DllImport("timsdata.dll")]
        public static extern uint tims_has_recalibrated_state(ulong handle);

        /// Read a range of scans from a single frame.
        ///
        /// Output layout: (N = scan_end - scan_begin = number of requested scans)
        ///   N x uint32_t: number of peaks in the N requested scans
        ///   N x (two uint32_t arrays: first indices, then intensities)
        ///
        /// Note: different threads must not read scans from the same storage handle
        /// concurrently.
        ///
        /// \returns 0 on error, otherwise the required buffer length in bytes (if this is
        /// larger than the provided length, you are missing data).
        ///
        [DllImport("timsdata.dll", CharSet=CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_read_scans_v2(
            ulong handle,
            long frame_id,      //< from .tdf SQLite: Frames.Id
            uint scan_begin,   //< first scan number to read (inclusive)
            uint scan_end,     //< last scan number (exclusive)
            uint[] buf,             //< destination buffer allocated by user
            uint len           //< length of buffer (in bytes)
            );

        public static double[] IndexToMz(ulong handle, long frame_id, double[] indexArray) {

            if (indexArray == null || indexArray.Length == 0) return null;
            var outArray = new double[indexArray.Length];
            var count = (uint)indexArray.Length;
            var success = tims_index_to_mz(handle, frame_id, indexArray, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }

        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_index_to_mz(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );

        public static double[] MzToIndex(ulong handle, long frame_id, double[] mzArray) {

            if (mzArray == null || mzArray.Length == 0) return null;
            var outArray = new double[mzArray.Length];
            var count = (uint)mzArray.Length;
            var success = tims_mz_to_index(handle, frame_id, mzArray, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }


        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_mz_to_index(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );

        public static double[] ScanToOneOverK0(ulong handle, long frame_id, double[] scanArray) {

            if (scanArray == null || scanArray.Length == 0) return null;
            var outArray = new double[scanArray.Length];
            var count = (uint)scanArray.Length;
            var success = tims_scannum_to_oneoverk0(handle, frame_id, scanArray, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }

        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_scannum_to_oneoverk0(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );

        public static double[] OneOverK0ToScan(ulong handle, long frame_id, double[] oneoverk0Array) {

            if (oneoverk0Array == null || oneoverk0Array.Length == 0) return null;
            var outArray = new double[oneoverk0Array.Length];
            var count = (uint)oneoverk0Array.Length;
            var success = tims_oneoverk0_to_scannum(handle, frame_id, oneoverk0Array, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }

        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_oneoverk0_to_scannum(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );

        public static double[] ScanToVoltage(ulong handle, long frame_id, double[] scanArray) {

            if (scanArray == null || scanArray.Length == 0) return null;
            var outArray = new double[scanArray.Length];
            var count = (uint)scanArray.Length;
            var success = tims_scannum_to_voltage(handle, frame_id, scanArray, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }

        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_scannum_to_voltage(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );


        public static double[] VoltageToScan(ulong handle, long frame_id, double[] voltageArray) {

            if (voltageArray == null || voltageArray.Length == 0) return null;
            var outArray = new double[voltageArray.Length];
            var count = (uint)voltageArray.Length;
            var success = tims_voltage_to_scannum(handle, frame_id, voltageArray, outArray, count);

            if (success > 0)
                return outArray;
            else
                return null;
        }

        [DllImport("timsdata.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint tims_voltage_to_scannum(
                ulong handle,
                long frame_id,      //< from .tdf SQLite: Frames.Id
                double[] index,   //<  in: array of values
                double[] mz,            //< out: array of values
                uint cnt           //< number of values to convert (arrays must have
                                   //< corresponding size)
            );
    }
}
