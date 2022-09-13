using GenshinImpactOverlay.GraphicWorkers;
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
	public Dictionary<string, SolidBrushWorker> Brushes { get; } = new();
	
	/// <summary>
	/// Словарь шрифтов
	/// </summary>
	public Dictionary<string, FontWorker> Fonts { get; } = new();

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

	#region Graphic resource adding methods
	public void AddFont(string name, string fontFamilyName, float size, bool bold = false, bool italic = false, bool wordWrapping = false)
	{
		bool removed = Fonts.Remove(name, out FontWorker? oldValue);
		if (removed && oldValue is not null) oldValue.Dispose();

		bool added = Fonts.TryAdd(name, new FontWorker(fontFamilyName, size, bold, italic, wordWrapping));
		if (!added) throw new Exception("Fonts not added");
	}

	public void AddSolidBrush(string name, Color color)
	{
		bool removed = Brushes.Remove(name, out SolidBrushWorker oldValue);
		if (removed && oldValue is not null) oldValue.Dispose();

		bool added = Brushes.TryAdd(name, new SolidBrushWorker(color));
		if (!added) throw new Exception("Solid Brush not added");
	}
	#endregion

	#region Graphic event methods
	/// <summary>
	/// Метод установки графических настроек
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void Overlay_SetupGraphics(object? sender, SetupGraphicsEventArgs e)
	{
		var gfx = e.Graphics;

		if (e.RecreateResources)
		{
			foreach (var pair in Brushes) pair.Value.Dispose();
			foreach (var pair in Fonts) pair.Value.Dispose();
		}

		foreach (var pair in Brushes) pair.Value.Create(gfx);
		foreach (var pair in Fonts) pair.Value.Create(gfx);

		if (e.RecreateResources) return;
	}

	/// <summary>
	/// Метод отрисовки графики
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void Overlay_DrawGraphics(object? sender, DrawGraphicsEventArgs e)
	{
		var gfx = e.Graphics;

		gfx.ClearScene();

		OnDrawGraphics?.Invoke(this, gfx);
	}

	/// <summary>
	/// Метод отчистки графики
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
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