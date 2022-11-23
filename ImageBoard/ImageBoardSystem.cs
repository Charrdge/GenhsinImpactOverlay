using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;

using static System.Net.Mime.MediaTypeNames;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class ImageBoardSystem : IDisposable
{
	private Dictionary<long, JsonDocument> Posts { get; init; } = new();

	private GraphicsWorker Worker { get; init; }

	private string FontIndex { get; init; }
	private string WhiteBrushIndex { get; init; }
	private string BlackBrushIndex { get; init; }

	public ImageBoardSystem(GraphicsWorker graphics)
	{
		Worker = graphics;

		FontIndex = graphics.AddFont("Consolas", 14);

		WhiteBrushIndex = graphics.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		BlackBrushIndex = graphics.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		HttpClient httpClient = new();

		JsonDocument thread = GetThread(httpClient);

		Console.WriteLine(thread.ToString());

		GetAllPosts(httpClient, thread);

		System.Timers.Timer timer = new(5000);

		timer.Elapsed += (sender, e) =>
		{
			long threadNum = Posts.First().Key;

			long postNum = Posts.Last().Key;

			string requestString = $"https://2ch.hk/api/mobile/v2/after/vg/{threadNum}/{postNum}";

			using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
			{
				var response = httpClient.Send(postsRequest);
				string strJson = response.Content.ReadAsStringAsync().Result;

				using (JsonDocument document = JsonDocument.Parse(strJson))
				{
					JsonElement root = document.RootElement;
					JsonElement posts = root.GetProperty("posts");
					foreach (JsonElement post in posts.EnumerateArray())
					{
						long num = post.GetProperty("num").GetInt64();
						Posts.TryAdd(num, JsonDocument.Parse(post.ToString()));
					}
				}
			}
		};
		timer.AutoReset = true;
		timer.Enabled = true;

		Worker.OnDrawGraphics += Graphics_OnDrawGraphics;
	}

	private void Graphics_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e) 
	{
		int row = 15;
		int noImageRows = 3;

		IEnumerable<KeyValuePair<long, JsonDocument>> LastPosts = Posts.TakeLast(3);
		//IEnumerable<KeyValuePair<long, JsonDocument>> LastPosts = Posts.Take(3);

		HttpClient client = new();

		int upper = 0;
		foreach (KeyValuePair<long, JsonDocument> pair in LastPosts.Reverse())
		{
			JsonDocument post = pair.Value;
			JsonElement root = post.RootElement;

			List<GameOverlay.Drawing.Image> images = new();

			if (root.TryGetProperty("files", out JsonElement filesJson) && filesJson.ToString() != "")
			{
				foreach (var item in filesJson.EnumerateArray())
				{
					string source = item.GetProperty("thumbnail").GetString() ?? throw new Exception();
					using HttpRequestMessage fileRequest = new(HttpMethod.Get, $"https://2ch.hk{source}");
					HttpResponseMessage responce = client.Send(fileRequest);
					byte[] stream = responce.Content.ReadAsByteArrayAsync().Result;

					GameOverlay.Drawing.Image image = new(e.Graphics, stream);

					images.Add(image);
				}
			}

			if (images.Count == 1)
			{
				GameOverlay.Drawing.Image image = images.First();

				#region math
				float coef = image.Height / image.Width;

				float width = 100f;
				float height = width * coef;

				float left = 80f;
				float right = left + width;
				float bottom = 720f - upper;
				float top = bottom - height;
				#endregion

				GameOverlay.Drawing.Rectangle rectangle = new(left, top, right, bottom);

				e.Graphics.DrawImage(image, rectangle, 0.5f);

				upper += (int)height;

				#region text
				string text = root.GetProperty("comment").GetString() ?? throw new NullReferenceException();
				text = Trim(text, 60);
				if (Cut(text, (int)(height / row), out text, out int rows))
				{
					text += "Развернуть...";
					rows++;
				}
				//upper += rows * row;

				GameOverlay.Drawing.Point point = new(right + 10, top); // h, v

				e.Graphics.DrawText(
					Worker.Fonts[FontIndex], // Шрифт текста
					(GameOverlay.Drawing.SolidBrush)Worker.Brushes[WhiteBrushIndex], // Цвет текста
					//(GameOverlay.Drawing.SolidBrush)Worker.Brushes[BlackBrushIndex], // Фон текста
					point, // Положение текста
					text); // Текст
				#endregion
			}
			else
			{
				#region text
				string text = root.GetProperty("comment").GetString() ?? throw new NullReferenceException();
				text = Trim(text, 60);
				if (Cut(text, noImageRows, out text, out int rows))
				{
					text += "Развернуть...";
					rows++;
				}
				upper += rows * row;

				GameOverlay.Drawing.Point point = new(80, 720 - upper); // h, v

				e.Graphics.DrawText(
					Worker.Fonts[FontIndex], // Шрифт текста
					(GameOverlay.Drawing.SolidBrush)Worker.Brushes[WhiteBrushIndex], // Цвет текста
					//(GameOverlay.Drawing.SolidBrush)Worker.Brushes[BlackBrushIndex], // Фон текста
					point, // Положение текста
					text); // Текст
				#endregion

				if (images.Count > 1)
				{
					upper += 10;

					float maxImgHeigth = 0;

					int i = 0;
					foreach (var image in images)
					{
						float coef = image.Height / image.Width;

						float width = 100f;
						float heigth = width * coef;

						float left = 80f + (i * (width + 10));
						float right = left + width;
						float bottom = 720f - upper;
						float top = bottom - heigth;

						GameOverlay.Drawing.Rectangle rectangle = new(left, top, right, bottom);

						e.Graphics.DrawImage(image, rectangle, 0.5f);

						if (heigth > maxImgHeigth) maxImgHeigth = heigth;
						i++;
					}

					upper += (int)maxImgHeigth;
				}
			}

			upper += row; //небольшой отступ между постами
		}
	}

	private static JsonDocument GetThread(HttpClient httpClient)
	{
		using (HttpRequestMessage threadsRequest = new(HttpMethod.Get, "https://2ch.hk/vg/catalog_num.json"))
		{
			var responce = httpClient.Send(threadsRequest);
			string text = responce.Content.ReadAsStringAsync().Result;

			using (JsonDocument document = JsonDocument.Parse(text))
			{
				JsonElement root = document.RootElement;
				JsonElement threads = root.GetProperty("threads");

				foreach (JsonElement thread in threads.EnumerateArray())
				{
					if (thread.TryGetProperty("tags", out JsonElement tag) && tag.GetString() == "genshin")
					{
						return JsonDocument.Parse(thread.ToString());
					}
				}

				throw new Exception();
			}
		}

	}

	private Dictionary<long, JsonDocument> GetAllPosts(HttpClient httpClient, JsonDocument thread)
	{
		Dictionary<long, JsonDocument> dict = new();

		long threadNum = thread.RootElement.GetProperty("num").GetInt64();

		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, $"https://2ch.hk/vg/res/{threadNum}.json"))
		{
			var responce = httpClient.Send(postsRequest);
			string strJson = responce.Content.ReadAsStringAsync().Result;

			using (JsonDocument document = JsonDocument.Parse(strJson))
			{
				JsonElement root = document.RootElement;
				JsonElement threads = root.GetProperty("threads");
				JsonElement posts = threads.EnumerateArray().First().GetProperty("posts");
				foreach (JsonElement post in posts.EnumerateArray())
				{
					long postNum = post.GetProperty("num").GetInt64();
					Posts.TryAdd(postNum, JsonDocument.Parse(post.ToString()));
				}
			}
		}

		return dict;
	}

	private static string Trim(string inputString, int maxLengthString)
	{
		inputString = inputString.Replace("\u0026#47;", "/");
		inputString = inputString.Replace("&gt;", ">");

		string outputLine = "";

		Regex regex = new(@"<(\w?\W?\s?)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

		foreach (string line in inputString.Split("<br>"))
		{
			var words = regex.Replace(line, "").Split(new Char[] { ' ' });
			int wordIndex = 0;
			var spaceLetter = " ";
			var currentLine = new StringBuilder();

			while (true)
			{
				if (currentLine.Length + words[wordIndex].Length + 1 > maxLengthString)// Определяем не привысила ли текущая строка максимальную длину
				{
					if (currentLine.Length > 0) outputLine += $"{currentLine}\n";
					currentLine.Remove(0, currentLine.Length);
				}
				currentLine.Append(words[wordIndex]);
				currentLine.Append(spaceLetter);
				wordIndex++;
				if (wordIndex == words.Length)
				{
					if (currentLine.Length > 1) outputLine += $"{currentLine}\n";
					break;
				}
			}
		}

		return outputLine;
	}

	private static bool Cut(string inputString, int maxRows, out string cuttedString, out int length)
	{
		string str = "";
		length = 0;
		bool cutted = false;

		string[] rows = inputString.Split('\n');

		for (int index = 0; index < rows.Length; index++)
		{
			length = index;

			string row = rows[index];

			if (index <= maxRows - 1)
			{
				str += $"{row}\n";
			}
			else
			{
				cutted = true;
				break;
			}
		}

		cuttedString = str;

		return cutted;
	}

	~ImageBoardSystem() => Dispose(disposing: false);

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