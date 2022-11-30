using System.Windows.Forms;

namespace GenshinImpactOverlay.Cooldowns;

/// <summary>
/// Система информирования отката навыков персонажа
/// </summary>
internal class CooldownSystem : IDisposable
{
	private const string SYSNAME = "Cooldown";

	/// <summary>
	/// Хранит время с последнего нажатия навыка каждого персонажа
	/// </summary>
	private Dictionary<Keys, DateTime> LastClickTimes { get; set; } = new();

	/// <summary>
	/// Кнопки для смены персонажей
	/// </summary>
	private Keys[] SwitchCharacterKeys { get; set; }

	/// <summary>
	/// Последняя нажатая клавиша переключения персонажа
	/// </summary>
	private Keys LastSelectCharKey { get; set; }

	/// <summary>
	/// Время, после которого таймер принудительно обнулится
	/// </summary>
	public TimeSpan? MaxCooldownTime { get; }

	/// <summary>
	/// Создаёт новый экземпляр системы контроля откатов
	/// </summary>
	/// <param name="graphics">Используемый обработчик графики</param>
	/// <param name="switchChararcterKeys">Кнопки для смены персонажей</param>
	/// <param name="maxCooldownTime">Время, после которого таймер принудительно обнулится</param>
	public CooldownSystem(GraphicsWorker graphics, Keys[] switchChararcterKeys, TimeSpan? maxCooldownTime = null)
	{
		LastSelectCharKey = switchChararcterKeys[0];

		SwitchCharacterKeys = switchChararcterKeys;
		foreach (Keys key in switchChararcterKeys) LastClickTimes.Add(key, DateTime.Now);

		string font = graphics.AddFont("Consolas", 14);

		string white = graphics.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		string black = graphics.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		InputHook.OnKeyUp += (_, eventArgs) =>
		{
			if (eventArgs.InputPriority > InputPriorityEnum.Normal && eventArgs.System != SYSNAME) return;

			Keys key = eventArgs.Key;

			if (LastClickTimes.ContainsKey(key)) LastSelectCharKey = key;
			else if (key == Keys.E) LastClickTimes[LastSelectCharKey] = DateTime.Now;
		};

		graphics.OnDrawGraphics += (sender, e) =>
		{
			Func<Keys, string> timerText = (Keys key) =>
			{
				TimeSpan span = DateTime.Now - LastClickTimes[key];
				if (span >= MaxCooldownTime) return $"00:00:00";
				return $"{span.Minutes}:{span.Seconds}:{span.Milliseconds / 10}";
			};

			int h = 1300; // расположение по горизонтали
			int v = 225; // расположение по вертикали
			int p = 55; // коэффициент смещения

			for (int i = 0; i < SwitchCharacterKeys.Length; i++)
			{
				var key = SwitchCharacterKeys[i];

				if (graphics.Brushes[white].IsInitialized && graphics.Brushes[black].IsInitialized)
				{ 
				e.Graphics.DrawTextWithBackground(
					graphics.Fonts[font], // Шрифт текста
					(GameOverlay.Drawing.SolidBrush)graphics.Brushes[white], // Цвет текста
					(GameOverlay.Drawing.SolidBrush)graphics.Brushes[black], // Фон текста
					h, v + (p * i), // Положение текста
					timerText(key)); // Текст
			}
			}
		};

		MaxCooldownTime = maxCooldownTime;
	}
	
	~CooldownSystem() => Dispose(disposing: false);

	#region IDisposable
	private bool _disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// TODO: освободить управляемое состояние (управляемые объекты)
			}



			// TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
			// TODO: установить значение NULL для больших полей
			_disposedValue = true;
		}
	}

	// // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов


	void IDisposable.Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}