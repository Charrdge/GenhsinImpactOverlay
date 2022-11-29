using System.Text.Json;
using System.Windows.Forms;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class ImageBoardSystem : IDisposable
{
	private const string SYSNAME = "Imageboard";

	private LinkedList<Post> Posts { get; set; } = new();
	private GraphicsWorker Worker { get; init; }
	private System.Timers.Timer GetNewPostsTimer { get; set; } = new(5000);

	private Post? LockedPost { get; set; }

	private bool NodeMode { get; set; } = false;
	private bool ExtendFocused { get; set; } = false;

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

		InputHook.OnKeyUp += InputHook_OnKeyUp;
	}

	private void InputHook_OnKeyUp(object? sender, EventsArgs.OnKeyUpEventArgs eventArgs)
	{
		if (eventArgs.InputPriority >= InputPriorityEnum.Locked && eventArgs.System != SYSNAME) return;

		Keys key = eventArgs.Key;

		switch (key)
		{
			case Keys.Up:
				if (eventArgs.System == SYSNAME)
				{
					if (LockedPost != Posts.First())
					{
						LockedPost = Posts.FindLast(LockedPost).Previous.Value;
					}
				}
				else
				{
					if (LockedPost is null)
					{
						bool locked = InputHook.TrySetSystemLock(SYSNAME);
						if (locked) LockedPost = Posts.Last();
					}
				}
				break;
			case Keys.Down:
				if (eventArgs.System == SYSNAME)
				{
					if (LockedPost is not null && LockedPost != Posts.Last()) LockedPost = Posts.FindLast(LockedPost).Next.Value;
					else
					{
						InputHook.TryClearSystemLock(SYSNAME);
						LockedPost = null;
					}
				}
				break;
			case Keys.Left:
				if (eventArgs.System == SYSNAME)
				{
					if (ExtendFocused) ExtendFocused = false;
					else
					{
						InputHook.TryClearSystemLock(SYSNAME);
						LockedPost = null;
					}

				}
				break;
			case Keys.Right:
				if (eventArgs.System == SYSNAME)
				{
					if (LockedPost is not null && !ExtendFocused) ExtendFocused = true;
				}	
				break;
			default:
				break;
		}
	}

	private void Graphics_OnDrawGraphics(object? sender, EventsArgs.OnDrawGraphicEventArgs e) 
	{
		int escape = 15;
		int bottom = 720;
		int left = 80;
		int upper = 0;

		if (Posts.Count == 0) return;

		IEnumerable<Post> showPosts;
		if (LockedPost is null) showPosts = Posts.TakeLast(5);
		else
		{
			var list = new List<Post>();

			LinkedListNode<Post> TargetPostNode = Posts.FindLast(LockedPost) ?? throw new NullReferenceException();

			if (TargetPostNode.Previous is not null)
			{
				if (TargetPostNode.Previous.Previous is not null) list.Add(TargetPostNode.Previous.Previous.Value);
				list.Add(TargetPostNode.Previous.Value);
			}

			list.Add(TargetPostNode.Value);

			if (TargetPostNode.Next is not null)
			{
				list.Add(TargetPostNode.Next.Value);
				if (TargetPostNode.Next.Next is not null) list.Add(TargetPostNode.Next.Next.Value);
			}

			showPosts = list;
		}

		foreach (Post post in showPosts.Reverse())
		{
			upper += post.DrawPost(e.Graphics, Worker, bottom - upper, left, LockedPost == post, LockedPost == post && ExtendFocused);

			upper += escape; //небольшой отступ между постами
		}
	}
	
	private void GetNewPosts(object? sender, System.Timers.ElapsedEventArgs e)
	{
		HttpClient httpClient = new();
		long threadNum = Posts.First().Num;
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

					if (!Posts.Any((arg) => arg.Num == num)) GetAllPosts(httpClient, thread);
				}
			}
		}

		int postNum = Posts.Last().Num;

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

					if (!Posts.Any((arg) => arg.Num == post.Num))
					{
						Posts.AddLast(post);
					}
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
		Posts = new();

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

					Posts.AddLast(post);
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