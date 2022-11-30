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

	private GameOverlay.Drawing.Image? _pathImage;
	private GameOverlay.Drawing.Image GetFile(GameOverlay.Drawing.Graphics graphics)
	{
		if (_pathImage is null)
		{
			HttpClient client = new();
			using HttpRequestMessage fileRequest = new(HttpMethod.Get, $"https://2ch.hk{Path}");
			HttpResponseMessage responce = client.Send(fileRequest);
			byte[] stream = responce.Content.ReadAsByteArrayAsync().Result;

			_pathImage = new(graphics, stream);
		}

		return _pathImage;
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

			Console.WriteLine(Thumbnail);
			Console.WriteLine(_thumbImage);
		}

		return _thumbImage;
	}

	public void DrawFile(GameOverlay.Drawing.Graphics graphics, int bottom, int left, int width, out int height)
	{
		float coef = Height / ((float)Width);

		height = (int)(width * coef);

		int right = left + width;
		int top = bottom - height;

		GameOverlay.Drawing.Rectangle rectangle = new(left, top, right, bottom);

		GameOverlay.Drawing.Image image = GetFile(graphics);

		graphics.DrawImage(image, rectangle, 0.5f);
	}

	#region Premath
	int? _lastBottom;
	int? _lastLeft;
	int? _lastWidth;

	int _mathHeight;
	GameOverlay.Drawing.Rectangle _mathRectangle;
	#endregion Premath
	public void DrawFileThumb(GameOverlay.Drawing.Graphics graphics, int bottom, int left, int width, out int height)
	{
		GameOverlay.Drawing.Rectangle rectangle;

		if (_lastBottom == bottom && _lastLeft == left && _lastWidth == width)
		{
			rectangle = _mathRectangle;
			height = _mathHeight;
		}
		else
		{
			float coef = TnHeight / ((float)TnWidth);

			_mathHeight = height = (int)(width * coef);
			int right = left + width;
			int top = bottom - height;

			_mathRectangle = rectangle = new(left, top, right, bottom);
		}

		GameOverlay.Drawing.Image image = GetFileThumb(graphics);

		graphics.DrawImage(image, rectangle, 0.5f);
	}
}