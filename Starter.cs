﻿using System.Windows.Forms;
using GameOverlay.Drawing;
using GenshinImpactOverlay.EventsArgs;
using GenshinImpactOverlay.Menus;

namespace GenshinImpactOverlay;

internal class Starter
{
	private const string SYSNAME = "Starter";

	GraphicsWorker GraphicsWorker { get; }

	Menu Menu { get; }

	#region Resources
	private string FontIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	private string BlackBrushIndex { get; init; }
	#endregion Resources

	bool? MusicSystem { get; set; }
	bool? CooldownSystem { get; set; }
	bool? ImageBoardSystem { get; set; }

	Task? InitSystemTask { get; set; }

	public Starter(GraphicsWorker graphicsWorker, Menu menu)
	{
		GraphicsWorker = graphicsWorker;
		Menu = menu;

		FontIndex = graphicsWorker.AddFont("Consolas", 14);
		WhiteBrushIndex = graphicsWorker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrushIndex = graphicsWorker.AddSolidBrush(new Color(0, 0, 0));

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
			if (key == Keys.NumPad5)
			{
				if (MusicSystem is null)
				{
					InitSystemTask = new Task(() =>
					{
						Music.MusicSystem system = new(GraphicsWorker);

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
						ImageBoard.ImageBoardSystem system = new(GraphicsWorker);

						ImageBoardSystem = true;
						InitSystemTask = null;

						Menu.TryAddSystem(system);
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
		
		if (GraphicsWorker.Fonts[FontIndex].IsInitialized &&
			GraphicsWorker.Brushes[WhiteBrushIndex].IsInitialized &&
			GraphicsWorker.Brushes[BlackBrushIndex].IsInitialized)
		{
			graphics.DrawTextWithBackground(
				GraphicsWorker.Fonts[FontIndex],
				GraphicsWorker.Brushes[WhiteBrushIndex],
				GraphicsWorker.Brushes[BlackBrushIndex],
				 new Point(50, 50),
				 text);
		}

	}
}