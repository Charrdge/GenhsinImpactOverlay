using System.Windows.Forms;
using GameOverlay.Drawing;
using GenshinImpactOverlay.EventsArgs;
using GenshinImpactOverlay.GraphicWorkers;
using GenshinImpactOverlay.Menus;
using NAudio.Wave;

namespace GenshinImpactOverlay.Music;

internal abstract class MusicSystem : SystemBase, IUseMenu
{
	public const string SYSNAME = "Music";

	#region Fonts
	private FontHandler? Font { get; set; }
	private SolidBrushHandler? BlackBrush { get; set; }
	private SolidBrushHandler? WhiteBrush { get; set; }
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

	private GraphicsWorker Graphic { get; init; }

	public MusicSystem(GraphicsWorker graphic, Action<string> updateLoadStatus) : base(graphic, updateLoadStatus)
	{
		Graphic = graphic;

		updateLoadStatus("Autorize");
		Autorize(GetLogin, GetPassword);

		updateLoadStatus("Start playing queue");
		PlayNexTrack();


		string GetLogin()
		{
			string? login = "";

			do
			{
				GraphicsWorker.DrawGraphic onDrawGraphic_LoginEvent = (object? sender, OnDrawGraphicEventArgs e) =>
				{
					if (Font.IsInitialized && WhiteBrush.IsInitialized && BlackBrush.IsInitialized)
					{
						e.Graphics.DrawTextWithBackground(
							Font, WhiteBrush, BlackBrush,
							new Point(50, 100),
							$"Write login: {login}");
					}

				};

				Graphic.OnDrawGraphics += onDrawGraphic_LoginEvent;
				login = InputHook.TryInputText((string loginProccess) => { login = loginProccess; return false; });

				Graphic.OnDrawGraphics -= onDrawGraphic_LoginEvent;
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
					if (Font.IsInitialized && WhiteBrush.IsInitialized && BlackBrush.IsInitialized)
					{
						e.Graphics.DrawTextWithBackground(
							Font, WhiteBrush, BlackBrush,
							 new Point(50, 100),
							 $"Write password: {password}");
					}

				};

				Graphic.OnDrawGraphics += onDrawGraphic_LoginEvent;

				password = InputHook.TryInputText((string loginProccess) => { password = loginProccess; return false; });

				Graphic.OnDrawGraphics -= onDrawGraphic_LoginEvent;

			} while (password is null || password.Length == 0);

			return password;
		}
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

	#region SystemBase
	protected override void Graphics_OnDrawGraphics(object? sender, OnDrawGraphicEventArgs e)
	{
		if (NowPlaying is not null)
		{
			Point point = new(15, 15);
			if (Font.IsInitialized && WhiteBrush.IsInitialized)
				e.Graphics.DrawText(Font, WhiteBrush, point, NowPlaying.TrackName);
		}
	}

	protected override void InputHook_OnKeyUp(object? sender, OnKeyUpEventArgs eventArgs)
	{
		if (eventArgs.InputPriority > InputPriorityEnum.Normal && eventArgs.System != SYSNAME) return;
	}

	protected override void AddGraphicResources(GraphicsWorker graphics)
	{
		Font = graphics.AddFont("Consolas", 14);
		WhiteBrush = graphics.AddSolidBrush(new Color(255, 255, 255));
		BlackBrush = graphics.AddSolidBrush(new Color(0, 0, 0));
	}
	#endregion

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