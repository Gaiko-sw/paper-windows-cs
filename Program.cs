using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

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
				case Win32.Msg.Destroy:
					Console.WriteLine($"Msg.Destroy. Win ID: {m.LParam}");
					wm_.RemoveWindow(m.LParam);
					break;
				case Win32.Msg.Move:
					Console.WriteLine($"Msg.Move. Win ID: {m.LParam}");
					wm_.RemoveWindow(m.LParam);
					break;
				case Win32.Msg.Size:
					Console.WriteLine($"Msg.Size. Win ID: {m.LParam}");
					break;
				case Win32.Msg.Killfocus:
					Console.WriteLine($"Msg.Killfocus. Win ID: {m.LParam}");
					break;
				case Win32.Msg.Activate:
				case Win32.Msg.Setfocus:
				case Win32.Msg.Focus:
					Console.WriteLine($"Msg.Focus. Win ID: {m.LParam}");
					wm_.FocusWindow(m.LParam);
					break;
				default:
					// Console.WriteLine($"Unhandled message {m.WParam}");
					break;
			}
		}
		else if (m.Msg == 0x0312) {

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
	private int startX_ = 0;

	private int getScreenHeight => Screen.PrimaryScreen.Bounds.Height;
	private int getScreenWidth => Screen.PrimaryScreen.Bounds.Width;

	public WindowManager() {

	}

	public bool IsWindowManaged(IntPtr hwnd) {
		return windows_.Any(w => w.hwnd == hwnd);
	}

	public int? GetWindowIndex(IntPtr hwnd) {
		foreach (var (win, i) in windows_.Select((win, i) => (win, i)))
		{
			if (win.hwnd == hwnd) {
				return i;
			}
		}
		return null;
	}

	public void NewWindow(IntPtr? hwnd = null)
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

	public void RemoveWindow(IntPtr hwnd)
	{
		Console.WriteLine("RemoveWindow");
		foreach (var (win, i) in windows_.Select((w, i) => (w, i)))
		{
			if (win.hwnd == hwnd)
			{
				if (i == focusedWindowIndex_)
				{
					focusedWindowIndex_--;
				}
				windows_.RemoveAt(i);
				Console.WriteLine($"Remove found window. Reflowing");
				ReflowWindows();
				break;
			}
		}
	}

	public void FocusWindow(IntPtr hwnd) {
		var i = GetWindowIndex(hwnd);
		if (i == null) { return; }
		int index = i.Value;
		FocusWindowAtIndex(index);
	}

	public void FocusWindowAtIndex(int index) {
		focusedWindowIndex_ = index;
		Win32.SetActiveWindow(windows_[index].hwnd);
		// todo replace scrolls with a DoOptionScroll() which can do other things
		ScrollWindowOnScreen(index);
	}

	public void FocusNext(){
		Console.WriteLine("FocusNext");

		if (focusedWindowIndex_ == -1)
		{
			if (windows_.Count > 0)
			{
				FocusWindowAtIndex(windows_.Count - 1);
			}
		}
		
		else if (focusedWindowIndex_ < windows_.Count) {
			FocusWindowAtIndex(focusedWindowIndex_ + 1);
		}
		else {
			FocusWindowAtIndex(focusedWindowIndex_);
		}
	}

	public void FocusPrev(){
		Console.WriteLine("FocusPrev");


		if (focusedWindowIndex_ == -1)
		{
			if (windows_.Count > 0)
			{
				FocusWindowAtIndex(0);
			}
		}
		else if (focusedWindowIndex_ > 0) {
			FocusWindowAtIndex(focusedWindowIndex_ - 1);
		}
		else {
			FocusWindowAtIndex(focusedWindowIndex_);
		}
	}

	/// <summary>
	/// Moves windows. Sets window manager's startX if provided
	/// </summary>
	/// <param name="newStartX"></param>
	public void ReflowWindows(int? newStartX = null) {
		if (newStartX != null) {
			startX_ = newStartX.Value;
		}
		var currentX = startX_;
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

	public void Scroll(int amount) {
		Console.WriteLine("Scroll");
		startX_ = startX_ + amount;
		ReflowWindows();
	}

	public void ScrollWindowOnScreen(int? index) {
		if (index == null) {
			index = focusedWindowIndex_;
		}
		int i = index.Value;
		var hwnd = windows_[i].hwnd;

		var r = Win32.GetWindowRect(hwnd);
		if (r == null) { return; }
		Rect rect = r.Value;

		if (rect.Left < 0) {
			PlaceWindow(i, 0);
		}
		else if (rect.Right > getScreenWidth) {
			PlaceWindow(i, getScreenWidth - rect.Width);
		}
	}

	private int getPriorWidth(int index) {
		int prior_width = 0;
		foreach (var (win, i) in windows_.Select((win, i) => (win, i)))
		{
			if (i == focusedWindowIndex_)
			{
				break;
			}
			var r = Win32.GetWindowRect(win.hwnd);
			if (r == null)
			{
				// todo remove that window?
				// return;
			}
			Rect rect = r.Value;
			prior_width += rect.Width;
		}
		return prior_width;
	}

	public void PlaceWindow(int index, int newWindowX) {
		var prior_width = getPriorWidth(index);
		ReflowWindows(-prior_width + newWindowX);
	}

	public void CentreCurrentWindow() {
		var hwnd = Win32.GetActiveWindow();
		var r = Win32.GetWindowRect(hwnd);
		if (r == null) {
			// todo remove window, focus other?
			// todo also check if the current window is managed
			return;
		}
		Rect rect = r.Value;
		var centring_width = getScreenWidth / 2;
		centring_width -= rect.Width / 2;

		var i = GetWindowIndex(hwnd);
		var prior_width = getPriorWidth(i.Value);
		ReflowWindows(centring_width - prior_width);
	}
}

public class KeyboardManager {
	private KeyboardHook hook;
	private WindowManager wm;

	public KeyboardManager(WindowManager wm) {
		this.wm = wm;
		hook = new KeyboardHook(true);
		hook.KeyDown += On_KeyDown;
	}

	private void On_KeyDown(Keys key, bool shift, bool ctrl, bool alt)
    {
        Console.WriteLine($"{key}, {shift}, {ctrl}, {alt}");
		if (alt) {
			switch (key) {
			case Keys.Insert:
				wm.NewWindow();
				break;
			case Keys.Home:
				wm.Scroll(-300);
				break;
			case Keys.PageUp:
				wm.CentreCurrentWindow();
				break;
			case Keys.Delete:
				wm.FocusPrev();
				break;
			case Keys.End:
				wm.Scroll(300);
				break;
			case Keys.PageDown:
				wm.FocusNext();
				break;
			}
		}
    }
}

static class Program {

	[STAThread]
	public static void Main(string[] args)
	{
		var wm = new WindowManager();
		var keyboard = new KeyboardManager(wm);
		// ApplicationConfiguration.Initialize();
		Application.Run(new ShellHookWindow(wm));
	}
}

internal class WinodwManager
{
}