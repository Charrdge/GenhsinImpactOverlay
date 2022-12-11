using System.Windows.Forms;
using GameOverlay.Drawing;
using GenshinImpactOverlay.EventsArgs;
using GenshinImpactOverlay.Menus;
using NAudio.Wave;

namespace GenshinImpactOverlay.Music;

internal abstract class MusicSystem : IUseMenu
{
	public const string SYSNAME = "Music";

	#region Fonts
	private string FontIndex { get; init; }
	private string BlackBrushIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	#endregion

	#region Player
	private MediaFoundationReader? UrlMusicReader { get; set; }
	private WasapiOut? Player { get; set; }
	#endregion Player

	#region Track list
	private LinkedList<ITrackData> Tracks { get; } = new();
	protected ITrackData? NowPlaying { get; private set; }
	#endregion Track list

	#region Volume
	private decimal _volume = 0.5m;
	private decimal Volume
	{
		get => _volume;
		set
		{
			_volume = value;
			UpdatePlayerVolume(_volume);
		}
	}
	#endregion Volume

	private GraphicsWorker Worker { get; init; }

	public MusicSystem(GraphicsWorker worker, Action<string> updateLoadStatus)
	{
		updateLoadStatus("Connect to graphic");
		Worker = worker;

		updateLoadStatus("Load graphic resources");
		FontIndex = worker.AddFont("Consolas", 14);
		WhiteBrushIndex = worker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrushIndex = worker.AddSolidBrush(new Color(0, 0, 0));

		updateLoadStatus("Autorize");
		Autorize(GetLogin, GetPassword);

		updateLoadStatus("Start playing queue");
		PlayNexTrack();

		InputHook.OnKeyUp += ButtonHook_OnKeyDown;

		worker.OnDrawGraphics += Worker_OnDrawGraphics;

		string GetLogin()
		{
			string? login = "";

			do
			{
				GraphicsWorker.DrawGraphic onDrawGraphic_LoginEvent = (object? sender, OnDrawGraphicEventArgs e) =>
				{
					if (Worker.Fonts[FontIndex].IsInitialized && Worker.Brushes[WhiteBrushIndex].IsInitialized && Worker.Brushes[BlackBrushIndex].IsInitialized)
					{
						e.Graphics.DrawTextWithBackground(
							Worker.Fonts[FontIndex], Worker.Brushes[WhiteBrushIndex], Worker.Brushes[BlackBrushIndex],
							new Point(50, 100),
							$"Write login: {login}");
					}

				};

				Worker.OnDrawGraphics += onDrawGraphic_LoginEvent;
				login = InputHook.TryInputText((string loginProccess) => { login = loginProccess; return false; });

				Worker.OnDrawGraphics -= onDrawGraphic_LoginEvent;
			} while (login is null || login.Length == 0);

			return login;
		}

		string GetPassword()
		{
			string? password = "";

			do
			{
				GraphicsWorker.DrawGraphic onDrawGraphic_LoginEvent = (object? sender, OnDrawGraphicEventArgs e) =>
				{
					if (Worker.Fonts[FontIndex].IsInitialized && Worker.Brushes[WhiteBrushIndex].IsInitialized && Worker.Brushes[BlackBrushIndex].IsInitialized)
					{
						e.Graphics.DrawTextWithBackground(
							Worker.Fonts[FontIndex], Worker.Brushes[WhiteBrushIndex], Worker.Brushes[BlackBrushIndex],
							 new Point(50, 100),
							 $"Write password: {password}");
					}

				};

				Worker.OnDrawGraphics += onDrawGraphic_LoginEvent;

				password = InputHook.TryInputText((string loginProccess) => { password = loginProccess; return false; });

				Worker.OnDrawGraphics -= onDrawGraphic_LoginEvent;

			} while (password is null || password.Length == 0);

			return password;
		}
	}

	private void Worker_OnDrawGraphics(object? sender, OnDrawGraphicEventArgs e)
	{
		if (NowPlaying is not null)
		{
			Point point = new(15, 15);
			if (Worker.Fonts[FontIndex].IsInitialized && Worker.Brushes[WhiteBrushIndex].IsInitialized)
				e.Graphics.DrawText(Worker.Fonts[FontIndex], Worker.Brushes[WhiteBrushIndex], point, NowPlaying.TrackName);
		}
	}

