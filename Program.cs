// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

string name = "GenshinImpact";
IntPtr handle = System.Diagnostics.Process.GetProcessesByName(name).First().MainWindowHandle;

GameOverlay.TimerService.EnableHighPrecisionTimers();

Task hook = new Task(ButtonHook.Run);
hook.Start();

using (var example = new GraphicWorker(handle))
{
	example.Run();
}