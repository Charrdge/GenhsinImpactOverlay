using Newtonsoft.Json.Linq;
using YandexMusicApi;
using NAudio.Wave;
using System.Windows.Forms;
using GameOverlay.Drawing;

namespace GenshinImpactOverlay.Music;

internal class MusicSystem
{
	private GraphicsWorker Worker { get; init; }

	private WasapiOut? Player { get; set; }
	private MediaFoundationReader? Mf { get; set; }
	private string? SoundName { get; set; }

	private string FontIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	private string BlackBrushIndex { get; init; }

	//private Task? Playertask { get; set; }

	public MusicSystem(GraphicsWorker worker)
	{
		Worker = worker;
		
		FontIndex = worker.AddFont("Consolas", 14);
		WhiteBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		BlackBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		Autorize();

		Console.WriteLine(Token.token);

		//var stateDash = Rotor.StationDashboard();
		//var station = stateDash["result"]["stations"].First;

		PlayNextStationTrack();

		ButtonHook.OnKeyDown += ButtonHook_OnKeyDown;

		worker.OnDrawGraphics += Worker_OnDrawGraphics;
	}

	private void Worker_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e)
	{
		if (SoundName is not null)
		{
			Point point = new(15, 15);
			e.Graphics.DrawText(Worker.Fonts[FontIndex], (SolidBrush) Worker.Brushes[WhiteBrushIndex], point, SoundName);
		}
	}

	private void ButtonHook_OnKeyDown(int vkCode)
	{
		var key = (Keys)vkCode;

		if (key == Keys.Right) PlayNextStationTrack();
	}

	private void PlayNextStationTrack()
	{
		string link = GetTrackFromStation("user:onyourwave", out string name);

		SoundName = name;

		PlayTrack(link);
	}

	private void PlayTrack(string link)
	{
		if (Mf is not null) Mf.Dispose();
		Mf = new(link);

		if (Player is not null)
		{
			Player.Stop();
			Player.Dispose();
		}

		Player = new();
		Player.PlaybackStopped += (sender, e) => PlayNextStationTrack();
		Player.Init(Mf);
		Player.Play();
	}

	private static string GetTrackFromStation(string stationId, out string name)
	{
		var tracks = Rotor.GetTrack(stationId);
		var track = tracks["result"]["sequence"].First["track"];
		var info = Track.GetDownloadInfoWithToken(track["id"].Value<string>());
		name = $"{track["artists"].First["name"].Value<string>()} - {track["title"].Value<string>()}";
		var link = Track.GetDirectLink(info["result"].First["downloadInfoUrl"].Value<string>());

		return link;
	}

	private static string Autorize()
	{
		string fileName = "token.txt";

		if (new FileInfo(fileName).Exists)
		{
			using StreamReader reader = new(fileName);
			Token.token = reader.ReadLine();
		}

		while (Token.token is null || Token.token == "" || Account.ShowInformAccount()["error"] is not null)
		{
			(string login, string password) = GetPassData();
			Token.GetToken(login, password);
		}

		using StreamWriter writer = new(fileName);
		writer.WriteLine(Token.token);

		return Token.token;

		static (string login, string password) GetPassData()
		{
			string? login;
			do
			{
				Console.WriteLine("Write login");
				login = Console.ReadLine();
			} while (login is null);

			string? password;
			do
			{
				Console.WriteLine("Write password");
				password = Console.ReadLine();
			} while (password is null);

			return (login, password);
		}
	}
}