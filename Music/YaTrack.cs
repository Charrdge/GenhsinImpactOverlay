using Newtonsoft.Json.Linq;

namespace GenshinImpactOverlay.Music;

internal readonly struct YaTrack : ITrackData
{
	public string Id { get; }

	/// <summary>
	/// Название трека
	/// </summary>
	public string TrackName { get; }

	/// <summary>
	/// Ссылка на трек
	/// </summary>
	public string Link 
	{ 
		get
		{
			var info = YandexMusicApi.Track.GetDownloadInfoWithToken(Id);
			var link = YandexMusicApi.Track.GetDirectLink(info["result"].First["downloadInfoUrl"].Value<string>());

			return link;
		}
	} 

	/// <summary>
	/// Создаёт новую структуру с данными трека
	/// </summary>
	/// <param name="trackName"></param>
	/// <param name="link"></param>
	public YaTrack(string id, string trackName)
	{
		Id = id;
		TrackName = trackName;
	}
}