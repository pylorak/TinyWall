// Building a Better ExtractAssociatedIcon
// Bradley Smith - 2010/07/28
// (updated 2014/11/13)

using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using pylorak.Utilities;

namespace pylorak.Windows
{
	public static class IconTools
	{
		public enum ShellIconSize : int
		{
			// Small (16x16) icon.
			SmallIcon = IconTools.SHGFI_ICON | IconTools.SHGFI_SMALLICON,
			// Large (32x32) icon.
			LargeIcon = IconTools.SHGFI_ICON | IconTools.SHGFI_LARGEICON
		}

		private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

		#region Win32

		private const int SHGFI_ICON = 0x100;
		private const int SHGFI_LARGEICON = 0x0;
		private const int SHGFI_SMALLICON = 0x1;
		private const int SHGFI_USEFILEATTRIBUTES = 0x10;

		[SuppressUnmanagedCodeSecurity]
		private class NativeMethods
		{
			[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr SHGetFileInfo(
				string pszPath,
				uint dwFileAttributes,
				ref SHFILEINFO psfi,
				uint cbSizeFileInfo,
				ShellIconSize uFlags
			);

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public extern static bool DestroyIcon(IntPtr handle);
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		};

		#endregion

		internal static Icon GetIconForFile(string filename, ShellIconSize size)
		{
			var shinfo = new SHFILEINFO();
			NativeMethods.SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), size);

			if (shinfo.hIcon != IntPtr.Zero)
			{
				try
				{
					return (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
				}
				finally
				{
                    NativeMethods.DestroyIcon(shinfo.hIcon);
                }
            }

			throw new UnexpectedResultExceptions(nameof(NativeMethods.SHGetFileInfo));
		}

		internal static Icon GetIconForExtension(string extension, ShellIconSize size)
		{
            size |= (ShellIconSize)SHGFI_USEFILEATTRIBUTES;
            try
            {
                return GetIconForFile(extension, size);
			}
			catch
			{
				var ext = Path.GetExtension(extension);
				if (string.IsNullOrEmpty(ext))
				{
					// If we cannot determine the file extension, assume an executable
					return GetIconForFile("file.exe", size);
				}
				else
				{
					// Get generic icon for whatever file extension was specified
					return GetIconForFile("file" + ext, size);
				}
            }
        }
	}
}