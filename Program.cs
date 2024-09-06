using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static partial class Win32
{
	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
	public static partial int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial int RegisterShellHookWindow(IntPtr hwnd);
	[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial int DeregisterShellHookWindow(IntPtr hwnd);

	[LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageA", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial uint RegisterWindowMessage(string lpString);

	public const int HSHELL_WINDOWCREATED = 1;

}

public partial class ShellHookWindow : Form
{
	private int WM_ShellHook_Message;
	
	public ShellHookWindow()
	{
		if (Win32.RegisterShellHookWindow(this.Handle) != 0) {
			WM_ShellHook_Message = (int)Win32.RegisterWindowMessage("SHELLHOOK");
		} else {
			// Application.Exit()
			throw new Exception();
		}
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		// Unregister the window when the form is closed
		Win32.DeregisterShellHookWindow(this.Handle);
		base.OnFormClosed(e);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == WM_ShellHook_Message)
		{
			int wParam = m.WParam.ToInt32();
			switch (wParam) {
				case Win32.HSHELL_WINDOWCREATED:
					Console.WriteLine($"New window created: {wParam}");
					NewWindow(m.LParam);
					break;
			}
		}
		base.WndProc(ref m);
	}

	public void NewWindow(IntPtr hwnd) {

	}

	[STAThread]
	public static void Main(string[] args)
	{
		// Invoke the function as a regular managed method.
		// Win32.MessageBox(IntPtr.Zero, "Command-line message box", "Attention!", 0);

		// ApplicationConfiguration.Initialize();
		Application.Run(new ShellHookWindow());
	}
}