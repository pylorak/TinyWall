using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using pylorak.Utilities;

namespace pylorak.Windows
{
    public class Hotkey : Disposable, IMessageFilter    // TODO: Can IMessageFilter be removed?
	{
        private static class NativeMethods
        {
			public const uint WM_HOTKEY = 0x312;
			public const uint MOD_ALT = 0x1;
			public const uint MOD_CONTROL = 0x2;
			public const uint MOD_SHIFT = 0x4;
			public const uint MOD_WIN = 0x8;
			public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

            [DllImport("user32.dll", SetLastError = true)]
			public static extern int RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, System.Windows.Forms.Keys vk);

            [DllImport("user32.dll", SetLastError = true)]
			public static extern int UnregisterHotKey(IntPtr hWnd, int id);
        }

		private static int currentID;
		private const int maximumID = 0xBFFF;
		
		private Keys keyCode;
        private bool shift;
        private bool control;
        private bool alt;
		private bool windows;

		private int id;
		private bool registered;

        internal event HandledEventHandler? Pressed;

        internal Hotkey()
            : this(Keys.None, false, false, false, false)
		{
			// No work done here!
		}

        protected override void Dispose(bool disposing)
        {
			if (IsDisposed)
				return;

            if (disposing)
            {
                // Release managed resources
            }

            // Release unmanaged resources.
            // Set large fields to null.
            Unregister();

			// Call Dispose on your base class.
            base.Dispose(disposing);
        }

		~Hotkey() => Dispose(false);


		internal Hotkey(Keys keyCode, bool shift, bool control, bool alt, bool windows)
		{
			// Assign properties
			this.KeyCode = keyCode;
			this.Shift = shift;
			this.Control = control;
			this.Alt = alt;
			this.Windows = windows;

			// Register us as a message filter
			System.Windows.Forms.Application.AddMessageFilter(this);
		}

        internal bool Register()
        {
            // Check that we have not registered
			if (this.registered)
			{ throw new NotSupportedException("You cannot register a hotkey that is already registered"); }
        
			// We can't register an empty hotkey
			if (this.Empty)
			{ throw new NotSupportedException("You cannot register an empty hotkey"); }

			// Get an ID for the hotkey and increase current ID
			this.id = Hotkey.currentID;
			Hotkey.currentID = (Hotkey.currentID + 1) % Hotkey.maximumID;

			// Translate modifier keys into unmanaged version
            uint modifiers = (this.Alt ? NativeMethods.MOD_ALT : 0) | (this.Control ? NativeMethods.MOD_CONTROL : 0) |
                            (this.Shift ? NativeMethods.MOD_SHIFT : 0) | (this.Windows ? NativeMethods.MOD_WIN : 0);

			// Register the hotkey
            if (NativeMethods.RegisterHotKey(IntPtr.Zero, this.id, modifiers, keyCode) == 0)
			{ 
				// Is the error that the hotkey is registered?
                if (Marshal.GetLastWin32Error() == NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED)
				{ return false; }
				else
				{ throw new Win32Exception(); } 
			}

			// Save the control reference and register state
			this.registered = true;

			// We successfully registered
			return true;
		}

        internal void Unregister()
		{
			// Check that we have registered
			if (!this.registered)
//            { throw new NotSupportedException("You cannot unregister a hotkey that is not registered"); }
            { return; }

			// Clean up after ourselves
            if (NativeMethods.UnregisterHotKey(IntPtr.Zero, this.id) == 0)
			{ throw new Win32Exception(); }

			// Clear the control reference and register state
			this.registered = false;
		}

		private void Reregister()
		{
			// Only do something if the key is already registered
			if (!this.registered)
			{ return; }

			// Unregister and then reregister again
			this.Unregister();
			this.Register();
		}

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public bool PreFilterMessage(ref System.Windows.Forms.Message message)
		{
			// Only process WM_HOTKEY messages
            if (message.Msg != NativeMethods.WM_HOTKEY)
			{ return false; }

			// Check that the ID is our key and we are registerd
			if (this.registered && (message.WParam.ToInt32() == this.id))
			{
				// Fire the event and pass on the event if our handlers didn't handle it
				return this.OnPressed();
			}
			else
			{ return false; }
		}

		private bool OnPressed()
		{
			// Fire the event if we can
			var handledEventArgs = new HandledEventArgs(false);
            this.Pressed?.Invoke(this, handledEventArgs);

            // Return whether we handled the event or not
            return handledEventArgs.Handled;
		}

        public override string ToString()
        {
			// We can be empty
			if (this.Empty)
			{ return "(none)"; }

			// Build key name
			string keyName = Enum.GetName(typeof(Keys), this.keyCode);;
			switch (this.keyCode)
			{
				case Keys.D0:
				case Keys.D1:
				case Keys.D2:
				case Keys.D3:
				case Keys.D4:
				case Keys.D5:
				case Keys.D6:
				case Keys.D7:
				case Keys.D8:
				case Keys.D9:
					// Strip the first character
					keyName = keyName.Substring(1);
					break;
				default:
					// Leave everything alone
					break;
			}

            // Build modifiers
            string modifiers = "";
            if (this.shift)
            { modifiers += "Shift+"; }
            if (this.control)
            { modifiers += "Control+"; }
            if (this.alt)
            { modifiers += "Alt+"; }
			if (this.windows)
			{ modifiers += "Windows+"; }

			// Return result
            return modifiers + keyName;
        }

        internal bool Empty
		{
			get { return this.keyCode == Keys.None; }
		}

        internal bool Registered
		{
			get { return this.registered; }
		}

        internal Keys KeyCode
        {
            get { return this.keyCode; }
            set
			{
				// Save and reregister
				this.keyCode = value;
				this.Reregister();
			}
        }

        internal bool Shift
        {
            get { return this.shift; }
            set 
			{
				// Save and reregister
				this.shift = value;
				this.Reregister();
			}
        }

        internal bool Control
        {
            get { return this.control; }
            set
			{ 
				// Save and reregister
				this.control = value;
				this.Reregister();
			}
        }

        internal bool Alt
        {
            get { return this.alt; }
            set
			{ 
				// Save and reregister
				this.alt = value;
				this.Reregister();
			}
        }

        internal bool Windows
		{
			get { return this.windows; }
			set 
			{
				// Save and reregister
				this.windows = value;
				this.Reregister();
			}
		}
    }
}
