using System.Text.Json;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class ImageBoardSystem : IDisposable
{
	private const string SYSNAME = "Imageboard";

	private SortedDictionary<int, Post> Posts { get; set; } = new();
	private GraphicsWorker Worker { get; init; }
	private System.Timers.Timer GetNewPostsTimer { get; set; } = new(5000);

	public ImageBoardSystem(GraphicsWorker worker)
	{
		Worker = worker;

		Post.FontIndex = worker.AddFont("Consolas", 14);
		Post.BFontIndex = worker.AddFont("Consolas", 14, bold: true);

		Post.WhiteBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		Post.BlackBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		HttpClient httpClient = new();

		JsonDocument thread = GetThread(httpClient);

		GetAllPosts(httpClient, thread);

		//GetNewPostsTimer = new(5000);
		GetNewPostsTimer.Elapsed += GetNewPosts;
		GetNewPostsTimer.AutoReset = true;
		GetNewPostsTimer.Enabled = true;

		Worker.OnDrawGraphics += Graphics_OnDrawGraphics;
	}

	private void Graphics_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e) 
	{
		int escape = 15;
		int bottom = 720;
		int left = 80;
		int upper = 0;

		if (Posts.Count == 0) return;

		IEnumerable<KeyValuePair<int, Post>> LastPosts = Posts.TakeLast(4);
		//IEnumerable<KeyValuePair<long, Post>> LastPosts = Posts.Take(6);

		foreach (KeyValuePair<int, Post> pair in LastPosts.Reverse())
		{
			Post post = pair.Value;

			upper += post.DrawPost(e.Graphics, Worker, bottom - upper, left);

			upper += escape; //небольшой отступ между постами
		}
	}
	
	private void GetNewPosts(object? sender, System.Timers.ElapsedEventArgs e)
	{
		HttpClient httpClient = new();
		long threadNum = Posts.First().Key;
		string requestString = $"https://2ch.hk/api/mobile/v2/info/vg/{threadNum}";

		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using (JsonDocument document = JsonDocument.Parse(strJson))
			{
				JsonElement root = document.RootElement;
				JsonElement posts = root.GetProperty("thread").GetProperty("posts");

				if (posts.TryGetInt32(out int postCount) && postCount > 999)
				{
					JsonDocument thread = GetThread(httpClient);

					int num = thread.RootElement.GetProperty("num").GetInt32();

					if (!Posts.ContainsKey(num)) GetAllPosts(httpClient, thread);
				}
			}
		}

		int postNum = Posts.Last().Key;

		requestString = $"https://2ch.hk/api/mobile/v2/after/vg/{threadNum}/{postNum}";

		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using (JsonDocument document = JsonDocument.Parse(strJson))
			{

				JsonElement root = document.RootElement;
				JsonElement posts = root.GetProperty("posts");
				foreach (JsonElement json in posts.EnumerateArray())
				{
					Post? post = JsonSerializer.Deserialize<Post>(JsonDocument.Parse(json.ToString()));

					if (post is null) throw new Exception();

					Posts.TryAdd(post.Num, post);
				}
			}
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

	private void GetAllPosts(HttpClient httpClient, JsonDocument thread)
	{
		Posts = new SortedDictionary<int, Post>();

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
				foreach (JsonElement json in posts.EnumerateArray())
				{
					JsonDocument doc = JsonDocument.Parse(json.ToString());
					//Console.WriteLine(doc.RootElement.ToString());
					//Console.WriteLine();
					Post? post = JsonSerializer.Deserialize<Post>(doc);

					if (post is null) throw new Exception();

					Posts.TryAdd(post.Num, post);
				}
			}
		}
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