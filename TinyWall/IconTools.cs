// Building a Better ExtractAssociatedIcon
// Bradley Smith - 2010/07/28
// (updated 2014/11/13)

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Defines a set of utility methods for extracting icons for files and file 
/// types.
/// </summary>
public static class IconTools {

    /// <summary>
    /// Represents the different icon sizes that can be extracted using the 
    /// <see cref="IconTools.ExtractAssociatedIcon"/> method.
    /// </summary>
    internal enum ShellIconSize : uint
    {

        /// <summary>
        /// Specifies a small (16x16) icon.
        /// </summary>
        SmallIcon = IconTools.SHGFI_ICON | IconTools.SHGFI_SMALLICON,
        /// <summary>
        /// Specifies a large (32x32) icon.
        /// </summary>
        LargeIcon = IconTools.SHGFI_ICON | IconTools.SHGFI_LARGEICON
    }

    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

	#region Win32

	/// <summary>
	/// Retrieve the handle to the icon that represents the file and the index 
	/// of the icon within the system image list. The handle is copied to the 
	/// hIcon member of the structure specified by psfi, and the index is 
	/// copied to the iIcon member.
	/// </summary>
	internal const uint SHGFI_ICON = 0x100;
	/// <summary>
	/// Modify SHGFI_ICON, causing the function to retrieve the file's large 
	/// icon. The SHGFI_ICON flag must also be set.
	/// </summary>
	internal const uint SHGFI_LARGEICON = 0x0;
	/// <summary>
	/// Modify SHGFI_ICON, causing the function to retrieve the file's small 
	/// icon. Also used to modify SHGFI_SYSICONINDEX, causing the function to 
	/// return the handle to the system image list that contains small icon 
	/// images. The SHGFI_ICON and/or SHGFI_SYSICONINDEX flag must also be set.
	/// </summary>
	internal const uint SHGFI_SMALLICON = 0x1;
	/// <summary>
	/// Indicates that the function should not attempt to access the file 
	/// specified by pszPath.
	/// </summary>
	const uint SHGFI_USEFILEATTRIBUTES = 0x10;

	/// <summary>
	/// Contains the native Win32 functions.
	/// </summary>
	private class NativeMethods {

		/// <summary>
		/// Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.
		/// </summary>
		/// <param name="pszPath">A pointer to a null-terminated string of maximum length MAX_PATH that contains the path and file name. Both absolute and relative paths are valid.</param>
		/// <param name="dwFileAttributes">A combination of one or more file attribute flags (FILE_ATTRIBUTE_ values as defined in Winnt.h).</param>
		/// <param name="psfi">The address of a SHFILEINFO structure to receive the file information.</param>
		/// <param name="cbSizeFileInfo">The size, in bytes, of the SHFILEINFO structure pointed to by the psfi parameter.</param>
		/// <param name="uFlags">The flags that specify the file information to retrieve.</param>
		/// <returns>Nonzero if successful, or zero otherwise.</returns>
		[DllImport("shell32.dll")]
		public static extern IntPtr SHGetFileInfo(
			string pszPath, 
			uint dwFileAttributes, 
			ref SHFILEINFO psfi, 
			uint cbSizeFileInfo, 
			ShellIconSize uFlags
		);

		/// <summary>
		/// Destroys an icon and frees any memory the icon occupied.
		/// </summary>
		/// <param name="handle">A handle to the icon to be destroyed. The icon must not be in use.</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public extern static bool DestroyIcon(IntPtr handle);
	}

	/// <summary>
	/// Contains information about a file object.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct SHFILEINFO {
		/// <summary>
		/// A handle to the icon that represents the file.
		/// </summary>
		public IntPtr hIcon;
		/// <summary>
		/// The index of the icon image within the system image list.
		/// </summary>
		public IntPtr iIcon;
		/// <summary>
		/// An array of values that indicates the attributes of the file object.
		/// </summary>
		public uint dwAttributes;
		/// <summary>
		/// A string that contains the name of the file as it appears in the Windows Shell, or the path and file name of the file that contains the icon representing the file.
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		/// <summary>
		/// A string that describes the type of file.
		/// </summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	};

	#endregion

	/// <summary>
	/// Returns an icon representation of the specified file.
	/// </summary>
	/// <param name="filename">The path to the file.</param>
	/// <param name="size">The desired size of the icon.</param>
	/// <returns>An icon that represents the file.</returns>
	internal static Icon GetIconForFile(string filename, ShellIconSize size) {
		SHFILEINFO shinfo = new SHFILEINFO();
        NativeMethods.SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), size);

		Icon icon = null;

		if (shinfo.hIcon.ToInt32() != 0) {
			// create the icon from the native handle and make a managed copy of it
			icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();

			// release the native handle
			NativeMethods.DestroyIcon(shinfo.hIcon);
		}

		return icon;
	}

    /// <summary>
    /// Returns the default icon representation for files with the specified extension.
    /// </summary>
    /// <param name="extension">File extension (including the leading period).</param>
    /// <param name="size">The desired size of the icon.</param>
    /// <returns>The default icon for files with the specified extension.</returns>
    internal static Icon GetIconForExtension(string extension, ShellIconSize size) {
		// repeat the process used for files, but instruct the API not to access the file
		size |= (ShellIconSize)SHGFI_USEFILEATTRIBUTES;
		return GetIconForFile(extension, size);
	}
}