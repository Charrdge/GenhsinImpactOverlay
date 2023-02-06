using GameOverlay.Drawing;

namespace GenshinImpactOverlay.EventsArgs;

/// <summary>
/// Аргументы события отрисовки графики
/// </summary>
public class OnDrawGraphicEventArgs : EventArgs
{
	/// <summary>
	/// Отрисовщик графики
	/// </summary>
	public Graphics Graphics { get; }

	/// <summary>
	/// Создаёт новый экземпляр типа с переданным отрисовщиком графики
	/// </summary>
	/// <param name="graphics"></param>
	public OnDrawGraphicEventArgs(Graphics graphics) => Graphics = graphics;
}