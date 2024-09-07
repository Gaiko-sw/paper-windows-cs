/// <summary>
/// Responsible for receiveing shell messages from windows and alerting its
/// WindowManager
/// </summary>
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
