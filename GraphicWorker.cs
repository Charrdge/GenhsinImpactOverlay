using GameOverlay.Drawing;
using GameOverlay.Windows;

using System.Text;
using System.Windows.Forms;

internal class GraphicWorker : IDisposable
{
	StickyWindow Overlay { get; init; }

	Dictionary<string, SolidBrush> Brushes { get; } = new();
	Dictionary<string, Font> Fonts { get; } = new();

	Dictionary<Keys, DateTime> LastClickTimes = new();

	Keys LastChar = Keys.Space;

	public GraphicWorker(IntPtr processhandle)
	{
		Graphics gfx = new()
		{
			MeasureFPS = true,
			PerPrimitiveAntiAliasing = true,
			TextAntiAliasing = true
		};

		Overlay = new(processhandle, gfx)
		{
			FPS = 15,
			IsTopmost = true,
			IsVisible = true
		};

		Overlay.SetupGraphics += Overlay_SetupGraphics;
		Overlay.DrawGraphics += Overlay_DrawGraphics;
		Overlay.DestroyGraphics += Overlay_DestroyGraphics;

		LastClickTimes.Add(Keys.D1, DateTime.Now);
		LastClickTimes.Add(Keys.D2, DateTime.Now);
		LastClickTimes.Add(Keys.D3, DateTime.Now);
		LastClickTimes.Add(Keys.D4, DateTime.Now);

		ButtonHook.OnKeyDown += (vkCode) =>
		{
			var key = (Keys)vkCode;

			if (LastClickTimes.ContainsKey(key)) LastChar = key;
			else if (key == Keys.E) LastClickTimes[LastChar] = DateTime.Now;
		};
	}

	~GraphicWorker() => Dispose(false);

	public void Run()
	{
		Overlay.Create();
		Overlay.Join();
	}

	#region Graphic event methods
	void Overlay_SetupGraphics(object? sender, SetupGraphicsEventArgs e)
	{
		var gfx = e.Graphics;

		if (e.RecreateResources)
		{
			foreach (var pair in Brushes) pair.Value.Dispose();
			foreach (var pair in Fonts) pair.Value.Dispose();
		}

		Brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
		Brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);

		Fonts["consolas"] = gfx.CreateFont("Consolas", 14);

		if (e.RecreateResources) return;
	}

	void Overlay_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
	{
		var gfx = e.Graphics;

		var text = (Keys key) => 
		{
			TimeSpan span = DateTime.Now - LastClickTimes[key];
			return $"{span.Minutes}:{span.Seconds}:{span.Milliseconds}";
		};

		gfx.ClearScene();

		int v = 1300;
		int h = 225;
		int p = 55;

		gfx.DrawTextWithBackground(Fonts["consolas"], Brushes["white"], Brushes["black"], v, h, text(Keys.D1));
		gfx.DrawTextWithBackground(Fonts["consolas"], Brushes["white"], Brushes["black"], v, h + p, text(Keys.D2));
		gfx.DrawTextWithBackground(Fonts["consolas"], Brushes["white"], Brushes["black"], v, h + (p * 2), text(Keys.D3));
		gfx.DrawTextWithBackground(Fonts["consolas"], Brushes["white"], Brushes["black"], v, h + (p * 3), text(Keys.D4));
	}

	void Overlay_DestroyGraphics(object? sender, DestroyGraphicsEventArgs e)
	{
		foreach (var pair in Brushes) pair.Value.Dispose();
		foreach (var pair in Fonts) pair.Value.Dispose();
	}
	#endregion

	#region IDisposable
	private bool disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			Overlay.Dispose();

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}