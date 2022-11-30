using System.Text.Json;
using System.Windows.Forms;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class ImageBoardSystem : IDisposable
{
	public const string SYSNAME = "Imageboard";

	private GraphicsWorker Worker { get; init; }

	private LinkedList<Post> Posts { get; set; } = new();
	private List<Post> ShowedPosts { get; set; } = new();

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
				UpdateShowingPosts();
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
				UpdateShowingPosts();
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
				UpdateShowingPosts();
				break;
			case Keys.Right:
				if (eventArgs.System == SYSNAME)
				{
					if (LockedPost is not null && !ExtendFocused) ExtendFocused = true;
				}	
				UpdateShowingPosts();
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

		foreach (Post post in ShowedPosts.Reverse<Post>())
		{
			upper += post.DrawPost(e.Graphics, Worker, bottom - upper, left, LockedPost == post, LockedPost == post && ExtendFocused);

			upper += escape; //небольшой отступ между постами
		}
	}
	
	private void GetNewPosts(object? sender, System.Timers.ElapsedEventArgs e)
	{
		//Console.WriteLine("Get new posts");

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

		//Console.WriteLine("New posts was get");

		UpdateShowingPosts();
	}

	private static JsonDocument GetThread(HttpClient httpClient)
	{
		//Console.WriteLine("getting thread");

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
						//Console.WriteLine("Thread was get");
						return JsonDocument.Parse(thread.ToString());
					}
				}

				throw new Exception();
			}
		}
	}

	private void GetAllPosts(HttpClient httpClient, JsonDocument thread)
	{
		//Console.WriteLine("Get all thread post");

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

		//Console.WriteLine("All thread posts getted");

		UpdateShowingPosts();
	}
	
	private void UpdateShowingPosts()
	{
		//Console.WriteLine("Update showing posts");

		if (LockedPost is null) ShowedPosts = new(Posts.TakeLast(5));
		else
		{
			LinkedListNode<Post> TargetPostNode = Posts.FindLast(LockedPost) ?? throw new NullReferenceException();

			List<Post> list = new()
			{
				TargetPostNode.Value
			};

			var prepList = new List<Post>();
			LinkedListNode<Post>? previous = TargetPostNode.Previous;
			while (prepList.Count < 4)
			{
				if (previous is null) break;

				prepList.Add(previous.Value);

				previous = previous.Previous;
			}

			var nextList = new List<Post>();
			LinkedListNode<Post>? next = TargetPostNode.Next;
			while (nextList.Count < 4)
			{
				if (next is null) break;

				nextList.Add(next.Value);

				next = next.Next;
			}

			int index = 0;
			while (list.Count < 5)
			{
				if (prepList.Count > index)
				{
					list.Insert(0, prepList[index]);
				}
				if (nextList.Count > index)
				{
					list.Add(nextList[index]);
				}
				index++;
			}

			ShowedPosts = new(list);
		}

		//Console.WriteLine("Showing posts was updated");
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