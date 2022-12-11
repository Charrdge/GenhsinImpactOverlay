namespace GenshinImpactOverlay.Music;

internal interface ITrackData
{
	/// <summary>
	/// Название трека
	/// </summary>
	public string TrackName { get; }

	/// <summary>
	/// Ссылка на трек
	/// </summary>
	public string Link { get; }
}