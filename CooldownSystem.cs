using System.Windows.Forms;

internal class CooldownSystem : IDisposable
{
	/// <summary>
	/// Хранит время с последнего нажатия навыка каждого персонажа
	/// </summary>
	private Dictionary<Keys, DateTime> LastClickTimes = new();

	private Keys[] SelectKeys { get; set; }

	/// <summary>
	/// Последняя нажатая клавиша переключения персонажа
	/// </summary>
	private Keys LastSelectCharKey { get; set; }

	public TimeSpan? MaxCooldownTime { get; }

	public CooldownSystem(GraphicsWorker graphics, TimeSpan? maxCooldownTime = null) 
		: this(graphics, maxCooldownTime, new Keys[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4 }) { }

	public CooldownSystem(GraphicsWorker graphics, TimeSpan? maxCooldownTime, Keys[] switchChararcterKeys)
	{
		LastSelectCharKey = switchChararcterKeys[0];

		SelectKeys = switchChararcterKeys;
		foreach (Keys key in switchChararcterKeys) LastClickTimes.Add(key, DateTime.Now);

		ButtonHook.OnKeyDown += (vkCode) =>
		{
			var key = (Keys)vkCode;

			if (LastClickTimes.ContainsKey(key)) LastSelectCharKey = key;
			else if (key == Keys.E) LastClickTimes[LastSelectCharKey] = DateTime.Now;
		};

		graphics.OnDrawGraphics += (sender, e) =>
		{
			var text = (Keys key) =>
			{
				TimeSpan span = DateTime.Now - LastClickTimes[key];
				if (span >= MaxCooldownTime) return $"00:00:00";
				return $"{span.Minutes}:{span.Seconds}:{span.Milliseconds / 10}";
			};

			int v = 1300; // расположение по горизонтали
			int h = 225; // расположение по вертикали
			int p = 55; // коэффициент смещения

			for (int i = 0; i < SelectKeys.Length; i++)
			{
				var key = SelectKeys[i];

				e.DrawTextWithBackground(graphics.Fonts["consolas"], graphics.Brushes["white"], graphics.Brushes["black"], v, h + (p * i), text(key));
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