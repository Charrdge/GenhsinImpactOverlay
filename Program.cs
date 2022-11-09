using System.Windows.Forms;

#if DEBUG
Console.WriteLine("Hello, Debug world!");
string name = "devenv";
#else
Console.WriteLine("Hello, world!");
string name = "GenshinImpact";
#endif

IntPtr handle = System.Diagnostics.Process.GetProcessesByName(name).First().MainWindowHandle;

GameOverlay.TimerService.EnableHighPrecisionTimers();

Task hook = new(ButtonHook.Run);
hook.Start();

using (GraphicsWorker graphicsWorker = new(handle))
{
	var keys = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };
	var cooldownSystem = new CooldownSystem(graphicsWorker, keys, new TimeSpan(0, 1, 30));



	graphicsWorker.Run();
}