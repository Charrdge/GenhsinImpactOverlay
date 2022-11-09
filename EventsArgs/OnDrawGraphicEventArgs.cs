using GameOverlay.Drawing;

namespace GenshinImpactOverlay.EventsArgs;

public class OnDrawGraphicEventArgs : EventArgs
{
	public Graphics Graphics { get; }

	public OnDrawGraphicEventArgs(Graphics graphics) => Graphics = graphics;
}