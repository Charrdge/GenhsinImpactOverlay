﻿using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

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
Task hook = new(ButtonHook.Run);
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

#region Foreground switching
[DllImport("user32.dll")]
static extern bool AllowSetForegroundWindow(int dwProcessId);

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

bool isAllowedSetForegroundWindow = AllowSetForegroundWindow(Environment.ProcessId);

#if DEBUG
Console.WriteLine(isAllowedSetForegroundWindow.ToString());
#endif

ButtonHook.OnKeyDown += (int vkCode) =>
{
	if ((Keys)vkCode == Keys.C)
	{
		bool foregroundWindowHasSet = SetForegroundWindow(overlayWindow);
#if DEBUG
		Console.WriteLine($"Set foreground window - {foregroundWindowHasSet}");
#endif
	}
};
#endregion

GameOverlay.TimerService.EnableHighPrecisionTimers();
using (GraphicsWorker graphicsWorker = new(handleWindow))
{
	var keys = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };
	//var cooldownSystem = new GenshinImpactOverlay.Cooldowns.CooldownSystem(graphicsWorker, keys, new TimeSpan(0, 1, 30));

	var chanSystem = new GenshinImpactOverlay.ImageBoard.ImageBoardSystem(graphicsWorker);

	graphicsWorker.Run();
}