using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using GenshinImpactOverlay.EventsArgs;

internal static class InputHook
{
	#region DllImport
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);
	#endregion DllImport

	#region Priorities
	public static InputPriorityEnum InputPriority { get; private set; } = InputPriorityEnum.Normal;

	public static string? SystemInputName { get; private set; }

	public static bool TrySwitchPriority(InputPriorityEnum priority)
	{
		InputPriority = priority;
		return true;
	}

	public static bool TryClearSystemLock(string systemName)
	{
		if (SystemInputName == systemName) SystemInputName = null;
		else return false;

		if (InputPriority == InputPriorityEnum.System) InputPriority = InputPriorityEnum.Normal;
		return true;
	}

	public static bool TrySetSystemLock(string systemName)
	{
		if (SystemInputName is null) SystemInputName = systemName;
		else return false;

		InputPriority = InputPriorityEnum.System;
		return true;
	}
	#endregion Priorities

	/// <summary>
	/// Callback делегат получения нажатий клавиш. 
	/// </summary>
	/// <param name="nCode"></param>
	/// <param name="wParam"></param>
	/// <param name="lParam"></param>
	/// <returns></returns>
	private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

	#region Keyboard
	#region Struct
	[Flags]
	public enum KBDLLHOOKSTRUCTFlags : uint
	{
		LLKHF_EXTENDED = 0x01,
		LLKHF_INJECTED = 0x10,
		LLKHF_ALTDOWN = 0x20,
		LLKHF_UP = 0x80,
	}

	[StructLayout(LayoutKind.Sequential)]
	public class KBDLLHOOKSTRUCT
	{
		public uint vkCode;
		public uint scanCode;
		public KBDLLHOOKSTRUCTFlags flags;
		public uint time;
		public UIntPtr dwExtraInfo;
	}
	#endregion Struct

	/// <summary>
	/// Идентификатор перехвата для получения низкоуровневых событий клавиатуры
	/// </summary>
	private const int WH_KEYBOARD_LL = 13;

	/// <summary>
	/// Поле хранящая делегат функции получения нажатий клавиш
	/// </summary>
	private static LowLevelProc _keyboardProc = KeyboardHookCallback;
	/// <summary>
	/// Идентификатор callback получения нажатий от системы
	/// </summary>
	private static IntPtr _keyboardHookID = IntPtr.Zero;

	public delegate void KeyUpDelegate(object? sender, OnKeyUpEventArgs eventArgs);
	public static event KeyUpDelegate? OnKeyUp;
	
	public static bool IsHandleInputLocked { get; private set; } = false;

	private static IntPtr SetKeyboardHook(LowLevelProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule)
		{
			return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		KBDLLHOOKSTRUCT lCode = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
		
		if ((Keys)lCode.vkCode == Keys.Oemtilde)
		{
			if (nCode == 0 && lCode.flags != KBDLLHOOKSTRUCTFlags.LLKHF_UP)
			{
				IsHandleInputLocked = !IsHandleInputLocked;
			}
		}
		else if ((Keys)lCode.vkCode == Keys.End)
		{
			//Process.GetCurrentProcess().Kill();
		}

		if (nCode == 0 && lCode.flags == KBDLLHOOKSTRUCTFlags.LLKHF_UP)
		{
#if DEBUG
			Console.WriteLine($"{(Keys)lCode.vkCode} | {InputPriority} | {SystemInputName}");
#endif
			OnKeyUp?.Invoke(null, new ((Keys)lCode.vkCode, InputPriority, SystemInputName));
		}
		
		if (IsHandleInputLocked) return (IntPtr)1;

		return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
	}

	#region TextInput
	private static string? _text;
	public static string TryInputText(Func<string, bool> onUpdateString, string baseStr = "", bool isMultiline = false)
	{
		InputPriority = InputPriorityEnum.Text;
		_text = baseStr;

		Keys endKey = isMultiline ? Keys.Escape : Keys.Enter;

		EventWaitHandle eventWait = new(false, EventResetMode.AutoReset);

		OnKeyUp += OnKeyUpEvent;
		
		eventWait.WaitOne();

		OnKeyUp -= OnKeyUpEvent;
		
		if (InputPriority == InputPriorityEnum.Text) InputPriority = InputPriorityEnum.Normal;

		string txt = _text;
		_text = null;
		
		return txt;

		void OnKeyUpEvent(object? sender, OnKeyUpEventArgs eventArgs)
		{
			if (eventArgs.InputPriority >= InputPriorityEnum.Locked || _text is null) return;
			
			Keys key = eventArgs.Key;


			if (key == endKey) eventWait.Set();
			else
			{
				if (key == Keys.Back)
				{
					if (_text.Length > 0)
						_text = _text[..^1];
				}
				else
				{
					if (key == Keys.OemPeriod) _text += ".";
					else if (key == Keys.Space) _text += " ";
					else if (key.ToString()[0] == 'D' && char.IsNumber(key.ToString()[1])) _text += key.ToString()[1];
					else _text += key;
				}

				if (onUpdateString(_text)) eventWait.Set();
			}
		}
	}
	#endregion TextInput
	#endregion Keyboard

	#region Mouse
	#region Struct
	public struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MSLLHOOKSTRUCT
	{
		/// <summary>
		/// Координаты курсора
		/// </summary>
		public POINT pt;
		/// <summary>
		/// 
		/// </summary>
		public int mouseData; // be careful, this must be ints, not uints (was wrong before I changed it...). regards, cmew.
		public int flags;
		public int time;
		public UIntPtr dwExtraInfo;
	}
	#endregion Struct

	private const int WH_MOUSE_LL = 14;

	/// <summary>
	/// Поле хранящее делегат функции получения нажатий клавиш
	/// </summary>
	private static LowLevelProc _MouseProc = MouseHookCallback;
	/// <summary>
	/// Идентификатор callback получения нажатий от системы
	/// </summary>
	private static IntPtr _MouseHookID = IntPtr.Zero;

	public delegate void MouseMoveDelegate(POINT Pos, bool isNowMoved);
	public static event MouseMoveDelegate? OnMouseMove;

	private static bool IsMoved { get; set; }

	private static IntPtr SetMouseHook(LowLevelProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule)
		{
			return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		MSLLHOOKSTRUCT lCode = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

		//return (IntPtr)1;

		return CallNextHookEx(_MouseHookID, nCode, wParam, lParam);
	}
	#endregion Mouse
	
	public static void Run()
	{
		#region
		//Task task = new(() =>
		//{
		//	while (true)
		//	{
		//		if (GetCursorPos(out Point point))
		//		{
		//			if (point.X != LastPoint.X || point.Y != LastPoint.Y)
		//			{
		//				OnMouseMove?.Invoke(LastPoint, point, true);
		//				LastPoint = point;
		//			}
		//		}
		//		Thread.Sleep(100);
		//	}
		//});
		//task.Start();
		#endregion

		_keyboardHookID = SetKeyboardHook(_keyboardProc);
		_MouseHookID = SetMouseHook(_MouseProc);
        Application.Run();
        UnhookWindowsHookEx(_keyboardHookID);
		UnhookWindowsHookEx(_MouseHookID);
	}
}