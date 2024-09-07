/// <summary>
/// Keeps track of windows and controls them.
/// </summary>

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
		Win32.SetForegroundWindow(windows_[index].hwnd);
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
		
		else if (focusedWindowIndex_ < windows_.Count - 1) {
			FocusWindowAtIndex(focusedWindowIndex_ + 1);
		}
		else {
			FocusWindowAtIndex(focusedWindowIndex_);
		}

		Console.WriteLine($"Focused Next, index: {focusedWindowIndex_}");
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
		Console.WriteLine($"Focused Prev, index: {focusedWindowIndex_}");
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
