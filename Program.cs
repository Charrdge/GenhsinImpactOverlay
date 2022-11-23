﻿using System.Runtime.InteropServices;
using System.Windows.Forms;

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

IntPtr handleWindow = System.Diagnostics.Process.GetProcessesByName(name).First().MainWindowHandle;
IntPtr overlayWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

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