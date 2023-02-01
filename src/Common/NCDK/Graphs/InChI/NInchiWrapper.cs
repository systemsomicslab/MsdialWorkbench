/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not, see <http://www.gnu.org/licenses/>.
 */

using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// <para>.NET Framework Wrapper for International Chemical Identifier (InChI) C++ library.</para>
    /// </summary>
    /// <remarks>
    /// <para>This class is not intended to be used directly, but should be accessed
    /// through subclasses that read data formats and load them into the InChI
    /// data structures.</para>
    ///
    /// <para>Subclasses should load data through the addAtom, addBond and addParity
    /// methods. Once the molecule is fully loaded then the generateInchi method
    /// should be called. Ideally this should all take place within the subclass's
    /// constructor. The public get methods will all return null until this has
    /// happened.</para>
    ///
    /// <para>See <tt>inchi_api.h</tt>.</para>
    /// </remarks>
    // @author Sam Adams
    // @author Kazuya Ujihara
    unsafe internal class NInchiWrapper
    {
        private const string DllBaseName = "libinchi";

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetDllDirectory(string lpPathName);

        private static readonly object sync_LoadDll = new object();

        static void LoadDll()
        {
            lock (sync_LoadDll)
            {
                OperatingSystem os = Environment.OSVersion;
                switch (os.Platform)
                {
                    case PlatformID.Win32NT:
                        const string DllFileName = DllBaseName + ".dll";

                        string subdir = null;
                        if (Environment.Is64BitProcess)
                        {
                            subdir = "x64";
                        }
                        else
                        {
                            subdir = "x86";
                        }

                        var executingAsm = System.Reflection.Assembly.GetExecutingAssembly();
                        {
                            var currPath = Path.GetDirectoryName(executingAsm.Location);
                            if (SetDllDirectoryIfFileExist(currPath, subdir, DllFileName))
                                goto L_Found;
                        }
                        {
                            var uri = new Uri(executingAsm.CodeBase);
                            if (uri.Scheme == "file")
                            {
                                var currPath = Path.GetDirectoryName(uri.AbsolutePath);
                                if (SetDllDirectoryIfFileExist(currPath, subdir, DllFileName))
                                    goto L_Found;
                            }
                        }
                    L_Found:
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// SetDllDirectory if <paramref name="directoryName"/>/<paramref name="subdir"/>/<paramref name="dllName"/> or <paramref name="directoryName"/>/<paramref name="dllName"/>exists.
        /// </summary>
        /// <param name="directoryName">Directory of the DLL.</param>
        /// <param name="subdir">Sub directory name, typically "x86" or "x64".</param>
        /// <param name="dllName">Base name of the DLL.</param>
        /// <returns><see langword="true"/> if file exists and set it.</returns>
        private static bool SetDllDirectoryIfFileExist(string directoryName, string subdir, string dllName)
        {
            if (subdir != null)
            {
                if (SetDllDirectoryIfFileExist(Path.Combine(directoryName, subdir), dllName))
                    return true;
            }

            if (SetDllDirectoryIfFileExist(directoryName, dllName))
                return true;

            return false;
        }

        /// <summary>
        /// SetDllDirectory if <paramref name="directoryName"/>/<paramref name="dllName"/> exists.
        /// </summary>
        /// <param name="directoryName">Directory of the DLL.</param>
        /// <param name="dllName">Base name of the DLL.</param>
        /// <returns><see langword="true"/> if file exists and set it.</returns>
        private static bool SetDllDirectoryIfFileExist(string directoryName, string dllName)
        {
            var dllPath = Path.Combine(directoryName, dllName);
            if (File.Exists(dllPath))
            {
                SetDllDirectory(null);
                SetDllDirectory(directoryName);
                return true;
            }
            return false;
        }

        static NInchiWrapper()
        {
            LoadDll();
        }

        /* sizes definitions */
        public const int MAX_ATOMS = 1024;            // defined in  ichisize.h

        public const int MAXVAL = 20;                 /* max number of bonds per atom */
        public const int ATOM_EL_LEN = 6;             /* length of ASCIIZ element symbol field */
        public const int NUM_H_ISOTOPES = 3;          /* number of hydrogen isotopes: protium, D, T   */
        public const int ISOTOPIC_SHIFT_FLAG = 10000; /* add to isotopic mass if isotopic_mass = (isotopic mass - average atomic mass) */
        public const int ISOTOPIC_SHIFT_MAX = 100;    /* max Abs(isotopic mass - average atomic mass) */

        [StructLayout(LayoutKind.Sequential)]
        public struct Inchi_Atom
        {
            /* atom coordinates */
            public double X;
            public double Y;
            public double Z;
            /* connectivity */
            public fixed Int16/*AT_NUM*/ neighbor[MAXVAL];     /* adjacency list: ordering numbers of  the adjacent atoms, >= 0 */
            public fixed SByte/*S_CHAR*/ bond_type[MAXVAL];    /* inchi_BondType 2D stereo */
            public fixed SByte/*S_CHAR*/ bond_stereo[MAXVAL];  /* inchi_BondStereo2D; negative if the sharp end points to opposite atom other atom properties */
            public fixed sbyte/*char*/ elname[ATOM_EL_LEN];    /* zero-terminated chemical element name: "H", "Si", etc. */
            public Int16/*AT_NUM*/ num_bonds;                  /* number of neighbors, bond types and bond stereo in the adjacency list */
            public fixed SByte/*S_CHAR*/ num_iso_H[NUM_H_ISOTOPES + 1]; /* implicit hydrogen atoms */
                                                                        /* [0]: number of implicit non-isotopic H
                                                                                (exception: num_iso_H[0]=-1 means INCHI
                                                                                adds implicit H automatically),
                                                                           [1]: number of implicit isotopic 1H (protium),
                                                                           [2]: number of implicit 2H (deuterium),
                                                                           [3]: number of implicit 3H (tritium) */
            public Int16/*AT_NUM*/ isotopic_mass;        /* 0 => non-isotopic; isotopic mass or ISOTOPIC_SHIFT_FLAG + mass - (average atomic mass) */
            public SByte/*S_CHAR*/ radical;              /* inchi_Radical */
            public SByte/*S_CHAR*/ charge;               /* positive or negative; 0 => no charge */
        }

        public const int NO_ATOM = (-1); /* non-existent (central) atom */

        [StructLayout(LayoutKind.Sequential)]
        public struct Inchi_Stereo0D
        {
            public fixed Int16/*AT_NUM*/ neighbor[4]; /* 4 atoms always */
            public Int16/*AT_NUM*/ central_atom;      /* central tetrahedral atom or a central atom of allene; otherwise NO_ATOM */
            public SByte/*S_CHAR*/ type;              /* inchi_StereoType0D */
            public SByte/*S_CHAR*/ parity;            /* inchi_StereoParity0D: may be a combination of two parities: ParityOfConnected | (ParityOfDisconnected << 3), see Note above */
        }

        /* Structure -> InChI, GetINCHI() / GetStdINCHI() */
        [StructLayout(LayoutKind.Sequential)]
        public struct Inchi_Input
        {
            /* the caller is responsible for the data allocation and deallocation */
            public IntPtr/*inchi_Atom**/ atom;         /* array of num_atoms elements */
            public IntPtr/*inchi_Stereo0D**/ stereo0D; /* array of num_stereo0D 0D stereo elements or NULL */
            public IntPtr/*char**/ szOptions;          /* InChI options: space-delimited; each is preceded by '/' or '-' depending on OS and compiler */
            public Int16/*AT_NUM*/ num_atoms;          /* number of atoms in the structure < 1024 */
            public Int16/*AT_NUM*/ num_stereo0D;       /* number of 0D stereo elements */
        }

        /* InChI -> Structure, GetStructFromINCHI()/GetStructFromStdINCHI() */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Inchi_InputINCHI
        {
            /* the caller is responsible for the data allocation and deallocation */
            public IntPtr/*char**/ szInChI;     /* InChI ASCIIZ string to be converted to a structure */
            public IntPtr/*char**/ szOptions;   /* InChI options: space-delimited; each is preceded by */
                                                /* '/' or '-' depending on OS and compiler */
        }

        /* Structure -> InChI */
        [StructLayout(LayoutKind.Sequential)]
        public struct Inchi_Output
        {
            /* zero-terminated C-strings allocated by GetStdINCHI() */
            /* to deallocate all of them call FreeStdINCHI() (see below) */
            public IntPtr/*char**/ szInChI;     /* InChI ASCIIZ string */
            public IntPtr/*char**/ szAuxInfo;   /* Aux info ASCIIZ string */
            public IntPtr/*char**/ szMessage;   /* Error/warning ASCIIZ message */
            public IntPtr/*char**/ szLog;       /* log-file ASCIIZ string, contains a human-readable list */
                                                /* of recognized options and possibly an Error/warning message */
        }

        /* InChI -> Structure */
        /// <summary>
        /// 4 pointers are allocated by GetStructFromINCHI()/GetStructFromStdINCHI()
        /// o deallocate all of them call FreeStructFromStdINCHI()/FreeStructFromStdINCHI()
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Inchi_OutputStruct
        {
            /// <summary>
            /// array of <see cref="Inchi_Atom"/> num_atoms elements
            /// </summary>
            public IntPtr atom;
            /// <summary>
            /// array of <see cref="Inchi_Stereo0D"/> num_stereo0D 0D stereo elements or NULL 
            /// </summary>
            public IntPtr stereo0D;
            public Int16/*AT_NUM*/ num_atoms;    /* number of atoms in the structure < 1024 */
            public Int16/*AT_NUM*/ num_stereo0D; /* number of 0D stereo elements */
            public IntPtr/*char**/ szMessage;    /* Error/warning ASCIIZ message */
            public IntPtr/*char**/ szLog;        /* log-file ASCIIZ string, contains a human-readable list  of recognized options and possibly an Error/warning message */
            public fixed UInt64 WarningFlags[4]; /* warnings, see INCHIDIFF in inchicmp.h */
                                                 /* [x][y]: x=0 => Reconnected if present in InChI otherwise Disconnected/Normal
                                                            x=1 => Disconnected layer if Reconnected layer is present
                                                            y=1 => Main layer or Mobile-H
                                                            y=0 => Fixed-H layer */
        }

        public const int STR_ERR_LEN = 256;

        [StructLayout(LayoutKind.Sequential)]
        public struct InchiInpData
        {
            /// <summary>
            /// A pointer to <see cref="Inchi_Input"/> that has all items 0 or NULL
            /// </summary>
            public IntPtr pInp;
            public int bChiral;                    /* 1 => the structure was marked as chiral, 2=> not chiral, 0=> not marked */
            public fixed SByte/*char*/ szErrMsg[STR_ERR_LEN];
        }

        // SafeNativeMethods
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int GetINCHI([In] ref Inchi_Input inp, [Out] out Inchi_Output outp);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int GetStdINCHI([In] ref Inchi_Input pIn, [Out] out Inchi_Output pOut);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void FreeINCHI([In] ref Inchi_Output pOut);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void FreeStdINCHI([In] ref Inchi_Output pOut);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetStringLength(char* p);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetStructFromINCHI([In] ref Inchi_InputINCHI pinpInChI, [Out] out Inchi_OutputStruct pOutStruct);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetStructFromStdINCHI([In] ref Inchi_InputINCHI pinpInChI, [Out] out Inchi_OutputStruct pOutStruct);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void FreeStructFromINCHI([In] ref Inchi_OutputStruct pOut);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeStructFromStdINCHI([In] ref Inchi_OutputStruct pOut);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetINCHIfromINCHI([In] ref Inchi_InputINCHI pinpInChI, [Out] out Inchi_Output pOut);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int Get_inchi_Input_FromAuxInfo([MarshalAs(UnmanagedType.LPStr)] string szInchiAuxInfo,
                                                      int bDoNotAddH,
                                                      int bDiffUnkUndfStereo,
                                                      [Out] out InchiInpData pInchiInp);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int Get_std_inchi_Input_FromAuxInfo(char* szInchiAuxInfo,
                                                          int bDoNotAddH,
                                                          InchiInpData* pInchiInp);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Free_inchi_Input([In] ref Inchi_Input pInp);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void Free_inchi_Input(Inchi_Input* pInp);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void Free_std_inchi_Input([In] ref Inchi_Input pInp);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int CheckINCHI([MarshalAs(UnmanagedType.LPStr)] [In] string szINCHI, int strict);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int GetINCHIKeyFromINCHI([MarshalAs(UnmanagedType.LPStr)][In] string szINCHISource,
                                               int xtra1,
                                               int xtra2,
                                               IntPtr szINCHIKey,
                                               IntPtr szXtra1,
                                               IntPtr szXtra2);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int GetStdINCHIKeyFromStdINCHI([MarshalAs(UnmanagedType.LPStr)] string szINCHISource,
                                                     [MarshalAs(UnmanagedType.LPStr)] string szINCHIKey);
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllBaseName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int CheckINCHIKey([MarshalAs(UnmanagedType.LPStr)] string szINCHIKey);

        public const string Id = "NInChI";
        public const string Version = "1.05_0";

        /// <summary>
        /// Flag indicating windows or linux.
        /// </summary>
        private const bool IS_WINDOWS = true;

        /// <summary>
        /// Switch character for passing options. / in windows, - on other systems.
        /// </summary>
        internal const string FlagChar = IS_WINDOWS ? "/" : "-";

        /// <summary>
        /// Checks and canonicalises options.
        /// </summary>
        /// <param name="ops">List of INCHI_OPTION</param>
        protected internal static string CheckOptions(IEnumerable<InChIOption> ops)
        {
            if (ops == null)
            {
                throw new ArgumentNullException(nameof(ops), "Null options");
            }
            var sbOptions = new StringBuilder();

            foreach (var op in ops)
            {
                sbOptions.Append(FlagChar + op.Name + " ");
            }

            return sbOptions.ToString();
        }

        /// <summary>
        /// Checks and canonicalises options.
        /// </summary>
        /// <param name="ops">Space delimited string of options to pass to InChI library.
        ///                     Each option may optionally be preceded by a command line
        ///                     switch (/ or -).</param>
        protected internal static string CheckOptions(string ops)
        {
            if (ops == null)
            {
                throw new ArgumentNullException(nameof(ops), "Null options");
            }

            var tok = Strings.Tokenize(ops);
            var sbOptions = string.Join(" ", tok.Select(n =>
            {
                string op = n;
                if (op.StartsWithChar('-') || op.StartsWithChar('/'))
                {
                    op = op.Substring(1);
                }
                var option = InChIOption.ValueOfIgnoreCase(op);
                if (option != null)
                {
                    return FlagChar + option.Name;
                }
                throw new NInchiException("Unrecognised InChI option");
            }));

            return sbOptions;
        }

        struct Set_inchi_Input_
        {
            public Inchi_Atom[] atoms;
            public Inchi_Stereo0D[] stereos;
        }

        static Set_inchi_Input_ InitInchiInput(NInchiInput input)
        {
            var natoms = input.Atoms.Count;
            var nstereo = input.Stereos.Count;
            var nbonds = input.Bonds.Count;

            if (natoms > MAX_ATOMS)
                throw new ArgumentException("Too many atoms");

            var atoms = new Inchi_Atom[natoms];
            {
                for (int i = 0; i < natoms; i++)
                {
                    var atom = input.Atoms[i];

                    Inchi_Atom iatom = new Inchi_Atom();
                    {
                        var elname = Encoding.ASCII.GetBytes(atom.ElementType);
                        for (int n = 0; n < elname.Length; n++)
                            iatom.elname[n] = (sbyte)elname[n];
                        iatom.elname[elname.Length] = 0;
                    }

                    iatom.X = atom.X;
                    iatom.Y = atom.Y;
                    iatom.Z = atom.Z;

                    iatom.charge = (SByte)atom.Charge;
                    iatom.radical = (SByte)atom.Radical;

                    iatom.num_iso_H[0] = (SByte)atom.ImplicitH;
                    iatom.num_iso_H[1] = (SByte)atom.ImplicitProtium;
                    iatom.num_iso_H[2] = (SByte)atom.ImplicitDeuterium;
                    iatom.num_iso_H[3] = (SByte)atom.ImplicitTritium;

                    iatom.isotopic_mass = (Int16)atom.IsotopicMass;

                    iatom.num_bonds = 0;

                    atoms[i] = iatom;
                }
            }

            {
                for (int i = 0; i < nbonds; i++)
                {
                    var bond = input.Bonds[i];
                    var atomO = bond.OriginAtom;
                    var atomT = bond.TargetAtom;
                    var bondType = bond.BondType;
                    var bondStereo = bond.BondStereo;

                    var iaO = input.Atoms.IndexOf(atomO);
                    var iaT = input.Atoms.IndexOf(atomT);

                    var iatom = atoms[iaO];
                    int numbonds = atoms[iaO].num_bonds;
                    if (numbonds == MAXVAL)
                    {
                        throw new ArgumentException("Too many bonds from one atom; maximum: " + MAXVAL);
                    }
                    iatom.neighbor[numbonds] = (Int16)iaT;
                    iatom.bond_type[numbonds] = (SByte)bondType;
                    iatom.bond_stereo[numbonds] = (SByte)bondStereo;
                    iatom.num_bonds++;
                    atoms[iaO] = iatom;
                }
            }

            var stereos = new Inchi_Stereo0D[nstereo];
            {
                for (var i = 0; i < input.Stereos.Count; i++)
                {
                    var stereo = input.Stereos[i];

                    var istereo = stereos[i];

                    var cat = stereo.CentralAtom;
                    var nat0 = stereo.Neighbors[0];
                    var nat1 = stereo.Neighbors[1];
                    var nat2 = stereo.Neighbors[2];
                    var nat3 = stereo.Neighbors[3];

                    istereo.central_atom = (Int16)input.Atoms.IndexOf(cat);
                    istereo.neighbor[0] = (Int16)input.Atoms.IndexOf(nat0);
                    istereo.neighbor[1] = (Int16)input.Atoms.IndexOf(nat1);
                    istereo.neighbor[2] = (Int16)input.Atoms.IndexOf(nat2);
                    istereo.neighbor[3] = (Int16)input.Atoms.IndexOf(nat3);
                    istereo.type = (SByte)stereo.StereoType;
                    istereo.parity = (SByte)stereo.Parity;

                    stereos[i] = istereo;
                }
            }

            var pre = new Set_inchi_Input_
            {
                atoms = atoms,
                stereos = stereos
            };
            return pre;
        }

        static NInchiOutput ToInchiOutput(int ret, Inchi_Output output)
        {
            var inchi = Marshal.PtrToStringAnsi(output.szInChI);
            var aux = Marshal.PtrToStringAnsi(output.szAuxInfo);
            var mes = Marshal.PtrToStringAnsi(output.szMessage);
            var log = Marshal.PtrToStringAnsi(output.szLog);

            return new NInchiOutput(ret, inchi, aux, mes, log);
        }

        /// <summary>
        /// <para>Generates the InChI for a chemical structure.</para>
        /// </summary>
        /// <remarks>
        /// <para>If no InChI creation/stereo modification options are specified then a standard
        /// InChI is produced, otherwise the generated InChI will be a non-standard one.</para>
        ///
        /// <para><b>Valid options:</b></para>
        /// <pre>
        ///  Structure perception (compatible with stdInChI):
        ///    /NEWPSOFF   /DoNotAddH   /SNon
        ///  Stereo interpretation (lead to generation of non-standard InChI)
        ///    /SRel /SRac /SUCF /ChiralFlagON /ChiralFlagOFF
        ///  InChI creation options (lead to generation of non-standard InChI)
        ///    /SUU /SLUUD   /FixedH  /RecMet  /KET /15T
        /// </pre>
        ///
        /// <para><b>Other options:</b></para>
        /// <pre>
        ///  /AuxNone    Omit auxiliary information (default: Include)
        ///  /Wnumber    Set time-out per structure in seconds; W0 means unlimited In InChI library the default value is unlimited
        ///  /OutputSDF  Output SDfile instead of InChI
        ///  /WarnOnEmptyStructure Warn and produce empty InChI for empty structure
        ///  /SaveOpt    Save custom InChI creation options (non-standard InChI)
        /// </pre>
        /// </remarks>
        /// <param name="input"></param>
        /// <returns></returns>
        public static NInchiOutput GetInchi(NInchiInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input), "Null input");

            var prep = InitInchiInput(input);

            fixed (Inchi_Atom* atoms = prep.atoms)
            fixed (Inchi_Stereo0D* stereos = prep.stereos)
            {
                Inchi_Input native_input = new Inchi_Input();
                Inchi_Output native_output = new Inchi_Output();

                native_input.atom = new IntPtr(atoms);
                native_input.num_atoms = (Int16)input.Atoms.Count;
                native_input.stereo0D = new IntPtr(stereos);
                native_input.num_stereo0D = (Int16)input.Stereos.Count;

                IntPtr szOptions = Marshal.StringToHGlobalAnsi(input.Options);
                try
                {
                    native_input.szOptions = szOptions;

                    var ret = GetINCHI(ref native_input, out native_output);
                    NInchiOutput oo = ToInchiOutput(ret, native_output);
                    FreeINCHI(ref native_output);

                    return oo;
                }
                finally
                {
                    Marshal.FreeHGlobal(szOptions);
                }
            }
        }

        /// <summary>
        /// <para>Calculates the Standard InChI string for a chemical structure.</para>
        /// <para>The only valid structure perception options are NEWPSOFF/DoNotAddH/SNon. In any other structural
        /// perception options are specified then the calculation will fail.</para>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static NInchiOutput GetStdInchi(NInchiInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input), "Null input");

            var prep = InitInchiInput(input);

            fixed (Inchi_Atom* atoms = prep.atoms)
            fixed (Inchi_Stereo0D* stereos = prep.stereos)
            {
                Inchi_Input native_input = new Inchi_Input();
                Inchi_Output native_output = new Inchi_Output();

                native_input.atom = new IntPtr(atoms);
                native_input.num_atoms = (Int16)input.Atoms.Count;
                native_input.stereo0D = new IntPtr(stereos);
                native_input.num_stereo0D = (Int16)input.Stereos.Count;

                var szOptions = Marshal.StringToHGlobalAnsi(input.Options);
                try
                {
                    native_input.szOptions = szOptions;

                    var ret = GetStdINCHI(ref native_input, out native_output);
                    NInchiOutput oo = ToInchiOutput(ret, native_output);
                    FreeStdINCHI(ref native_output);

                    return oo;
                }
                finally
                {
                    Marshal.FreeHGlobal(szOptions);
                }
            }
        }

        /// <summary>
        /// <para>Converts an InChI into an InChI for validation purposes (the same as the -InChI2InChI option).</para>
        /// <para>This method may also be used to filter out specific layers. For instance, /Snon would remove the
        /// stereochemical layer; Omitting /FixedH and/or /RecMet would remove Fixed-H or Reconnected layers.
        /// In order to keep all InChI layers use options string "/FixedH /RecMet"; option /InChI2InChI is not needed.</para>         
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static NInchiOutput GetInchiFromInchi(NInchiInputInchi input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Null input");
            }

            var native_input = new Inchi_InputINCHI();

            var szInChI = Marshal.StringToHGlobalAnsi(input.Inchi);
            var szOptions = Marshal.StringToHGlobalAnsi(input.Options);
            try
            {
                native_input.szInChI = szInChI;
                native_input.szOptions = szOptions;

                var native_output = new Inchi_Output();

                var ret = GetINCHIfromINCHI(ref native_input, out native_output);

                NInchiOutput oo = ToInchiOutput(ret, native_output);
                FreeStdINCHI(ref native_output);
                return oo;
            }
            finally
            {
                Marshal.FreeHGlobal(szOptions);
                Marshal.FreeHGlobal(szInChI);
            }
        }

        private static void CreateAtoms(NInchiStructure output, int numatoms, IntPtr intPtrAtoms)
        {
            var iatoms = (Inchi_Atom*)intPtrAtoms.ToPointer();
            for (int i = 0; i < numatoms; i++)
            {
                Inchi_Atom iatom = iatoms[i];
                var natom = new NInchiAtom(iatom.X, iatom.Y, iatom.Z, new string(iatom.elname))
                {
                    Charge = iatom.charge,
                    Radical = (INCHI_RADICAL)iatom.radical,
                    ImplicitH = iatom.num_iso_H[0],
                    ImplicitProtium = iatom.num_iso_H[1],
                    ImplicitDeuterium = iatom.num_iso_H[2],
                    ImplicitTritium = iatom.num_iso_H[3],
                    IsotopicMass = iatom.isotopic_mass
                };
                output.Atoms.Add(natom);
            }
        }

        private static void CreateBonds(NInchiStructure output, int numatoms, IntPtr intPtrAtoms)
        {
            var iatoms = (Inchi_Atom*)intPtrAtoms.ToPointer();
            for (int i = 0; i < numatoms; i++)
            {
                Inchi_Atom iatom = iatoms[i];
                int numbonds = iatom.num_bonds;
                if (numbonds > 0)
                {
                    var atO = output.Atoms[i];
                    for (int j = 0; j < numbonds; j++)
                    {
                        /* Bonds get recorded twice, so only pick one direction... */
                        if (iatom.neighbor[j] < i)
                        {
                            var atT = output.Atoms[iatom.neighbor[j]];
                            var nbond = new NInchiBond(atO, atT, (INCHI_BOND_TYPE)iatom.bond_type[j], (INCHI_BOND_STEREO)iatom.bond_stereo[j]);
                            output.Bonds.Add(nbond);
                        }
                    }
                }
            }
        }

        private static void CreateStereos(NInchiStructure output, int numstereo, IntPtr intPtrStereos)
        {
            var istereos = (Inchi_Stereo0D*)intPtrStereos.ToPointer();
            for (int i = 0; i < numstereo; i++)
            {
                // jobject atC, an0, an1, an2, an3, stereo;
                Inchi_Stereo0D istereo = istereos[i];

                NInchiAtom atC = null;
                if (istereo.central_atom != NO_ATOM)
                    atC = output.Atoms[istereo.central_atom];
                var an0 = output.Atoms[istereo.neighbor[0]];
                var an1 = output.Atoms[istereo.neighbor[1]];
                var an2 = output.Atoms[istereo.neighbor[2]];
                var an3 = output.Atoms[istereo.neighbor[3]];
                var stereo = new NInchiStereo0D(atC, an0, an1, an2, an3, (INCHI_STEREOTYPE)istereo.type, (INCHI_PARITY)istereo.parity);
                output.Stereos.Add(stereo);
            }
        }

        /// <summary>
        /// Generated 0D structure from an InChI string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static NInchiOutputStructure GetStructureFromInchi(NInchiInputInchi input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Null input");
            }

            var native_input = new Inchi_InputINCHI();

            var szInChI = Marshal.StringToHGlobalAnsi(input.Inchi);
            var szOptions = Marshal.StringToHGlobalAnsi(input.Options);
            try
            {
                native_input.szInChI = szInChI;
                native_input.szOptions = szOptions;

                var native_output = new Inchi_OutputStruct();

                var ret = GetStructFromINCHI(ref native_input, out native_output);

                NInchiOutputStructure output = new NInchiOutputStructure(ret,
                    Marshal.PtrToStringAnsi(native_output.szMessage),
                    Marshal.PtrToStringAnsi(native_output.szLog),
                    native_output.WarningFlags[0], native_output.WarningFlags[1],
                    native_output.WarningFlags[2], native_output.WarningFlags[3]);

                CreateAtoms(output, native_output.num_atoms, native_output.atom);
                CreateBonds(output, native_output.num_atoms, native_output.atom);
                CreateStereos(output, native_output.num_stereo0D, native_output.stereo0D);

                FreeStructFromINCHI(ref native_output);
                return output;
            }
            finally
            {
                Marshal.FreeHGlobal(szOptions);
                Marshal.FreeHGlobal(szInChI);
            }
        }

        /// <summary>
        /// Calculates the InChIKey for an InChI string.
        /// </summary>
        /// <param name="inchi">source InChI string</param>
        /// <returns>InChIKey output</returns>
        /// <exception cref="NInchiException"></exception>
        public static NInchiOutputKey GetInchiKey(string inchi)
        {
            if (inchi == null)
            {
                throw new ArgumentNullException(nameof(inchi), "Null InChI");
            }

            var szINCHIKey = Marshal.AllocHGlobal(28);
            var szXtra1 = Marshal.AllocHGlobal(65);
            var szXtra2 = Marshal.AllocHGlobal(65);
            var ret = GetINCHIKeyFromINCHI(inchi, 1, 2, szINCHIKey, szXtra1, szXtra2);

            NInchiOutputKey oo = new NInchiOutputKey(ret, Marshal.PtrToStringAnsi(szINCHIKey));

            Marshal.FreeHGlobal(szXtra2);
            Marshal.FreeHGlobal(szXtra1);
            Marshal.FreeHGlobal(szINCHIKey);
            return oo;
        }

        /// <summary>
        /// Checks whether a string represents valid InChIKey.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static INCHI_KEY_STATUS CheckInchiKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "Null InChI key");
            }
            var ret = CheckINCHIKey(key);
            return (INCHI_KEY_STATUS)ret;
        }

        /// <summary>
        /// <para>Checks if the string represents valid InChI/standard InChI.</para>
        /// </summary>
        /// <param name="inchi">source InChI</param>
        /// <param name="strict">if <see langword="false"/>, just briefly check for proper layout (prefix, version, etc.) The result
        ///               may not be strict.
        ///               If <see langword="true"/>, try to perform InChI2InChI conversion and returns success if a resulting
        ///               InChI string exactly match source. The result may be 'false alarm' due to imperfectness of</param>
        public static INCHI_STATUS CheckInchi(string inchi, bool strict)
        {
            if (inchi == null)
            {
                throw new ArgumentNullException(nameof(inchi), "Null InChI");
            }
            int ret = CheckINCHI(inchi, strict ? 1 : 0);
            return (INCHI_STATUS)ret;
        }

        public static NInchiInputData GetInputFromAuxInfo(string auxInfo)
        {
            if (auxInfo == null)
            {
                throw new ArgumentNullException(nameof(auxInfo), "Null AuxInfo");
            }

            var native_output = new InchiInpData();
            var iiii = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Inchi_Input)));
            try
            {
                native_output.pInp = iiii;

                int ret;
                ret = Get_inchi_Input_FromAuxInfo(auxInfo, 0, 0, out native_output);

                NInchiInput oos = new NInchiInput();
                Inchi_Input* pii = (Inchi_Input*)native_output.pInp.ToPointer();
                {
                    CreateAtoms(oos, pii->num_atoms, pii->atom);
                    CreateBonds(oos, pii->num_atoms, pii->atom);
                    CreateStereos(oos, pii->num_stereo0D, pii->stereo0D);
                }

                var oo = new NInchiInputData(ret, oos, native_output.bChiral, new string(native_output.szErrMsg));
                return oo;
            }
            finally
            {
                Free_inchi_Input((Inchi_Input*)iiii.ToPointer());
            }
        }
    }
}