	private void ButtonHook_OnKeyDown(object? _, OnKeyUpEventArgs eventArgs)
	{
		if (eventArgs.InputPriority > InputPriorityEnum.Normal && eventArgs.System != SYSNAME) return;
	}

	#region Player control methods
	private void PlayNexTrack()
	{
		ITrackData next;
		
		if (Tracks.First is null) Tracks.AddFirst(GetNextTrack(Array.Empty<ITrackData>()));
		
		if (NowPlaying is null) next = Tracks.First.Value;
		else
		{
			LinkedListNode<ITrackData> item = Tracks.FindLast(NowPlaying) ?? throw new NullReferenceException();
			if (item.Next is null) Tracks.AddLast(GetNextTrack(Tracks.ToArray()));
			next = item.Next.Value;
		}

		PlayTrack(next);
	}

	private void PlayPrevTrack()
	{
		if (Tracks.Any() && NowPlaying is not null)
		{
			LinkedListNode<ITrackData>? prev = Tracks.Find(NowPlaying)?.Previous;
			if (prev is not null) PlayTrack(prev.Value);
		}
	}

	private void SwitchTrackPause()
	{
		if (Player is not null) switch (Player.PlaybackState)
			{
				case PlaybackState.Paused:
					Player.Play();
					break;
				case PlaybackState.Playing:
					Player.Pause();
					break;
			}
	}

	private void UpdatePlayerVolume(decimal vol)
	{
		if (Player is not null && Player.AudioStreamVolume.ChannelCount > 0)
		{
			for (int index = 0; index < Player.AudioStreamVolume.ChannelCount; index++)
			{
				Player.AudioStreamVolume.SetChannelVolume(index, Convert.ToSingle(vol));
			}
		}
	}
	#endregion Player control methods

	private void PlayTrack(ITrackData track)
	{
		NowPlaying = track;
		if (UrlMusicReader is not null) UrlMusicReader.Close();
		UrlMusicReader = new(track.Link);

		Player?.Pause();

		Player = new();
		Player.PlaybackStopped += (sender, e) =>
		{
			if (sender is not null) ((WasapiOut)sender).Pause();
			PlayNexTrack();
		};
		Player.Init(UrlMusicReader);
		Player.Play();
		UpdatePlayerVolume(Volume);
	}

	/// <summary>
	/// Инициирует авторизацию в системе
	/// </summary>
	/// <param name="getLogin">Метод для получения логина от пользователя</param>
	/// <param name="getPassword">Метод для получения пароля от пользователя</param>
	protected abstract void Autorize(Func<string> getLogin, Func<string> getPassword);

	/// <summary>
	/// Возвращает данные трека для добавления в очередь воспроизведения
	/// </summary>
	/// <param name="tracks">Треки которые уже присутствуют в очереди</param>
	/// <returns></returns>
	protected abstract ITrackData GetNextTrack(ITrackData[] tracks);

	#region IUseMenu
	private MenuItem? _menuItem;
	MenuItem? IUseMenu.GetMenu(Action<MenuItem> updateMenuFunc, Action<bool?> keyInputSwitchFunc)
	{
		const string PATH = "Resources/Icons";

		if (_menuItem is null)
		{
			Dictionary<Keys, Action> actions = new()
			{
				{ Keys.Return, () => keyInputSwitchFunc(null) },
				{ Keys.Up, () => { if (Volume < 1m) Volume += 0.1m; } },
				{ Keys.Down, () => { if (Volume > 0m) Volume -= 0.1m; } },
				{ Keys.Clear, () => SwitchTrackPause() },
				{ Keys.Left, () => PlayPrevTrack() },
				{ Keys.Right, () => PlayNexTrack() },
			};

			_menuItem = new(SYSNAME, $"{PATH}/headphones.png", actions);
		}

		return _menuItem;
	}
	#endregion IUseMenu
}