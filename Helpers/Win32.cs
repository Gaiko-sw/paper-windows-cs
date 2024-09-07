using System.Runtime.InteropServices;

public static partial class Win32
{
	// todo fix marshalling bool returns. 
	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
	public static partial int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

	// Shell Registering
	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial int RegisterShellHookWindow(IntPtr hwnd);
	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial int DeregisterShellHookWindow(IntPtr hwnd);
	[LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageA", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial uint RegisterWindowMessage(string lpString);

	// Window manipulation
	// not sure difference between setwindowpos & movewindow
	// [LibraryImport("user32.dll")]
	// public static partial uint SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
	[LibraryImport("user32.dll")]
	public static partial int MoveWindow(IntPtr hwnd, int x, int y, int nwidth, int nheight, int bRepaint = 1);
	[LibraryImport("user32.dll")]
	// todo marshall this IntPtr? return type. dunno what happens if it's null
	public static partial IntPtr SetActiveWindow(IntPtr hwnd);
	[LibraryImport("user32.dll")]
	public static partial IntPtr SetForegroundWindow(IntPtr hwnd);
	[LibraryImport("user32.dll")]
	public static partial nint GetActiveWindow();
	[LibraryImport("user32.dll")]
	public static partial int GetWindowRect(IntPtr hWnd, out Rect lpRect);
	public static Rect? GetWindowRect(IntPtr hWnd) {
		var ret = new Rect();
		int success = GetWindowRect(hWnd, out ret);
		
		return success == 1 ? ret : null;
	}

	// Keyboard
	[LibraryImport("user32.dll")]
	private static partial int RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
	[LibraryImport("user32.dll")]
	private static partial int UnregisterHotKey(IntPtr hWnd, int id);

	public enum Msg {
		Create = 1,
		Destroy = 2,
		Move = 3,
		Size = 5,
		Activate = 6,
		Setfocus = 7,
		Killfocus = 8,
		Focus = 32772,

		Hotkey = 0x0312,
	}

}
