using System.Text.Json;
using Newtonsoft.Json.Linq;
using YandexMusicApi;
using System.Text.Json.Nodes;

namespace GenshinImpactOverlay.Music;

internal class YandexMusicSystem : MusicSystem
{
	/// <summary>
	/// Создвёт новый экземпляр системы воспроизведения музыки через Яндекс.Музыку
	/// </summary>
	/// <param name="worker"></param>
	/// <param name="updateLoadStatus"></param>
	public YandexMusicSystem(GraphicsWorker worker, Action<string> updateLoadStatus) : base(worker, updateLoadStatus) { }

	/// <summary>
	/// Идентификатор станции в очереди воспроизведения
	/// </summary>
	private string StationId { get; set; } = "user:onyourwave";

	/// <summary>
	/// Возвращает данные трека для добавления в очередь воспроизведения
	/// </summary>
	/// <param name="tracks">Треки которые уже присутствуют в очереди</param>
	/// <returns></returns>
	protected override ITrackData GetNextTrack(ITrackData[] tracks)
	{
		do
		{
			JToken rotor = Rotor.GetTrack(StationId)["result"]["sequence"] ?? throw new NullReferenceException();

			foreach (var item in rotor)
			{
				JToken track = item["track"];

				string id = item["track"]["id"].Value<string>() ?? throw new NullReferenceException();

				Func<ITrackData, bool> predict = (ITrackData track) => track is YaTrack ya && ya.Id == id;
				if (!tracks.Any(predict))
				{
					string name = $"{track["artists"].First["name"].Value<string>()} - {track["title"].Value<string>()}";
					return new YaTrack(id, name);
				}
			}

		} while (true);
	}

	/// <summary>
	/// Инициирует авторизацию в системе
	/// </summary>
	/// <param name="getLogin">Метод для получения логина от пользователя</param>
	/// <param name="getPassword">Метод для получения пароля от пользователя</param>
	/// <exception cref="NullReferenceException"></exception>
	protected override void Autorize(Func<string> getLogin, Func<string> getPassword)
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
			Token.GetToken(getLogin(), getPassword());
		}

		JsonNode rootNode = JsonNode.Parse(GetJsonFileAsString(fileName)) ?? throw new NullReferenceException();

		if (rootNode[yaMusicJsonName] is not null) rootNode[yaMusicJsonName][tokenJsonName] = Token.token;
		else rootNode[yaMusicJsonName] = new JsonObject 
		{
			[tokenJsonName] = Token.token
		};
		using StreamWriter streamWriter = new(fileName);
		streamWriter.Write(rootNode.ToString());

		static string GetJsonFileAsString(string fileName)
		{
			using StreamReader reader = new(fileName);
			string file = reader.ReadToEnd();
			return file;
		}
	}
}