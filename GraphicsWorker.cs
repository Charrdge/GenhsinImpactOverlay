using GameOverlay.Drawing;
using GameOverlay.Windows;
using GenshinImpactOverlay.GraphicWorkers;
using GenshinImpactOverlay.EventsArgs;
using System.Windows.Forms;

/// <summary>
/// Обработчик отображаемой графики
/// </summary>
internal class GraphicsWorker : IDisposable
{
	/// <summary>
	/// Объект через который происходит генерация графики
	/// </summary>
	private StickyWindow Overlay { get; init; }

	#region Resources
	/// <summary>
	/// Словарь цветов
	/// </summary>
	public Dictionary<string, SolidBrushHandler> Brushes { get; } = new();
	
	/// <summary>
	/// Словарь шрифтов
	/// </summary>
	public Dictionary<string, FontHandler> Fonts { get; } = new();
	#endregion Resources

	#region OnDrawGraphic event
	/// <summary>
	/// Делегат для события отрисовки графикк
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void DrawGraphic(object? sender, OnDrawGraphicEventArgs e);

	/// <summary>
	/// Событие отрисовки графики
	/// </summary>
	public event DrawGraphic? OnDrawGraphics;
	#endregion OnDrawGraphic event

	public bool IsHidden { get; private set; } = false;

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

		InputHook.OnKeyDown += (Keys key) =>
		{
			if (key == Keys.NumPad1) IsHidden = !IsHidden;
		};
	}

	~GraphicsWorker() => Dispose(false);

	/// <summary>
	/// Запускает отображение и обработку графики
	/// </summary>
	public void Run()
	{
		Overlay.Create();
		Overlay.Join();
	}

	#region Graphic resource adding methods
	/// <summary>
	/// Добавляет новый шрифт в коллекцию шрифтов
	/// </summary>
	/// <param name="fontFamilyName">Семейство шрифтов</param>
	/// <param name="size">Размер шрифта</param>
	/// <param name="bold"></param>
	/// <param name="italic"></param>
	/// <param name="wordWrapping"></param>
	/// <returns>Имя шрифта</returns>
	/// <exception cref="ArgumentException"></exception>
	public string AddFont(string fontFamilyName, float size, bool bold = false, bool italic = false, bool wordWrapping = false)
	{
		string name = GenerateName(fontFamilyName, size, bold, italic, wordWrapping);

		if (Fonts.ContainsKey(name)) return name;
		bool removed = Fonts.Remove(name, out FontHandler? oldValue);
		if (removed && oldValue is not null) oldValue.Dispose();

		bool added = Fonts.TryAdd(name, new FontHandler(fontFamilyName, size, bold, italic, wordWrapping));
		if (!added) throw new ArgumentException("Fonts not added");

		if (Overlay.IsInitialized) Overlay.Recreate();

		return name;

		static string GenerateName(string fontFamilyName, float size, bool bold, bool italic, bool wordWrapping)
		{
			string name = $"{fontFamilyName}{size}";
			if (bold) name += $"{bold}";
			if (italic) name += $"{italic}";
			if (wordWrapping) name += $"{wordWrapping}";
			return name;
		}
	}

	/// <summary>
	/// Добавляет новый цвет в коллекцию цветов
	/// </summary>
	/// <param name="name"></param>
	/// <param name="color"></param>
	/// <exception cref="ArgumentException">Возникает в случае неудачи при попытке добавить цвет в коллекцию</exception>
	public string AddSolidBrush(Color color)
	{
		string name = GenerateName(color);

		if (Brushes.ContainsKey(name)) return name;
		bool removed = Brushes.Remove(name, out SolidBrushHandler? oldValue);
		if (removed && oldValue is not null) oldValue.Dispose();

		bool added = Brushes.TryAdd(name, new SolidBrushHandler(color));
		if (!added) throw new ArgumentException("Solid Brush not added", nameof(color));

		if (Overlay.IsInitialized) Overlay.Recreate();

		return name;

		static string GenerateName(Color color) => $"{color.A}:{color.R}:{color.G}:{color.B}";
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

		if (!IsHidden) OnDrawGraphics?.Invoke(this, new OnDrawGraphicEventArgs(gfx));
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