using System.Text.Json.Serialization;

namespace GenshinImpactOverlay.ImageBoard;

public class File
{
	public enum FileType
	{
        FileTypeNone = 0,
        FileTypeJpg = 1,
        FileTypePng = 2,
        FileTypeAPng = 3,
        FileTypeGif = 4,
        FileTypeBmp = 5,
        FileTypeWebm = 6,
        FileTypeMp3 = 7, // не используется в данный момент.
        FileTypeOgg = 8, // не используется в данный момент.
        FileTypeMp4 = 10,
        FileTypeSticker = 100,
	}

	#region Required
	[JsonPropertyName("name")] public string Name { get; set; }
	[JsonPropertyName("fullname")] public string Fullname { get; set; }
	[JsonPropertyName("displayname")] public string Displayname { get; set; }
	[JsonPropertyName("path")] public string Path { get; set; }
	[JsonPropertyName("thumbnail")] public string Thumbnail { get; set; }
	[JsonPropertyName("type")] public FileType Type { get; set; }
	[JsonPropertyName("size")] public int Size { get; set; }
	[JsonPropertyName("width")] public int Width { get; set; }
	[JsonPropertyName("height")] public int Height { get; set; }
	[JsonPropertyName("tn_width")] public int TnWidth { get; set; }
	[JsonPropertyName("tn_height")] public int TnHeight { get; set; }
	#endregion Required

	#region Optional
	[JsonPropertyName("md5")] public string? Md5 { get; set; }
	[JsonPropertyName("nsfw")] public int? Nsfw { get; set; }
	[JsonPropertyName("duration")] public string? Duration { get; set; }
	[JsonPropertyName("duration_secs")] public int? DurationSecs { get; set; }
	[JsonPropertyName("pack")] public string? Pack { get; set; }
	[JsonPropertyName("sticker")] public string? Sticker { get; set; }
	[JsonPropertyName("install")] public string? Install { get; set; }
	#endregion Optional

	public byte[] GetFile()
	{
		throw new NotImplementedException();
	}

	private GameOverlay.Drawing.Image? _thumbImage;
	private GameOverlay.Drawing.Image GetFileThumb(GameOverlay.Drawing.Graphics graphics)
	{
		if (_thumbImage is null)
		{
			HttpClient client = new();
			using HttpRequestMessage fileRequest = new(HttpMethod.Get, $"https://2ch.hk{Thumbnail}");
			HttpResponseMessage responce = client.Send(fileRequest);
			byte[] stream = responce.Content.ReadAsByteArrayAsync().Result;

			_thumbImage = new(graphics, stream);
		}

		return _thumbImage;
	}

	public void DrawFileThumb(GameOverlay.Drawing.Graphics graphics, int bottom, int left, int width, out int height)
	{
		float coef = TnHeight / ((float)TnWidth);

		height = (int)(width * coef);

		int right = left + width;
		int top = bottom - height;

		GameOverlay.Drawing.Rectangle rectangle = new(left, top, right, bottom);
		//GameOverlay.Drawing.Rectangle rectangle = new(400, 300, 600, 100);

		GameOverlay.Drawing.Image image = GetFileThumb(graphics);

		graphics.DrawImage(image, rectangle, 0.5f);
	}
}