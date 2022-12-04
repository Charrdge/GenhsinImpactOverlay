using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using GenshinImpactOverlay.Map;

#region Hello
#if DEBUG
	Console.WriteLine("Hello, Debug world!");
string name = "devenv"; // PhotosApp devenv
#else
Console.WriteLine("Hello, world!");
string name = "GenshinImpact";
#endif
#endregion

#region Button hook
Task hook = new(InputHook.Run);
hook.Start();
#endregion

[DllImport("user32.dll")]
static extern bool IsWindow(IntPtr hWnd);

while (Process.GetProcessesByName(name).Length == 0 || !IsWindow(Process.GetProcessesByName(name).First().MainWindowHandle))
{
	Console.WriteLine($"Waiting for running {name}");
	Thread.Sleep(5000);
}

IntPtr handleWindow = Process.GetProcessesByName(name).First().MainWindowHandle;
IntPtr overlayWindow = Process.GetCurrentProcess().MainWindowHandle;

Console.WriteLine($"Get process {name} successfuly");

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

var handle = GetConsoleWindow();
ShowWindow(handle, 5);

bool showConsole = true;

InputHook.OnKeyUp += (_, eventArgs) =>
{
	Keys key = eventArgs.Key;

	if (key == Keys.NumPad8)
	{
		ShowWindow(handle, showConsole ? 0 : 5); // 0 - hide . 5 1- show
		showConsole = !showConsole;
	}
};

GameOverlay.TimerService.EnableHighPrecisionTimers();
using (GraphicsWorker graphicsWorker = new(handleWindow))
{
	var menu = new GenshinImpactOverlay.Menus.Menu(graphicsWorker);
	_ = new GenshinImpactOverlay.Starter(graphicsWorker, menu);

	graphicsWorker.Run();
}

Process.GetProcessesByName(name).First().WaitForExit();
Process.GetCurrentProcess().Kill();