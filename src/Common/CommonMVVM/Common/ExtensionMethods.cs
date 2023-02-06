using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// This code is used to create EMF format, i.e. vector image, from the wpf code.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="fileName"></param>
        public static void SaveAsEmf(this Metafile me, string fileName)
        {
            /* http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/12a1c749-b320-4ce9-aff7-9de0d7fd30ea 
                How to save or serialize a Metafile: Solution found 
                by : SWAT Team member _1 
                Date : Friday, February 01, 2008 1:38 PM 
             */
            int enfMetafileHandle = me.GetHenhmetafile().ToInt32();
            int bufferSize = GetEnhMetaFileBits(enfMetafileHandle, 0, null); // Get required buffer size.  
            byte[] buffer = new byte[bufferSize]; // Allocate sufficient buffer  
            if (GetEnhMetaFileBits(enfMetafileHandle, bufferSize, buffer) <= 0) // Get raw metafile data.  
                throw new SystemException("Fail");

            FileStream ms = File.Open(fileName, FileMode.Create);
            ms.Write(buffer, 0, bufferSize);
            ms.Close();
            ms.Dispose();
            if (!DeleteEnhMetaFile(enfMetafileHandle)) //free handle  
                throw new SystemException("Fail Free");
        }

        [DllImport("gdi32")]
        public static extern int GetEnhMetaFileBits(int hemf, int cbBuffer, byte[] lpbBuffer);

        [DllImport("gdi32")]
        public static extern bool DeleteEnhMetaFile(int hemfbitHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [DllImport("gdi32.dll")]
        public static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteEnhMetaFile(IntPtr hemf);

        /// <see href="http://msdn2.microsoft.com/en-us/library/ms648063.aspx"/>
        [DllImport("User32.dll", SetLastError = true)]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
