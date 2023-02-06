using System.Windows.Forms;
using GameOverlay.Drawing;

using GenshinImpactOverlay.GraphicWorkers;

namespace GenshinImpactOverlay.Menus;

internal class Menu
{
	private const string SYSNAME = nameof(Menu);

	/// <summary>
	/// Обработчик графики
	/// </summary>
	private GraphicsWorker Worker { get; set; }

	/// <summary>
	/// Коллекция с корневыми меню систем
	/// </summary>
	private List<MenuItem> Menus { get; set; } = new();
	
	/// <summary>
	/// Окрыто ли какое-либо меню
	/// </summary>
	private bool IsOpen { get; set; } = false;

	/// <summary>
	/// Заблокированы ли навигационные клавиши
	/// </summary>
	private bool IsLocked { get; set; } = false;

	/// <summary>
	/// Текущее меню и меню явлевшееся ему родительским
	/// </summary>
	private (MenuItem? parent, MenuItem node)? OpenNode { get; set; }

	/// <summary>
	/// Системы, которые имеют свои меню для реализации
	/// </summary>
	private List<IUseMenu> UseMenuSystems { get; init; }

	#region Resources
	private FontHandler FontIndex { get; init; }
	private SolidBrushHandler WhiteBrushIndex { get; init; }
	private SolidBrushHandler BlackBrushIndex { get; init; }
	#endregion Resources

	public Menu(GraphicsWorker worker, params IUseMenu[] useMenuSystems)
	{
		Worker = worker;

		FontIndex = Worker.AddFont("Consolas", 14);
		WhiteBrushIndex = Worker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrushIndex = Worker.AddSolidBrush(new Color(0, 0, 0));

		InputHook.OnKeyUp += InputHook_OnKeyUp;
		worker.OnDrawGraphics += Worker_OnDrawGraphics;
		UseMenuSystems = new(useMenuSystems);
	}

	/// <summary>
	/// Метод добавляющий новую систему для отрисоввки меню
	/// </summary>
	/// <param name="system"></param>
	/// <returns>Возвращает <see langword="true"/>, если система успешно добавлена или была добавлена ранее.</returns>
	public bool TryAddSystem(IUseMenu system)
	{
		if (UseMenuSystems.Any((arg) => arg == system)) return true;
		else UseMenuSystems.Add(system);

		return true;
	}

	private int _curNodeIndex;
	private void InputHook_OnKeyUp(object? sender, EventsArgs.OnKeyUpEventArgs eventArgs)
	{
		try
		{
            if (eventArgs.InputPriority >= InputPriorityEnum.Locked && eventArgs.System != SYSNAME) return;

            Keys key = eventArgs.Key;

			switch (key)
			{
				case Keys.Home:
					IsLocked = false;
					IsOpen = !IsOpen;
					_curNodeIndex = 0;
					break;
				case Keys.Up when IsOpen && !IsLocked:
					if (OpenNode is null)
					{
						MenuItem curNode = Menus[_curNodeIndex];
						List<MenuItem>? childs = curNode.ChildMenus;
						if (childs is not null && childs.Count > 0) OpenNode = (null, Menus[_curNodeIndex]);
						else goto default;
					}
					else
					{
						MenuItem curNode = OpenNode.Value.node.ChildMenus[_curNodeIndex];
						if (curNode.ChildMenus is not null && curNode.ChildMenus.Count > 0) OpenNode = (OpenNode.Value.node, curNode);
						else goto default;
					}
					break;
				case Keys.Right or Keys.Left when IsOpen && !IsLocked:
					List<MenuItem>? items = OpenNode is null ? Menus : OpenNode.Value.node.ChildMenus;
					if (items is not null && items.Count > 0)
					{
						if (key == Keys.Right)
						{
							if (items.Count == _curNodeIndex + 1) _curNodeIndex = 0;
							else _curNodeIndex++;
						}
						else
						{
							if (_curNodeIndex == 0) _curNodeIndex = items.Count - 1;
							else _curNodeIndex--;
						}
					}
					else goto default;
					break;
				case Keys.Down when IsOpen && !IsLocked:
					if (OpenNode is null) goto case Keys.Home;
					else if (OpenNode.Value.parent is null) OpenNode = null;
					else OpenNode = (null, OpenNode.Value.parent);
					break;
				default:
					if (!IsOpen) return;
					items = (OpenNode is null ? Menus : OpenNode.Value.node.ChildMenus) ?? throw new NullReferenceException();
					Console.WriteLine($"{items.Count} {_curNodeIndex}");
					if (items.Count <= _curNodeIndex) _curNodeIndex = 0;

					Dictionary<Keys, Action>? hotkeys = items[_curNodeIndex].Hotkeys;

					if (hotkeys is not null && hotkeys.TryGetValue(key, out Action? action))
					{
						Task task = new(action);
						task.Start();
					}
					break;
			}

			if (IsOpen)
			{
				InputHook.TrySetSystemLock(SYSNAME);
				if (OpenNode is null)
				{
					Menus = new();
					foreach (var item in UseMenuSystems)
					{
						var menu = item.GetMenu(UpdateMenu, KeyInputSwitchFunc);
						if (menu is not null) Menus.Add(menu);
					}
				}
				else Menus = OpenNode.Value.node.ChildMenus;
			}
			else
			{
				Menus.Clear();
				InputHook.TryClearSystemLock(SYSNAME);
			}
			
			void UpdateMenu(MenuItem? menu)
			{
				int index = Menus.FindIndex((item) => item.Name == menu.Name);

				if (index > -1) Menus[index] = menu;
			}

			void KeyInputSwitchFunc(bool? state = null) => IsLocked = state is null ? !IsLocked : state.Value;
		}
		catch(Exception e)
		{
			IsOpen = false;
			IsLocked = false;
			OpenNode = null;
			Menus?.Clear();
#if DEBUG
			Console.WriteLine(e.Message);
#endif
		}
	}

	private void Worker_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs eventArgs)
	{
		if (Menus is null || Menus.Count == 0) return;

		int left = 10;
		int bottom = 5;
		int width = 25;
		int height = 25;
		int pad = 10;

		var gfx = eventArgs.Graphics;

		try
		{
			Rectangle rect = new(left, bottom - height, left + width, bottom);
			if (OpenNode is not null) OpenNode.Value.node.DrawIcon(gfx, rect);

			for (int index = 0; index < Menus.Count; index++)
			{
				MenuItem menu = Menus[index];
				int hPad = index * (width + pad);
				int vPad = height + pad;
				rect = new(left + hPad, 768 - bottom - vPad - height, left + width + hPad, 768 - bottom - vPad);

				menu.DrawIcon(gfx, rect);

				if (index == _curNodeIndex && WhiteBrushIndex.IsInitialized) gfx.DrawCircle(
					WhiteBrushIndex, left + hPad + (width / 2), 768 - bottom - vPad - (height / 2), width / 2, 1f);
			}
		}
		catch(Exception e)
		{
#if DEBUG
			Console.WriteLine(e.Message);
#endif
		}
	}
}