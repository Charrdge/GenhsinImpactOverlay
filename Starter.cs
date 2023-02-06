using System.Windows.Forms;
using GameOverlay.Drawing;
using GenshinImpactOverlay.EventsArgs;
using GenshinImpactOverlay.GraphicWorkers;
using GenshinImpactOverlay.Menus;

namespace GenshinImpactOverlay;

internal class Starter
{
	private const string SYSNAME = "Starter";

	GraphicsWorker GraphicsWorker { get; }

	Menu Menu { get; }

	#region Resources
	private FontHandler Font { get; init; }
	private SolidBrushHandler WhiteBrush { get; init; }
	private SolidBrushHandler BlackBrush { get; init; }
	#endregion Resources

	bool? MusicSystem { get; set; }
	bool? CooldownSystem { get; set; }
	bool? ImageBoardSystem { get; set; }

	Task? InitSystemTask { get; set; }

	public Starter(GraphicsWorker graphicsWorker, Menu menu, params Action<GraphicsWorker, Action<string>>[] systems)
	{
		GraphicsWorker = graphicsWorker;
		Menu = menu;

		Font = graphicsWorker.AddFont("Consolas", 14);
		WhiteBrush = graphicsWorker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrush = graphicsWorker.AddSolidBrush(new Color(0, 0, 0));

		GraphicsWorker.OnDrawGraphics += GraphicsWorker_OnDrawGraphics;

		InputHook.TrySetSystemLock(SYSNAME);

		InputHook.OnKeyUp += InputHook_OnKeyDown;
	}

	private void InputHook_OnKeyDown(object? _, OnKeyUpEventArgs eventArgs)
	{
		if (eventArgs.InputPriority >= InputPriorityEnum.Locked && eventArgs.System != SYSNAME) return;

		Keys key = eventArgs.Key;

		if (InitSystemTask is null)
		{
			if (key is Keys.Clear or Keys.NumPad5)
			{
				if (MusicSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						Music.YandexMusicSystem system = new(GraphicsWorker, (str) => { });

						MusicSystem = true;
						InitSystemTask = null;
						Menu.TryAddSystem(system);
					});
					InitSystemTask.Start();
				}
				else if (CooldownSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						var keys = new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 };
						var system = new Cooldowns.CooldownSystem(GraphicsWorker, keys, new TimeSpan(0, 1, 30));

						CooldownSystem = true;
						InitSystemTask = null;
						//Menu.TryAddSystem(system);
					});
					InitSystemTask.Start();
				}
				else if (ImageBoardSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						ImageBoard.DvachSystem system = new(GraphicsWorker, (str) => { });

						ImageBoardSystem = true;
						InitSystemTask = null;

						Menu.TryAddSystem(system);
					});
					InitSystemTask.Start();
				}
			}
			else if (key is Keys.Right or Keys.NumPad6)
			{
				if (MusicSystem is null) MusicSystem = false;
				else if (CooldownSystem is null) CooldownSystem = false;
				else if (ImageBoardSystem is null) ImageBoardSystem = false;
			}

			if (MusicSystem is not null && CooldownSystem is not null && ImageBoardSystem is not null)
			{
				InputHook.OnKeyUp -= InputHook_OnKeyDown;
				InputHook.TryClearSystemLock(SYSNAME);
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
		
		if (Font.IsInitialized &&
			WhiteBrush.IsInitialized &&
			BlackBrush.IsInitialized)
		{
			graphics.DrawTextWithBackground(
				Font,
				WhiteBrush,
				BlackBrush,
				 new Point(50, 50),
			text);
		}
	}
}