/// <summary>
/// Uses KeyboardHook to get key press events, interpret them, and then call
/// WindowManager functions
/// </summary>

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
