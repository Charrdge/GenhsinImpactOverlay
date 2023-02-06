namespace GenshinImpactOverlay;

/// <summary>
/// Базовый класс для всех систем
/// </summary>
internal abstract class SystemBase
{
	/// <summary>
	/// Обработчик графики
	/// </summary>
	protected GraphicsWorker Graphics { get; init; }

	public SystemBase(GraphicsWorker graphics, Action<string> updateLoadStatus)
	{
		updateLoadStatus("Load graphic resources");

		Graphics = graphics;

		AddGraphicResources(graphics);

		Graphics.OnDrawGraphics += Graphics_OnDrawGraphics;

		InputHook.OnKeyUp += InputHook_OnKeyUp;
	}

	/// <summary>
	/// Метод отрисовки графики
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected abstract void Graphics_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e);

	/// <summary>
	/// Метод срабатывающий при отжатии клавиши пользователем
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="eventArgs"></param>
	protected abstract void InputHook_OnKeyUp(object? sender, EventsArgs.OnKeyUpEventArgs eventArgs);

	/// <summary>
	/// Метод в котором происходит добавление графических ресурсов
	/// </summary>
	/// <param name="graphics"></param>
	protected abstract void AddGraphicResources(GraphicsWorker graphics);
}