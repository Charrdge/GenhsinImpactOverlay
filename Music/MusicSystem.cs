using System.Text.Json;
using Newtonsoft.Json.Linq;
using YandexMusicApi;
using NAudio.Wave;
using System.Windows.Forms;
using GameOverlay.Drawing;
using System.Text.Json.Nodes;
using GenshinImpactOverlay.EventsArgs;
using static GraphicsWorker;

namespace GenshinImpactOverlay.Music;

internal class MusicSystem
{
	public const string SYSNAME = "Music";

	private GraphicsWorker Worker { get; init; }

	#region Player fields
	private WasapiOut? Player { get; set; }
	private MediaFoundationReader? Mf { get; set; }
	#endregion Player fields

	private string? SoundName { get; set; }
	private string? PlayTrackId { get; set; }

	#region Resources
	private string FontIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	private string BlackBrushIndex { get; init; }
	#endregion Resources

	#region Station data
	private string StationId { get; set; } = "user:onyourwave";
	private Dictionary<string, JToken> StationTracks { get; } = new();
	#endregion Station data
	
	public MusicSystem(GraphicsWorker worker)
	{
		Worker = worker;
		
		FontIndex = worker.AddFont("Consolas", 14);
		WhiteBrushIndex = worker.AddSolidBrush(new Color(255, 255, 255));
		BlackBrushIndex = worker.AddSolidBrush(new Color(0, 0, 0));

		Autorize();

		Console.WriteLine(Token.token);
		
		//var stateDash = Rotor.StationDashboard();
		//var station = stateDash["result"]["stations"].First;

		PlayNextStationTrack();

		InputHook.OnKeyUp += ButtonHook_OnKeyDown;

		worker.OnDrawGraphics += Worker_OnDrawGraphics;
	}

	private void Worker_OnDrawGraphics(object? sender, OnDrawGraphicEventArgs e)
	{
		if (SoundName is not null)
		{
			Point point = new(15, 15);
			if (Worker.Fonts[FontIndex].IsInitialized && Worker.Brushes[WhiteBrushIndex].IsInitialized)
				e.Graphics.DrawText(Worker.Fonts[FontIndex], Worker.Brushes[WhiteBrushIndex], point, SoundName);
		}
	}

	private void ButtonHook_OnKeyDown(object? _, OnKeyUpEventArgs eventArgs)
	{
		if (eventArgs.InputPriority > InputPriorityEnum.Normal && eventArgs.System != SYSNAME) return;

		Keys key = eventArgs.Key;
		if (key == Keys.NumPad6) PlayNextStationTrack();
		else if (key == Keys.NumPad5) SwitchTrackPause();
		else if (key == Keys.NumPad4) PlayPrevStationTrack();
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

	private void PlayNextStationTrack()
	{
		if (!StationTracks.Any()) UpdateStationTracksQueue(StationId);
		if (PlayTrackId is null) PlayTrackId = StationTracks.First().Key;

		var unplayedTracks = StationTracks.SkipWhile((pair) => pair.Key != PlayTrackId).Skip(1);

		if (unplayedTracks.Count() < 2) UpdateStationTracksQueue(StationId); 

		var track = unplayedTracks.Any() ? unplayedTracks.First() : StationTracks.First();

		string link = GetTrackLink(track.Key);

		PlayTrackId = track.Key;

		SoundName = $"{track.Value["artists"].First["name"].Value<string>()} - {track.Value["title"].Value<string>()}";

		PlayTrack(link);
	}

	private void PlayPrevStationTrack()
	{
		if (StationTracks.Any())
		{
			var prevTracks = StationTracks.Reverse().SkipWhile((pair) => pair.Key != PlayTrackId).Skip(1);

			if (prevTracks.Any())
			{
				var track = prevTracks.First();

				string link = GetTrackLink(track.Key);

				PlayTrackId = track.Key;

				SoundName = $"{track.Value["artists"].First["name"].Value<string>()} - {track.Value["title"].Value<string>()}";

				PlayTrack(link);
			}
		}
	}

	private void PlayTrack(string link)
	{
		if (Mf is not null) Mf.Dispose();
		Mf = new(link);

		if (Player is not null)
		{
			Player.Pause();
		}

		Player = new();
		Player.PlaybackStopped += (sender, e) =>
		{
			if (sender is not null) ((WasapiOut)sender).Pause();
			PlayNextStationTrack();
		};
		Player.Init(Mf);
		Player.Play();
	}

	private void UpdateStationTracksQueue(string stationId)
	{
		int count = StationTracks.Count;

		do
		{
			var tracks = Rotor.GetTrack(stationId)["result"]["sequence"];

			foreach (var item in tracks)
			{
				string id = item["track"]["id"].Value<string>();

				if (!StationTracks.ContainsKey(id)) StationTracks.Add(id, item["track"]);
			}

			Thread.Sleep(100); // Ограничивает скорость отправки запросов во избежание фризов

		} while (StationTracks.Count < count + 2);
		//if (StationTracks.Count > 10) StationTracks = new(StationTracks.Skip(5));
	}

	private static string GetTrackLink(string trackId)
	{
		var info = Track.GetDownloadInfoWithToken(trackId);
		var link = Track.GetDirectLink(info["result"].First["downloadInfoUrl"].Value<string>());

		return link;
	}

	private string Autorize()
	{
		string fileName = "config.json";
		string yaMusicJsonName = "yandex_music";
		string tokenJsonName = "token";

		JsonElement rootElement = JsonDocument.Parse(GetJsonFileAsString(fileName)).RootElement;
		if (rootElement.TryGetProperty(yaMusicJsonName, out JsonElement yaMusicJson) && yaMusicJson.TryGetProperty(tokenJsonName, out JsonElement tokenJson))
		{
			Token.token = tokenJson.GetString();
		}

		while (Token.token is null || Token.token == "" || Account.ShowInformAccount()["error"] is not null)
		{
			(string login, string password) = GetPassData();
			Token.GetToken(login, password);
		}

		JsonNode rootNode = JsonNode.Parse(GetJsonFileAsString(fileName)) ?? throw new NullReferenceException();

		if (rootNode[yaMusicJsonName] is not null) rootNode[yaMusicJsonName][tokenJsonName] = Token.token;
		else rootNode[yaMusicJsonName] = new JsonObject 
		{
			[tokenJsonName] = Token.token
		};
		using StreamWriter streamWriter = new(fileName);
		streamWriter.Write(rootNode.ToString());

		return Token.token;

		(string login, string password) GetPassData()
		{
			string? login = "";
			do
			{
				DrawGraphic onDrawGraphic_LoginEvent = (object? sender, OnDrawGraphicEventArgs e) =>
				{
					if (Worker.Fonts[FontIndex].IsInitialized &&
						Worker.Brushes[WhiteBrushIndex].IsInitialized &&
						Worker.Brushes[BlackBrushIndex].IsInitialized)
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

			string? password = "";
			do
			{
				DrawGraphic onDrawGraphic_LoginEvent = (object? sender, OnDrawGraphicEventArgs e) =>
				{
					if (Worker.Fonts[FontIndex].IsInitialized && 
						Worker.Brushes[WhiteBrushIndex].IsInitialized && 
						Worker.Brushes[BlackBrushIndex].IsInitialized)
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

			return (login, password);
		}
	
		static string GetJsonFileAsString(string fileName)
		{
			using StreamReader reader = new(fileName);
			string file = reader.ReadToEnd();
			return file;
		}
	}
}