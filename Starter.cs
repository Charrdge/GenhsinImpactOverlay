using System.Windows.Forms;

using GameOverlay.Drawing;

namespace GenshinImpactOverlay;

internal class Starter
{
	GraphicsWorker GraphicsWorker { get; }

	#region Resources
	private string FontIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	private string BlackBrushIndex { get; init; }
	#endregion Resources

	bool? MusicSystem { get; set; }
	bool? CooldownSystem { get; set; }
	bool? ImageBoardSystem { get; set; }

	Task? InitSystemTask { get; set; }

	public Starter(GraphicsWorker graphicsWorker)
	{
		GraphicsWorker = graphicsWorker;

		FontIndex = graphicsWorker.AddFont("Consolas", 14);
		WhiteBrushIndex = graphicsWorker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrushIndex = graphicsWorker.AddSolidBrush(new Color(0, 0, 0));

		GraphicsWorker.OnDrawGraphics += GraphicsWorker_OnDrawGraphics;

		InputHook.OnKeyDown += InputHook_OnKeyDown;
	}

	private void InputHook_OnKeyDown(Keys key)
	{
		if (InitSystemTask is null)
		{
			if (key == Keys.NumPad5)
			{
				if (MusicSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						_ = new Music.MusicSystem(GraphicsWorker);

						MusicSystem = true;
						InitSystemTask = null;
					});
					InitSystemTask.Start();
				}
				else if (CooldownSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						var keys = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };
						_ = new Cooldowns.CooldownSystem(GraphicsWorker, keys, new TimeSpan(0, 1, 30));

						CooldownSystem = true;
						InitSystemTask = null;
					});
					InitSystemTask.Start();
				}
				else if (ImageBoardSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						_ = new ImageBoard.ImageBoardSystem(GraphicsWorker);

						ImageBoardSystem = true;
						InitSystemTask = null;
					});
					InitSystemTask.Start();
				}
			}
			else if (key == Keys.NumPad6)
			{
				if (MusicSystem is null) MusicSystem = false;
				else if (CooldownSystem is null) CooldownSystem = false;
				else if (ImageBoardSystem is null) ImageBoardSystem = false;
			}
		}	

	}

	private void GraphicsWorker_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e)
	{
		var graphics = e.Graphics;

		string text = "";

		if (MusicSystem is null)
		{
			if (InitSystemTask is null) text = "Start Music player?\nNum5 - start\nNum6 - skip";
			else text = "Running music player";

		}
		else if (CooldownSystem is null)
		{
			if (InitSystemTask is null) text = "Show cooldown counter?\nNum5 - show\nNum6 - skip";
			else text = "Running cooldown counter";
		}
		else if (ImageBoardSystem is null)
		{
			if (InitSystemTask is null) text = "Show imageboard posting?\nNum5 - show\nNum6 - skip";
			else text = "Running imageboard posting";
		}
		else GraphicsWorker.OnDrawGraphics -= GraphicsWorker_OnDrawGraphics;

		graphics.DrawTextWithBackground(
			GraphicsWorker.Fonts[FontIndex],
			GraphicsWorker.Brushes[WhiteBrushIndex],
			GraphicsWorker.Brushes[BlackBrushIndex],
			 new Point(50, 50),
			 text);
	}
}