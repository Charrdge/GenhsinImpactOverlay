using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Windows.Forms;

internal class GraphicsWorker : IDisposable
{
	/// <summary>
	/// Объект через который происходит генерация графики
	/// </summary>
	private StickyWindow Overlay { get; init; }

	/// <summary>
	/// Словарь цветов
	/// </summary>
	public Dictionary<string, SolidBrush> Brushes { get; } = new();
	
	/// <summary>
	/// Словарь шрифтов
	/// </summary>
	public Dictionary<string, Font> Fonts { get; } = new();

	/// <summary>
	/// Создаёт новый экземпляр обработчика графики
	/// </summary>
	/// <param name="processhandle"></param>
	public GraphicsWorker(IntPtr processhandle)
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
	}

	~GraphicsWorker() => Dispose(false);

	public void Run()
	{
		Overlay.Create();
		Overlay.Join();
	}

	public delegate void DrawGraphic(object? sender, Graphics graphics);

	public event DrawGraphic? OnDrawGraphics;

	#region Graphic event methods
	private void Overlay_SetupGraphics(object? sender, SetupGraphicsEventArgs e)
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

	private void Overlay_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
	{
		var gfx = e.Graphics;

		gfx.ClearScene();

		OnDrawGraphics?.Invoke(this, gfx);
	}

	private void Overlay_DestroyGraphics(object? sender, DestroyGraphicsEventArgs e)
	{
		foreach (var pair in Brushes) pair.Value.Dispose();
		foreach (var pair in Fonts) pair.Value.Dispose();
	}
	#endregion

	#region IDisposable
	private bool _disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			Overlay.Dispose();

			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}