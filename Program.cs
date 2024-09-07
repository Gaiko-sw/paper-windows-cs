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