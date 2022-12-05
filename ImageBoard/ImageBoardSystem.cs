using System.Text.Json;
using System.Windows.Forms;
using GenshinImpactOverlay.Menus;

namespace GenshinImpactOverlay.ImageBoard;

/// <summary>
/// Система отображения постов из Aib
/// </summary>
internal class ImageBoardSystem : IUseMenu
{
	/// <summary>
	/// Название системы для генерации названий
	/// </summary>
	public string Sysname => nameof(ImageBoardSystem);

	/// <summary>
	/// Обработчик графики
	/// </summary>
	private GraphicsWorker Worker { get; init; }

	/// <summary>
	/// Все посты треда
	/// </summary>
	private LinkedList<Post> Posts { get; } = new();
	/// <summary>
	/// Посты для отображения
	/// </summary>
	private List<Post> ShowedPosts { get; set; } = new();

	/// <summary>
	/// Частота обновления постов
	/// </summary>
	private System.Timers.Timer GetNewPostsTimer { get; set; } = new(5000);

	/// <summary>
	/// Выбранный пост
	/// </summary>
	private Post? FocusPost { get; set; }
	/// <summary>
	/// Разворот поста на котором фокус
	/// </summary>
	private bool ExtendFocused { get; set; } = true;
	/// <summary>
	/// Режим веток
	/// </summary>
	private bool NodeMode { get; set; } = false;
	/// <summary>
	/// Прозрачность изображений на постах
	/// </summary>
	private float Opacity { get; set; } = 0.5f;

	public ImageBoardSystem(GraphicsWorker worker)
	{
		Worker = worker;

		Post.FontIndex = worker.AddFont("Consolas", 14);
		Post.BFontIndex = worker.AddFont("Consolas", 14, bold: true);

		Post.WhiteBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(255, 255, 255));
		Post.BlackBrushIndex = worker.AddSolidBrush(new GameOverlay.Drawing.Color(0, 0, 0));

		HttpClient httpClient = new();

		JsonDocument thread = GetThreadsByTag(httpClient);

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
			upper += post.DrawPost(Worker, e.Graphics, Opacity, bottom - upper, left, FocusPost == post, FocusPost == post && ExtendFocused);

			upper += escape; //небольшой отступ между постами
		}
	}
	
	private void GetNewPosts(object? sender, System.Timers.ElapsedEventArgs e)
	{
		HttpClient httpClient = new();
		long threadNum = Posts.First().Num;
		string requestString = $"https://2ch.hk/api/mobile/v2/info/vg/{threadNum}";

		#region Check new thread
		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(strJson);
			JsonElement root = document.RootElement;
			JsonElement posts = root.GetProperty("thread").GetProperty("posts");

			if (posts.TryGetInt32(out int postCount) && postCount > 999)
			{
				JsonDocument thread = GetThreadsByTag(httpClient);

				int num = thread.RootElement.GetProperty("num").GetInt32();

				if (!Posts.Any((arg) => arg.Num == num)) GetAllPosts(httpClient, thread);
			}
		}
		#endregion Check new thread

		int postNum = Posts.Last().Num;

		requestString = $"https://2ch.hk/api/mobile/v2/after/vg/{threadNum}/{postNum}";

		#region Check new posts in thread

		using (HttpRequestMessage postsRequest = new(HttpMethod.Get, requestString))
		{
			var response = httpClient.Send(postsRequest);
			string strJson = response.Content.ReadAsStringAsync().Result;

			using JsonDocument document = JsonDocument.Parse(strJson);
			JsonElement root = document.RootElement;
			JsonElement posts = root.GetProperty("posts");
			foreach (JsonElement json in posts.EnumerateArray())
			{
				Post? post = JsonSerializer.Deserialize<Post>(JsonDocument.Parse(json.ToString()));

				if (post is null) throw new Exception();

				if (!Posts.Any((arg) => arg.Num == post.Num))
				{
					Posts.AddLast(post);
					SyncPostLinks(post);
				}
			}
		}

		#endregion Check new posts in thread

		UpdateShowingPosts();
	}

	private static JsonDocument GetThreadsByTag(HttpClient httpClient, string tag = "genshin")
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
					if (thread.TryGetProperty("tags", out JsonElement tagJson) && tagJson.GetString() == tag)
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

		Posts.Clear();

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
					SyncPostLinks(post);
				}
			}
		}

		UpdateShowingPosts();
	}

	private void SyncPostLinks(Post? post)
	{
		List<int> links = post.GetPostLinks().ToList();

		if (links.Count > 0)
		{
			foreach (var item in Posts)
			{
				if (!links.Any()) break;
				if (item is null) continue;

				if (links.Contains(item.Num))
				{
					//Console.Write($"{item.Num} ");
					item.AddReference(post.Num);
					_ = links.Remove(item.Num);
				}
			}
		}
	}

	private void UpdateShowingPosts()
	{
		if (FocusPost is null) ShowedPosts = new(Posts.TakeLast(5));
		else if (!NodeMode)
		{
			LinkedListNode<Post> TargetPostNode = Posts.FindLast(FocusPost) ?? throw new NullReferenceException();

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
		else
		{

		}
	}

	#region IUseMenu
	private MenuItem? _menuItem;
	MenuItem? IUseMenu.GetMenu(Action<MenuItem> updateMenuFunc, Action<bool?> keyInputSwitchFunc)
	{
		const string PATH = "Resources/Icons";

		_menuItem ??= new(Sysname, $"{PATH}/conversation.png", childMenus: new()
		{
			GetSettingsMenu(),
			GetSurfMenu(),
		});

		return _menuItem;

		MenuItem GetSettingsMenu()
		{
			const string SETNAME = "settings";

			List<MenuItem> childs = new()
			{
				new($"{Sysname}_{SETNAME}_{nameof(ExtendFocused)}", $"{PATH}/magnifying-glass.png", new() { 
					{ Keys.Clear, () => ExtendFocused = !ExtendFocused } 
				}),
				new($"{Sysname}_{SETNAME}_{nameof(Opacity)}", $"{PATH}/headphones.png", new() { 
					{ Keys.Clear, () => keyInputSwitchFunc(null) },
					{ Keys.Left, () => { if (Opacity > 0f) Opacity -= 0.1f; } },
					{ Keys.Right, () => { if (Opacity < 1f) Opacity += 0.1f; } }
				}),
			};

			return new($"{Sysname}_{SETNAME}", $"{PATH}/pause-button.png", childMenus: childs);
		}
	
		MenuItem GetSurfMenu()
		{
			const string SETNAME = "surf";

			return new($"{Sysname}_{SETNAME}", $"{PATH}/mesh-network.png", new()
			{
				{ Keys.Clear, () => { 
					if (FocusPost is null)
					{
						keyInputSwitchFunc(true);
						FocusPost = Posts.Last();
					}
					else
					{
						FocusPost = null;
						keyInputSwitchFunc(false);
					}

					UpdateShowingPosts();
				} },
				{ Keys.Up, () => {
					keyInputSwitchFunc(true);
					if (FocusPost is null) FocusPost = Posts.Last();
					else if (FocusPost != Posts.First()) FocusPost = Posts.FindLast(FocusPost).Previous.Value;
					else return;

					UpdateShowingPosts();
				} },
				{ Keys.Down, () => {
					if (FocusPost is null) return;
					else  if (FocusPost != Posts.Last()) FocusPost = Posts.FindLast(FocusPost).Next.Value;
					else
					{
						FocusPost = null;
						keyInputSwitchFunc(false);
					}
					UpdateShowingPosts();
				} },
			});
		}
	}
	#endregion
}