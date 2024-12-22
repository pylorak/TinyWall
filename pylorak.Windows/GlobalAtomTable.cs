using System;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace pylorak.Windows
{
    public static class GlobalAtomTable
    {
        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            public const int ERROR_SUCCESS = 0;
            public const int ERROR_FILE_NOT_FOUND = 2;
            public const int ERROR_INVALID_HANDLE = 6;
            public const int ERROR_INVALID_PARAMETER = 87;
            public const int MAX_ATOM_NAME_LENGTH = 256;   // 255 + null temrinator

            [DllImport("kernel32", SetLastError = true)]
            public static extern void SetLastError(uint dwErrorCode);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern ushort GlobalAddAtom([In] string lpString);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern ushort GlobalFindAtom([In] string lpString);

            [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
            public static extern ushort GlobalDeleteAtom(ushort nAtom);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern uint GlobalGetAtomName(ushort nAtom, [Out] StringBuilder lpBuffer, int nSize);
        }

        public class AtomNotFoundException : Win32Exception
        {
            public AtomNotFoundException() : base(NativeMethods.ERROR_FILE_NOT_FOUND, "The atom name was not found.")
            { }
        }
        public class InvalidAtomNameException : Win32Exception
        {
            public InvalidAtomNameException() : base(NativeMethods.ERROR_INVALID_PARAMETER, "Invalid atom name.")
            { }
        }
        public class InvalidAtomHandleException : Win32Exception
        {
            public InvalidAtomHandleException() : base(NativeMethods.ERROR_INVALID_HANDLE, "Invalid atom handle.")
            { }
        }

        private static void TranslateWin32LastError()
        {
            int win32err = Marshal.GetLastWin32Error();
            if (NativeMethods.ERROR_SUCCESS != win32err)
            {
                throw win32err switch
                {
                    NativeMethods.ERROR_FILE_NOT_FOUND => new AtomNotFoundException(),
                    NativeMethods.ERROR_INVALID_HANDLE => new InvalidAtomHandleException(),
                    NativeMethods.ERROR_INVALID_PARAMETER => new InvalidAtomNameException(),
                    _ => new Win32Exception(),
                };
            }
        }

        public static ushort Add(string name)
        {
            var ret = NativeMethods.GlobalAddAtom(name);
            if (0 == ret)
                TranslateWin32LastError();
            return ret;
        }

        public static ushort Find(string name)
        {
            var ret = NativeMethods.GlobalFindAtom(name);
            if (0 == ret)
                TranslateWin32LastError();
            return ret;
        }

        public static bool Exists(string name)
        {
            try
            {
                return 0 != Find(name);
            }
            catch (AtomNotFoundException)
            {
                return false;
            }
        }

        public static void Delete(ushort atom)
        {
            NativeMethods.SetLastError(NativeMethods.ERROR_SUCCESS);
            NativeMethods.GlobalDeleteAtom(atom);
            TranslateWin32LastError();
        }

        public static void Delete(string name)
        {
            Delete(Find(name));
        }

        public static string GetName(ushort atom)
        {
            var buffer = new StringBuilder(NativeMethods.MAX_ATOM_NAME_LENGTH);
            var ret = NativeMethods.GlobalGetAtomName(atom, buffer, buffer.Capacity);
            if (0 == ret)
                TranslateWin32LastError();

            return buffer.ToString(0, (int)ret);
        }

    }

}
