using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

public static partial class Win32
{
	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
	public static partial int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

	// Register
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
	public static partial nint GetActiveWindow();
	[LibraryImport("user32.dll")]
	public static partial int GetWindowRect(IntPtr hWnd, out Rect lpRect);
	public static Rect? GetWindowRect(IntPtr hWnd) {
		var ret = new Rect();
		int success = GetWindowRect(hWnd, out ret);
		
		return success == 1 ? ret : null;
	}

	public enum Msg {
		Create = 1,
		Destroy = 2,
		Move = 3,
		Size = 5,
		Activate = 6,
		Setfocus = 7,
		Killfocus = 8,
		Focus = 32772,
	}

}

public partial class ShellHookWindow : Form
{
	private NotifyIcon notifyIcon;
	private int WM_ShellHook_Message = 0;

	private WindowManager wm_;
	
	public ShellHookWindow(WindowManager wm)
	{
		wm_ = wm;

		notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Exit_Action);

		this.Hide();
	}

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
		Win32.RegisterShellHookWindow(this.Handle);
		// if (Win32.RegisterShellHookWindow(this.Handle) != 0) {
		// 	WM_ShellHook_Message = (int)Win32.RegisterWindowMessage("SHELLHOOK");
		// }
		// else {
		// 	Application.Exit();
		// }
	}

    private void Exit_Action(object sender, EventArgs e)
	{
		notifyIcon.Visible = false;
		Application.Exit();
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		// Win32.DeregisterShellHookWindow(this.Handle);
		base.OnFormClosed(e);
	}

	protected override void WndProc(ref Message m)
	{
		Console.WriteLine($"{m.Msg},    {m.WParam},     {m.LParam}");
		// if (m.Msg == WM_ShellHook_Message)
		if (m.Msg == 49192)
		{
			switch ((Win32.Msg)m.WParam) {
				case Win32.Msg.Create:
					Console.WriteLine($"Msg.Create. Win ID: {m.LParam}");
					if (m.LParam == this.Handle) {return;}
					wm_.NewWindow(m.LParam);
					break;
				default:
					// Console.WriteLine($"Unhandled message {m.WParam}");
					break;
			}
		}
		base.WndProc(ref m);
	}

}

public struct Window {
	public IntPtr hwnd;
}
public struct Rect {
	public int Left;
	public int Top;
	public int Right;
	public int Bottom;
	public int Width => Right - Left;
	public int Height => Bottom - Top;
}
public class WindowManager {
	private List<Window> windows_ = new();
	private int focusedWindowIndex_ = -1;

	private int getScreenHeight => Screen.PrimaryScreen.Bounds.Height;

	public WindowManager() {

	}

	public void NewWindow(IntPtr? hwnd)
	{
		if (hwnd == null) {
			hwnd = Win32.GetActiveWindow();
		}
		if (hwnd == null) {
			return;
		} 

		if (windows_.Any(w => w.hwnd == hwnd)) {
			return;
		}

		var r = Win32.GetWindowRect(hwnd.Value);
		if (r == null) { return; }
		Rect rect = r.Value;
		if (rect.Height < getScreenHeight) {
			// return;
		}

		int index;
		if (focusedWindowIndex_ == -1) {
			index = windows_.Count;
		} else {
			index = focusedWindowIndex_ + 1;
		}

		// if (index > windows_.Count) {
		// 	index = windows_.Count
		// }
		Console.WriteLine($"Inserting new window at {index}");
		windows_.Insert(index, new Window(){hwnd = hwnd.Value});

		ReflowWindows();
		FocusWindowAtIndex(index);
		// ScrollWindowOnScreen();
	}

	public void ReflowWindows(int startX = 0) {
		var currentX = startX;
		foreach (var window in windows_) {
			var r = Win32.GetWindowRect(window.hwnd);
			if (r == null) {
				// todo remove this window
				return;
			}
			Rect rect = r.Value;
			Win32.MoveWindow(window.hwnd, currentX, 0, rect.Width, getScreenHeight);
			currentX += rect.Width;
		}
	}

	public void FocusWindowAtIndex(int index) {

	}

	public void RemoveWindow(IntPtr hwnd) {
		foreach (var (win, i) in windows_.Select((w, i) => (w, i))) {
			if (win.hwnd == hwnd) {
				if (i == focusedWindowIndex_) {
					focusedWindowIndex_--;
				}
				windows_.RemoveAt(i);
				Console.WriteLine($"Remove found window. Reflowing");
				ReflowWindows();
				break;
			}
		}
	}
}

static class Program {
	[STAThread]
	public static void Main(string[] args)
	{
		// Invoke the function as a regular managed method.
		// Win32.MessageBox(IntPtr.Zero, "Command-line message box", "Attention!", 0);

		var wm = new WindowManager();
		// ApplicationConfiguration.Initialize();
		Application.Run(new ShellHookWindow(wm));
	}
}