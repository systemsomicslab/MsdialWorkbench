using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CompMs.Graphics.Window
{
    public class SelectFolderDialog {
        // reference https://shikaku-sh.hatenablog.com/entry/wpf-folder-selection-dialog

        private class NativeMethods
        {
            #region DllImports
            [DllImport("shell32.dll")]
            public static extern int SHILCreateFromPath([MarshalAs(UnmanagedType.LPWStr)] string pszPath, out IntPtr ppIdl, ref uint rgfInOut);

            [DllImport("shell32.dll")]
            public static extern int SHCreateShellItem(IntPtr pidlParent, IntPtr psfParent, IntPtr pidl, out IShellItem ppsi);
            #endregion

            #region Fields
            public const uint ERROR_CANCELLED = 0x800704C7;
            #endregion

        }

        #region Private Classes & Interfaces
        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileBrowseDialogInternal { }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler();
            void GetParent();
            void GetDisplayName(in SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes();
            void Compare();
        }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            uint Show([In] IntPtr parent);
            void SetFileTypes();
            void SetFileTypeIndex([In] uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise();
            void Unadvise();
            void SetOptions([In] _FILEOPENDIALOGOPTIONS fos);
            void GetOptions(out _FILEOPENDIALOGOPTIONS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid();
            void ClearClientData();
            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
            void GetResults([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenum);
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppsai);
        }
        #endregion

        #region Properties
        public string? SelectedPath { get; set; }
        public string? Title { get; set; }
        #endregion

        #region Initializes
        public SelectFolderDialog() { }
        #endregion

        #region Events
        #endregion

        #region Public Methods
        public DialogResult ShowDialog() {
            return ShowDialog(IntPtr.Zero);
        }

        public DialogResult ShowDialog(System.Windows.Window owner) {
            if (owner == null) {
                throw new ArgumentNullException("Null pointer was passed. Couldn't set owner.");
            }

            var handle = new WindowInteropHelper(owner).Handle;
            return ShowDialog(handle);
        }

        public DialogResult ShowDialog(IntPtr owner) {
            var dialog = new FileBrowseDialogInternal() as IFileOpenDialog;

            try {
                IShellItem item;

                var options = _FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | _FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM;
                dialog.SetOptions(options);

                if (!string.IsNullOrEmpty(SelectedPath)) {
                    uint attributes = 0;

                    if (NativeMethods.SHILCreateFromPath(SelectedPath, out IntPtr idl, ref attributes) == 0) {
                        if (NativeMethods.SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, idl, out item) == 0) {
                            dialog.SetFolder(item);
                        }

                        if (idl != IntPtr.Zero) {
                            Marshal.FreeCoTaskMem(idl);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Title)) {
                    dialog.SetTitle(Title);
                }

                var hr = dialog.Show(owner);

                if (hr == NativeMethods.ERROR_CANCELLED) return DialogResult.Cancel;
                if (hr != 0) return DialogResult.Abort;

                dialog.GetResult(out item);

                if (item != null) {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out string selectedPath);
                    SelectedPath = selectedPath;
                }
                else {
                    return DialogResult.Abort;
                }
                return DialogResult.OK;
            }
            finally {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        #endregion
    }

    public enum DialogResult : int
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7,
    }

    public enum SIGDN : uint
    {
        SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
        SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
        SIGDN_FILESYSPATH = 0x80058000,
        SIGDN_NORMALDISPLAY = 0x0,
        SIGDN_PARENTRELATIVE = 0x80080001,
        SIGDN_PARENTRELATIVEEDITING = 0x80031001,
        SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
        SIGDN_PARENTRELATIVEPARSING = 0x80018001,
        SIGDN_URL = 0x80068000,
    }

    [Flags]
    public enum _FILEOPENDIALOGOPTIONS : uint
    {
        FOS_OVERWRITEPROMPT = 0x00000002,
        FOS_STRICTFILETYPES = 0x00000004,
        FOS_NOCHANGEDIR = 0x00000008,
        FOS_PICKFOLDERS = 0x00000020,
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_ALLNONSTORAGEITEMS = 0x00000080,
        FOS_NOVALIDATE = 0x00000100,
        FOS_ALLOWMULTISELECT = 0x00000200,
        FOS_PATHMUSTEXIST = 0x00000800,
        FOS_FILEMUSTEXIST = 0x00001000,
        FOS_CREATEPROMPT = 0x00002000,
        FOS_SHAREAWARE = 0x00004000,
        FOS_NOREADONLYRETURN = 0x00008000,
        FOS_NOTESTFILECREATE = 0x00010000,
        FOS_HIDEMRUPLACES = 0x00020000,
        FOS_HIDEPINNEDPLACES = 0x00040000,
        FOS_NODEREFERENCELINKS = 0x00100000,
        FOS_DONTADDTORECENT = 0x02000000,
        FOS_FORCESHOWHIDDEN = 0x10000000,
        FOS_DEFAULTNOMINIMODE = 0x20000000,
        FOS_FORCEPREVIEWPANEON = 0x40000000,
        FOS_SUPPORTSTREAMABLEITEMS = 0x80000000
    }
}
